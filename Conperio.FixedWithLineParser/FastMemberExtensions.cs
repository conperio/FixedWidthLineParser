using FastMember;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Conperio.FixedWithLineParser
{

    public static class FastMemberExtensions
    {
        public static T GetPrivateField<T>(this object obj, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = obj.GetType();
            FieldInfo field = type.GetField(name, flags);
            return (T)field.GetValue(obj);
        }

        public static T GetPrivateProperty<T>(this object obj, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = obj.GetType();
            PropertyInfo field = type.GetProperty(name, flags);
            return (T)field.GetValue(obj, null);
        }

        public static MemberInfo GetMemberInfo(this Member member)
        {
            return member.GetPrivateField<MemberInfo>("member");
        }

        public static T GetMemberAttribute<T>(this Member member) where T : Attribute
        {
            return member.GetPrivateField<MemberInfo>("member").GetCustomAttribute<T>();
        }

        public static IEnumerable<T> GetMemberAttributes<T>(this Member member) where T : Attribute
        {
            return member.GetPrivateField<MemberInfo>("member").GetCustomAttributes<T>();
        }
    }

}
