namespace TaiwanUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[DebuggerDisplay("{ToString(),nc}")]
partial class PostalAddress : IFormattable
{
    public override string ToString()
    {
        return GetRawValue();
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return ToString();
    }
}
