require 'git_version/parser'

module GitVersion
  def git_version(args = nil)
    Parser.new(args)
  end
end
