require 'git_version'

describe GitVersion do
  include described_class

  it 'should create a ' + GitVersion::Parser.to_s do
    expect(git_version).to be_an_instance_of(GitVersion::Parser)
  end

  it 'should create a singleton ' + GitVersion::Parser.to_s do
    expect(git_version).to equal(git_version)
  end

  describe 'passing arguments' do
    it 'should yield the same instance per argument' do
      expect(git_version('foo')).to equal(git_version('foo'))
    end

    it 'should yield different instances for different arguments' do
      expect(git_version('foo')).not_to equal_no_diff(git_version('bar'))
    end

    def equal_no_diff(expected)
      expected = equal(expected)

      # Turn off diffing for this matcher as it calls GitVersion::Parser#to_ary which will fail because GitVersion.exe
      # cannot be found.
      def expected.diffable?
        false
      end

      expected
    end
  end
end
