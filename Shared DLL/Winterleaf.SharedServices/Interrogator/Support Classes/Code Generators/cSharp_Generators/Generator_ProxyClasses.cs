using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using Winterleaf.SharedServices.Interrogator.Configuration;
using Winterleaf.SharedServices.Interrogator.Containers;
using Winterleaf.SharedServices.Interrogator.Parsing;

namespace Winterleaf.SharedServices.Interrogator.cSharp_Generators
{
    internal class Generator_ProxyClasses
    {
        private readonly string _mcSharpNamespace;
        private readonly string _mcSharpUserLocation;
        private readonly CodeParsing mCP;
        private readonly Logger.Logger mLogger;
        private ConfigFiles MDntcConfig;
        private string mCSharpProjectFolder;

        public Generator_ProxyClasses(string CSharpProjectFolder, ref Logger.Logger logger, ref ConfigFiles cf, ref CodeParsing cp, string cSharpUserLocation, string cSharpNamespace)
        {
            mCSharpProjectFolder = CSharpProjectFolder;
            mLogger = logger;
            MDntcConfig = cf;
            mCP = cp;
            _mcSharpUserLocation = cSharpUserLocation;
            _mcSharpNamespace = cSharpNamespace;
        }

        private string FindEnumValue(string classname, string MElementCount)
        {
            string tv = MElementCount;
            foreach (EnumData ed in mCP.Data_Enumerations.Values)
                {
                if (ed.mClass != classname)
                    continue;

                if (!ed.mBody.Contains(" " + MElementCount + " "))
                    continue;
                string nocomments = Helpers.removeComments(ed.mBody);
                tv = "";
                for (int i = MElementCount.Length + nocomments.IndexOf(MElementCount, StringComparison.Ordinal); i < nocomments.Length; i++)
                    {
                    if (nocomments[i] != ',' && nocomments[i] != '}' && nocomments[i] != ' ' && nocomments[i] != '=')
                        tv += nocomments[i];
                    else if (nocomments[i] == ',' || nocomments[i] == '}')
                        break;
                    }
                if (tv.Trim() == "")
                    continue;
                tv = tv.Trim();
                break;
                }

            if (!tv.Replace("+", "").Replace("-", "").Replace("*", "").Replace("\\", "").IsNumeric())
                {
                List<string> parts = new List<string>();
                string part = "";

                for (int i = 0; i < tv.Length; i++)
                    {
                    if (tv[i] == '+' || tv[i] == '-' || tv[i] == '*' || tv[i] == '/')
                        {
                        parts.Add(part);
                        parts.Add(tv[i] + "");
                        part = "";
                        }
                    else
                        part = part + tv[i];
                    }
                parts.Add(part);
                for (int i = 0; i < parts.Count; i++)
                    {
                    if (parts[i] == "+" || parts[i] == "-" || parts[i] == "*" || parts[i] == "/")
                        {
                        }
                    else
                        parts[i] = FindEnumValue(classname, parts[i]);
                    }
                tv = string.Join<string>("", parts);
                }

            return tv;
        }

