using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Winterleaf.SharedServices.Interrogator.Configuration;
using Winterleaf.SharedServices.Interrogator.Containers;
using Winterleaf.SharedServices.Interrogator.Parsing;

namespace Winterleaf.SharedServices.Interrogator.cSharp_Generators
{
    internal class Generator_pInvokes
    {
        private readonly string mCSharpSourceLocation;
        private readonly CodeParsing mCodeParsing;
        private readonly Logger.Logger mLogger;
        private ConfigFiles MDntcConfig;

        public Generator_pInvokes(String CSharpSourceLocation, ref Logger.Logger logger, ref CodeParsing cp, ref ConfigFiles cf)
        {
            mCSharpSourceLocation = CSharpSourceLocation;
            mLogger = logger;
            mCodeParsing = cp;
            MDntcConfig = cf;
        }

        internal string WriteCallWrapper(Externdata ed)
        {
            if (ed.m_objecttype.Trim() != "")
                mLogger.NewEvent("", "(START) Generating Call Wrapper for " + ed.m_returntype + " " + ed.m_objecttype + "::" + ed.m_name + "(" + ed.m_params + ").");
            else
                mLogger.NewEvent("", "(START) Generating Call Wrapper for " + ed.m_returntype + " " + ed.m_name + "(" + ed.m_params + ").");

            string result = "";
            if (ed.m_helptext.Trim().Length != 0)
                {
                result += "/// <summary>\r\n";
                string[] line = ed.m_helptext.Split('\r');
                result = line.Aggregate(result, (current, s) => current + ("/// " + s.Replace("\n", "").Replace("\\n", "").Replace("&lt;", "<").Replace("&gt;", ">")).Replace("<", "").Replace("&rt;", "") + "\r\n");
                result += "/// </summary>\r\n";
                }
            string callshort = ed.m_name.Substring(ed.m_name.IndexOf("_", StringComparison.Ordinal) + 1);
            result += "public ";

            string t = "";
            if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ed.m_returntype.Replace("*", "")))
                t = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "")].cstype;
            else
                {
                t = Helpers.convertC2Cs(ed.m_returntype, false, ref MDntcConfig);
                if (t == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                    t = "string";
                }

            callshort = Helpers.GiveMeSafeName(callshort);

            result += " " + t + " " + callshort + "(";
            string[] parameters = ed.m_params.Trim().ToLower() == "void" ? new string[0] : ed.m_params.Split(',');
            int c = 0;
            if (ed.m_objecttype.Trim().Length > 0)
                {
                List<string> tp = parameters.ToList();
                tp.Insert(0, ed.m_objecttype.Trim() + "* " + ed.m_objecttype.ToLower());
                parameters = tp.ToArray();
                }

            #region "Defaults"

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

            #endregion

            string codeInsert = "";
            int paramIndex = -1;
            foreach (string p in parameters)
                {
                paramIndex++;
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
                    tt = Helpers.convertC2Cs(ptype, false, ref MDntcConfig);
                    if (tt == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                        tt = "string";
                    }

                if (ed.m_minparams > -1)
                    {
                    if (c >= ed.m_minparams)
                        result += tt + " " + pname + @"= """"";
                    else
                        result += tt + " " + pname;
                    }
                    //else
                    //    {
                    //    result += tt + " " + pname;
                    //    }
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
                    }

                c++;
                }
            result += "){\r\n" + codeInsert + "\r\n";
            if (ed.m_returntype != "void")
                {
                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ed.m_returntype.Replace("*", "").Trim()))
                    {
                    switch (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "").Trim()].itype)
                        {
                            case CPPEntityType.Class:
                                result += "\r\nreturn new " + MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "").Trim()].cstype + " ( m_ts." + ed.m_name + "(";
                                break;
                            case CPPEntityType.Enum:
                                result += "\r\nreturn (" + MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ed.m_returntype.Replace("*", "").Trim()].cstype + ")( m_ts." + ed.m_name + "(";
                                break;
                            default:
                            {
                            mLogger.NewErrorEvent("", "Unable to determine CSharp equivalent to '" + ed.m_returntype.Replace("*", "").Trim());
                                throw new Exception("Unknown conversion type");
                            }
                        }
                    }
                else
                    result += "\r\nreturn m_ts." + ed.m_name + "(";
                }
            else
                result += "\r\nm_ts." + ed.m_name + "(";
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

                    if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ptyper))
                        {
                        if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ptyper].itype == CPPEntityType.Class)
                            result += pname + ".AsString()";
                        else
                            result += "(int)" + pname + " ";
                        }
                    else
                        result += pname;

                    c++;
                    }
                }

            if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ed.m_returntype.Replace("*", "").Trim()))
                result += ")";

            result += ");\r\n";

            result += "}\r\n";
            mLogger.NewEvent("", "(END) Generating Call Wrapper");
            return result;
        }

        public string ProcessTorqueScriptTemplate()
        {
            string fcalls = "";
            IEnumerable<string> un = (from i in mCodeParsing.Data_Data select i.m_objecttype).Distinct();

            double total = un.Count();
            double pos = 0;

            foreach (string ss in un)
                {
                string s = ss == "" ? "Util" : ss;

                fcalls += "\t_m" + s + " = new " + s + "Object(ref c);\r\n";
                }
            fcalls += "}\r\n";
            //private readonly ConsoleObject _mConsoleobject;
            un = (from i in mCodeParsing.Data_Data select i.m_objecttype).Distinct();
            foreach (string ss in un)
                {
                pos += 1;
                mLogger.onProgressSubChange(pos/total, ss);

                string s = ss == "" ? "Util" : ss;
                fcalls += "public " + s + "Object _m" + s + ";\r\n";
                fcalls += @"        /// <summary>
        /// 
        /// </summary>
";
                //if (s.ToLower() == "util")
                fcalls += "public ";
                //else
                //fcalls += "internal ";

                fcalls += s + "Object " + s + "{get { return _m" + s + "; }}\r\n";
                }

            un = (from i in mCodeParsing.Data_Data orderby i.m_objecttype orderby i.m_objecttype select i.m_objecttype).Distinct();
            foreach (string ss in un)
                {
                string s = ss == "" ? "Util" : ss;
                IEnumerable<Externdata> un1 = (from i in mCodeParsing.Data_Data where i.m_objecttype == ss orderby i.m_name select i);
                fcalls += @"   /// <summary>
        /// 
        /// </summary>
";

                fcalls += " public class " + s + "Object\r\n";
                fcalls += "{\r\n";
                fcalls += "private Omni m_ts;\r\n";
                fcalls += @"     /// <summary>
     /// 
     /// </summary>
     /// <param name=""ts""></param> 
";
                fcalls += "public " + s + "Object(ref Omni ts){m_ts = ts;}\r\n";
                foreach (Externdata s1 in un1)
                    fcalls += WriteCallWrapper(s1);
                fcalls += "}\r\n";
                }

            return fcalls;
        }

        public void Start()
        {
            mLogger.SectionStart("Generate pInvokes");
            try
                {
                mLogger.SubSectionStart("Reading Template File");
                mLogger.NewEvent("", "Reading File '" + Path.GetDirectoryName(Application.ExecutablePath) + "\\Templates\\CodeFiles\\pInvokes.cs.txt" + "'");
                string data;
                //using (TextReader sr = new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) + "\\Templates\\CodeFiles\\pInvokes.cs.txt"))
                //    {
                //    data = sr.ReadToEnd();
                //    sr.Close();
                //    }
                data = CodeTemplates.pInvokes_cs_txt;

                mLogger.SubSectionEnd();

                mLogger.SubSectionStart("Generating Code");
                data = data.Replace("###INSERTAUTOGEN###", ProcessTorqueScriptTemplate());
                mLogger.SubSectionEnd();

                mLogger.SubSectionStart("Writing Output");
                mLogger.NewEvent("", "Writing File '" + Path.GetDirectoryName(Application.ExecutablePath) + "\\Templates\\CodeFiles\\Interopt\\pInvokes.cs" + "'");

                if (Interrogator.self.mCSProject_Engine != null)
                    {
                    ProjectItem pi;
                    if (!Interrogator.self.findProjectItem(Interrogator.self.mCSProject_Engine.ProjectItems, "\\Classes\\Interopt\\pInvokes.cs", out pi))
                        throw new Exception("Could not find \\Classes\\Interopt\\pInvokes.cs.");
                    Window win = pi.Open();
                    win.Visible = true;
                    TextDocument textDoc = (TextDocument) pi.Document.Object("TextDocument");
                    EditPoint editPoint = (EditPoint) textDoc.StartPoint.CreateEditPoint();
                    EditPoint endPoint = (EditPoint) textDoc.EndPoint.CreateEditPoint();
                    editPoint.Delete(endPoint);
                    editPoint.Insert(data);
                    pi.Save();
                    win.Close();
                    }
                else
                    {
                    using (StreamWriter file = new StreamWriter(mCSharpSourceLocation + "\\Classes\\Interopt\\pInvokes.cs", false))
                        file.WriteLine(data);
                    }
                mLogger.SubSectionEnd();
                }
            catch (Exception err)
                {
                mLogger.NewErrorEvent("", "Failed To Generate '" + mCSharpSourceLocation + "\\Omni.Auto.cs" + "' " + err.Message + " " + err.StackTrace);
                mLogger.SectionEnd();
                throw;
                }
        }
    }
}