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
    internal class Generator_SafeNativeMethods
    {
        private readonly CodeParsing mCP;
        private readonly string mCSharpSourceLocation;
        private readonly Logger.Logger mLogger;
        private ConfigFiles mCF;

        public Generator_SafeNativeMethods(string CSharpSourceLocation, ref Logger.Logger logger, ref CodeParsing cp, ref ConfigFiles cf)
        {
            mCSharpSourceLocation = CSharpSourceLocation;
            mLogger = logger;
            mCF = cf;
            mCP = cp;
        }

        public void Start()
        {
            mLogger.SectionStart("Generate SafeNativeMethods");
            string data = string.Empty;
            try
                {
                //using (
                //    TextReader sr =
                //        new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) +
                //                         "\\Templates\\CodeFiles\\SafeNativeMethods_cs.txt"))
                //{
                //    data = sr.ReadToEnd();
                //    sr.Close();
                //}

                data = CodeTemplates.SafeNativeMethods_cs_txt;

                data = data.Replace("###INSERTAUTOGEN###", GenerateAllcSharpDelegates() + GenerateRelease() + GenerateAlldynoExternstuff());
                try
                    {
                    if (Interrogator.self.mCSProject_Engine != null)
                        {
                        ProjectItem pi;
                        if (!Interrogator.self.findProjectItem(Interrogator.self.mCSProject_Engine.ProjectItems, "Classes\\Interopt\\SafeNativeMethods.cs", out pi))
                            throw new Exception("Could not find Classes\\Interopt\\SafeNativeMethods.cs file.");
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
                        using (StreamWriter file = new StreamWriter(mCSharpSourceLocation + "\\Classes\\Interopt\\SafeNativeMethods.cs", false))
                            file.WriteLine(data);
                        }
                    }
                catch (Exception)
                    {
                    throw new Exception("Cannot write to " + mCSharpSourceLocation + "\\Classes\\Interopt\\SafeNativeMethods.cs. Is it readonly?");
                    }

                mLogger.NewEvent("", "Finished Generating '" + mCSharpSourceLocation + "\\Classes\\Interopt\\SafeNativeMethods.cs" + "'.");
                }
            catch (Exception err)
                {
                mLogger.NewErrorEvent("", "GENERATION FAILED! " + err.Message + " " + err.StackTrace);
                throw err;
                }
            mLogger.SectionEnd();
        }

        internal string GenerateAlldynoExternstuff()
        {
            mLogger.SubSectionStart("Generating Code");
            mLogger.NewEvent("", "Generating Code To Capture CSharp Delegates (Function Pointers)");
            string rval = " static private  void MapDynamicExterns(string dllname){\r\n";
            rval += mCP.Data_Data.Aggregate("", (current, externdata) => current + (dynoExternstuff(externdata)));
            rval += "}";
            mLogger.SubSectionEnd();
            return rval;
        }

        internal string dynoExternstuff(Externdata ed)
        {
            return "mwle_" + ed.m_name + "= (wle_" + ed.m_name + @")Marshal.GetDelegateForFunctionPointer(GetProcAddress(dllname, ""wle_" + ed.m_name + @"""), typeof(wle_" + ed.m_name + "));" + "\r\n";
        }

        internal string GenerateRelease()
        {
            mLogger.SubSectionStart("Generating Code");
            mLogger.NewEvent("", "Generating Code To Release CSharp Delegates (Function Pointers)");
            string rval = " static private  void ClearAutoExterns(){\r\n";
            rval += mCP.Data_Data.Aggregate("", (current, externdata) => current + ("mwle_" + externdata.m_name + "= null;\r\n"));
            rval += "}";
            mLogger.SubSectionEnd();
            return rval;
        }

        internal string GenerateAllcSharpDelegates()
        {
            mLogger.SubSectionStart("Generate All CSharp Delegates");

            StringBuilder result = new StringBuilder();
            double total = mCP.Data_Data.Count;
            double pos = 0;
            foreach (Externdata d in mCP.Data_Data)
                {
                pos += 1;
                mLogger.onProgressSubChange(pos/total, d.m_name);
                result.Append(GenerateCSharpExternDelegates(d));
                }
            return result.ToString();
            //return mCP.Data_Data.Aggregate("",
            //    (current, externdata) => current + (this.GenerateCSharpExternDelegates(externdata)));
        }

        internal string GenerateCSharpExternDelegates(Externdata ed)
        {
            if (ed.m_objecttype.Trim() != "")
                mLogger.NewEvent("", "(START) Generating CSharp Delegate for " + ed.m_returntype + " " + ed.m_objecttype + "::" + ed.m_name + "(" + ed.m_params + ").");
            else
                mLogger.NewEvent("", "(START) Generating CSharp Delegate for " + ed.m_returntype + " " + ed.m_name + "(" + ed.m_params + ").");

            string csharpfunct = "static internal wle_" + ed.m_name + " mwle_" + ed.m_name + ";\r\n";

            csharpfunct += "[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)] \r\n internal delegate " + Helpers.returnvalType(ed.m_returntype, ref mCF) + " wle_" + ed.m_name + "(";

            string[] parameters;
            if (ed.m_params.Trim().ToLower() == "void")
                parameters = new string[0];
            else
                parameters = ed.m_params.Split(',');

            //string[] parameters = ed.m_params.Split(',');
            int c = 0;
            if (ed.m_objecttype.Trim().Length > 0)
                {
                List<string> tp = parameters.ToList();
                tp.Insert(0, ed.m_objecttype.Trim() + "* " + ed.m_objecttype.ToLower());
                parameters = tp.ToArray();
                }
            foreach (string p in parameters)
                {
                string parameter = p;
                if (parameter.Trim().ToLower() == "void")
                    continue;
                if (parameter.Trim().Length > 0)
                    {
                    if (c > 0)
                        {
                        if (!csharpfunct.Trim().EndsWith(","))
                            csharpfunct += ", ";
                        }
                    parameter = Helpers.getridofdoublespace(parameter);
                    int i = parameter.Trim().LastIndexOf(' ');
                    string ptype = parameter.Substring(0, i).Trim();
                    string pname = parameter.Substring(i).Trim();

                    pname = Helpers.GiveMeSafeName(pname);

                    csharpfunct += Helpers.c2cI(ptype, ref mCF) + " " + pname;

                    c++;
                    }
                }

            if (!mCF.PreGen_CPP_TypeConv.ContainsKey(ed.m_returntype))
                {
                if (ed.m_returntype != "void")
                    {
                    if (c > 0)
                        {
                        if (csharpfunct.Trim().EndsWith(","))
                            csharpfunct += "[MarshalAs(UnmanagedType.LPStr)] [Out] StringBuilder retval";
                        else
                            csharpfunct += ",[MarshalAs(UnmanagedType.LPStr)] [Out] StringBuilder retval";
                        }
                    else
                        csharpfunct += "[MarshalAs(UnmanagedType.LPStr)] [Out] StringBuilder retval";
                    }
                }
            csharpfunct += ");\r\n";

            mLogger.NewEvent("", "(END) Generating CSharp Delegate.");

            return csharpfunct;
        }
    }
}