using System;
using System.CommandLine;
using System.Linq;

namespace GitVersion.Cli
{
    /// <summary>
    /// This is a custom argument that parses `--log-to Console` and `--log-to File "c:/foo.txt"
    /// </summary>
    public class LogToArgument : Argument<LoggingOptions>
    {
        public LogToArgument() : base(result =>
          {
              var options = new LoggingOptions();
              var logTargetToken = result.Tokens.First();

              options.LogTo = Enum.Parse<LogToTarget>(logTargetToken.Value);
              if (options.LogTo == LogToTarget.File)
              {
                  var logFilePathToken = result.Tokens[1];
                  options.LogFilePath = logFilePathToken.Value;
              }
              else
              {
                  // only --log-to File can have an additional symbol for the file path.
                  if (result.Tokens.Count > 1)
                  {
                      throw new ArgumentException();
                  }
              }

              return options;
          })
        {
            Arity = new ArgumentArity(1, 2);
        }
    }

}
