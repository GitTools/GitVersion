require 'json'
require 'open3'

class GitVersion
  attr_reader :args
  attr_accessor :gitversion_exe

  def initialize(args = [])
    @args = args
  end

  def method_missing(symbol, *args)
    keys = [symbol.to_s, pascal_case(symbol.to_s)]

    found_key = keys.find { |key| json.has_key?(key) }
    return json[found_key] if found_key

    super
  end

  def json
    @json ||= run_gitversion
  end

  def gitversion_exe
    @gitversion_exe ||= File.join(File.dirname(__FILE__), '../bin/GitVersion.exe')
  end

  private
  def run_gitversion
    cmd = [gitversion_exe]
    cmd << args
    cmd = cmd.flatten.reject(&:nil?)

    stdout_and_stderr, status = Open3.capture2e(*cmd)

    raise StandardError.new("Failed running #{cmd.join(' ')}, #{status}. We received the following output:\n#{stdout_and_stderr}") unless status.success?

    JSON.parse(stdout_and_stderr)
  end

  def pascal_case(str)
    str
      .to_s
      .split('_')
      .inject([]) { |buffer, e| buffer.push(e.capitalize) }
      .join
  end
end
