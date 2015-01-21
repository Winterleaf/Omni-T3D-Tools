#region

using System.Collections.Generic;
using System.Text.RegularExpressions;

#endregion

namespace Winterleaf.SharedServices.Interrogator.Containers
{
    public class EnumData
    {
        private readonly bool _mIsScript;
        private string _mBody = "";
        private string _mClass = "";
        private bool _mIsUsed;
        private string _mName = "";
        private string _mRealEnum = "";

        public EnumData(string c, string n, string b, bool s)
        {
            _mClass = c;
            _mName = n;
            _mBody = b;
            _mIsUsed = false;
            _mIsScript = s;
        }

        public string mName
        {
            get { return _mName; }
            set { _mName = value; }
        }

        public string mBody
        {
            get { return _mBody; }
            set { _mBody = value; }
        }

        public bool mIsUsed
        {
            get { return _mIsUsed; }
            set { _mIsUsed = value; }
        }

        public bool IsScript
        {
            get { return _mIsScript; }
        }

        public string mClass
        {
            get { return _mClass; }
            set { _mClass = value; }
        }

        public string mRealEnum
        {
            get { return _mRealEnum; }
            set { _mRealEnum = value; }
        }

        public string ParseToCSharp(ref Logger.Logger mlogger)
        {
            string enumtext = "";
            if (!IsScript)
                {
                #region Parsing Standard Enum

                bool foundfirstbrace = false;
                int i = 0;

                while (i < _mBody.Length)
                    {
                    if (_mBody[i] == '{' && !foundfirstbrace)
                        {
                        foundfirstbrace = true;
                        enumtext = "\r\n/// <summary>\r\n";
                        //enumtext += "/// " + mName.Replace("::", "__").Trim() + "\r\n";
                        enumtext += "/// " + mName.Replace("::", "__").Trim() + "\r\n";
                        enumtext += "/// </summary>\r\n";
                        enumtext += "public enum  " + mName.Replace("::", "__");
                        if (_mBody.Contains(" BIT("))
                            enumtext += ": uint ";
                        enumtext += "{";
                        enumtext += "\r\n/// <summary>\r\n";
                        enumtext += "/// \r\n";
                        enumtext += "/// </summary>\r\n";
                        i++;
                        continue;
                        }

                    if ((foundfirstbrace) && _mBody[i] == '}')
                        {
                        enumtext += "};";
                        break;
                        }

                    if (i + " BIT(".Length < _mBody.Length)
                        {
                        if (_mBody.Substring(i, " BIT(".Length) == " BIT(")
                            {
                            i = i + " BIT(".Length;
                            string number = "";
                            while (_mBody[i] != ')')
                                {
                                number = number + _mBody[i];
                                i++;
                                }

                            int test;
                            if (!int.TryParse(number, out test))
                                return "";

                            enumtext = enumtext + " " + BitConvert(int.Parse(number));
                            i++;
                            continue;
                            }
                        }
                    enumtext += _mBody[i];
                    if (_mBody[i] == ',')
                        {
                        enumtext += "\r\n/// <summary>\r\n";
                        enumtext += "/// \r\n";
                        enumtext += "/// </summary>\r\n";
                        }

                    i++;
                    }

                #endregion
                }
            else
                {
                enumtext = "\r\n/// <summary>\r\n";
                enumtext += "/// " + mName.Replace("::", "__").Trim() + "\r\n";
                enumtext += "/// </summary>\r\n";
                enumtext += @"
 public sealed class " + mName.Replace("::", "__").Trim() + @"  : iEnum
        {
         readonly int realEnum;
        readonly string tag;
        readonly string description;

        static readonly Dictionary<int, " + mName.Replace("::", "__").Trim() + @"> mdict = new Dictionary<int, " + mName.Replace("::", "__").Trim() + @">();
        static readonly Dictionary<string, " + mName.Replace("::", "__").Trim() + @"> msdict = new Dictionary<string, " + mName.Replace("::", "__").Trim() + @">();

        public static implicit operator " + mName.Replace("::", "__").Trim() + @"(int i)
            {
            return mdict[i];
            }

        public static implicit operator int(" + mName.Replace("::", "__").Trim() + @" t)
            {
            return t.realEnum;
            }

        public static implicit operator " + mName.Replace("::", "__").Trim() + @"(string s)
            {
            return msdict[s];
            }


        private " + mName.Replace("::", "__").Trim() + @"(int realEnum, String tag,string description)
            {
            mdict.Add(realEnum,this);
            msdict.Add(tag,this);
            this.realEnum = realEnum;
            this.tag = tag;
            this.description = description;
            }

        public int RealEnum
            {
            get { return realEnum; }
            }

        public string Description
            {
            get { return description; }
            }

        public string Tag
            {
            get { return tag; }
            }

        public override String ToString()
            {
            return Tag;
            }

        public string AsString()
            {
            return tag;
            }

        public List<string> keyList
            {
            get { return msdict.Keys.ToList(); }
            }
        public object this[string key]
            {
            get { return msdict[key]; }
            }

";
                Match match = Regex.Match(_mBody, "{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\"[ \t]*,[ \t]*\"(?<Description>[a-zA-Z0-9' .:;\\\\,-/()]*)\"[ \t]*}[ \t]*,*|{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\"[ \t]*}|{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\",(?<Description>\r\n.*)}|{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\",[ \r\n]*\"(?<Description>[@a-zA-Z0-9' .:;\\\\,-/()\"\t\n\r ]*)}");
                while (match.Success)
                    {
                    string enumvalue = match.Groups["RealName"].Value;
                    if (enumvalue.IndexOf(":") > -1)
                        enumvalue = enumvalue.Substring(enumvalue.LastIndexOf(':') + 1);

                    enumtext = enumtext + "      public static readonly " + mName.Replace("::", "__").Trim() + " " + match.Groups["Value"].Value.Replace(' ', '_').Replace("+", "Plus").Replace("-", "Minus") + " = new " + mName.Replace("::", "__").Trim() + "((int)" + mRealEnum.Replace(':', '_') + "." + enumvalue + ",\"" + match.Groups["Value"].Value + "\",\"";

                    if (match.Groups["Description"].Success)
                        enumtext = enumtext + match.Groups["Description"].Value.Replace('"', ' ').Replace('\r', ' ').Replace('\n', ' ').Replace('\\', ' ');

                    enumtext = enumtext + "\");\r\n";

                    match = match.NextMatch();
                    }

                enumtext += "      };";
                }
            return enumtext; //.Replace("\r", "").Replace("\n", "");
        }

        public List<string> getUsedRealTypes()
        {
            List<string> result = new List<string>();
            Match match = Regex.Match(_mBody, "{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\"[ \t]*,[ \t]*\"(?<Description>[a-zA-Z0-9' .:;\\\\,-/()]*)\"[ \t]*}[ \t]*,*|{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\"[ \t]*}|{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\",(?<Description>\r\n.*)}|{[ \t]*(?<RealName>[a-zA-Z0-9:_]*)[ \t]*,[ \t]*\"(?<Value>[ a-zA-Z0-9_+-]*)\",[ \r\n]*\"(?<Description>[@a-zA-Z0-9' .:;\\\\,-/()\"\t\n\r ]*)}");
            while (match.Success)
                {
                result.Add(match.Groups["RealName"].Value);
                match = match.NextMatch();
                }
            return result;
        }

        private string BitConvert(int i)
        {
            switch (i)
                {
                    case 0:
                        return "0x00000000";
                    case 1:
                        return "0x00000001";
                    case 2:
                        return "0x00000002";
                    case 3:
                        return "0x00000004";
                    case 4:
                        return "0x00000008";
                    case 5:
                        return "0x00000010";
                    case 6:
                        return "0x00000020";
                    case 7:
                        return "0x00000040";
                    case 8:
                        return "0x00000080";
                    case 9:
                        return "0x00000100";
                    case 10:
                        return "0x00000200";
                    case 11:
                        return "0x00000400";
                    case 12:
                        return "0x00000800";
                    case 13:
                        return "0x00001000";
                    case 14:
                        return "0x00002000";
                    case 15:
                        return "0x00004000";
                    case 16:
                        return "0x00008000";
                    case 17:
                        return "0x00010000";
                    case 18:
                        return "0x00020000";
                    case 19:
                        return "0x00040000";
                    case 20:
                        return "0x00080000";
                    case 21:
                        return "0x00100000";
                    case 22:
                        return "0x00200000";
                    case 23:
                        return "0x00400000";
                    case 24:
                        return "0x00800000";
                    case 25:
                        return "0x01000000";
                    case 26:
                        return "0x02000000";
                    case 27:
                        return "0x04000000";
                    case 28:
                        return "0x08000000";
                    case 29:
                        return "0x10000000";
                    case 30:
                        return "0x20000000";
                    case 31:
                        return "0x40000000";
                    case 32:
                        return "0x80000000";
                }
            return "";
        }
    }
}