require 'git_version'

describe GitVersion::Parser do
  describe 'defaults' do
    its(:gitversion_exe) { should match(%r|/bin/GitVersion.exe$|) }
    its(:args) { should be_empty }
  end

  describe 'GitVersion.exe invocation' do
    before {
      allow(Open3).to receive(:capture2e).and_return(gitversion_exe_return)
    }

    let(:gitversion_exe_return) { ['{ "Sha": 1234 }', OpenStruct.new(success?: true)] }

    describe 'cached results' do
      context 'accessing multiple properties' do
        it 'should run GitVersion.exe only once' do
          subject.sha
          subject.sha

          expect(Open3).to have_received(:capture2e).once
        end
      end
    end

    describe 'arguments' do
      subject { described_class.new(args) }

      let(:args) { %w(some additional args) }

      it 'should pass args to the executable' do
        subject.sha

        # with(array_including(exe_and_args)) doesn't seem to work here.
        expect(Open3).to have_received(:capture2e) do |*with|
          exe_and_args = [subject.gitversion_exe] + args
          expect(with).to include(*exe_and_args)
        end
      end
    end

    context 'GitVersion.exe fails' do
      context 'without error log' do
        let(:gitversion_exe_return) { ['some error output', OpenStruct.new(success?: false)] }

        it 'should fail' do
          expect { subject.it_does_not_matter }.to raise_error(StandardError, /Failed running.*some error output/m)
        end
      end
    end
  end

  describe 'after the executable has been built' do
    before {
      subject.gitversion_exe = File.expand_path('../../GemBuild/bin/GitVersion.exe')
    }

    its(:json) { should_not be_nil }
    its(:sha) { should match(/[0-9 a-f]{40}/) }

    context 'GitVersion.exe fails' do
      let(:repository) {
        tmp = Dir.mktmpdir('repository-')

        Dir.chdir(tmp) { `git init` }

        tmp
      }

      after {
        FileUtils.rm_rf(repository)
      }

      context 'without log specified' do
        subject { described_class.new(repository) }

        it 'should write the log contents' do
          expect { subject.json }.to raise_error(StandardError, /No Tip found\. Has repo been initialized\?/m)
        end
      end

      context 'with log specified by the user' do
        let(:log_file) { File.join(Dir.mktmpdir('log-path-'), 'log.txt') }

        subject { described_class.new([repository, '/l', log_file]) }

        after {
          FileUtils.rm_rf(File.dirname(log_file))
        }

        it 'should write the log contents' do
          expect { subject.json }.to raise_error(StandardError, /No Tip found\. Has repo been initialized\?/m)
        end

        it 'should use the user-supplied log file' do
          allow(Open3).to receive(:capture2e)

          subject.json rescue nil

          expect(Open3).to have_received(:capture2e) do |*with|
            log_switch_and_user_log_file = ['/l', log_file]
            expect(with).to include(*log_switch_and_user_log_file)
          end
        end

        it 'should have only one log switch' do
          allow(Open3).to receive(:capture2e)

          subject.json rescue nil

          expect(Open3).to have_received(:capture2e) do |*with|
            one_log_switch = Proc.new { |args| args.one? { |arg| arg == '/l' } }
            expect(with).to satisfy { |args| one_log_switch.call(args) }
          end
        end
      end
    end
  end

  describe 'accessing properties' do
    before {
      allow(subject).to receive(:json).and_return(json)
    }

    let(:json) { { 'InformationalVersion' => 'blah' } }

    it 'should translate snake case to pascal case' do
      expect(subject.informational_version).to eq('blah')
    end

    it 'should support pascal case' do
      pascal = subject.InformationalVersion
      snake = subject.informational_version

      expect(pascal).to eq(snake)
    end

    context 'with nil value' do
      let(:json) { { 'is_nil' => nil } }

      it 'should yield nil' do
        expect(subject.is_nil).to be_nil
      end
    end
  end

  describe '#inspect' do
    context 'no properties accessed yet' do
      it 'should write what will happen' do
        expect(subject.inspect).to match(/.+GitVersion.+\nWill invoke .+GitVersion.exe when first used./)
      end
    end

    context 'properties accessed' do
      before {
        allow(Open3).to receive(:capture2e).and_return(['{ "Sha": 1234 }', OpenStruct.new(success?: true)])
      }

      it 'should write what happened' do
        subject.sha
        expect(subject.inspect).to match(/.+GitVersion.+\nInvoked .+GitVersion.exe and parsed its output:\n.+/)
      end
    end
  end
end
