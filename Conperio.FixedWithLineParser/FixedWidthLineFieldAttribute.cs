using FastMember;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Conperio.FixedWithLineParser
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class FixedWidthLineFieldAttribute : Attribute
    {
        public override object TypeId { get { return this; } } // overriding done because of AllowMultiple == true
        public virtual int Start { get; set; }
        public virtual int Length { get; set; }
        public virtual string Format { get; set; }
    }

}
