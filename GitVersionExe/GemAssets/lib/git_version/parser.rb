require 'json'
require 'open3'

module GitVersion
  class Parser
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
      @gitversion_exe ||= File.expand_path(File.join(File.dirname(__FILE__), '../../bin/GitVersion.exe'))
    end

    def inspect
      unless @json

        return <<EOF
#{to_s}
Will invoke #{cmd_string} when first used.
EOF

      else

        return <<EOF
#{to_s}
Invoked #{cmd_string} and parsed its output:
#{json.inspect}
EOF

      end
    end

    private
    def run_gitversion
      stdout_and_stderr, status = Open3.capture2e(*cmd)

      raise StandardError.new("Failed running #{cmd_string}, #{status}. We received the following output:\n#{stdout_and_stderr}") unless status.success?

      JSON.parse(stdout_and_stderr)
    end

    def cmd
      cmd = [gitversion_exe]
      cmd << args
      cmd.flatten.reject(&:nil?)
    end

    def cmd_string
      cmd.join(' ')
    end

    def pascal_case(str)
      str
      .to_s
      .split('_')
      .inject([]) { |buffer, e| buffer.push(e.capitalize) }
      .join
    end
  end
end
