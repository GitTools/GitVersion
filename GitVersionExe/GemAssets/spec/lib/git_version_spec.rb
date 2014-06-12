require 'git_version'

describe GitVersion do
  include described_class

  it 'creates a GitVersionParser' do
    expect(git_version).to be_an_instance_of(GitVersion::Parser)
  end
end
