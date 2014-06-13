require 'git_version/parser'

module GitVersion
  def git_version(args = nil)
    parsers.fetch(args) { |a| parsers[a] = Parser.new(a) }
  end

  private
  def parsers
    @parsers ||= {}
  end
end
