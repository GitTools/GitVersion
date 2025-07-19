namespace GitVersion.Formatting
{
    internal interface IExpressionCompiler
    {
        Func<object, object?> CompileGetter(Type type, MemberInfo[] memberPath);
    }
}
