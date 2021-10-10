using FastMember;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Conperio.FixedWithLineParser
{

    public class FixedWithLineParser<T> where T : class, new()
    {
        private TypeAccessor _accessor = TypeAccessor.Create(typeof(T), true);
        private IEnumerable<Member> _membersData;
        private Dictionary<string, FixedWidthLineFieldAttribute> _attributesDict = new Dictionary<string, FixedWidthLineFieldAttribute>();

        private Dictionary<int, (ParserHandler parser, Type underlyingType, string valueTypeName, string format)> _parseHandlers = new Dictionary<int, (ParserHandler parser, Type underlyingType, string valueTypeName, string format)>();

        public FixedWithLineParser()
        {
            var memberSet = _accessor.GetMembers().Where(a => a.IsDefined(typeof(FixedWidthLineFieldAttribute)));
            var membersDict = new Dictionary<int, Member>();

            foreach (var member in memberSet)
            {
                var attribute = member.GetMemberAttributes<FixedWidthLineFieldAttribute>().SingleOrDefault();
                if (attribute != null)
                {
                    membersDict.Add(attribute.Start, member);
                    _attributesDict.Add(member.Name, attribute);
                }
            }
            _membersData = membersDict.OrderBy(a => a.Key).Select(a => a.Value);
        }

        public virtual DefaultConfig DefaultConfig { get; set; } = new DefaultConfig();


        public T Parse(string line)
        {
            var data = new T();

            foreach (var memberData in _membersData)
            {
                var attribute = _attributesDict[memberData.Name];
                var valueString = line.Substring(attribute.Start - 1, attribute.Length);

                if (string.IsNullOrEmpty(valueString))
                {
                    continue;
                }

                valueString = valueString.Trim();

                _accessor[data, memberData.Name] = ParseStringValueToObject(valueString, memberData, attribute);
            }

            return data;
        }

        protected object ParseStringValueToObject(string valueString, Member member, FixedWidthLineFieldAttribute attribute)
        {
            object value = null;
            var Parser = GetParserHandler(member, attribute);
            try
            {
                if (string.IsNullOrEmpty(valueString))
                {
                    if (Parser.underlyingType == null && Parser.valueTypeName != nameof(String))
                    {
                        throw new InvalidOperationException($"Empty string cannot be parsed to not nullable Property: Name={member.Name}, Type={Parser.valueTypeName}");
                    }
                    return value;
                }
                value = Parser.parser(valueString, Parser.valueTypeName, Parser.format);
            }
            catch
            {
                throw new InvalidCastException($"Property: Name={member.Name}, Value ={valueString}, Format={Parser.format} cannot be parsed to Type={Parser.valueTypeName}");
            }

            return value;
        }

        private (ParserHandler parser, Type underlyingType, string valueTypeName, string format) GetParserHandler(Member member, FixedWidthLineFieldAttribute attribute)
        {
            if (_parseHandlers.TryGetValue(attribute.Start, out var parserHandler))
            {
                return parserHandler;
            }
            else
            {
                var underlyingType = Nullable.GetUnderlyingType(member.Type);
                string valueTypeName = underlyingType != null ? underlyingType.Name : member.Type.Name;



                ParserHandler Parser = null;
                string format = attribute.Format;

                switch (valueTypeName)
                {
                    case nameof(String):
                        Parser = ParserString;
                        break;

                    case nameof(Char):
                        Parser = ParserChar;
                        break;

                    case nameof(Decimal):
                    case nameof(Single):
                    case nameof(Double):
                    case nameof(Int32):
                    case nameof(Int64):
                    {
                        format = format ?? DefaultConfig.FormatNumberDecimal;
                        Parser = ParserNumber;
                        break;
                    }

                    case nameof(Boolean):
                    {
                        format = format ?? DefaultConfig.FormatBoolean;
                        Parser = ParserBoolean;
                        break;
                    }

                    case nameof(DateTime):
                    {
                        format = format ?? DefaultConfig.FormatDateTime;
                        Parser = ParserDateTime;
                        break;
                    }
                }
                _parseHandlers.Add(attribute.Start, (Parser, underlyingType, valueTypeName, format));
                return (Parser, underlyingType, valueTypeName, format);
            }

        }


        private delegate object ParserHandler(string valueString, string typeName, string format);

        private object ParserString(string valueString, string typeName, string format)
        {
            return valueString;
        }

        private object ParserChar(string valueString, string typeName, string format)
        {
            return valueString[0];
        }

        private object ParserNumber(string valueString, string typeName, string format)
        {
            object value = null;

            int signMultiplier = 1;
            if (valueString.Contains("-"))
            {
                valueString = valueString.Replace("-", string.Empty);
                signMultiplier = -1;
            }

            switch (typeName)
            {
                case nameof(Decimal): // decimal
                    value = signMultiplier * decimal.Parse(valueString, CultureInfo.InvariantCulture);
                    if (format.Contains(";")) //';' - Special custom Format that removes decimal separator ("0;00": 123.45 -> 12345)
                        value = (decimal)value / (decimal)Math.Pow(10, format.Length - 2); // "0;00".Length == 4 - 2 = 2 (10^2 = 100)
                    break;
                case nameof(Single): // float
                    value = signMultiplier * float.Parse(valueString, CultureInfo.InvariantCulture);
                    if (format.Contains(";"))
                        value = (float)value / (float)Math.Pow(10, format.Length - 2);
                    break;
                case nameof(Double):  // double
                    value = signMultiplier * double.Parse(valueString, CultureInfo.InvariantCulture);
                    if (format.Contains(";"))
                        value = (double)value / (double)Math.Pow(10, format.Length - 2);
                    break;
                case nameof(Int32): // int
                    value = signMultiplier * int.Parse(valueString);
                    break;
                case nameof(Int64): // long
                    value = signMultiplier * long.Parse(valueString);
                    break;
            }

            return value;
        }

        private object ParserBoolean(string valueString, string typeName, string format)
        {
            var valueFormatIndex = format.Split(';').ToList().FindIndex(a => a == valueString);

            object value = valueFormatIndex == 0 ? true : valueFormatIndex == 2 ? false : (bool?)null;

            return value;
        }

        private object ParserDateTime(string valueString, string typeName, string format)
        {
            object value = DateTime.ParseExact(valueString, format, CultureInfo.InvariantCulture);

            return value;
        }
    }

}
