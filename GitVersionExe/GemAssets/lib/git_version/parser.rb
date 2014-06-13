require 'json'
require 'open3'
require 'tempfile'

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
      (inspect_past if @json) || inspect_future
    end

    private
    def run_gitversion
      cmd, log_dir, log_file = needs_log_file(command)

      begin
        stdout_and_stderr, status = Open3.capture2e(*cmd)

        raise_on_error(status, cmd, stdout_and_stderr, log_file)

        JSON.parse(stdout_and_stderr)
      ensure
        FileUtils.rm_rf(log_dir) unless log_dir.nil?
      end
    end

    def raise_on_error(status, cmd, stdout_and_stderr, log_file)
      message = <<MSG
Failed running #{cmd.join(' ')}, #{status}.

The log file written by GitVersion.exe contains:
#{File.read(log_file) if File.readable?(log_file)}

We received the following output:
#{stdout_and_stderr}
MSG
      raise StandardError.new(message) unless status.success?
    end

    def command
      cmd = [gitversion_exe]
      cmd << args
      cmd.flatten.reject(&:nil?)
    end

    def needs_log_file(cmd)
      log_dir, log_file = log(cmd)

      cmd << '/l' << log_file if log_dir

      [cmd, log_dir, log_file]
    end

    def log(cmd)
      if log_file = log_file_specified_by_user(cmd)
        return [nil, log_file]
      end

      log_dir = Dir.mktmpdir('gitversion-log-')
      log_file = File.join(log_dir, 'gitversion.log')

      [log_dir, log_file]
    end

    def log_file_specified_by_user(cmd)
      return unless index = cmd.find_index('/l')
      cmd[index + 1]
    end

    def pascal_case(str)
      str
      .to_s
      .split('_')
      .inject([]) { |buffer, e| buffer.push(e.capitalize) }
      .join
    end

    def inspect_future
      <<MSG
#{to_s}
Will invoke #{command.join(' ')} when first used.
MSG
    end

    def inspect_past
      <<MSG
#{to_s}
Invoked #{command.join(' ')} and parsed its output:
#{json.inspect}
MSG
    end
  end
end
