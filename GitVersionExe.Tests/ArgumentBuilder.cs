using System.IO;
using System.Text;

public class ArgumentBuilder
{
    public ArgumentBuilder(string workingDirectory)
    {
        this.workingDirectory = workingDirectory;
    }

    public string WorkingDirectory
    {
        get { return workingDirectory; }
    }

    public string Exec { get; set; }

    public string ExecArgs { get; set; }

    public string ProjectFile { get; set; }

    public string ProjectArgs { get; set; }

    public string LogFile { get; set; }

    public bool IsTeamCity { get; set; }

    public string AdditionalArguments { get; set; }

    public override string ToString()
    {
        var arguments = new StringBuilder();

        arguments.AppendFormat("\"{0}\"", WorkingDirectory);

        if (!string.IsNullOrWhiteSpace(Exec))
            arguments.AppendFormat(" /exec \"{0}\"", Exec);

        if (!string.IsNullOrWhiteSpace(ExecArgs))
            arguments.AppendFormat(" /execArgs \"{0}\"", ExecArgs);

        if (!string.IsNullOrWhiteSpace(ProjectFile))
            arguments.AppendFormat(" /proj \"{0}\"", ProjectFile);

        if (!string.IsNullOrWhiteSpace(ProjectArgs))
            arguments.AppendFormat(" /projargs \"{0}\"", ProjectArgs);

        arguments.Append(AdditionalArguments);

        if (!string.IsNullOrWhiteSpace(LogFile))
        {
            arguments.AppendFormat(" /l \"{0}\"", LogFile);
        }

        return arguments.ToString();
    }


    readonly string workingDirectory;
}