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

        expect(Open3).to have_received(:capture2e).with(*([subject.gitversion_exe] + args))
      end
    end

    context 'GitVersion.exe fails' do
      let(:gitversion_exe_return) { ['some error output', OpenStruct.new(success?: false)] }

      it 'should fail' do
        expect { subject.it_does_not_matter }.to raise_error(StandardError, /Failed running.*some error output/m)
      end
    end
  end

  describe 'after the executable has been built' do
    before {
      subject.gitversion_exe = File.expand_path('../../GemBuild/bin/GitVersion.exe')
    }

    its(:json) { should_not be_nil }
    its(:sha) { should match(/[0-9 a-f]{40}/) }
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