        public string tsc_GenerateWork(String classname)
        {
            mLogger.SubSectionStart("Generating CSharp class code");
            mLogger.NewEvent("", "Generating CSharp Class object for T3D class '" + classname + "'.");
            StringBuilder Code = new StringBuilder();

            StringBuilder _constructor = new StringBuilder();
            _constructor.Append("public " + classname + @"_Base (){");

            Code.Append(@"


#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinterLeaf.Engine;
using WinterLeaf.Engine.Classes;
using WinterLeaf.Engine.Containers;
using WinterLeaf.Engine.Enums;
using System.ComponentModel;
using System.Threading;
using  WinterLeaf.Engine.Classes.Interopt;
using WinterLeaf.Engine.Classes.Decorations;
using WinterLeaf.Engine.Classes.Extensions;
using WinterLeaf.Engine.Classes.Helpers;
using " + _mcSharpNamespace + "." + Interrogator.constants.namespaces.userObjects_ProxyObjects + @";
#endregion

namespace " + _mcSharpNamespace + "." + Interrogator.constants.namespaces.ProxyObjects_Base + @"
    {
    /// <summary>
    /// 
    /// </summary>
    [TypeConverter(typeof(TypeConverterGeneric<" + Interrogator.constants.general.classPrefix + classname + @"_Base>))]
    public partial class " + Interrogator.constants.general.classPrefix);
            Code.Append(classname + "_Base");
            if (classname.ToLower().Trim() == "simobject")
                Code.Append(": ModelBase\r\n{\r\n");
            else
                Code.Append(": " + Interrogator.constants.general.classPrefix + MDntcConfig.ObjectParent[classname] + "\r\n{\r\n");

            //            if (1 == 0)

            #region Operator Overides

            Code.Append(@"
#region ProxyObjects Operator Overrides
        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <param name=""simobjectid""></param>
        /// <returns></returns>
        public static bool operator ==(" + Interrogator.constants.general.classPrefix + classname + @"_Base ts, string simobjectid)
            {
            return object.ReferenceEquals(ts, null) ? object.ReferenceEquals(simobjectid, null) : ts.Equals(simobjectid);
            }
  /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
            {
            return base.GetHashCode();
            }
  /// <summary>
        /// 
        /// </summary>
        /// <param name=""obj""></param>
        /// <returns></returns>
        public override bool Equals(object obj)
            {
            
            return (this._ID ==(string)myReflections.ChangeType( obj,typeof(string)));
            }
        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <param name=""simobjectid""></param>
        /// <returns></returns>
        public static bool operator !=(" + Interrogator.constants.general.classPrefix + classname + @"_Base ts, string simobjectid)
            {
            if (object.ReferenceEquals(ts, null))
                return !object.ReferenceEquals(simobjectid, null);
            return !ts.Equals(simobjectid);

            }


            /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator string( " + Interrogator.constants.general.classPrefix + classname + @"_Base ts)
            {
            if (object.ReferenceEquals(ts, null))
                 return ""0"";
            return ts._ID;
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator " + Interrogator.constants.general.classPrefix + classname + @"_Base(string ts)
            {
            uint simobjectid = resolveobject(ts);
           return  (" + Interrogator.constants.general.classPrefix + classname + @"_Base) Omni.self.getSimObject(simobjectid,typeof(" + Interrogator.constants.general.classPrefix + classname + @"_Base));
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator int( " + Interrogator.constants.general.classPrefix + classname + @"_Base ts)
            {
            return (int)ts._iID;
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""simobjectid""></param>
        /// <returns></returns>
        public static implicit operator " + Interrogator.constants.general.classPrefix + classname + @"_Base(int simobjectid)
            {
            return  (" + Interrogator.constants.general.classPrefix + classname + @") Omni.self.getSimObject((uint)simobjectid,typeof(" + Interrogator.constants.general.classPrefix + classname + @"_Base));
            }


        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator uint( " + Interrogator.constants.general.classPrefix + classname + @"_Base ts)
            {
            return ts._iID;
            }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static implicit operator " + Interrogator.constants.general.classPrefix + classname + @"_Base(uint simobjectid)
            {
            return  (" + Interrogator.constants.general.classPrefix + classname + @"_Base) Omni.self.getSimObject(simobjectid,typeof(" + Interrogator.constants.general.classPrefix + classname + @"_Base));
            }
#endregion
");
            Code.Append(@"#region Init Persists
");

            #endregion

            foreach (InitPersistData ipd in mCP.Data_InitPersists)
                {

                if (ipd.MClassName != classname)
                    continue;

                if (classname.ToLower()=="posteffect" && ipd.MName.Trim() == "isEnabled")
                    Console.WriteLine("");

                if (ipd.MElementCount.Trim() != "")
                    {
                    if (!ipd.MElementCount.IsNumeric())
                        {
                        bool set = false;
                        foreach (DefinedValues dv in mCP.Data_Defined)
                            {
                            if (dv._name.ToLower() == ipd.MElementCount.ToLower())
                                {
                                ipd.MElementCount = dv._value;
                                set = true;
                                }
                            }

                        if (ipd.MElementCount == "PDC_NUM_KEYS")
                            Console.WriteLine("");

                        if (!ipd.MElementCount.IsNumeric())
                            {
                            List<string> classparents = new List<string>();

                            string mclass = classname;

                            while (mclass != "")
                                {
                                classparents.Add(mclass);
                                if (MDntcConfig.ObjectParent.ContainsKey(mclass))
                                    mclass = MDntcConfig.ObjectParent[mclass];
                                else
                                    mclass = "";
                                }

                            foreach (EnumData ed in mCP.Data_Enumerations.Values)
                                {
                                if (!classparents.Contains(ed.mClass))
                                    continue;

                                //if (ed.mClass != classname)
                                //    continue;
                                if (ed.mBody.Contains(" " + ipd.MElementCount + " "))
                                    {
                                    string nocomments = Helpers.removeComments(ed.mBody);
                                    string tv = "";
                                    for (int i = ipd.MElementCount.Length + nocomments.IndexOf(ipd.MElementCount, StringComparison.Ordinal); i < nocomments.Length; i++)
                                        {
                                        if (nocomments[i] != ',' && nocomments[i] != '}' && nocomments[i] != ' ' && nocomments[i] != '=')
                                            tv += nocomments[i];
                                        else if (nocomments[i] == ',' || nocomments[i] == '}')
                                            break;
                                        }
                                    if (tv.Trim() != "")
                                        {
                                        tv = tv.Trim();
                                        set = true;
                                        if (!tv.IsNumeric())
                                            tv = FindEnumValue(classname, tv);

                                        ipd.MElementCount = tv.Trim();
                                        break;
                                        }
                                    }
                                }
                            if (!set)
                                throw new Exception("OPPS COULDNOT RESOLVE ARRAY SIZE PARAM (" + ipd.MElementCount + ")");
                            }
                        }
                    }
                else
                    ipd.MElementCount = "1";

                string safename = ipd.MName.Trim();

                safename = Helpers.GiveMeSafeName(safename);

                foreach (ImplementCallback ic in
                    mCP.Data_ImplementCallback.Where(ic => ic.mClassname.ToLower() == classname.ToLower()))
                    {
                    if (safename != ic.mFunction)
                        continue;
                    safename += "x";
                    break;
                    }

                bool isScript = false;

                if (ipd.MElementCount == "1")
                    {
                    if (ipd.mStructureType == InitPersistData.StructureType.TypeEnumeration)
                        {
                        #region Enumeration

                        mLogger.NewEvent("", "     Adding Init Persist (ENUMERATION) for '" + ipd.MName + "' typeof '" + ipd.mCSharpType + "'.");

                        Code.Append("/// <summary>\r\n");
                        Code.Append("/// " + ipd.MComment.Replace("<", "").Replace(">", "").Replace("&", "and") + "\r\n");
                        Code.Append("/// </summary>\r\n");

                        //todo Blowing Up here w/ __
                        Code.Append("[MemberGroup(\"" + ipd.MGroup.Trim() + "\")]\r\n");
                        if (mCP.Data_Enumerations[ipd.mCSharpType.Replace("__", "::")].IsScript)
                            {
                            isScript = true;
                            Code.Append("public " + ipd.mCSharpType + " " + safename + "\r\n");
                            Code.Append("       {\r\n");
                            Code.Append("       get\r\n");
                            Code.Append("          {");
                            Code.Append("          return (" + ipd.mCSharpType + ") Omni.self.GetVar(_ID + \"." + ipd.MName + "\");\r\n");
                            Code.Append("          }\r\n");
                            Code.Append("       set\r\n");
                            Code.Append("          {\r\n");
                            Code.Append("          Omni.self.SetVar(_ID + \"." + ipd.MName + "\", value.ToString());\r\n");
                            Code.Append("          }\r\n");
                            Code.Append("       }\r\n");
                            }
                        else
                            {
                            if (ipd.mCSharpType.StartsWith("R::"))
                                Console.WriteLine("");

                            Code.Append("public " + ipd.mCSharpType + " " + safename + "\r\n");
                            Code.Append("       {\r\n");
                            Code.Append("       get\r\n");
                            Code.Append("          {");
                            Code.Append("          return (" + ipd.mCSharpType + ")Enum.Parse(typeof(" + ipd.mCSharpType + "), Omni.self.GetVar(_ID + \"." + ipd.MName + "\"));\r\n");
                            Code.Append("          }\r\n");
                            Code.Append("       set\r\n");
                            Code.Append("          {\r\n");
                            Code.Append("          Omni.self.SetVar(_ID + \"." + ipd.MName + "\", value.ToString());\r\n");
                            Code.Append("          }\r\n");
                            Code.Append("       }\r\n");
                            }

                        #endregion
                        }
                    else if (ipd.mStructureType == InitPersistData.StructureType.TypeVariable)
                        {
                        //todo changed here
                        //if (ipd.mCSharpType.StartsWith("Type"))
                        //ipd.mCSharpType = ipd.mCSharpType.Substring("Type".Length);

                        #region Type Variable

                        mLogger.NewEvent("", "     Adding Init Persist (SYSTEM TYPE) for '" + ipd.MName + "' typeof '" + ipd.mCSharpType + "'.");
                        Code.Append("/// <summary>\r\n");
                        Code.Append("/// " + ipd.MComment.Replace("<", "").Replace(">", "").Replace("&", "and") + "\r\n");
                        Code.Append("/// </summary>\r\n");
                        Code.Append("[MemberGroup(\"" + ipd.MGroup.Trim() + "\")]\r\n");
                        Code.Append("public " + ipd.mCSharpType + " " + safename + "\r\n");
                        Code.Append("       {\r\n");
                        Code.Append("       get\r\n");
                        Code.Append("          {\r\n");
                        Code.Append("          return Omni.self.GetVar(_ID + \"." + ipd.MName + "\").As" + Helpers.UppercaseFirst(ipd.mCSharpType) + "();\r\n");
                        Code.Append("          }\r\n");
                        Code.Append("       set\r\n");
                        Code.Append("          {\r\n");
                        Code.Append("          Omni.self.SetVar(_ID + \"." + ipd.MName + "\", value.AsString());\r\n");
                        Code.Append("          }\r\n");
                        Code.Append("       }\r\n");

                        #endregion
                        }
                    else if (ipd.mStructureType == InitPersistData.StructureType.T3DObject)
                        {
                        #region T3DObject

                        mLogger.NewEvent("", "     Adding Init Persist (T3D OBJECT) for '" + ipd.MName + "' typeof '" + ipd.mCSharpType + "'.");
                        Code.Append("/// <summary>\r\n");
                        Code.Append("/// " + ipd.MComment.Replace("<", "").Replace(">", "").Replace("&", "and") + "\r\n");
                        Code.Append("/// </summary>\r\n");
                        Code.Append("[MemberGroup(\"" + ipd.MGroup.Trim() + "\")]\r\n");
                        Code.Append("public " + ipd.mCSharpType + " " + safename + "\r\n");
                        Code.Append("       {\r\n");
                        Code.Append("       get\r\n");
                        Code.Append("          {\r\n");
                        Code.Append("          return Omni.self.GetVar(_ID + \"." + ipd.MName + "\");\r\n");
                        Code.Append("          }\r\n");
                        Code.Append("       set\r\n");
                        Code.Append("          {\r\n");
                        Code.Append("          Omni.self.SetVar(_ID + \"." + ipd.MName + "\", value.ToString());\r\n");
                        Code.Append("          }\r\n");
                        Code.Append("       }\r\n");

                        #endregion
                        }
                    }
                else
                    {
                    mLogger.NewEvent("", "     Adding Init Persist (ENUMERATION) for '" + ipd.MName + "' typeof '" + ipd.mCSharpType + "'.");
                    Code.Append("[MemberGroup(\"" + ipd.MGroup.Trim() + "\")]\r\n");
                    Code.Append("public arrayObject<" + ipd.mCSharpType + "> " + safename + ";\r\n");
                    //+ " = new arrayObject<" + ipd.mCSharpType + ">(" + ipd.MElementCount + ",\"" +
                    //safename + "\",\"" + ipd.mStructureType + "\"," + isScript.ToString().ToLower() + ");    \r\n");
                    //internal arrayObject<float> test=new arrayObject<float>(5,"test");

                    _constructor.Append(safename + " = new arrayObject<" + ipd.mCSharpType + ">(" + ipd.MElementCount + ",\"" + safename + "\",\"" + ipd.mStructureType + "\"," + isScript.ToString().ToLower() + ",this);    \r\n");
                    }
                }
            Code.Append("\r\n#endregion\r\n#region Member Functions\r\n");

            foreach (Externdata externdata in
                mCP.Data_Data.Where(externdata => externdata.m_objecttype.ToLower() == classname.ToLower()))
                Code.Append(writeCallWrapperForTsObject(externdata, classname, false));
            Code.Append("\r\n#endregion\r\n#region T3D Callbacks\r\n");
            foreach (ImplementCallback ic in
                mCP.Data_ImplementCallback.Where(ic => ic.mClassname.ToLower() == classname.ToLower()))
                Code.Append(WriteImplementCallBack(ic));
            Code.Append("\r\n#endregion\r\n");

            _constructor.Append("}\r\n");
            Code.Append(_constructor);

            Code.Append("}}");
            mLogger.SubSectionEnd();
            return Code.ToString();
        }

        internal string WriteImplementCallBack(ImplementCallback ic)
        {
            string result = @"
        /// <summary>
        /// " + ic.mComments.Replace("\r\n", "") + @"
        /// </summary>
        [ConsoleInteraction(true)]
";
            result += "public virtual ";
            string t = "";
            if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ic.mReturnType.Replace("*", "")))
                t = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ic.mReturnType.Replace("*", "")].cstype;
            else
                {
                t = Helpers.convertC2Cs(ic.mReturnType, false, ref MDntcConfig);
                if (t == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                    t = "string";
                }

            //result += " " + t + " event_" + ic.mFunction + "(";
            result += " " + t + " " + ic.mFunction + "(";
            string[] parameters;
            int c = 0;
            parameters = ic.mParams.Trim().ToLower() == "void" ? new string[0] : ic.mParams.Split(',');
            foreach (string p in parameters)
                {
                if (p.Trim().ToLower() == "void")
                    continue;

                //here is is
                // this is where I want to do it.
                string parameter = p;
                if (parameter.Trim().Length <= 0)
                    continue;

                if (c > 0)
                    {
                    if (!result.Trim().EndsWith(","))
                        result += ", ";
                    }
                parameter = Helpers.getridofdoublespace(parameter);
                int i = parameter.Trim().LastIndexOf(' ');
                string ptypeo = parameter.Substring(0, i).Trim();
                string ptype = ptypeo;
                string pname = parameter.Substring(i).Trim();
                pname = Helpers.GiveMeSafeName(pname);

                string tt = ptype;

                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ptype.Replace("*", "")))
                    tt = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ptype.Replace("*", "")].cstype;
                else
                    {
                    bool found = false;
                    if (ptype.Contains("*"))
                        {
                        string tmp = ptype.Replace("*", "");

                        if (MDntcConfig.ObjectParent.ContainsKey(tmp))
                            {
                            tt = Interrogator.constants.general.classPrefix + tmp;
                            found = true;
                            }
                        else
                            {
                            if (MDntcConfig.ObjectParent.ContainsValue(tmp))
                                {
                                tt = Interrogator.constants.general.classPrefix + tmp;
                                found = true;
                                }
                            }
                        }
                    if (!found)
                        {
                        tt = Helpers.convertC2Cs(ptype, false, ref MDntcConfig);
                        if (tt == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                            tt = "string";
                        }
                    }

                result += tt + " " + pname;

                c++;
                }
            result += "){";
            if (t != "void")
                result += @"return ""0"".As" + Helpers.UppercaseFirst(t) + "();";
            result += "}\r\n";

            return result;
        }

        private List<string> ParseParams(string sparams)
        {
            List<string> mytypes = new List<string>();
            string[] parameters = sparams.Trim().ToLower() == "void" ? new string[0] : sparams.Split(',');

            foreach (string p in parameters)
                {
                if (p.Trim().ToLower() == "void")
                    continue;
                if (p.Trim().Length <= 0)
                    continue;
                string parameter = Helpers.getridofdoublespace(p);
                int i = parameter.Trim().LastIndexOf(' ');
                string ptypeo = parameter.Substring(0, i).Trim();
                string ptype = ptypeo;
                string pname = parameter.Substring(i).Trim();

                pname = Helpers.GiveMeSafeName(pname);

                string tt = ptype;
                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ptype.Replace("*", "")))
                    tt = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ptype.Replace("*", "")].cstype;
                else
                    {
                    tt = Helpers.convertC2Cs(ptype, false, ref MDntcConfig);
                    if (tt == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                        tt = "string";
                    }

                mytypes.Add(tt);
                }

            return (mytypes);
        }

        private bool doParamsMatch(string params1, string params2)
        {
            List<string> parseparams1 = ParseParams(params1);
            List<string> parseparams2 = ParseParams(params2);
            if (parseparams1.Count != parseparams2.Count)
                return false;
            return parseparams1.All(t => t == t);
        }

        private bool DoesAParentContainFunction(string tsclass, string function, string sparams)
        {
            if (function.StartsWith("fn_"))
                function = function.Substring(3);

            if (MDntcConfig.ObjectParent.ContainsKey(tsclass))
                {
                string parent = MDntcConfig.ObjectParent[tsclass];

                if (parent.Trim() != "")
                    {
                    foreach (Externdata ed in mCP.Data_Data)
                        {
                        if (ed.m_objecttype.ToLower().Trim() == parent.ToLower().Trim())
                            {
                            string tname = ed.m_name;
                            if (tname.StartsWith("fn_"))
                                tname = tname.Substring(3);
                            if (tname.Substring((tname.IndexOf("_", StringComparison.Ordinal) + 1)) == function)
                                {
                                if (doParamsMatch(ed.m_params, sparams))
                                    return true;
                                }
                            }
                        }

                    return DoesAParentContainFunction(parent, function, sparams);
                    }
                }

            return false;
        }

        internal string writeCallWrapperForTsObject(Externdata ed, string classname, bool isnew)
        {
            if (ed.m_name == "fnsetNetPort")
                Console.WriteLine("");
            string result = "";
            string codeInsert = "\r\n";
            if (ed.m_helptext.Trim().Length != 0)
                {
                result += "/// <summary>\r\n";
                string[] line = ed.m_helptext.Split('\r');
                result = line.Aggregate(result, (current, s) => current + ("/// " + s.Replace("\n", "").Replace("\\n", "").Replace("&lt;", "<").Replace("&gt;", ">")).Replace("<", "").Replace("&rt;", "") + "\r\n");
                result += "/// </summary>\r\n";
                }
            result += "[MemberFunctionConsoleInteraction(true)]\r\n";

            string tname = ed.m_name;
            if (tname.StartsWith("fn_"))
                tname = tname.Substring(3);

            string callshort = tname.Substring(tname.IndexOf("_", StringComparison.Ordinal) + 1);

            foreach (InitPersistData ipd in mCP.Data_InitPersists)
                {
                if (ipd.MClassName == ed.m_objecttype)
                    {
                    if (ipd.MName == callshort)
                        callshort = callshort + "X";
                    }
                }

            result += "public ";

            if (DoesAParentContainFunction(classname, callshort, ed.m_params))
                result += " new ";

            string t = "";
            if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ed.m_returntype.Replace("*", "")))
                t = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "")].cstype;
            else
                {
                t = Helpers.convertC2Cs(ed.m_returntype, false, ref MDntcConfig);
                if (t == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                    t = "string";
                }

            result += " " + t + " " + callshort + "(";
            string[] parameters;
            parameters = ed.m_params.Trim().ToLower() == "void" ? new string[0] : ed.m_params.Split(',');
            int c = 0;
            if (ed.m_objecttype.Trim().Length > 0)
                {
                List<string> tp = parameters.ToList();
                tp.Insert(0, ed.m_objecttype.Trim() + "* " + ed.m_objecttype.ToLower());
                parameters = tp.ToArray();
                }

            string logparams = "";

            List<string> ldefaults = new List<string>();
            char prevchar = ' ';
            bool inQuotes = false;
            bool inParans = false;
            string def = "";
            if (ed.m_defaults != "")
                {
                for (int i = 0; i < ed.m_defaults.Length; i++)
                    {
                    if (ed.m_defaults[i] == '"')
                        inQuotes = !inQuotes;
                    else if (ed.m_defaults[i] == '(' && !inQuotes)
                        inParans = true;
                    else if (ed.m_defaults[i] == ')' && !inQuotes)
                        inParans = false;
                    else if (!inQuotes && !inParans && ed.m_defaults[i] == ',')
                        {
                        ldefaults.Add(def);
                        def = "";
                        continue;
                        }

                    def += ed.m_defaults[i];
                    }
                if (def.Trim() != "")
                    ldefaults.Add(def);

                int tcount = ldefaults.Count;

                for (int i = 0; i < parameters.Count() - tcount; i++)
                    ldefaults.Insert(0, "");
                }
            int paramIndex = 0;
            foreach (string p in parameters)
                {
                if (p.Trim().ToLower() == "void")
                    {
                    paramIndex++;
                    continue;
                    }

                //here is is
                // this is where I want to do it.
                string parameter = p;
                if (parameter.Trim().Length <= 0)
                    {
                    paramIndex++;
                    continue;
                    }

                if (c > 0)
                    {
                    if (!result.Trim().EndsWith(","))
                        result += ", ";
                    }
                parameter = Helpers.getridofdoublespace(parameter);
                int i = parameter.Trim().LastIndexOf(' ');
                string ptypeo = parameter.Substring(0, i).Trim();
                string ptype = ptypeo;
                string pname = parameter.Substring(i).Trim();

                pname = Helpers.GiveMeSafeName(pname);

                string tt = ptype;

                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ptype.Replace("*", "")))
                    tt = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ptype.Replace("*", "")].cstype;
                else
                    {
                    tt = Helpers.convertC2Cs(ptype, false, ref MDntcConfig);
                    if (tt == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                        tt = "string";
                    }

                if (pname.Trim().ToLower() == classname.Trim().ToLower())
                    {
                    paramIndex++;
                    continue;
                    }

                if (ed.m_minparams > -1)
                    {
                    if (c >= ed.m_minparams)
                        {
                        result += tt + " " + pname + @"= """"";
                        logparams += tt + " " + pname + @"= """"";
                        }
                    else
                        {
                        result += tt + " " + pname;
                        logparams += tt + " " + pname;
                        }
                    }
                else
                    {
                    if (ldefaults.Count > 0)
                        {
                        if (ldefaults[paramIndex] != "")
                            {
                            if (MDntcConfig.PreGen_CPP_Constants.ContainsKey(ldefaults[paramIndex]))
                                {
                                codeInsert += @"if (" + pname + "== null) {" + pname + " = " + MDntcConfig.PreGen_CPP_Constants[ldefaults[paramIndex]] + ";}\r\n";
                                result += tt + " " + pname + " = null ";
                                }
                            else
                                result += tt + " " + pname + " = " + ldefaults[paramIndex];
                            }
                        else
                            result += tt + " " + pname;
                        }
                    else
                        result += tt + " " + pname;

                    logparams += tt + " " + pname;
                    }
                paramIndex++;
                c++;
                }
            mLogger.NewEvent("", "     Adding Member Function: " + t + " " + callshort + "(" + logparams + ").");

            result += "){";
            result += codeInsert;
            if (ed.m_returntype != "void")
                {
                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ed.m_returntype.Replace("*", "").Trim()))
                    {
                    switch (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "").Trim()].itype)
                        {
                            case CPPEntityType.Class:
                                result += "\r\nreturn new " + MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "").Trim()].cstype + " ( pInvokes.m_ts." + ed.m_name + "(";

                                break;
                            case CPPEntityType.Enum:
                                result += "\r\nreturn (" + MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "").Trim()].cstype + ")( pInvokes.m_ts." + ed.m_name + "(";

                                break;
                            default:
                                throw new Exception("Unknown conversion type");
                        }
                    }
                else
                    {
                    //if (ed.m_returntype == "F32")
                    //    result += "\r\nreturn (float)m_ts." + ed.m_name + "(";
                    //else
                    result += "\r\nreturn pInvokes.m_ts." + ed.m_name + "(";
                    }
                }
            else
                result += "\r\npInvokes.m_ts." + ed.m_name + "(";
            c = 0;

            foreach (string p in parameters)
                {
                string parameter = p;
                if (parameter.Trim().ToLower() == "void")
                    continue;
                if (parameter.Trim().Length > 0)
                    {
                    if (c > 0)
                        {
                        if (!result.Trim().EndsWith(","))
                            result += ", ";
                        }
                    parameter = Helpers.getridofdoublespace(parameter);
                    int i = parameter.Trim().LastIndexOf(' ');
                    string ptypeo = parameter.Substring(0, i).Trim();
                    string ptype = ptypeo;
                    string pname = parameter.Substring(i).Trim();

                    pname = Helpers.GiveMeSafeName(pname);

                    string ptyper = ptype.Replace("*", "").Trim();

                    if (pname.Trim().ToLower() == classname.Trim().ToLower())
                        result += "_ID";
                    else
                        {
                        if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ptyper))
                            {
                            if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ptyper].itype == CPPEntityType.Class)
                                result += pname + ".AsString()";
                            else
                                result += "(int)" + pname + " ";
                            }
                        else
                            result += pname;
                        }

                    c++;
                    }
                }

            if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ed.m_returntype.Replace("*", "").Trim()))
                result += ")";

            result += ");\r\n";

            result += "}\r\n";
            return result;
        }

        public string tsc_GenerateWork_Partial(String classname)
        {
            mLogger.SubSectionStart("Generating CSharp class code");
            mLogger.NewEvent("", "Generating CSharp Class object for T3D class '" + classname + "'.");
            StringBuilder Code = new StringBuilder();
            Code.Append(@"

#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinterLeaf.Engine;
using WinterLeaf.Engine.Classes;
using WinterLeaf.Engine.Containers;
using WinterLeaf.Engine.Enums;
using System.ComponentModel;
using System.Threading;
using  WinterLeaf.Engine.Classes.Interopt;
using WinterLeaf.Engine.Classes.Decorations;
using WinterLeaf.Engine.Classes.Extensions;
using WinterLeaf.Engine.Classes.Helpers;
using " + _mcSharpNamespace + "." + Interrogator.constants.namespaces.ProxyObjects_Base + @";
#endregion

namespace " + _mcSharpNamespace + "." + Interrogator.constants.namespaces.userObjects_ProxyObjects + @"
    {");

            Code.Append(@"
    /// <summary>
    /// 
    /// </summary>
    [TypeConverter(typeof(TypeConverterGeneric<" + Interrogator.constants.general.classPrefix + classname + @">))]
     public  partial class " + Interrogator.constants.general.classPrefix);
            Code.Append(classname);
            Code.Append(": " + Interrogator.constants.general.classPrefix + classname + "_Base\r\n{\r\n");
            //if (1 == 0)

            #region "Operator overides"

            Code.Append(@"
#region ProxyObjects Operator Overrides
        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <param name=""simobjectid""></param>
        /// <returns></returns>
        public static bool operator ==(" + Interrogator.constants.general.classPrefix + classname + @" ts, string simobjectid)
            {
            return object.ReferenceEquals(ts, null) ? object.ReferenceEquals(simobjectid, null) : ts.Equals(simobjectid);
            }
  /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
            {
            return base.GetHashCode();
            }
  /// <summary>
        /// 
        /// </summary>
        /// <param name=""obj""></param>
        /// <returns></returns>
        public override bool Equals(object obj)
            {
            
            return (this._ID ==(string)myReflections.ChangeType( obj,typeof(string)));
            }
        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <param name=""simobjectid""></param>
        /// <returns></returns>
        public static bool operator !=(" + Interrogator.constants.general.classPrefix + classname + @" ts, string simobjectid)
            {
            if (object.ReferenceEquals(ts, null))
                return !object.ReferenceEquals(simobjectid, null);
            return !ts.Equals(simobjectid);

            }


            /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator string( " + Interrogator.constants.general.classPrefix + classname + @" ts)
            {
            return ReferenceEquals(ts, null) ? ""0"" : ts._ID;
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator " + Interrogator.constants.general.classPrefix + classname + @"(string ts)
            {
            uint simobjectid = resolveobject(ts);
           return  (" + Interrogator.constants.general.classPrefix + classname + @") Omni.self.getSimObject(simobjectid,typeof(" + Interrogator.constants.general.classPrefix + classname + @"));
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator int( " + Interrogator.constants.general.classPrefix + classname + @" ts)
            {
            return (int)ts._iID;
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""simobjectid""></param>
        /// <returns></returns>
        public static implicit operator " + Interrogator.constants.general.classPrefix + classname + @"(int simobjectid)
            {
            return  (" + Interrogator.constants.general.classPrefix + classname + @") Omni.self.getSimObject((uint)simobjectid,typeof(" + Interrogator.constants.general.classPrefix + classname + @"));
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""ts""></param>
        /// <returns></returns>
        public static implicit operator uint( " + Interrogator.constants.general.classPrefix + classname + @" ts)
            {
            return ts._iID;
            }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static implicit operator " + Interrogator.constants.general.classPrefix + classname + @"(uint simobjectid)
            {
            return  (" + Interrogator.constants.general.classPrefix + classname + @") Omni.self.getSimObject(simobjectid,typeof(" + Interrogator.constants.general.classPrefix + classname + @"));
            }
#endregion

");

            #endregion

            Code.Append(@"}}");
            return Code.ToString();
        }

        public void Start()
        {
            mLogger.SectionStart("T3D CSharp Objects");
            double total = MDntcConfig.ObjectParent.Keys.Count;
            double pos = 0;

            ProjectItem piProxyObjectsBase = null;
            ProjectItem piuserObjectsProxyObjects = null;

            if (Interrogator.self.mCSProject_GameLogic != null)
                {
                if (!Interrogator.self.findProjectItem(Interrogator.self.mCSProject_GameLogic.ProjectItems, Interrogator.constants.fileLocations.ProxyObjects_Base, out piProxyObjectsBase))
                    throw new Exception("Unable to find ProxyObject_Base sub-folder.");

                if (!Interrogator.self.findProjectItem(Interrogator.self.mCSProject_GameLogic.ProjectItems, Interrogator.constants.fileLocations.userObjects_ProxyObjects, out piuserObjectsProxyObjects))
                    throw new Exception("Unable to find \\userObjects\\ProxyObjects\\ sub-folder.");
                }

            foreach (string tsclass in MDntcConfig.ObjectParent.Keys)
                {
                pos += 1;
                mLogger.onProgressSubChange(pos/total, tsclass);
                string code = tsc_GenerateWork(tsclass);

                if (piProxyObjectsBase == null)
                    {
                    try
                        {
                        using (StreamWriter file = new StreamWriter(_mcSharpUserLocation + Interrogator.constants.fileLocations.ProxyObjects_Base + Interrogator.constants.general.classPrefix + tsclass + "_Base.cs", false))
                            file.WriteLine(code);
                        }
                    catch (Exception)
                        {
                        throw new Exception("Cannot write to " + _mcSharpUserLocation + Interrogator.constants.fileLocations.ProxyObjects_Base + Interrogator.constants.general.classPrefix + tsclass + "_Base.cs.  Is it readonly?");
                        }
                    }
                else
                    {
                    try
                        {
                        string filename = piProxyObjectsBase.Properties.Item("FullPath").Value.ToString() + Interrogator.constants.general.classPrefix + tsclass + "_Base.cs";
                        using (StreamWriter file = new StreamWriter(filename, false))
                            file.WriteLine(code);

                        //ProjectItem addeditem =
                        piProxyObjectsBase.ProjectItems.AddFromFile(filename);
                        //addeditem.Properties.Item("CustomTool").Value = "OmniClassHelper";
                        }
                    catch (Exception err)
                        {
                        throw err;
                        }
                    }

                code = tsc_GenerateWork_Partial(tsclass);
                if (piuserObjectsProxyObjects == null)
                    {
                    try
                        {
                        using (StreamWriter file = new StreamWriter(_mcSharpUserLocation + Interrogator.constants.fileLocations.userObjects_ProxyObjects + Interrogator.constants.general.classPrefix + tsclass + ".cs", false))
                            file.WriteLine(code);
                        }
                    catch (Exception)
                        {
                        throw new Exception("Cannot write to " + _mcSharpUserLocation + Interrogator.constants.fileLocations.userObjects_ProxyObjects + Interrogator.constants.general.classPrefix + tsclass + ".cs.  Is it readonly?");
                        }
                    }
                else
                    {
                    try
                        {
                        string filename = piuserObjectsProxyObjects.Properties.Item("FullPath").Value.ToString() + Interrogator.constants.general.classPrefix + tsclass + ".cs";
                        using (StreamWriter file = new StreamWriter(filename, false))
                            file.WriteLine(code);
                        piuserObjectsProxyObjects.ProjectItems.AddFromFile(filename);
                        }
                    catch (Exception err)
                        {
                        throw err;
                        }
                    }
                }
            mLogger.SectionEnd();
        }
    }
}