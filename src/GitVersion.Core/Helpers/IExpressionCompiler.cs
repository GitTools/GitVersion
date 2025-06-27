namespace GitVersion.Helpers
{
    internal interface IExpressionCompiler
    {
        Func<object, object?> CompileGetter(Type type, MemberInfo[] memberPath);
    }
}
