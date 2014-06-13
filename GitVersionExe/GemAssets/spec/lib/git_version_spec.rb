require 'git_version'

describe GitVersion do
  include described_class

  it 'should create a ' + GitVersion::Parser.to_s do
    expect(git_version).to be_an_instance_of(GitVersion::Parser)
  end
end
