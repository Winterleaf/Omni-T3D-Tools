using System;
using System.Linq;
using System.Text;
using Winterleaf.SharedServices.Interrogator.Configuration;

namespace Winterleaf.SharedServices.Interrogator
{
    internal static class Helpers
    {
        internal static string c2cI(string vt, ref ConfigFiles MDntcConfig)
        {
            string r = convertC2Cs(vt, false, ref MDntcConfig);
            if (r == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                r = r.Insert(r.IndexOf(" ", StringComparison.Ordinal), " [In] ");
            else
                r = "[In] " + r;
            return r;
        }

        internal static string c2cO(string vt, ref ConfigFiles MDntcConfig)
        {
            string r = convertC2Cs(vt, false, ref MDntcConfig);
            if (r == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                r = r.Insert(r.IndexOf(" ", StringComparison.Ordinal), " [Out] ");
            else
                r = "[Out] " + r;
            return r;
        }

        internal static string convertC2Cs(string vt, bool nodefault, ref ConfigFiles MDntcConfig) // = false
        {
            if (vt == "bool")
                return "bool";
            if (MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(vt))
                vt = MDntcConfig.PreGen_CPP_TypeConv[vt];

            switch (vt)
                {
                    case "U8":
                        return "byte";
                    case "S8":
                        return "sbyte";
                    case "S32":
                        return "int";
                    case "bool":
                        return "bool";
                    case "F64":
                        return "double";
                    case "F32":
                        return "float";
                    case "float":
                        return "float";
                    case "U32":
                        return "uint";
                    case "int":
                        return "int";
                    case "void":
                        return "void";
                    default:
                        if (!nodefault)
                            return "[MarshalAs(UnmanagedType.LPStr)] StringBuilder";
                        break;
                }
            return vt;
        }

        internal static string returnvalType(string vt, ref ConfigFiles MDntcConfig)
        {
            if (MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(vt))
                vt = MDntcConfig.PreGen_CPP_TypeConv[vt];

            switch (vt)
                {
                    case "U8":
                        return "byte";
                    case "S8":
                        return "sbyte";
                    case "S32":
                        return "int";
                    case "bool":
                        return "int";
                    case "F32":
                        return "float";
                    case "F64":
                        return "double";
                    case "float":
                        return "float";
                    case "U32":
                        return "uint";
                    case "int":
                        return "int";

                    default:
                        return "void";
                }
        }

        internal static string getridofdoublespace(string sparams)
        {
            sparams = sparams.Trim();
            string newstring = "";
            char lastchar = ' ';
            for (int i = 0; i < sparams.Count(); i++)
                {
                if (sparams[i] != ' ')
                    newstring += sparams[i];
                else if ((sparams[i] == ' ') && (lastchar != ' '))
                    {
                    if (sparams[i + 1] != '*')
                        newstring += sparams[i];
                    }
                if (sparams[i] == '*')
                    newstring += ' ';
                lastchar = sparams[i];
                }

            sparams = newstring;
            newstring = "";
            foreach (char v in sparams)
                {
                if (v == '*')
                    {
                    newstring += '*';
                    newstring += ' ';
                    }
                else
                    newstring += v;
                }
            return newstring;
        }

        internal static string GiveMeSafeName(string name)
        {
            switch (name.ToLower())
                {
                    case "string":
                        return "stringx";
                    case "lock":
                        return "lockx";
                    case "locked":
                        return "lockedx";
                    case "object":
                        return "objectx";
                    case "checked":
                        return "checkedx";
                    case "class":
                        return "classx";
                    case "static":
                        return "staticx";
                    case "sizeof":
                        return "sizeofx";
                    case "event":
                        return "eventx";
                    case "params":
                        return "paramsx";
                }
            return name;
            
        }

        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string removeComments(string data)
        {
            bool inquotes = false;
            bool inMacro = false;
            StringBuilder newcode = new StringBuilder();
            int i = 0;
            while (i < data.Length)
                {
                if (data.Length < i + 2)
                    {
                    newcode.Append(data[i]);
                    break;
                    }

                if (data[i] == '"' && data[i - 1] != '\\' && !inquotes)
                    {
                    inquotes = true;
                    newcode.Append(data[i]);
                    i++;
                    continue;
                    }
                if (data[i] == '"' && data[i - 1] != '\\' && inquotes)
                    {
                    inquotes = false;
                    newcode.Append(data[i]);
                    i++;
                    continue;
                    }
                if (inquotes)
                    {
                    newcode.Append(data[i]);
                    i++;
                    continue;
                    }

                if (data.Substring(i, 2) == "/*")
                    {
                    i = i + 2;
                    while (true)
                        {
                        if (data.Length < i + 2)
                            break;
                        if (data.Substring(i, 2) == "*/")
                            {
                            i = i + 2;
                            break;
                            }
                        i++;
                        }
                    continue;
                    }

                if (data.Substring(i, 2) == "//")
                    {
                    i = i + 2;
                    while (true)
                        {
                        if (i >= data.Length)
                            break;
                        if (data[i] == '\n')
                            {
                            i++;
                            break;
                            }
                        i++;
                        }
                    continue;
                    }
                //#ifndef
                //#if
                //#else
                //#endif
                bool domacrocheck = false;
                if ((i == 0) && data[i] == '#')
                    domacrocheck = true;
                else if (data[i] == '#' && data[i - 1] == '\n')
                    domacrocheck = true;

                if (domacrocheck)
                    {
                    if (i + "#ifndef".Length < data.Length)
                        {
                        if (data.Substring(i, "#ifndef".Length) == "#ifndef")
                            {
                            inMacro = true;
                            newcode.Append("\r\n");
                            }
                        }
                    if (i + "#if".Length < data.Length)
                        {
                        if (data.Substring(i, "#if".Length) == "#if")
                            {
                            inMacro = true;
                            newcode.Append("\r\n");
                            }
                        }
                    if (i + "#endif".Length < data.Length)
                        {
                        if (data.Substring(i, "#endif".Length) == "#endif")
                            {
                            inMacro = false;
                            newcode.Append("\r\n");
                            }
                        }
                    }

                newcode.Append(data[i]);
                i++;
                }

            string y = newcode.ToString();
            return newcode.ToString();
        }

        public static bool IsNumeric(this string numberString)
        {
            foreach (byte c in numberString.ToArray())
                {
                if ((((c < 48) || (c > 57)) && (c != 46) && (c != 44) && c != '-' && c != '+'))
                    return false;
                }
            return true;
        }
    }
}