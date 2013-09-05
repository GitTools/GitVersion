using Mono.Cecil;

public static class CecilExtensions
{
    public static bool IsStrongNamed(this ModuleDefinition moduleDefinition)
    {
        return !moduleDefinition.Assembly.Name.PublicKey.IsEmpty();
    }
}