using System;

namespace GitVersion
{
    public interface IObjectId : IEquatable<IObjectId>
    {
        string Sha { get; }
        string ToString(int prefixLength);
    }
}
