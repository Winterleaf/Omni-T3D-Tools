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
    internal class Generator_Auto
    {
        private readonly string mCSharpSourceLocation;
        private readonly Logger.Logger mLogger;
        private readonly CodeParsing mParsing;
        private ConfigFiles mCF;

        public Generator_Auto(string WLECSharpSourceLocation, ref Logger.Logger logger, ref CodeParsing p, ref ConfigFiles cf)
        {
            mCSharpSourceLocation = WLECSharpSourceLocation;
            mLogger = logger;
            mParsing = p;
            mCF = cf;
        }

        private string Process_DnTorque_Auto_CS(Externdata ed)
        {
            if (ed.m_objecttype.Trim() != "")
                mLogger.NewEvent("", "(START) Generating Auto Code for " + ed.m_returntype + " " + ed.m_objecttype + "::" + ed.m_name + "(" + ed.m_params + ").");
            else
                mLogger.NewEvent("", "(START) Generating Auto Code for " + ed.m_returntype + " " + ed.m_name + "(" + ed.m_params + ").");

            string sbtext = "";
            string fncall = "";

            string csharpfunct = "\r\n";
            if (ed.m_helptext.Trim().Length != 0)
                {
                csharpfunct += "/// <summary>\r\n";
                string[] line = ed.m_helptext.Split('\r');
                csharpfunct = line.Aggregate(csharpfunct, (current, s) => current + ("/// " + s.Replace("\n", "").Replace("\\n", "").Replace("&lt;", "<").Replace("&gt;", ">")).Replace("<", "").Replace("&rt;", "") + "\r\n");
                csharpfunct += "/// </summary>\r\n";
                }
            csharpfunct += "\r\npublic ";
            string t = Helpers.convertC2Cs(ed.m_returntype, false, ref mCF);
            if ((t != "[MarshalAs(UnmanagedType.LPStr)] StringBuilder") && (ed.m_returntype != "void"))
                {
                fncall = "return  SafeNativeMethods.mwle_" + ed.m_name + "(";
                if (ed.m_returntype != "void")
                    csharpfunct += t;
                else
                    csharpfunct += "void";
                }
            else
                {
                if (ed.m_returntype.Contains("char"))
                    sbtext += "var returnbuff = new StringBuilder(" + ConfigFiles.BufferReturn_Character + ");\r\n";
                else if (ed.m_returntype != "void")
                    sbtext += "var returnbuff = new StringBuilder(1024);\r\n";
                if (ed.m_returntype != "void")
                    csharpfunct += "string";
                else
                    csharpfunct += "void";

                fncall = "SafeNativeMethods.mwle_" + ed.m_name + "(";
                }
            csharpfunct += " " + ed.m_name + " (";
            string[] parameters = ed.m_params.Trim().ToLower() == "void" ? new string[0] : ed.m_params.Split(',');

            int c = 0;
            if (ed.m_objecttype.Trim().Length > 0)
                {
                List<string> tp = parameters.ToList();
                tp.Insert(0, ed.m_objecttype.Trim() + "* " + ed.m_objecttype.ToLower());
                parameters = tp.ToArray();
                }
            string paramst = "";
            foreach (string p in parameters)
                {
                if (p.Trim().ToLower() == "void")
                    continue;
                string parameter = p;
                if (parameter.Trim().Length <= 0)
                    continue;
                if (c > 0)
                    {
                    if (!csharpfunct.Trim().EndsWith(","))
                        csharpfunct += ", ";
                    }
                if (c > 0)
                    {
                    if (!fncall.Trim().EndsWith(","))
                        fncall += ", ";
                    }
                parameter = Helpers.getridofdoublespace(parameter);
                int i = parameter.Trim().LastIndexOf(' ');
                string ptypeo = parameter.Substring(0, i).Trim();
                string ptype = ptypeo;

                string pname = parameter.Substring(i).Trim();

                pname = Helpers.GiveMeSafeName(pname);

                paramst += pname + ",";

                string tt = Helpers.convertC2Cs(ptype, false, ref mCF);
                if (tt == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                    tt = "string";
                csharpfunct += tt + " " + pname;
                if (tt == "string")
                    {
                    //if (ptypeo.Contains("char"))
                    //  {

                    sbtext += "StringBuilder sb" + pname + " = null;\r\n";
                    sbtext += "if (" + pname + " != null)\r\n";
                    sbtext += "     sb" + pname + " = new StringBuilder(" + pname + ", 1024);\r\n";
                    //sbtext += "StringBuilder sb" + pname + " = new StringBuilder(" + pname + ", 1024);\r\n";
                    //}
                    //else
                    //    {
                    //    sbtext += "var sb" + pname + " = new StringBuilder(" + pname + ", 1024);\r\n";
                    //    }
                    fncall += "sb" + pname;
                    }
                else
                    fncall += pname;
                c++;
                }

            if (ed.m_returntype != "void")
                {
                if (t == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                    {
                    if (c > 0)
                        {
                        if (!fncall.Trim().EndsWith(","))
                            fncall += ", ";
                        }
                    fncall += "returnbuff";
                    }
                }
            if (ed.m_returntype == "bool")
                fncall += ")>=1;";
            else
                fncall += ");";

            if (ed.m_returntype != "void")
                {
                if (t == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                    fncall += "\r\nreturn returnbuff.ToString();\r\n";
                }

            if (paramst.EndsWith(","))
                paramst = paramst.Substring(0, paramst.LastIndexOf(""));

            string hh = "if(Debugging)\r\n";
            if (paramst.Trim() != "")
                {
                hh += "System.Console.WriteLine(\"----------------->Extern Call '" + ed.m_name + "'\" + string.Format(\"";

                for (int i = 0; i <= paramst.Split(',').Count() - 1; i++)
                    hh += "\\\"" + "{" + i + "}" + "\\\"" + " ";
                hh += "\"," + paramst + "));";
                }
            else
                hh += "System.Console.WriteLine(\"----------------->Extern Call '" + ed.m_name + "'\");\r\n";

            csharpfunct += ")\r\n{\r\n";
            //csharpfunct += "System.Console.WriteLine(\"----------PInvoke: " + ed.m_name + "\");\r\n";
            csharpfunct += hh + "\r\n";
            csharpfunct += sbtext;
            csharpfunct += "\r\n" + fncall;
            csharpfunct += "\r\n}";

            return csharpfunct;
        }

        public void Start()
        {
            mLogger.SubSectionStart("Generating DNTorque_Auto.cs");
            try
                {
                string data;
                //using (TextReader sr = new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) + "\\Templates\\CodeFiles\\Omni.Auto.cs.txt"))
                //    {
                //    data = sr.ReadToEnd();
                //    sr.Close();
                //    }

                data = CodeTemplates.Omni_Auto_cs_txt;

                StringBuilder ag = new StringBuilder();
                double total = mParsing.Data_Data.Count;
                double pos = 0;
                foreach (Externdata d in mParsing.Data_Data)
                    {
                    pos += 1;
                    mLogger.onProgressSubChange(pos/total, d.m_name);
                    ag.Append(Process_DnTorque_Auto_CS(d));
                    }
                data = data.Replace("###INSERTAUTOGEN###", ag.ToString());

                if (Interrogator.self.mCSProject_Engine != null)
                    {
                    ProjectItem pi;
                    if (!Interrogator.self.findProjectItem(Interrogator.self.mCSProject_Engine.ProjectItems, "\\Omni.Auto.cs", out pi))
                        throw new Exception("Could not find Omni.Auto.cs.");
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
                    try
                        {
                        using (StreamWriter file = new StreamWriter(mCSharpSourceLocation + "\\Omni.Auto.cs", false))
                            file.WriteLine(data);
                        }
                    catch (Exception)
                        {
                        throw new Exception("Cannot write to Omni.Auto.cs.  Is it readonly?");
                        }
                    }
                }
            catch (Exception err)
                {
                mLogger.NewErrorEvent("", "Failed To Generate '" + mCSharpSourceLocation + "\\Omni.Auto.cs" + "' " + err.Message + " " + err.StackTrace);
                mLogger.SubSectionEnd();
                throw err;
                }
            mLogger.SubSectionEnd();
        }
    }
}