using Cake.Frosting;

[TaskName("Default")]
[IsDependentOn(typeof(BuildTask))]
public class DefaultTask : FrostingTask
{
}