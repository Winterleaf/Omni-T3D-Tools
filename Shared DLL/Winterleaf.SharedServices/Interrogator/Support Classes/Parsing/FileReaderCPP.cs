using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Winterleaf.SharedServices.Interrogator.Configuration;
using Winterleaf.SharedServices.Interrogator.Containers;

namespace Winterleaf.SharedServices.Interrogator.Parsing
    {
    internal class FileReaderCPP
        {
        public List<DefinedValues> _DefinedClasses = new List<DefinedValues>();
        public List<Externdata> _mData;
        public List<DefinedValues> _mDefined = new List<DefinedValues>();
        private ConfigFiles _mDntcConfig;
        public List<ImplementGlobalCallback> _mGlobalCallbacks = new List<ImplementGlobalCallback>();
        public List<ImplementCallback> _mImplementCallback = new List<ImplementCallback>();
        private Logger.Logger _mLogger;
        public Dictionary<string, EnumData> _menumerations = new Dictionary<string, EnumData>();
        public List<InitPersistData> _midata;

        public List<string> classes = new List<string>();
        public Dictionary<string, string> mConsoleTypes;

        #region "Utilities"

        private int GetLineNumber(string sdata, string spart)
            {
            int fileloc = sdata.IndexOf(spart);
            return sdata.Substring(0, fileloc).Split('\r').GetUpperBound(0);
            }

        private bool CheckValid(string objecttype, string function, string filename, int linenumber, string fty)
            {
            objecttype = objecttype.Trim().ToLower();

            if ((objecttype == "classname") || (function == "name"))
                return false;
            function = function.Trim().ToLower();

            if (_mDntcConfig.PreGen_CPP_IgnoreClassFunction.ContainsKey(objecttype))
                {
                if (_mDntcConfig.PreGen_CPP_IgnoreClassFunction[objecttype].functions.Contains("*"))
                    {
                    _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), fty + "Skip Function Found", objecttype + "->*Wildcard (" + function + ")", filename, linenumber);
                    return false;
                    }
                }

            if (_mDntcConfig.PreGen_CPP_IgnoreClassFunction.ContainsKey(objecttype))
                {
                if (_mDntcConfig.PreGen_CPP_IgnoreClassFunction[objecttype].functions.Contains(function))
                    {
                    _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), fty + "Skip Function Found", objecttype + "->" + function, filename, linenumber);
                    return false;
                    }
                }

            _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), fty + "Valid Function Found", objecttype + "->" + function, filename, linenumber);
            return true;
            }

        #endregion

        private void ParseConsoleFunctions(string filename, string data)
            {
            string[] parts = Regex.Split(data, "[\n \t]+ConsoleFunction");
            foreach (string spart in parts)
                {
                if (!spart.Trim().StartsWith("("))
                    continue;

                if (spart.Trim().StartsWith("(name,returnType,minArgs,maxArgs,usage1)"))
                    continue;

                if (spart.Split(',').GetUpperBound(0) < 4)
                    {
                    _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), @"<div style='background-color: red'>(ConsoleFunction) Failed To Parse , Expected 4 Commas</div>", spart, filename, GetLineNumber(data, spart));
                    continue;
                    }

                string function = spart.Split(',')[0].Trim();
                function = function.Replace("(", "").Trim();
                string returntype = spart.Split(',')[1].Trim();

                string minParamcount = spart.Split(',')[2].Trim();
                string maxParamcount = spart.Split(',')[3].Trim();

                if (maxParamcount == "")
                    maxParamcount = "0";

                if (minParamcount == "")
                    minParamcount = "0";

                if (maxParamcount == "0")
                    maxParamcount = "20";

                if (minParamcount == "0")
                    minParamcount = "20";

                int oi;
                if (!int.TryParse(maxParamcount, out oi))
                    {
                    _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), @"<div style='background-color: red'>(ConsoleFunction) Unable to Parse Max Parameter Count</div>", maxParamcount, filename, GetLineNumber(data, spart));
                    continue;
                    }

                string fparams = "";

                //we are limiting unlimited params to 20;
                for (int counter = 1; counter < (int.Parse(maxParamcount) == 0 ? 20 : int.Parse(maxParamcount)); counter++)
                    {
                    if (fparams.Length == 0)
                        fparams += "const char* a" + counter.ToString(CultureInfo.InvariantCulture);
                    else
                        fparams += ",const char* a" + counter.ToString(CultureInfo.InvariantCulture);
                    }

                string junk = spart; //.Split(',')[5].Trim();
                char prevchar = ' ';
                string ht = string.Empty;
                int pos = 0;
                bool inquote = false;
                foreach (char c in junk)
                    {
                    if ((c == '"') && (prevchar != '\\'))
                        inquote = !inquote;
                    else if ((!inquote) && (c == '{'))
                        break;
                    else
                        ht += c;
                    prevchar = c;
                    pos++;
                    }
                string helptext = ht;
                int count = 0;
                string body = string.Empty;

                foreach (char c in junk.Substring(pos))
                    {
                    if (c == '{')
                        count++;
                    if (c == '}')
                        count--;
                    body += c;
                    if (count == 0)
                        break;
                    }
                string newbody = "";
                newbody += "{\r\n";
                if (int.Parse(maxParamcount) > 1)
                    newbody += "S32 argc = " + maxParamcount + ";\r\n";
                if ((int.Parse(maxParamcount) == 0 ? 20 : int.Parse(maxParamcount)) > 1)
                    {
                    for (int ci = int.Parse(maxParamcount) - 1; ci > int.Parse(minParamcount); ci--)
                        newbody += "if (dStrlen(a" + ci + ")==0)\r\n";

                    for (int ci = int.Parse(minParamcount) + 1; ci <= int.Parse(maxParamcount); ci++)
                        {
                        newbody += "argc=" + ci.ToString(CultureInfo.InvariantCulture) + ";\r\n";
                        if (ci < int.Parse(maxParamcount))
                            newbody += "else\r\n";
                        }
                    newbody += "std::vector<const char*> arguments;\r\n";
                    newbody += @"arguments.push_back("""");" + "\r\n";
                    for (int ci = 1; ci < int.Parse(maxParamcount); ci++)
                        {
                        if (ci + 1 > int.Parse(minParamcount))
                            newbody += "if (argc>=" + (ci + 1).ToString(CultureInfo.InvariantCulture) + ")\r\n";
                        newbody += "arguments.push_back(a" + ci.ToString(CultureInfo.InvariantCulture) + ");\r\n";
                        }
                    newbody += "const char** argv = &arguments[0];\r\n";
                    }
                newbody += body + "\r\n";
                newbody += "}\r\n";
                string extername = "fn__" + function;

                if (newbody.IndexOf("argc", StringComparison.Ordinal) == newbody.LastIndexOf("argc", StringComparison.Ordinal))
                    newbody = newbody.Replace("S32 argc = 2;", "");
                if (newbody.IndexOf("argv", StringComparison.Ordinal) == newbody.LastIndexOf("argv", StringComparison.Ordinal))
                    newbody = newbody.Replace("const char** argv = &arguments[0];", "");

                if (CheckValid("*", "_" + function, filename, GetLineNumber(data, spart), "(ConsoleFunction) "))
                    _mData.Add(new Externdata(filename, extername, returntype, "", fparams, newbody, helptext, int.Parse(minParamcount) - 1, int.Parse(maxParamcount) - 1, ""));
                }
            }

        private void ParseConsoleMethod(string filename, string data)
            {
            string[] parts = Regex.Split(data, "[\n \t]+ConsoleMethod");
            foreach (string spart in parts)
                {
                if (!spart.Trim().StartsWith("(") || spart.Split(',').GetUpperBound(0) <= 4)
                    {
                    if (spart.Trim().StartsWith("("))
                        _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), @"<div style='background-color: red'>(ConsoleMethod) Failed To Parse, Expected 4 Commas<div>", spart, filename, GetLineNumber(data, spart));
                    continue;
                    }
                string objecttype = spart.Split(',')[0];
                objecttype = objecttype.Replace("(", "").Trim();
                string returntype = spart.Split(',')[2].Trim();
                string function = spart.Split(',')[1].Trim();
                function = function.Trim();

                if (objecttype.Trim() == "")
                    function = "_" + function;

                if (function.Length > 100)
                    {
                    _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), "<div style='background-color: red'>(ConsoleMethod) Function Name longer than 100<div>", function, filename, GetLineNumber(data, spart));
                    continue;
                    }

                string maxParamcount = spart.Split(',')[4].Trim();
                string minParamcount = spart.Split(',')[3].Trim();

                if (maxParamcount == "")
                    maxParamcount = "0";
                if (minParamcount == "")
                    minParamcount = "0";

                if (maxParamcount == "0")
                    maxParamcount = "20";
                if (minParamcount == "0")
                    minParamcount = "20";

                int oi;
                string fparams = "";
                if (int.TryParse(maxParamcount, out oi))
                    {
                    for (int counter = 2; counter < (int.Parse(maxParamcount) == 0 ? 20 : int.Parse(maxParamcount)); counter++)
                        {
                        if (fparams.Length == 0)
                            fparams += "const char* a" + counter.ToString(CultureInfo.InvariantCulture);
                        else
                            fparams += ",const char* a" + counter.ToString(CultureInfo.InvariantCulture);
                        }

                    string junk = spart; //.Split(',')[5].Trim();
                    char prevchar = ' ';
                    string ht = string.Empty;
                    int pos = 0;
                    bool inquote = false;
                    foreach (char c in junk)
                        {
                        if ((c == '"') && (prevchar != '\\'))
                            inquote = !inquote;
                        else if ((!inquote) && (c == '{'))
                            break;
                        else
                            ht += c;
                        prevchar = c;
                        pos++;
                        }
                    string helptext = ht;
                    int count = 0;
                    string body = string.Empty;

                    foreach (char c in junk.Substring(pos))
                        {
                        if (c == '{')
                            count++;
                        if (c == '}')
                            count--;
                        body += c;
                        if (count == 0)
                            break;
                        }
                    string newbody = "";
                    newbody += "{\r\n";
                    if (int.Parse(maxParamcount) > 1)
                        newbody += "S32 argc = " + maxParamcount + ";\r\n";

                    if (((int.Parse(maxParamcount) == 0 ? 20 : int.Parse(maxParamcount)) > 2))
                        {
                        for (int ci = int.Parse(maxParamcount) - 1; ci > int.Parse(minParamcount) - 1; ci--)
                            newbody += "if ( dStrlen(a" + ci + ") == 0 )\r\n";

                        for (int ci = int.Parse(minParamcount); ci <= int.Parse(maxParamcount); ci++)
                            {
                            newbody += "argc=" + ci.ToString(CultureInfo.InvariantCulture) + ";\r\n";
                            if (ci < int.Parse(maxParamcount))
                                newbody += "else\r\n";
                            }
                        newbody += "std::vector<const char*> arguments;\r\n";
                        newbody += @"arguments.push_back("""");" + "\r\n";
                        newbody += @"arguments.push_back("""");" + "\r\n";
                        for (int ci = 2; ci < int.Parse(maxParamcount); ci++)
                            {
                            if (ci + 1 > int.Parse(minParamcount))
                                newbody += "if ( argc >" + (ci).ToString(CultureInfo.InvariantCulture) + " )\r\n";
                            newbody += "arguments.push_back(a" + ci.ToString(CultureInfo.InvariantCulture) + ");\r\n";
                            }

                        newbody += "const char** argv = &arguments[0];\r\n";
                        }
                    newbody += body + "\r\n";

                    newbody += "}\r\n";

                    string extername = "fn" + objecttype + "_" + function;

                    if (newbody.IndexOf("argc", StringComparison.Ordinal) == newbody.LastIndexOf("argc", StringComparison.Ordinal))
                        newbody = newbody.Replace("S32 argc = 2;", "");
                    if (newbody.IndexOf("argv", StringComparison.Ordinal) == newbody.LastIndexOf("argv", StringComparison.Ordinal))
                        newbody = newbody.Replace("const char** argv = &arguments[0];", "");
                    //

                    if (CheckValid(objecttype, function, filename, GetLineNumber(data, spart), "(ConsoleMethod) "))
                        _mData.Add(new Externdata(filename, extername, returntype, objecttype, fparams, newbody, helptext, int.Parse(minParamcount) - 2, int.Parse(maxParamcount) - 2, ""));
                    }
                else
                    _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), "(ConsoleMethod) Failed to Parse Max Param Count", maxParamcount, filename, GetLineNumber(data, spart));
                }
            }

        private void ParseDefineEngineFunction(string filename, string data)
            {
            string[] parts = Regex.Split(data, "DefineEngineFunction");
            foreach (string spart in parts)
                {
                if (!spart.Trim().StartsWith("("))
                    continue;
                string part = spart.Trim();
                string fn = part.Substring(1, part.IndexOf(",", StringComparison.Ordinal) - 1).Trim();
                string extername = "fn" + "_" + fn.Trim();

                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);
                string returntype = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                part = part.Substring(part.IndexOf("(", StringComparison.Ordinal) + 1);
                string sparams = part.Substring(0, part.IndexOf(")", StringComparison.Ordinal));
                sparams = sparams.Replace("\r\n", "");
                part = part.Substring(part.IndexOf(")", StringComparison.Ordinal) + 1);

                string defaults = "";
                bool foundComma = false;
                for (int i = 0; i < part.Length; i++)
                    {
                    if (part[i] == ',' && !foundComma)
                        foundComma = true;
                    else if (part[i] == '(' && foundComma)
                        {
                        defaults = part.Substring(part.IndexOf("(") + 1).Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                        defaults = defaults.Substring(0, defaults.IndexOf("),\""));
                        break;
                        }
                    else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                        continue;
                    else
                        break;
                    }

                string ht = "";
                char prevchar = ' ';
                string helptext = part.Substring(part.IndexOf("\"", StringComparison.Ordinal));
                bool inquote = false;
                int pos = 0;
                foreach (char c in helptext)
                    {
                    if ((c == '"') && (prevchar != '\\'))
                        inquote = !inquote;
                    else if ((!inquote) && (c == '{'))
                        break;
                    else
                        ht += c;
                    prevchar = c;
                    pos++;
                    }
                part = helptext.Substring(pos);
                helptext = ht;
                string body = "";
                int count = 0;
                foreach (char c in part)
                    {
                    if (c == '{')
                        count++;
                    if (c == '}')
                        count--;
                    body += c;
                    if (count == 0)
                        break;
                    }
                sparams = sparams.Replace('\t', ' ');
                char lastchar = ' ';
                string np = "";
                foreach (char c in sparams)
                    {
                    if (!((c == ' ') && (lastchar == ' ')))
                        np = np + c;
                    lastchar = c;
                    }
                if (returntype != ("Torque::UUID"))
                    {
                    if (CheckValid("*", fn, filename, GetLineNumber(data, spart), "(DefineEngineFunction) "))
                        _mData.Add(new Externdata(filename, extername, returntype, "", np, body, helptext, defaults));
                    }
                }
            }

        private void ParseDefineEngineMethod(string filename, string data)
            {
            string[] parts = Regex.Split(data, "DefineEngineMethod");
            foreach (string spart in parts)
                {
                if (!spart.Trim().StartsWith("("))
                    continue;
                string part = spart.Trim();
                string objecttype = part.Substring(1, part.IndexOf(",", StringComparison.Ordinal) - 1).Trim();
                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);
                string fn = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                string extername = "fn" + objecttype.Trim() + "_" + fn.Trim();
                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);
                string returntype = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                part = part.Substring(part.IndexOf("(", StringComparison.Ordinal) + 1);
                string sparams = part.Substring(0, part.IndexOf(")", StringComparison.Ordinal));
                sparams = sparams.Replace("\r\n", "");
                part = part.Substring(part.IndexOf(")", StringComparison.Ordinal) + 1);

                string defaults = "";
                bool foundComma = false;
                for (int i = 0; i < part.Length; i++)
                    {
                    if (part[i] == ',' && !foundComma)
                        foundComma = true;
                    else if (part[i] == '(' && foundComma)
                        {
                        defaults = part.Substring(part.IndexOf("(") + 1).Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                        defaults = defaults.Substring(0, defaults.IndexOf("),\""));

                        break;
                        }
                    else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                        continue;
                    else
                        break;
                    }

                string ht = "";
                char prevchar = ' ';
                string helptext = part.Substring(part.IndexOf("\"", StringComparison.Ordinal));
                bool inquote = false;
                int pos = 0;
                foreach (char c in helptext)
                    {
                    if ((c == '"') && (prevchar != '\\'))
                        inquote = !inquote;
                    else if ((!inquote) && (c == '{'))
                        break;
                    else
                        ht += c;
                    prevchar = c;
                    pos++;
                    }
                part = helptext.Substring(pos);
                helptext = ht;
                string body = "";
                int count = 0;
                foreach (char c in part)
                    {
                    if (c == '{')
                        count++;
                    if (c == '}')
                        count--;
                    body += c;
                    if (count == 0)
                        break;
                    }
                sparams = sparams.Replace('\t', ' ');
                char lastchar = ' ';
                string np = "";
                foreach (char c in sparams)
                    {
                    if (!((c == ' ') && (lastchar == ' ')))
                        np = np + c;
                    lastchar = c;
                    }
                if (CheckValid(objecttype, fn, filename, GetLineNumber(data, spart), "(DefineEngineMethod) "))
                    _mData.Add(new Externdata(filename, extername, returntype, objecttype, np, body, helptext, defaults));
                }
            }

        private void DefineTSShapeConstructorMethod(string filename, string data)
            {



            string[] parts = Regex.Split(data, "DefineTSShapeConstructorMethod");
            foreach (string spart in parts)
                {
                try
                    {
                    if (!spart.Trim().StartsWith("("))
                        continue;
                    string part = spart.Trim();
                    const string objecttype = "TSShapeConstructor";

                    string fn = part.Substring(1, part.IndexOf(",", StringComparison.Ordinal) - 1).Trim();
                    part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);
                    string extername = "fn" + objecttype.Trim() + "_" + fn.Trim();

                    // string fn = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                    //part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);

                    string returntype = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                    part = part.Substring(part.IndexOf("(", StringComparison.Ordinal) + 1);
                    string sparams = part.Substring(0, part.IndexOf(")", StringComparison.Ordinal));
                    sparams = sparams.Replace("\r\n", "");
                    part = part.Substring(part.IndexOf(")", StringComparison.Ordinal) + 1);

                    string defaults = "";
                    bool foundComma = false;
                    bool foundLastparan = false;
                    bool foundFirstParan = false;
                    int i;

                    foundComma = false;
                    foundLastparan = false;
                    foundFirstParan = false;
                    for (i = 0; i < part.Length; i++)
                        {
                        if (part[i] == ',' && !foundComma)
                            foundComma = true;

                        else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                            continue;
                        else if (part[i] == '(' && foundComma)
                            {
                            foundFirstParan = true;
                            defaults += part[i];
                            }
                        else if (part[i] == ')' && foundComma)
                            {
                            foundLastparan = true;
                            defaults += part[i];
                            }
                        else if (foundLastparan && foundComma && part[i] == ',')
                            {
                            break;
                            }
                        else if (!foundLastparan && !foundFirstParan && foundComma && part[i] == ',')
                            {
                            break; //None specified
                            }
                        else
                            defaults += part[i];
                        }

                    defaults = defaults.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace("(","").Replace(")","");

                    part = part.Substring(i);

                    string rawArgs = "";
                    foundComma = false;
                    foundLastparan = false;
                    foundFirstParan = false;
                    for (i = 0; i < part.Length; i++)
                        {
                        if (part[i] == ',' && !foundComma)
                            foundComma = true;

                        else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                            continue;
                        else if (part[i] == '(' && foundComma)
                            {
                            foundFirstParan = true;
                            rawArgs += part[i];
                            }
                        else if (part[i] == ')' && foundComma)
                            {
                            foundLastparan = true;
                            rawArgs += part[i];
                            }
                        else if (foundLastparan && foundComma && part[i] == ',')
                            {
                            break;
                            }
                        else if (!foundLastparan && !foundFirstParan && foundComma && part[i] == ',')
                            {
                            break; //None specified
                            }
                        else
                            rawArgs += part[i];
                        }

                    rawArgs = rawArgs.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

                    part = part.Substring(i);

                    string defRet = "";
                    foundComma = false;
                    //foundLastparan = false;
                    //foundFirstParan = false;
                    for (i = 0; i < part.Length; i++)
                        {
                        if (part[i] == ',' && !foundComma)
                            foundComma = true;

                        else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                            continue;
                        else if (part[i] == ',' && foundComma)
                            {
                            break;
                            }
                        //else if (part[i] == ')' && foundComma)
                        //    {
                        //    foundLastparan = true;
                        //    defRet += part[i];
                        //    }
                        //else if (foundLastparan && foundComma && part[i] == ',')
                        //    {
                        //    break;
                        //    }
                        //else if (!foundLastparan && !foundFirstParan && foundComma && part[i] == ',')
                        //    {
                        //    break; //None specified
                        //    }
                        else
                            defRet += part[i];
                        }

                    defRet = defRet.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

                    part = part.Substring(i);


                    string ht = "";
                    char prevchar = ' ';
                    string helptext = part.Substring(part.IndexOf("\"", StringComparison.Ordinal));
                    bool inquote = false;
                    int pos = 0;
                    foreach (char c in helptext)
                        {
                        if ((c == '"') && (prevchar != '\\'))
                            inquote = !inquote;
                        else if ((!inquote) && (c == '{'))
                            break;
                        else
                            ht += c;
                        prevchar = c;
                        pos++;
                        }
                    part = helptext.Substring(pos);
                    helptext = ht;
                    string body = "";
                    int count = 0;
                    foreach (char c in part)
                        {
                        if (c == '{')
                            count++;
                        if (c == '}')
                            count--;
                        body += c;
                        if (count == 0)
                            break;
                        }
                    sparams = sparams.Replace('\t', ' ');
                    char lastchar = ' ';
                    string np = "";
                    foreach (char c in sparams)
                        {
                        if (!((c == ' ') && (lastchar == ' ')))
                            np = np + c;
                        lastchar = c;
                        }
                    body = @"
{
      /* Check that shape is loaded */
      if( !object->getShape() ) 
      {
         Con::errorf( ""TSShapeConstructor::" + fn + @" - shape not loaded"" );
         return " + defRet + @";
      } 
      return object->" + fn + @" " + rawArgs + @";
   } 
";


                    if (CheckValid(objecttype, fn, filename, GetLineNumber(data, spart), "(DefineTSShapeConstructorMethod) "))
                        _mData.Add(new Externdata(filename, extername, returntype, objecttype, np, body, helptext, defaults));
                    }
                catch (Exception)
                    {
                    }
                }

            }

        private void ParseDefineEngineStaticMethod(string filename, string data)
            {
            string[] parts = Regex.Split(data, "DefineEngineStaticMethod");
            foreach (string spart in parts)
                {
                if (!spart.Trim().StartsWith("("))
                    continue;

                string part = spart.Trim();
                string objectname = part.Substring(1, part.IndexOf(",", StringComparison.Ordinal) - 1).Trim();
                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);
                string fn = part.Substring(1, part.IndexOf(",", StringComparison.Ordinal) - 1).Trim();
                string extername = "fn" + "_" + objectname.Trim() + "_" + fn.Trim();

                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);

                string returntype = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                part = part.Substring(part.IndexOf("(", StringComparison.Ordinal) + 1);

                string sparams = part.Substring(0, part.IndexOf(")", StringComparison.Ordinal));
                sparams = sparams.Replace("\r\n", "");
                part = part.Substring(part.IndexOf(")", StringComparison.Ordinal) + 1);

                string defaults = "";
                bool foundComma = false;
                for (int i = 0; i < part.Length; i++)
                    {
                    if (part[i] == ',' && !foundComma)
                        foundComma = true;
                    else if (part[i] == '(' && foundComma)
                        {
                        defaults = part.Substring(part.IndexOf("(") + 1).Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                        defaults = defaults.Substring(0, defaults.IndexOf("),\""));
                        break;
                        }
                    else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                        continue;
                    else
                        break;
                    }

                string ht = "";
                char prevchar = ' ';
                string helptext = part.Substring(part.IndexOf("\"", StringComparison.Ordinal));
                bool inquote = false;
                int pos = 0;
                foreach (char c in helptext)
                    {
                    if ((c == '"') && (prevchar != '\\'))
                        inquote = !inquote;
                    else if ((!inquote) && (c == '{'))
                        break;
                    else
                        ht += c;
                    prevchar = c;
                    pos++;
                    }
                part = helptext.Substring(pos);
                helptext = ht;
                string body = "";
                int count = 0;
                foreach (char c in part)
                    {
                    if (c == '{')
                        count++;
                    if (c == '}')
                        count--;
                    body += c;
                    if (count == 0)
                        break;
                    }
                sparams = sparams.Replace('\t', ' ');
                char lastchar = ' ';
                string np = "";
                foreach (char c in sparams)
                    {
                    if (!((c == ' ') && (lastchar == ' ')))
                        np = np + c;
                    lastchar = c;
                    }
                if (CheckValid(objectname, fn, filename, GetLineNumber(data, spart), "(DefineEngineStaticMethod) "))
                    _mData.Add(new Externdata(filename, extername, returntype, objectname.Trim(), np, body, helptext, defaults));
                }
            }

        private void ParseDefineConsoleFunction(string filename, string data)
            {
            string[] parts = Regex.Split(data, "DefineConsoleFunction");
            foreach (string spart in parts)
                {
                if (!spart.Trim().StartsWith("("))
                    continue;
                string part = spart.Trim();
                string fn = part.Substring(1, part.IndexOf(",", StringComparison.Ordinal) - 1).Trim();
                if (fn == "setNetPort")
                    Console.WriteLine("");
                string extername = "fn" + "_" + fn.Trim();
                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);
                string returntype = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                part = part.Substring(part.IndexOf("(", StringComparison.Ordinal) + 1);
                string sparams = part.Substring(0, part.IndexOf(")", StringComparison.Ordinal));
                sparams = sparams.Replace("\r\n", "");
                part = part.Substring(part.IndexOf(")", StringComparison.Ordinal) + 1);

                #region HideME

                string defaults = "";
                bool foundComma = false;
                for (int i = 0; i < part.Length; i++)
                    {
                    if (part[i] == ',' && !foundComma)
                        foundComma = true;
                    else if (part[i] == '(' && foundComma)
                        {
                        defaults = part.Substring(part.IndexOf("(") + 1).Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                        defaults = defaults.Substring(0, defaults.IndexOf("),\""));
                        break;
                        }
                    else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                        continue;
                    else
                        break;
                    }

                string ht = "";
                char prevchar = ' ';
                string helptext = part.Substring(part.IndexOf("\"", StringComparison.Ordinal));
                bool inquote = false;
                int pos = 0;
                foreach (char c in helptext)
                    {
                    if ((c == '"') && (prevchar != '\\'))
                        inquote = !inquote;
                    else if ((!inquote) && (c == '{'))
                        break;
                    else
                        ht += c;
                    prevchar = c;
                    pos++;
                    }
                part = helptext.Substring(pos);
                helptext = ht;
                string body = "";
                int count = 0;
                foreach (char c in part)
                    {
                    if (c == '{')
                        count++;
                    if (c == '}')
                        count--;
                    body += c;
                    if (count == 0)
                        break;
                    }
                sparams = sparams.Replace('\t', ' ');
                char lastchar = ' ';
                string np = "";
                foreach (char c in sparams)
                    {
                    if (!((c == ' ') && (lastchar == ' ')))
                        np = np + c;
                    lastchar = c;
                    }
                if (CheckValid("*", fn, filename, GetLineNumber(data, spart), "(DefineConsoleFunction) "))
                    _mData.Add(new Externdata(filename, extername, returntype, "", np, body, helptext, defaults));
                }

                #endregion
            }

        private void ParseDefineConsoleMethod(string filename, string data)
            {
            //DefineConsoleMethod( SimObject, save, bool, ( const char* fileName, bool selectedOnly, const char* preAppendString ), ( false, "" ),
            string[] parts = Regex.Split(data, "DefineConsoleMethod");
            foreach (string spart in parts)
                {
                if (!spart.Trim().StartsWith("("))
                    continue;

                string part = spart.Trim();
                string objectname = part.Substring(1, part.IndexOf(",", StringComparison.Ordinal) - 1).Trim();
                if (objectname == "AIConnection")
                    Console.WriteLine("");

                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);
                string fn = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal) - 0).Trim();
                string extername = "fn" + "_" + objectname.Trim() + "_" + fn.Trim();

                part = part.Substring(part.IndexOf(",", StringComparison.Ordinal) + 1);

                string returntype = part.Substring(0, part.IndexOf(",", StringComparison.Ordinal)).Trim();
                part = part.Substring(part.IndexOf("(", StringComparison.Ordinal) + 1);

                string sparams = part.Substring(0, part.IndexOf(")", StringComparison.Ordinal));
                sparams = sparams.Replace("\r\n", "");
                part = part.Substring(part.IndexOf(")", StringComparison.Ordinal) + 1);
                //Needs to be +2 to get ride of comma

                string defaults = "";
                bool foundComma = false;
                for (int i = 0; i < part.Length; i++)
                    {
                    if (part[i] == ',' && !foundComma)
                        foundComma = true;
                    else if (part[i] == '(' && foundComma)
                        {
                        defaults = part.Substring(part.IndexOf("(") + 1).Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                        defaults = defaults.Substring(0, defaults.IndexOf("),\""));
                        break;
                        }
                    else if (part[i] == ' ' || part[i] == '\r' || part[i] == '\n' || part[i] == '\t')
                        continue;
                    else
                        break;
                    }

                string ht = "";
                char prevchar = ' ';
                string helptext = "";
                try
                    {
                    helptext = part.Substring(part.IndexOf("\"", StringComparison.Ordinal));
                    bool inquote = false;
                    int pos = 0;
                    foreach (char c in helptext)
                        {
                        if ((c == '"') && (prevchar != '\\'))
                            inquote = !inquote;
                        else if ((!inquote) && (c == '{'))
                            break;
                        else
                            ht += c;
                        prevchar = c;
                        pos++;
                        }
                    part = helptext.Substring(pos);
                    helptext = ht;
                    }
                catch (Exception)
                    {
                    }

                string body = "";
                int count = 0;
                foreach (char c in part)
                    {
                    if (c == '{')
                        count++;
                    if (c == '}')
                        count--;
                    body += c;
                    if (count == 0)
                        break;
                    }
                sparams = sparams.Replace('\t', ' ');
                char lastchar = ' ';
                string np = "";
                foreach (char c in sparams)
                    {
                    if (!((c == ' ') && (lastchar == ' ')))
                        np = np + c;
                    lastchar = c;
                    }
                if (CheckValid(objectname, fn, filename, GetLineNumber(data, spart), "(DefineConsoleMethod) "))
                    _mData.Add(new Externdata(filename, extername, returntype, objectname.Trim(), np, body, helptext, defaults));
                }
            }

        private void ParseIMPLEMENT_CALLBACK(string filename, string data)
            {
            string sdata = removeComments(data);

            Match match = Regex.Match(sdata, @"IMPLEMENT_CALLBACK[ *\r*\n*\t]*[\( *] *(?<ClassName>[A-Za-z0-9]*)[ *\r*\n*\t]*,[ *\r*\n*\t]*(?<Function>[ A-Za-z0-9]*)[ *\r*\n*\t]*,[ *\r*\n*\t]*(?<ReturnType>[ A-Za-z0-9* ]*)[ *\r*\n*\t]*,[ *\r*\n*\t]*[\(](?<Parameters>[ A-Za-z0-9*&,]*)[\\)][ *\r*\n*\t]*,[ *\r*\n*\t]*[\(][a-zA-Z0-9, *\r*\n*\t]*[\\)][ *\r*\n*\t]*,[ *\r*\n*\t]*(?<Description>[\x21\x22\x23\x24\x25\x26\x27\x28\x29\x2A\x2B\x2C\x2D\x2E\x2F\x30\x31\x32\x33\x34\x35\x36\x37\x38\x39\x3A\x3C\x3D\x3E\x3F\x40\x41\x42\x43\x44\x45\x46\x47\x48\x49\x4A\x4B\x4C\x4D\x4E\x4F\x50\x51\x52\x53\x54\x55\x56\x57\x58\x59\x5A\x5B\x5C\x5D\x5E\x5F\x60\x61\x62\x63\x64\x65\x66\x67\x68\x69\x6A\x6B\x6C\x6D\x6E\x6F\x70\x71\x72\x73\x74\x75\x76\x77\x78\x79\x7A\x7B\x7C\x7D \r*\n*\t]*\))");
            while (match.Success)
                {
                ImplementCallback ic = new ImplementCallback();

                if (match.Groups["ClassName"].Value.Trim() != "")
                    ic.mClassname = match.Groups["ClassName"].Value.Trim();

                if (match.Groups["Function"].Value.Trim() != "")
                    ic.mFunction = match.Groups["Function"].Value.Trim();

                if (match.Groups["ReturnType"].Value.Trim() != "")
                    ic.mReturnType = match.Groups["ReturnType"].Value.Trim();

                if (match.Groups["Description"].Value.Trim() != "")
                    ic.mComments = match.Groups["Description"].Value.Trim().Replace("\r", "").Replace("\n", "").Replace("\\n", "").Replace("\"", "");

                if (match.Groups["Parameters"].Value.Trim() != "")
                    ic.mParams = match.Groups["Parameters"].Value.Trim();

                _mImplementCallback.Add(ic);

                match = match.NextMatch();
                }
            match = Regex.Match(sdata, @"IMPLEMENT_GLOBAL_CALLBACK[ *\r*\n*\t]*[\( *] *[ *\r*\n*\t]*(?<Function>[ A-Za-z0-9]*)[ *\r*\n*\t]*,[ *\r*\n*\t]*(?<ReturnType>[ A-Za-z0-9* ]*)[ *\r*\n*\t]*,[ *\r*\n*\t]*[\(](?<Parameters>[ A-Za-z0-9*&,]*)[\\)][ *\r*\n*\t]*,[ *\r*\n*\t]*[\(][a-zA-Z0-9, *\r*\n*\t]*[\\)][ *\r*\n*\t]*,[ *\r*\n*\t]*(?<Description>[\x21\x22\x23\x24\x25\x26\x27\x28\x29\x2A\x2B\x2C\x2D\x2E\x2F\x30\x31\x32\x33\x34\x35\x36\x37\x38\x39\x3A\x3C\x3D\x3E\x3F\x40\x41\x42\x43\x44\x45\x46\x47\x48\x49\x4A\x4B\x4C\x4D\x4E\x4F\x50\x51\x52\x53\x54\x55\x56\x57\x58\x59\x5A\x5B\x5C\x5D\x5E\x5F\x60\x61\x62\x63\x64\x65\x66\x67\x68\x69\x6A\x6B\x6C\x6D\x6E\x6F\x70\x71\x72\x73\x74\x75\x76\x77\x78\x79\x7A\x7B\x7C\x7D \r*\n*\t]*\))");
            while (match.Success)
                {
                ImplementGlobalCallback ic = new ImplementGlobalCallback();
                if (match.Groups["Function"].Value.Trim() != "")
                    ic.mFunction = match.Groups["Function"].Value.Trim();

                if (match.Groups["ReturnType"].Value.Trim() != "")
                    ic.mReturnType = match.Groups["ReturnType"].Value.Trim();

                if (match.Groups["Description"].Value.Trim() != "")
                    ic.mComments = match.Groups["Description"].Value.Trim().Replace("\r", "").Replace("\n", "").Replace("\\n", "").Replace("\"", "");

                if (match.Groups["Parameters"].Value.Trim() != "")
                    ic.mParams = match.Groups["Parameters"].Value.Trim();

                _mGlobalCallbacks.Add(ic);
                match = match.NextMatch();
                }

            //int i = 0;
            //while (i < sdata.Length)
            //    {

            //    if (i + "IMPLEMENT_GLOBAL_CALLBACK".Length < sdata.Length)
            //        {
            //        if (sdata.Substring(i, "IMPLEMENT_GLOBAL_CALLBACK".Length) == "IMPLEMENT_GLOBAL_CALLBACK")
            //            {
            //            ImplementGlobalCallback ic = new ImplementGlobalCallback();
            //            while (sdata[i] != '(')
            //                i++;
            //            i++;

            //            while (sdata[i] != ',')
            //                {
            //                ic.mFunction += sdata[i];
            //                i++;
            //                }
            //            i++;
            //            while (sdata[i] != ',')
            //                {
            //                ic.mReturnType += sdata[i];
            //                i++;
            //                }
            //            i++;
            //            while (sdata[i] != '(')
            //                {
            //                i++;
            //                }
            //            i++;
            //            while (sdata[i] != ')')
            //                {
            //                ic.mParams += sdata[i];
            //                i++;
            //                }
            //            i++;
            //            while (sdata[i] != ',')
            //                {
            //                i++;
            //                }
            //            i++;
            //            while (sdata[i] != ',')
            //                {
            //                i++;
            //                }
            //            i++;
            //            bool inquote = false;
            //            while (true)
            //                {
            //                if (sdata[i] == '"')
            //                    if (inquote == false)
            //                        inquote = true;
            //                    else
            //                        inquote = false;

            //                if (sdata[i] == ')' && !inquote)
            //                    break;

            //                ic.mComments += sdata[i];
            //                i++;
            //                }

            //            if (ic.mFunction.Trim().ToLower() != "name")
            //                {
            //                ic.trim();
            //                }
            //            _mGlobalCallbacks.Add(ic);
            //            }
            //        }
            //    i++;

            //    }
            }

        private void ParseInitPersist(string filename, string data)
            {
            //if (filename.ToLower().Contains("guitypes.cpp"))
            //    Console.WriteLine("");
            data = data.Replace("Parent::initPersistFields();", "");
            data = removeComments(data);

            Match match = Regex.Match(data, "#define *(?<DefineName>[A-Za-z_]*) *(?<Value>[0-9.-]*)");
            while (match.Success)
                {
                if (match.Groups["Value"].Value.Trim() != "")
                    _mDefined.Add(new DefinedValues(match.Groups["DefineName"].Value, match.Groups["Value"].Value));
                match = match.NextMatch();
                }

            match = Regex.Match(data, @"void (?<Class>\w+)::initPersistFields\( *\)(?:\r*\n*){(?<Code>(?:\r*\n*)[()\,;\[\].""*\w\s//<>:&\-@\\#$'=%]*)}");

            while (match.Success)
                {
                for (int i = 0; i < match.Groups["Class"].Captures.Count; i++)
                    _ParseInitPersist(filename, match.Groups["Class"].Captures[i].Value, match.Groups["Code"].Captures[i].Value);

                match = match.NextMatch();
                }
            }

        private void _ParseInitPersist(string filename, string className, string part)
            {
            if (filename.ToLower().Contains("posteffect.cpp"))
                Console.Write("");
            string currentgroup = "";
            for (int i = 0; i < part.Length; i++)
                {
                if (i + "addGroup(\"".Length < part.Length)
                    {
                    if (part.Substring(i, "addGroup(".Length) == "addGroup(")
                        {
                        #region AddGroup

                        bool foundFirstQuote = false;
                        i = i + "addGroup(".Length;
                        char c = ' ';
                        while (c != '"')
                            {
                            c = part[i];
                            if (c != '"')
                                currentgroup += c;
                            else if (!foundFirstQuote && c == '"')
                                {
                                c = ' ';
                                foundFirstQuote = true;
                                }
                            i++;
                            }

                        #endregion

                        continue;
                        }
                    }
                if (part.Length >= i + "endGroup(".Length)
                    {
                    if (part.Substring(i, "endGroup(".Length) == "endGroup(")
                        {
                        i = i + "endGroup(".Length;
                        currentgroup = "";
                        continue;
                        }
                    }

                if (part.Length >= i + "addFieldV".Length)
                    {
                    if (part.Substring(i, "addFieldV".Length) == "addFieldV")
                        {
                        #region addFieldV

                        int start = i;
                        char lc = ' ';
                        while (true)
                            {
                            i++;
                            if (part[i] == ';' && lc == ')')
                                {
                                #region

                                i++;
                                string code = part.Substring(start, i - start).Replace("\r", "").Replace("\n", "").Replace("  ", " ");
                                InitPersistData d = new InitPersistData();

                                int readingpart = 0;
                                bool isreadingname = false;
                                bool isreadingtype = false;
                                bool isReadingValidator = false;

                                char lastnonspacechar = ' ';
                                bool readall = false;

                                foreach (char c in code)
                                    {
                                    switch (readingpart)
                                        {
                                        case 0:
                                            if (c == '"' && !isreadingname)
                                                isreadingname = true;
                                            else if (c == '"' && isreadingname)
                                                {
                                                isreadingname = false;
                                                readingpart = 1;
                                                }
                                            else if (isreadingname)
                                                d.MName += c;
                                            break;
                                        case 1:
                                            if (c == ',' && !isreadingtype)
                                                isreadingtype = true;
                                            else if (c == ',' && isreadingtype)
                                                {
                                                isreadingtype = false;
                                                readingpart = 2;
                                                }
                                            else if (isreadingtype)
                                                d.MType += c;
                                            break;
                                        case 2:
                                            if (c == '&')
                                                readingpart = 3;
                                            else if ((c == ';') && (lastnonspacechar == ')'))
                                                {
                                                readall = true;
                                                readingpart = 5;
                                                }
                                            else
                                                d.MOffsetClass += c;
                                            break;
                                        case 3:
                                            if (c == ',')
                                                readingpart = 4;
                                            break;

                                        case 4:
                                            if ((c == ';') && (lastnonspacechar == ')'))
                                                {
                                                readingpart = 5;
                                                readall = true;
                                                }
                                            else
                                                d.MComment += c;
                                            break;
                                        case 5:
                                            readall = true;
                                            break;
                                        }
                                    if (c != ' ')
                                        lastnonspacechar = c;
                                    }
                                if (readall)
                                    {
                                    d.MName = d.MName.Trim();
                                    d.MType = d.MType.Trim();
                                    d.MOffsetClass = d.MOffsetClass.Trim();
                                    d.MComment = d.MComment.Trim();
                                    d.MGroup = currentgroup;
                                    if (d.MComment != "")
                                        {
                                        d.MComment = d.MComment.Trim().Replace("\"", "");
                                        d.MComment = d.MComment.Substring(0, d.MComment.LastIndexOf(')'));
                                        }
                                    d.MClassName = className;
                                    _mLogger.Stage11Log(filename, d.ToString());
                                    _midata.Add(d);
                                    //foundaf++;
                                    }

                                break;

                                #endregion
                                }

                            if (part[i] != ' ')
                                lc = part[i];
                            }

                        #endregion

                        continue;
                        }
                    }

                if (part.Length >= i + "addField".Length)
                    {
                    if (part.Substring(i, "addField".Length) == "addField")
                        {
                        #region addField

                        int start = i;
                        char lc = ' ';
                        while (true)
                            {
                            i++;
                            if (part[i] == ';' && lc == ')')
                                {
                                #region

                                i++;
                                string code = part.Substring(start, i - start).Replace("\r", "").Replace("\n", "").Replace("  ", " ");
                                InitPersistData d = new InitPersistData();

                                int readingpart = 0;
                                bool isreadingname = false;
                                bool isreadingtype = false;

                                char lastnonspacechar = ' ';
                                bool readall = false;

                                foreach (char c in code)
                                    {
                                    switch (readingpart)
                                        {
                                        case 0:
                                            if (c == '"' && !isreadingname)
                                                isreadingname = true;
                                            else if (c == '"' && isreadingname)
                                                {
                                                isreadingname = false;
                                                readingpart = 1;
                                                }
                                            else if (isreadingname)
                                                d.MName += c;
                                            break;
                                        case 1:
                                            if (c == ',' && !isreadingtype)
                                                isreadingtype = true;
                                            else if (c == ',' && isreadingtype)
                                                {
                                                isreadingtype = false;
                                                readingpart = 2;
                                                }
                                            else if (isreadingtype)
                                                d.MType += c;
                                            break;
                                        case 2:
                                            if (c == ',' && lastnonspacechar == ')')
                                                readingpart = 3;
                                            else if ((c == ';') && (lastnonspacechar == ')'))
                                                {
                                                readall = true;
                                                readingpart = 5;
                                                }
                                            else
                                                d.MOffsetClass += c;
                                            break;
                                        case 3:

                                            if (c == '"')
                                                readingpart = 4;
                                            else if (c != ' ' && c != ',')
                                                d.MElementCount += c;
                                            else if (c == ',')
                                                readingpart = 4;

                                            break;
                                        case 4:
                                            if ((c == ';') && (lastnonspacechar == ')'))
                                                {
                                                readingpart = 5;
                                                readall = true;
                                                }
                                            else
                                                {
                                                //if (c != ' ')
                                                //    lastnonspacechar = c;
                                                d.MComment += c;
                                                }
                                            break;
                                        case 5:
                                            readall = true;
                                            break;
                                        }
                                    if (c != ' ')
                                        lastnonspacechar = c;
                                    }
                                if (readall)
                                    {
                                    d.MName = d.MName.Trim();
                                    d.MType = d.MType.Trim();
                                    d.MOffsetClass = d.MOffsetClass.Trim();
                                    d.MComment = d.MComment.Trim();
                                    d.MGroup = currentgroup;
                                    if (d.MComment != "")
                                        {
                                        d.MComment = d.MComment.Trim().Replace("\"", "");
                                        d.MComment = d.MComment.Substring(0, d.MComment.LastIndexOf(')'));
                                        }
                                    d.MClassName = className;
                                    _mLogger.Stage11Log(filename, d.ToString());
                                    _midata.Add(d);
                                    //foundaf++;
                                    }

                                break;

                                #endregion
                                }

                            if (part[i] != ' ')
                                lc = part[i];
                            }

                        #endregion

                        continue;
                        }
                    }
                if (part.Length >= i + "addProtectedField".Length)
                    {
                    if (part.Substring(i, "addProtectedField".Length) == "addProtectedField")
                        {
                        #region AddProtectedField

                        int start = i;
                        char lc = ' ';
                        while (true)
                            {
                            i++;
                            if (part[i] == ';' && lc == ')')

                            #region

                                {
                                i++;
                                string code = part.Substring(start, i - start).Replace("\r", "").Replace("\n", "").Replace("  ", " ");
                                //addProtectedField( "stateToken", TYPEID< RenderPassStateToken >(), Offset( mStateToken, RenderPassStateBin ),   _setStateToken, _getStateToken );
                                InitPersistData d = new InitPersistData();

                                int readingpart = 0;
                                bool isreadingname = false;
                                bool isreadingtype = false;
                                bool isreadingcomment = false;

                                char lastnonspacechar = ' ';
                                bool readall = false;

                                foreach (char c in code)
                                    {
                                    #region

                                    switch (readingpart)
                                        {
                                        case 0:
                                            if (c == '"' && !isreadingname)
                                                isreadingname = true;
                                            else if (c == '"' && isreadingname)
                                                {
                                                isreadingname = false;
                                                readingpart = 1;
                                                }
                                            else if (isreadingname)
                                                d.MName += c;
                                            break;
                                        case 1:
                                            if (c == ',' && !isreadingtype)
                                                isreadingtype = true;
                                            else if (c == ',' && isreadingtype)
                                                {
                                                isreadingtype = false;
                                                readingpart = 2;
                                                }
                                            else if (isreadingtype)
                                                d.MType += c;
                                            break;
                                        case 2:
                                            if (c == ',' && lastnonspacechar == ')')
                                                {
                                                readingpart = 3;
                                                lastnonspacechar = ' ';
                                                }
                                            else if (d.MOffsetClass.Trim() == "NULL")
                                                {
                                                lastnonspacechar = ' ';
                                                readingpart = 3;
                                                }
                                            else if (c == ';' && lastnonspacechar == ')')
                                                {
                                                readingpart = 4;
                                                readall = true;
                                                }
                                            else
                                                {
                                                d.MOffsetClass += c;
                                                if (c != ' ')
                                                    lastnonspacechar = c;
                                                }
                                            break;
                                        case 3:
                                            if (!isreadingcomment && c == '"')
                                                isreadingcomment = true;
                                            else if (c == ';' && lastnonspacechar == ')')
                                                {
                                                readingpart = 4;
                                                readall = true;
                                                }

                                            else if (isreadingcomment && (c == ';') && (lastnonspacechar == ')'))
                                                {
                                                readingpart = 4;
                                                readall = true;
                                                }
                                            else if (isreadingcomment)
                                                d.MComment += c;
                                            break;
                                        case 4:
                                            readall = true;
                                            break;
                                        }

                                    #endregion

                                    if (c != ' ')
                                        lastnonspacechar = c;
                                    }

                                if (readall)
                                    {
                                    d.MName = d.MName.Trim();
                                    d.MType = d.MType.Trim();
                                    d.MOffsetClass = d.MOffsetClass.Trim();
                                    d.MComment = d.MComment.Trim();
                                    d.MGroup = currentgroup;
                                    if (d.MComment != "")
                                        {
                                        d.MComment = d.MComment.Trim().Replace("\"", "");
                                        d.MComment = d.MComment.Substring(0, d.MComment.LastIndexOf(')'));
                                        }
                                    d.MClassName = className;
                                    _mLogger.Stage11Log(filename, d.ToString());
                                    _midata.Add(d);
                                    //foundapf++;
                                    }
                                break;
                                }

                            #endregion

                            if (part[i] != ' ')
                                lc = part[i];
                            }

                        #endregion

                        continue;
                        }
                    }
                if (part.Length >= i + "addDeprecatedField".Length)
                    {
                    if (part.Substring(i, "addDeprecatedField".Length) == "addDeprecatedField")
                        {
                        #region addDeprecatedField

                        int start = i;
                        char lc = ' ';
                        while (true)
                            {
                            i++;
                            if (part[i] == ';' && lc == ')')
                                {
                                string code = part.Substring(start, i - start).Replace("\r", "").Replace("\n", "").Replace("  ", " ");
                                bool foundFirstQuote = false;
                                string varname = "";
                                foreach (char c in code)
                                    {
                                    if (c == '"' && !foundFirstQuote)
                                        {
                                        foundFirstQuote = true;
                                        continue;
                                        }
                                    if (c == '"' && foundFirstQuote)
                                        {
                                        InitPersistData d = new InitPersistData();
                                        d.MName = varname;
                                        d.MClassName = className;
                                        d.MType = "TypeString";
                                        d.MGroup = currentgroup;
                                        _mLogger.Stage11Log(filename, d.ToString());
                                        _midata.Add(d);
                                        break;
                                        }
                                    if (foundFirstQuote)
                                        varname = varname + c;
                                    }
                                break;
                                }
                            if (part[i] != ' ')
                                lc = part[i];
                            }

                        #endregion
                        }
                    }
                }
            }

        private void ParseInitPersist1(string filename, string data)
            {
            data = data.Replace("Parent::initPersistFields();", "");

            string nocomments = removeComments(data);
            string part = nocomments;
                {
                part = part.Replace("void addProtectedField", "");
                part = part.Replace("::addProtectedField", "");

                part = part.Replace(@"addProtectedField(
      in_pFieldname,", "");

                part = part.Replace("void addField(c", "");
                part = part.Replace("me) addField(#fie", "");

                int expectedapf = (Regex.Split(part, "addProtectedField")).Count() - 1;
                // +(Regex.Split(part, "addField")).Count() - 1;
                int expectedaf = (Regex.Split(part, "addField")).Count() - 1;
                int foundapf = 0;
                int foundaf = 0;
                string classname = "";
                    {
                    int i = -1;
                    while (i < part.Length)
                        {
                        int start;
                        i++;

                        //void GuiControl::initPersistFields()
                        if (i + 5 < part.Length)
                            {
                            if (part.Substring(i, 5) == "void ")
                                {
                                int lcounterstart = i + 5;
                                bool readingname = false;
                                string tclassname = "";
                                while (true)
                                    {
                                    if (part[lcounterstart] != ' ' && !readingname)
                                        {
                                        readingname = true;
                                        tclassname += part[lcounterstart];
                                        }
                                    else if (part[lcounterstart] == ':' && readingname)
                                        {
                                        classname = tclassname;
                                        break;
                                        }
                                    else if (readingname)
                                        tclassname += part[lcounterstart];

                                    lcounterstart++;
                                    if (lcounterstart >= part.Length)
                                        break;
                                    if (lcounterstart > i + 50)
                                        break;
                                    }
                                }
                            }

                        #region AddProtectedField

                        if (part.Length >= i + "addProtectedField".Length)
                            {
                            if (part.Substring(i, "addProtectedField".Length) == "addProtectedField")
                                {
                                start = i;
                                char lc = ' ';
                                while (true)
                                    {
                                    i++;
                                    if (part[i] == ';' && lc == ')')

                                    #region

                                        {
                                        i++;
                                        string code = part.Substring(start, i - start).Replace("\r", "").Replace("\n", "").Replace("  ", " ");
                                        //addProtectedField( "stateToken", TYPEID< RenderPassStateToken >(), Offset( mStateToken, RenderPassStateBin ),   _setStateToken, _getStateToken );
                                        InitPersistData d = new InitPersistData();

                                        int readingpart = 0;
                                        bool isreadingname = false;
                                        bool isreadingtype = false;
                                        bool isreadingcomment = false;

                                        char lastnonspacechar = ' ';
                                        bool readall = false;

                                        foreach (char c in code)
                                            {
                                            #region

                                            switch (readingpart)
                                                {
                                                case 0:
                                                    if (c == '"' && !isreadingname)
                                                        isreadingname = true;
                                                    else if (c == '"' && isreadingname)
                                                        {
                                                        isreadingname = false;
                                                        readingpart = 1;
                                                        }
                                                    else if (isreadingname)
                                                        d.MName += c;
                                                    break;
                                                case 1:
                                                    if (c == ',' && !isreadingtype)
                                                        isreadingtype = true;
                                                    else if (c == ',' && isreadingtype)
                                                        {
                                                        isreadingtype = false;
                                                        readingpart = 2;
                                                        }
                                                    else if (isreadingtype)
                                                        d.MType += c;
                                                    break;
                                                case 2:
                                                    if (c == ',' && lastnonspacechar == ')')
                                                        {
                                                        readingpart = 3;
                                                        lastnonspacechar = ' ';
                                                        }
                                                    else if (d.MOffsetClass.Trim() == "NULL")
                                                        {
                                                        lastnonspacechar = ' ';
                                                        readingpart = 3;
                                                        }
                                                    else if (c == ';' && lastnonspacechar == ')')
                                                        {
                                                        readingpart = 4;
                                                        readall = true;
                                                        }
                                                    else
                                                        {
                                                        d.MOffsetClass += c;
                                                        if (c != ' ')
                                                            lastnonspacechar = c;
                                                        }
                                                    break;
                                                case 3:
                                                    if (!isreadingcomment && c == '"')
                                                        isreadingcomment = true;
                                                    else if (c == ';' && lastnonspacechar == ')')
                                                        {
                                                        readingpart = 4;
                                                        readall = true;
                                                        }

                                                    else if (isreadingcomment && (c == ';') && (lastnonspacechar == ')'))
                                                        {
                                                        readingpart = 4;
                                                        readall = true;
                                                        }
                                                    else if (isreadingcomment)
                                                        d.MComment += c;
                                                    break;
                                                case 4:
                                                    readall = true;
                                                    break;
                                                }

                                            #endregion

                                            if (c != ' ')
                                                lastnonspacechar = c;
                                            }

                                        if (readall)
                                            {
                                            d.MName = d.MName.Trim();
                                            d.MType = d.MType.Trim();
                                            d.MOffsetClass = d.MOffsetClass.Trim();
                                            d.MComment = d.MComment.Trim();
                                            if (d.MComment != "")
                                                {
                                                d.MComment = d.MComment.Trim().Replace("\"", "");
                                                d.MComment = d.MComment.Substring(0, d.MComment.LastIndexOf(')'));
                                                }
                                            d.MClassName = classname;
                                            _mLogger.Stage11Log(filename, d.ToString());
                                            _midata.Add(d);
                                            foundapf++;
                                            }
                                        break;
                                        }

                                    #endregion

                                    if (part[i] != ' ')
                                        lc = part[i];
                                    }
                                }
                            }

                        #endregion

                        #region addField

                        if (part.Length >= i + "addField".Length)
                            {
                            if (part.Substring(i, "addField".Length) == "addField")
                                {
                                start = i;
                                char lc = ' ';
                                while (true)
                                    {
                                    i++;
                                    if (part[i] == ';' && lc == ')')
                                        {
                                        #region

                                        i++;
                                        string code = part.Substring(start, i - start).Replace("\r", "").Replace("\n", "").Replace("  ", " ");
                                        InitPersistData d = new InitPersistData();

                                        int readingpart = 0;
                                        bool isreadingname = false;
                                        bool isreadingtype = false;

                                        char lastnonspacechar = ' ';
                                        bool readall = false;

                                        foreach (char c in code)
                                            {
                                            switch (readingpart)
                                                {
                                                case 0:
                                                    if (c == '"' && !isreadingname)
                                                        isreadingname = true;
                                                    else if (c == '"' && isreadingname)
                                                        {
                                                        isreadingname = false;
                                                        readingpart = 1;
                                                        }
                                                    else if (isreadingname)
                                                        d.MName += c;
                                                    break;
                                                case 1:
                                                    if (c == ',' && !isreadingtype)
                                                        isreadingtype = true;
                                                    else if (c == ',' && isreadingtype)
                                                        {
                                                        isreadingtype = false;
                                                        readingpart = 2;
                                                        }
                                                    else if (isreadingtype)
                                                        d.MType += c;
                                                    break;
                                                case 2:
                                                    if (c == '"')
                                                        readingpart = 3;
                                                    else if ((c == ';') && (lastnonspacechar == ')'))
                                                        {
                                                        readall = true;
                                                        readingpart = 4;
                                                        }
                                                    else
                                                        d.MOffsetClass += c;
                                                    break;
                                                case 3:
                                                    if ((c == ';') && (lastnonspacechar == ')'))
                                                        {
                                                        readingpart = 4;
                                                        readall = true;
                                                        }
                                                    else
                                                        {
                                                        //if (c != ' ')
                                                        //    lastnonspacechar = c;
                                                        d.MComment += c;
                                                        }
                                                    break;
                                                case 4:
                                                    readall = true;
                                                    break;
                                                }
                                            if (c != ' ')
                                                lastnonspacechar = c;
                                            }
                                        if (readall)
                                            {
                                            d.MName = d.MName.Trim();
                                            d.MType = d.MType.Trim();
                                            d.MOffsetClass = d.MOffsetClass.Trim();
                                            d.MComment = d.MComment.Trim();
                                            if (d.MComment != "")
                                                {
                                                d.MComment = d.MComment.Trim().Replace("\"", "");
                                                d.MComment = d.MComment.Substring(0, d.MComment.LastIndexOf(')'));
                                                }
                                            d.MClassName = classname;
                                            _mLogger.Stage11Log(filename, d.ToString());
                                            _midata.Add(d);
                                            foundaf++;
                                            }

                                        break;

                                        #endregion
                                        }

                                    if (part[i] != ' ')
                                        lc = part[i];
                                    }
                                }
                            }

                        #endregion
                        }
                    }
                    if (expectedapf != foundapf)
                        {
                        _mLogger.Stage11Log(filename, "********************************************************************************************************************");
                        _mLogger.Stage11Log(filename, "WARNING: Expected Protected Field InitPersists Does Not Match Discovered: " + expectedapf + " Found " + foundapf);
                        _mLogger.Stage11Log(filename, "********************************************************************************************************************");
                        }
                    if (expectedaf != foundaf)
                        {
                        _mLogger.Stage11Log(filename, "********************************************************************************************************************");
                        _mLogger.Stage11Log(filename, "WARNING: Expected Field InitPersists Does Not Match Discovered: " + expectedaf + " Found " + foundaf);
                        _mLogger.Stage11Log(filename, "********************************************************************************************************************");
                        }
                }
            }

        private void ParseConsoleTypes(string filename, string data)
            {
            MatchCollection matches = Regex.Matches(data, "DefineConsoleType *\\( *(?<Type>.*) *\\)");
            for (int i = 0; i < matches.Count; i++)
                {
                string body = matches[i].Groups["Type"].Value;
                if (body.Contains(","))
                    {
                    string consoletypename = body.Split(',')[0].Trim();
                    string mappedtype = body.Split(',')[1].Trim();
                    mappedtype = mappedtype.Replace("*", "");
                    if (!mConsoleTypes.ContainsKey(consoletypename))
                        {
                        _mLogger.Stage13Log(filename, "Found: '" + consoletypename + "' Mapped To '" + mappedtype + "'.");
                        mConsoleTypes.Add(consoletypename, mappedtype);
                        }
                    else if (mConsoleTypes[consoletypename] != mappedtype)
                        throw new Exception("Again?");
                    }
                else
                    throw new Exception("Really?");
                }
            }

        private int ParseEnumerations(string filename, string data, int depth = 0, string prevclassnames = "")
            {
            if (filename.ToLower().Contains("tspathshape"))

            
                Console.WriteLine("");
            foreach (string s in _mDntcConfig.PreGen_IgnoreSourceFilesForEnumeration)
                {
                if (filename.ToLower().Contains(s.ToLower()))
                    return 0;
                }

            int bracecount = 0;
            bool inquote = false;
            data = removeComments(data);
            int i = 0;
            bool firstbrace = false;
            string classOrStructureName = "";
            while (i < data.Length)
                {
                if (i > 0)
                    {
                    if (data[i] == '"')
                        {
                        if (data[i - 1] != '\\')
                            inquote = !inquote;
                        }
                    }

                if (inquote)
                    {
                    i++;
                    continue;
                    }

                if (i + "ImplementEnumType".Length < data.Length)
                    {
                    if (data.Substring(i, "ImplementEnumType".Length) == "ImplementEnumType")
                        {
                        #region ImplementEnumType

                        bool dumpout = true;
                        if (i != 0)
                            {
                            if ((data[i - 1] == ' ') || (data[i - 1] == '\n'))
                                dumpout = false;
                            }
                        else
                            dumpout = false;

                        if (!dumpout)
                            {
                            int start = i;
                            i = i + "ImplementEnumType".Length;
                            string enumname = "";
                            while (data[i] == ' ')
                                i++;
                            while (data[i] == '(')
                                i++;
                            while (data[i] == ' ')
                                i++;
                            while (data[i] != ',')
                                {
                                enumname += data[i];
                                i++;
                                }
                            while (data[i] != ';')
                                i++;

                            i++;
                            enumname = enumname.Trim();
                            enumname = "Type" + enumname.Trim();
                            string enumbody = data.Substring(start, i - start);
                            if (!_menumerations.ContainsKey(enumname))
                                {
                                _menumerations.Add(enumname, new EnumData("", enumname, enumbody, true));
                                _mLogger.Stage12Log(filename, "Found: '" + enumname + "'.");
                                _mLogger.NewEvent("", "BEGIN ENUM BODY");
                                _mLogger.NewEvent("", enumbody);
                                _mLogger.NewEvent("", "END ENUM BODY");
                                }
                            else
                                throw new Exception("REALLY?");
                            //{
                            //if (enumbody != _menumerations[enumname].mBody)
                            //    {
                            //    if (!_menumerations[enumname].IsScript)
                            //        {
                            //        _menumerations.Remove(enumname);
                            //        _menumerations.Add(enumname, new EnumData("", enumname, enumbody, true));
                            //        }
                            //    else
                            //        throw new Exception("DUPLICATE ENUMERATION FOUND WITH DIFFERENT BODIES?");
                            //    }

                            //}
                            }

                        #endregion
                        }
                    }

                if (i + "ImplementBitfieldType".Length < data.Length)
                    {
                    if (data.Substring(i, "ImplementBitfieldType".Length) == "ImplementBitfieldType")
                        {
                        #region ImplementEnumType

                        bool dumpout = true;
                        if (i != 0)
                            {
                            if ((data[i - 1] == ' ') || (data[i - 1] == '\n'))
                                dumpout = false;
                            }
                        else
                            dumpout = false;

                        if (!dumpout)
                            {
                            int start = i;
                            i = i + "ImplementBitfieldType".Length;
                            string enumname = "";
                            while (data[i] == ' ')
                                i++;
                            while (data[i] == '(')
                                i++;
                            while (data[i] == ' ')
                                i++;
                            while (data[i] != ',')
                                {
                                enumname += data[i];
                                i++;
                                }
                            while (data[i] != ';')
                                i++;

                            i++;
                            enumname = enumname.Trim();
                            enumname = "Type" + enumname.Trim();
                            string enumbody = data.Substring(start, i - start);
                            if (!_menumerations.ContainsKey(enumname))
                                {
                                _menumerations.Add(enumname, new EnumData("", enumname, enumbody, true));
                                _mLogger.Stage12Log(filename, "Found: '" + enumname + "'.");
                                _mLogger.NewEvent("", "BEGIN ENUM BODY");
                                _mLogger.NewEvent("", enumbody);
                                _mLogger.NewEvent("", "END ENUM BODY");
                                }
                            else
                                throw new Exception("REALLY?");
                            }

                        #endregion
                        }
                    }

                #region "Standard c++ enum"

                if (i + "enum ".Length < data.Length)
                    {
                    #region k

                    bool parseme = false;

                    if (data.Substring(i, "enum ".Length) == "enum ")
                        parseme = true;
                    else if (data.Substring(i, "enum".Length) == "enum")
                        {
                        for (int z = i + "enum".Length; z < data.Length; z++)
                            {
                            if (data[z] == ' ' || data[z] == '\r' || data[z] == '\t' || data[z] == '\n')
                                continue;
                            if (data[z] == '{')
                                parseme = true;
                            break;
                            }
                        }

                    if (parseme)
                        {
                        bool dumpout = true;
                        if (i != 0)
                            {
                            if ((data[i - 1] == ' ') || (data[i - 1] == '\n') || (data[i - 1] == '\t'))
                                dumpout = false;
                            }
                        else
                            dumpout = false;

                        if (!dumpout)
                            {
                            string enumname = "";
                            string enumbody = "";
                            i += "enum".Length;

                            while (data[i] == ' ')
                                i++;

                            while (data[i] != '{')
                                {
                                enumname += data[i];
                                i++;
                                if (i == data.Length)
                                    return i;
                                }

                            // if (enumname.Trim() != "")
                            while (true)
                                {
                                if (data[i] != '}')
                                    {
                                    enumbody += data[i];
                                    i++;
                                    continue;
                                    }

                                enumbody += data[i] + "};".Replace("\r", "");
                                enumbody += data[i] + "};".Replace("\n", "");

                                while (enumbody.Contains("  "))
                                    enumbody = enumbody.Replace("  ", " ");

                                enumname = prevclassnames + "::" + enumname.Trim().Replace("\r\n", "");

                                enumname = enumname.Replace("::::", "::");

                                if (enumname.StartsWith("::"))
                                    enumname = enumname.Substring(2);

                                enumname = ("R::" + enumname).Replace("\r", "");
                                enumname = enumname.Replace("\n", "");

                                if (!_menumerations.ContainsKey(enumname))
                                    _menumerations.Add(enumname, new EnumData(prevclassnames, enumname, enumbody, false));

                                i++;
                                break;
                                }
                            }
                        }

                    #endregion
                    }

                #endregion

                #region "namespace, class, struct"

                int skip = 0;
                if (i + "namespace ".Length < data.Length)
                    {
                    if (data.Substring(i, "namespace ".Length) == "namespace ")
                        skip = "namespace ".Length;
                    }
                if (i + "class ".Length < data.Length)
                    {
                    if (data.Substring(i, "class ".Length) == "class ")
                        skip = "class ".Length;
                    }
                if (i + "struct ".Length < data.Length)
                    {
                    if (data.Substring(i, "struct ".Length) == "struct ")
                        skip = "struct ".Length;
                    }

                if (skip > 0)
                    {
                    int start = i;

                    if (skip > 0)
                        {
                        if (i - "friend ".Length >= 0)
                            {
                            if (data.Substring(i - "friend ".Length, "friend class ".Length) == "friend class ")
                                {
                                i++;
                                continue;
                                }
                            }

                        if (i - "template< typename T > ".Length >= 0)
                            {
                            if (data.Substring(i - "template< typename T > ".Length, "template< typename T > ".Length) == "template< typename T > ")
                                {
                                i++;
                                continue;
                                }
                            }

                        if (i - "typedef ".Length >= 0)
                            {
                            if (data.Substring(i - "typedef ".Length, "typedef struct ".Length) == "typedef struct ")
                                {
                                i++;
                                continue;
                                }
                            }

                        bool foundfirstchar = false;
                        int spos = i + skip;
                        string tname = "";
                        while (spos < data.Length)
                            {
                            if (data[spos] != ' ' && !foundfirstchar)
                                {
                                foundfirstchar = true;
                                tname += data[spos];
                                }
                            else if ((data[spos] == ':' || data[spos] == ';' || data[spos] == ' ' || data[spos] == '{') && foundfirstchar)
                                {
                                //ignore empty classes
                                if (data[spos] == ';')
                                    break;

                                if (tname.Contains("#"))
                                    {
                                    //spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains(">"))
                                    {
                                    ///spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains("<"))
                                    {
                                    // spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains(","))
                                    {
                                    //spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains("{"))
                                    {
                                    //spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains("}"))
                                    {
                                    //spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains(")"))
                                    {
                                    //spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains("("))
                                    {
                                    // spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains(" "))
                                    {
                                    //spos += tname.Length;
                                    break;
                                    }
                                if (tname.Contains("="))
                                    {
                                    // spos += tname.Length;
                                    break;
                                    }
                                if (tname.Trim() == "")
                                    {
                                    // spos += tname.Length;
                                    break;
                                    }
                                if (tname.Trim() == "T")
                                    {
                                    // spos += tname.Length;
                                    break;
                                    }
                                if (tname.Trim() == "\t")
                                    {
                                    // spos += tname.Length;
                                    break;
                                    }

                                classOrStructureName = tname.Replace("\r\n", "");

                                string gg = prevclassnames != "" ? prevclassnames + "::" + tname.Replace("\r\n", "") : tname.Replace("\r\n", "");
                                if (!classes.Contains(gg))
                                    classes.Add(gg);

                                int oldspos = spos;

                                spos = start + (oldspos - start) + ParseEnumerations(filename, data.Substring(spos), depth + 1, (prevclassnames != "" ? prevclassnames + "::" + classOrStructureName : classOrStructureName));

                                break;
                                }
                            else if (foundfirstchar)
                                tname += data[spos];
                            spos++;
                            }
                        i = spos;
                        }
                    }

                #endregion

                if (data.Length <= i)
                    break;
                    {
                    if (data[i] == '{')
                        {
                        firstbrace = true;
                        bracecount++;
                        }
                    else if (data[i] == '}')
                        bracecount--;

                    if (firstbrace)
                        {
                        if (bracecount == 0)
                            {
                            if (depth != 0)
                                return i + 1;
                            }
                        }
                    }

                    i++;
                }
            return i;
            }

        public string removeComments(string data)
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

        private bool Stringhasnonalphanumeric(string str)
            {
            if (string.IsNullOrEmpty(str))
                return false;

            return str.All(t => (char.IsLetter(t)) || ((char.IsNumber(t))));
            }

        public void DetermineParent(string filename, string data)
            {
            for (int i = 0; i < data.Length; i++)
                {
                if (i + 6 < data.Length)
                    {
                    if (data[i] == '\n')
                        {
                        bool docheck = false;
                        int offset = i;
                        int soffset = 0;
                        int start = 0;
                        if (data.Substring(i + 1, 6) == "class ")
                            {
                            offset += 6;
                            soffset = 6;
                            docheck = true;
                            start = i;
                            }
                        else if (i + 7 < data.Length)
                            {
                            if (data.Substring(i + 1, 7) == "struct ")
                                {
                                offset += 7;
                                soffset = 7;
                                docheck = true;
                                start = i;
                                }
                            }

                        if (docheck)
                            {
                            bool gotclassname = false;
                            bool gotparent = false;
                            string classname = string.Empty;
                            string parent = string.Empty;

                            while (!gotclassname)
                                {
                                if (data[offset] != ':' && data[offset] != '{' && data[offset] != ';')
                                    classname += data[offset];
                                else if (data[offset] == ':')
                                    gotclassname = true;
                                else
                                    break;
                                offset++;
                                }

                            if (gotclassname)
                                {
                                while (data[offset] != '{')
                                    offset++;
                                offset++;
                                int openparens = 1;
                                while (!gotparent)
                                    {
                                    if (offset + 7 == data.Length)
                                        break;
                                    if (data.Substring(offset, 7) == "typedef")
                                        {
                                        offset = offset + 7;
                                        bool finishedparent = false;
                                        bool foundfirstchar = false;
                                        // while (!finishedparent)
                                        //   {
                                        while (!foundfirstchar)
                                            {
                                            if (data[offset] != ' ')
                                                foundfirstchar = true;
                                            else
                                                offset++;
                                            }
                                        while (data[offset] != ';')
                                            {
                                            parent += data[offset];
                                            offset++;
                                            }
                                        if (parent.EndsWith("Parent"))
                                            {
                                            parent = parent.Substring(0, parent.IndexOf("Parent", StringComparison.Ordinal));
                                            gotparent = true;
                                            }
                                        else
                                            parent = "";

                                        // }
                                        }
                                    else
                                        {
                                        if (data[offset] == '{')
                                            openparens++;
                                        if (data[offset] == '}')
                                            openparens--;
                                        offset++;
                                        }
                                    if (openparens == 0)
                                        break;
                                    }
                                classname = classname.Trim();
                                parent = parent.Trim();
                                if (((classname.Trim().ToLower() == "simobject") || (parent.Trim() != "")) && Stringhasnonalphanumeric(parent.Trim()))
                                    {
                                    if (_mDntcConfig.ObjectParent.ContainsKey(classname))
                                        {
                                        if (_mDntcConfig.ObjectParent[classname] != parent)
                                            throw new Exception("problem here.");
                                        }
                                    else
                                        {
                                        _mDntcConfig.ObjectParent.Add(classname, parent);
                                        _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), "Parent Object Defined", "Parent to '" + classname + "' is C++ Class: '" + parent + "'", filename, 0);
                                        }
                                    i = offset;
                                    }
                                else
                                    _mLogger.Stage1Log(MethodBase.GetCurrentMethod(), "Parent Object NOT Defined", "Parent to '" + classname + "' is C++ Class: 'NOT FOUND'", filename, 0);
                                }
                            }
                        }
                    }
                }
            }

        private bool isAllowedFile(string filename)
            {
            foreach (string c in _mDntcConfig.PreGen_IgnoreSourceFiles)
                {
                if (filename.ToLower().Contains(c))
                    return false;
                }
            return true;
            }

        private void _ParseDefinedClasses(string filename, string data)
            {
            //_DefinedClasses
            data = removeComments(data);
            Match match = Regex.Match(data, "typedef[ \\t]*(?<SourceType>[A-Za-z0-9_:]*)[ \\t]*(?<TargetType>[A-Za-z0-9_]*);");
            while (match.Success)
                {
                _DefinedClasses.Add(new DefinedValues(match.Groups["SourceType"].Value, match.Groups["TargetType"].Value));
                match = match.NextMatch();
                }
            }

        public void ReadCFiles(List<string> mfilepaths, ref ConfigFiles dntconfig, ref Logger.Logger logger)
            {
            _mDntcConfig = dntconfig;
            _mLogger = logger;
            _mData = new List<Externdata>();
            _midata = new List<InitPersistData>();
            mConsoleTypes = new Dictionary<string, string>();
            mfilepaths.Sort();

            double TotalFiles = mfilepaths.Count;
            double currentpos = 0;

            foreach (string filename in mfilepaths)
                {
                currentpos = currentpos + 1.0;
                logger.onProgressSubChange(currentpos / TotalFiles, filename);
                if (isAllowedFile(filename))
                    {
                    _mLogger.SubSectionStart("Interrogating C++");
                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing C++ in file '" + filename + "'.");
                    TextReader sr = new StreamReader(filename);
                    string data = sr.ReadToEnd();
                    data = removeComments(data);
                    sr.Close();
                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing ConsoleFunctions.");
                    ParseConsoleFunctions(filename, data);
                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing ConsoleMethods.");
                    ParseConsoleMethod(filename, data);
                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing DefineConsoleMethod.");
                    ParseDefineConsoleMethod(filename, data);
                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing DefineConsoleFunction.");
                    ParseDefineConsoleFunction(filename, data);
                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing DefineEngineFunction.");
                    ParseDefineEngineFunction(filename, data);
                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing DefineEngineMethod.");
                    ParseDefineEngineMethod(filename, data);

                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Processing DefineTSShapeConstructorMethod.");
                    DefineTSShapeConstructorMethod(filename, data);

                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Determining Object Parent Relationships.");
                    DetermineParent(filename, data);

                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Determining Object InitPersists.");
                    ParseInitPersist(filename, data);

                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Determining Object Console Types.");
                    ParseConsoleTypes(filename, data);

                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Determining Object dependant Enumerations.");

                    ParseEnumerations(filename, data);

                    _mLogger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, filename, "         Parsing define's classes.");
                    _ParseDefinedClasses(filename, data);

                    ParseIMPLEMENT_CALLBACK(filename, data);
                    _mLogger.SubSectionEnd();
                    }
                }
            }
        }
    }