namespace GitVersion
{
    using System;
    using NDesk.Options;

    /// <summary>
    /// Backwards comaptible option set that:
    ///  * accepts case-insensitive option names
    /// </summary>
    /// <remarks>
    /// Inspired by: 
    /// http://www.ndesk.org/doc/ndesk-options/NDesk.Options/Option.html#M:NDesk.Options.Option.OnParseComplete(NDesk.Options.OptionContext)
    /// </remarks>
    class CaseInsensitiveOptionSet : OptionSet
    {
        // sadly, I do not know how to override the collection initialize to make sure option are passed in lower case.

        protected override void InsertItem(int index, Option item)
        {
            if (item.Prototype.ToLower() != item.Prototype)
            {
                throw new ArgumentException("Option prototypes must be lower-case");
            }
            base.InsertItem(index, item);
        }
        
        protected override OptionContext CreateOptionContext()
        {
            return new OptionContext(this);
        }

        protected override bool Parse(string option, OptionContext c)
        {
            string f, n, s, v;
            var haveParts = GetOptionParts(option, out f, out n, out s, out v);
            var newOption = option;

            if (haveParts)
            {
                newOption = f + n.ToLower() + (v != null ? s + v : "");
            }

            return base.Parse(newOption, c);
        }

    }
}
