using System;

namespace GitVersion
{
    public interface IHelpWriter
    {
        void Write();
        void WriteTo(Action<string> writeAction);
    }
}
