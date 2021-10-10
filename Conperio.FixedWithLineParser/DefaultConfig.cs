using FastMember;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Conperio.FixedWithLineParser
{

    public class DefaultConfig
    {
        // Formats
        public virtual string FormatNumberInteger { get; set; } = "0";
        public virtual string FormatNumberDecimal { get; set; } = "0.00";
        public virtual string FormatBoolean { get; set; } = "1; ;0";
        public virtual string FormatDateTime { get; set; } = "yyyyMMdd";
    }

}
