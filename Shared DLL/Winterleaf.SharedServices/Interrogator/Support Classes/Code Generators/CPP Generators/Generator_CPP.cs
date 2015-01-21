using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Winterleaf.SharedServices.Interrogator.Configuration;
using Winterleaf.SharedServices.Interrogator.Containers;
using Winterleaf.SharedServices.Interrogator.Parsing;

namespace Winterleaf.SharedServices.Interrogator
{
    internal class Generator_CPP
    {
        private readonly ConfigFiles MDntcConfig;
        //private string WLECPP_SourceLocation;
        private readonly CodeParsing mCP;
        private readonly Logger.Logger mLogger;

        //public Generator_CPP(string CPP_SourceLocation, ref Logger.Logger logger, ref ConfigFiles cf, ref CodeParsing cp)
        public Generator_CPP(ref Logger.Logger logger, ref ConfigFiles cf, ref CodeParsing cp)
        {
            //WLECPP_SourceLocation = CPP_SourceLocation;
            mLogger = logger;
            MDntcConfig = cf;

            mCP = cp;
        }

        public void Start()
        {
            bool haderrors = false;
            try
                {
                //Done
                string CPP = "";

                //mheaders = new List<string>();

                #region Generate C++ Functions

                mLogger.SectionStart("C++ Code Generation");
                mLogger.SubSectionStart("Generating C++ Functions");
                NewGenerateCPP();

                #endregion

                mLogger.SubSectionEnd();

                #region Find and Replace

                //mLogger.SubSectionStart("Processing Find and Replaces");
                //foreach (KeyValuePair<string, string> keyValuePair in MDntcConfig.PosGen_CPP_FindReplace)
                //    {
                //    try
                //        {
                //        CPP = CPP.Replace(keyValuePair.Key, keyValuePair.Value);
                //        }
                //    catch (Exception err)
                //        {
                //        mLogger.NewErrorEvent("",
                //            "Failure doing replace on '" + keyValuePair.Key + "' {" + err.Message + "(" + err.StackTrace +
                //            ")" + "}");
                //        haderrors = true;
                //        }
                //    }
                //mLogger.SubSectionEnd();
                //if (haderrors)
                //    {
                //    throw new Exception("Stage 3 had errors.");
                //    }

                #endregion

                #region Read in Template code from DotNetC_cpp

                //mLogger.SubSectionStart("Reading Template");
                //mLogger.NewEvent("",
                //    "Reading Template from " + Path.GetDirectoryName(Application.ExecutablePath) +
                //    "\\Templates\\CodeFiles\\DotNetC_cpp.txt");
                //string data = "";
                //try
                //    {
                //    using (
                //        TextReader sr =
                //            new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) +
                //                             "\\Templates\\CodeFiles\\DotNetC_cpp.txt"))
                //        {
                //        data = sr.ReadToEnd();
                //        sr.Close();
                //        }
                //    }
                //catch (Exception err)
                //    {
                //    mLogger.NewErrorEvent("",
                //        "Failure reading  DotNetC_cpp.txt {" + err.Message + "(" + err.StackTrace + ")" + "}");
                //    haderrors = true;
                //    }
                //mLogger.SubSectionEnd();
                //if (haderrors)
                //    {
                //    //mLogger.HTML_Report_Stage_3_Part_3.Clear();
                //    throw new Exception("Stage 3 had errors.");
                //    }
                //mLogger.SubSectionEnd();

                #endregion

                #region Write new file DotNetC_cpp

                //mLogger.SubSectionStart("Writing C++ file");
                //mLogger.NewEvent("", "Writing C++ file '" + WLECPP_SourceLocation + "\\DotNetC.CPP" + "'");
                //try
                //    {
                //    using (StreamWriter file = new StreamWriter(WLECPP_SourceLocation + "\\DotNetC.CPP", false))
                //        {
                //        file.WriteLine(data + CPP);
                //        }
                //    }
                //catch (Exception err)
                //    {
                //    mLogger.NewErrorEvent("",
                //        "Failure writing DotNetC_cpp.txt {" + err.Message + "(" + err.StackTrace + ")" + "}");
                //    haderrors = true;
                //    }

                //if (haderrors)
                //    {
                //    //mLogger.HTML_Report_Stage_3_Part_3.Clear();
                //    throw new Exception("Stage 3 had errors.");
                //    }
                //mLogger.SubSectionEnd();

                #endregion

                mLogger.SectionEnd();
                }
            catch (Exception err)
                {
                //mLogger.NewErrorEvent("",
                //    "GENERIC ERROR, Please report to Winterleaf Entertainment. {" + err.Message + "(" + err.StackTrace +
                //    ")" + "}");
                throw err;
                }
        }

        private void NewGenerateCPP()
        {
            List<string> m_Files = new List<string>();

            foreach (Externdata ed in mCP.Data_Data.Where(ed => !m_Files.Contains(ed.m_filename)))
                m_Files.Add(ed.m_filename);

            double total = m_Files.Count;
            double pos = 0;

            foreach (String file in m_Files)
                {
                pos += 1;

                mLogger.onProgressSubChange(pos/total, file);

                string cpp = mCP.Data_Data.Where(ed => ed.m_filename == file).Aggregate("", (current, ed) => current + "\r\n" + GenerateFunction(ed));

                String fileData = "";
                using (TextReader sr = new StreamReader(file))
                    {
                    fileData = sr.ReadToEnd();
                    sr.Close();
                    }
                if (fileData.Contains("//---------------DNTC AUTO-GENERATED---------------//"))
                    fileData = fileData.Substring(0, fileData.IndexOf("//---------------DNTC AUTO-GENERATED---------------//", StringComparison.Ordinal));
                fileData = fileData.Trim();

                for (int i = 0; i < 50; i++)
                    fileData += "\r\n";

                fileData = fileData + "\r\n" + "//---------------DNTC AUTO-GENERATED---------------//";

                fileData += "\r\n#include <vector>\r\n";
                fileData += "\r\n#include <string>\r\n";
                fileData += "\r\n#include \"core/strings/stringFunctions.h\"\r\n";
                fileData += "\r\n" + "//---------------DO NOT MODIFY CODE BELOW----------//\r\n";
                fileData += cpp.Replace("TORQUE_UNUSED(argc);", "").Replace("TORQUE_UNUSED(argv);", "").Replace("argc; argv;", "");
                fileData += "\r\n" + "//---------------END DNTC AUTO-GENERATED-----------//\r\n";

                try
                    {
                    using (StreamWriter sw = new StreamWriter(file, false))
                        sw.WriteLine(fileData);
                    }
                catch (Exception)
                    {
                    throw new Exception("Cannot write to file " + file + ".  Is it readonly?");
                    }
                }
        }

        private string GenerateFunction(Externdata ed)
        {
            if (ed.m_objecttype.Trim() != "")
                mLogger.NewEvent("", "(START) Processing Extern Data for " + ed.m_returntype + " " + ed.m_objecttype + "::" + ed.m_name + "(" + ed.m_params + ").");
            else
                mLogger.NewEvent("", "(START) Processing Extern Data for " + ed.m_returntype + " " + ed.m_name + "(" + ed.m_params + ").");

            List<string> errors = new List<string>();

            string[] parameters;
            parameters = ed.m_params.Trim().ToLower() == "void" ? new string[0] : ed.m_params.Split(',');

            string externfunctionhead = "extern \"C\" __declspec(dllexport) ";
            string treturntype = "void";

            bool addStringReturnParamter = false;

            if (MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(ed.m_returntype.Trim())) //treturntype = ed.m_returntype == "F32" ? "F64" : PreGen_CPP_TypeConv[ed.m_returntype.Trim()];
                treturntype = MDntcConfig.PreGen_CPP_TypeConv[ed.m_returntype.Trim()];
            else if (ed.m_returntype != "void")
                addStringReturnParamter = true;

            externfunctionhead += treturntype + "  __cdecl wle_" + ed.m_name + "(";
            //Now cycle through the parameters translating them.

            //Need to add code here to put the object
            //which is the target object in as a first param.
            int c = 0;
            if (ed.m_objecttype.Trim().Length > 0)
                {
                //externfunctionhead += " " + ed.m_objecttype.Trim() + "* x__m_obj";
                List<string> tp = parameters.ToList();
                tp.Insert(0, ed.m_objecttype.Trim() + "* object");
                parameters = tp.ToArray();
                }

            //here is where we need to check min and max params
            //if they are not set to -1... then we need to set
            //default values for everything pass the min.

            foreach (string rparameter in parameters)
                {
                if (rparameter.Trim().Length <= 0)
                    continue;
                string parameter = rparameter.Trim();
                if (c > 0)
                    {
                    if (!externfunctionhead.Trim().EndsWith(","))
                        externfunctionhead += ", ";
                    }

                if (parameter.Trim().Length <= 0)
                    continue;
                parameter = Helpers.getridofdoublespace(parameter);
                int i = parameter.Trim().LastIndexOf(' ');
                if (i == -1)
                    parameter = "";
                if (parameter.Trim().Length <= 0)
                    continue;
                string ptype = "";
                string pname = "";
                try
                    {
                    ptype = parameter.Substring(0, i).Trim();
                    pname = parameter.Substring(i).Trim();
                    }
                catch (Exception err)
                    {
                    errors.Add("Failed to parse parameter and parameter types from '" + parameter + "'.");
                    }

                if (!((ptype == "S32") || (ptype == "bool") || (ptype == "F32") || (ptype == "float") || (ptype == "U32") || (ptype == "int") || (ptype == "F64") || (ptype == "S8") || (ptype == "U8")))
                    {
                    if (MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(ptype))
                        externfunctionhead += MDntcConfig.PreGen_CPP_TypeConv[ptype] + " x__" + pname;
                    else
                        {
                        ptype = "char *";
                        externfunctionhead += ptype + " x__" + pname;
                        }
                    }
                else
                    externfunctionhead += ptype + " " + pname;
                c++;
                }

            if (addStringReturnParamter)
                {
                if (c > 0)
                    {
                    if (!externfunctionhead.Trim().EndsWith(","))
                        externfunctionhead += ", ";

                    externfunctionhead += " char* retval";
                    }
                else
                    externfunctionhead += "char* retval";
                }

            externfunctionhead += ")";
            // mheaders.Add(externfunctionhead + ";");

            externfunctionhead += "\r\n{\r\n";
            //externfunctionhead += "Con::printf(\"--------> wle_" + ed.m_name + "\");\r\n";
            //Ok, if the return type is a string or char* we expect a 4k block
            //otherwise it should be 1024.
            if (addStringReturnParamter)
                {
                if (ed.m_returntype.Contains("char"))
                    externfunctionhead += @"dSprintf(retval," + ConfigFiles.BufferReturn_Character + @","""");";
                else
                    externfunctionhead += @"dSprintf(retval,1024,"""");";
                }

            foreach (string ssparameter in parameters)
                {
                string parameter = ssparameter.Trim();

                if (parameter.Length > 0 && parameter.Trim().ToLower() != "void")
                    {
                    parameter = Helpers.getridofdoublespace(parameter);
                    int i = parameter.Trim().LastIndexOf(' ');
                    string ptype = parameter.Substring(0, i).Trim();
                    string pname = parameter.Substring(i).Trim();
                    externfunctionhead += "\r\n";

                    bool isobject = false;

                    if (!((ptype == "S32") || (ptype == "bool") || (ptype == "F32") || (ptype == "float") || (ptype == "U32") || (ptype == "int") || (ptype == "F64") || (ptype == "S8") || (ptype == "U8")))
                        {
                        if (MDntcConfig.PreGen_CPP_SimObjectBaseClasses.Contains(ptype.Replace("*", "")))
                            {
                            externfunctionhead += ptype + " " + pname + "; ";
                            externfunctionhead += "Sim::findObject(x__" + pname + ", " + pname + " ); ";
                            isobject = true;
                            }
                        else
                            {
                            //This is stupid, but for these objects we want a local object to the function,
                            //but pass it as a reference to the stock extern... yeah fucked up.
                            string ptyper = ptype.Replace("*", "");

                            if (MDntcConfig.PreGen_CPP_ObjParseDef.ContainsKey(ptyper))
                                {
                                string d = string.Format(MDntcConfig.PreGen_CPP_ObjParseDef[ptyper].deserializestring, ptyper, pname);
                                d = d.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\"", "\"");
                                externfunctionhead += d;
                                }
                            else
                                {
                                //mLogger.Stage32Log(ed.m_name, "<div style='background-color: red'>Failed</div>", "Missing function parameter type " + ptype + " <br> if it is a simobject add it to PreGen_CPP_SimObjectBaseClasses.cfg <br> otherwise define in PreGen_CPP_ObjParseDef.cfg");
                                errors.Add("Missing Type " + ptyper + " <br> if it is a simobject add it to 'C++ SimObject Based Classes' <br> otherwise define in 'C++ Class pInvoke Serializations'");
                                }
                            }
                        //Add in the validator to make sure the parameters passed are objects
                        //and return the correct type based on the return type of the 
                        //extern.
                        //Appears this is not needed.
                        if (isobject && pname == "object")
                            {
                            externfunctionhead += "\r\nif (!" + pname + ")\r\n";
                            if (MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(ed.m_returntype))
                                {
                                if ((MDntcConfig.PreGen_CPP_TypeConv[ed.m_returntype] == "S32") || (MDntcConfig.PreGen_CPP_TypeConv[ed.m_returntype] == "F32") || (MDntcConfig.PreGen_CPP_TypeConv[ed.m_returntype] == "U32") || (MDntcConfig.PreGen_CPP_TypeConv[ed.m_returntype] == "F64"))
                                    externfunctionhead += "\t return 0;";
                                }
                            else
                                externfunctionhead += "\t return;";
                            externfunctionhead += "\r\n";
                            }
                        }
                    }
                externfunctionhead += "\r\n";
                }

            //so, we gotta work through the body.
            //First case is that we are returning nothing, a void.
            //If it's a void, just return it.
            switch (ed.m_returntype)
                {
                    case "void":
                        externfunctionhead += ed.m_body;
                        break;
                    case "int":
                    case "U32":
                    case "float":
                    case "F32":
                    case "F64":
                    case "S32":

                        string sexternfunctionhead = "";
                        externfunctionhead += ed.m_body;
                        foreach (string spart in Regex.Split(externfunctionhead, ";"))
                            {
                            string part = spart;
                            //part = part.Replace(" return", "return (" + (ed.m_returntype == "F32" ? "F64" : ed.m_returntype) + ")(");
                            part = part.Replace(" return", "return (" + (ed.m_returntype) + ")(");
                            if (part.Contains(" return") || part.Contains(")return") || part.Contains("return (" + (ed.m_returntype) + ")("))
                                sexternfunctionhead += part + ");";
                            else
                                sexternfunctionhead += part + ";";
                            }
                        externfunctionhead = sexternfunctionhead;

                        break;
                    default:
                    {
                        //Create a local variable to handle the returned object.  This only gets used when 
                        //the return type is not a void or a primitive.
                        externfunctionhead += ed.m_returntype + " wle_returnObject;\r\n";
                        //Split the lines of code on the semi-colon since that marks a end of line.
                        //Also, lets remove any carriage return line feeds from the text as well, they will
                        //just confuse the parser.
                        string[] lines = ed.m_body.Replace("\r", "").Split('\n');
                        //now according to my logic... 
                        //if the line starts with "return " when need to replace it with "wle_returnObject ="

                        //foreach (string line in lines)
                        for (int i = 0; i < lines.Count(); i++)
                            {
                            string line = lines[i];
                            if (line.Trim() == "return String();")
                                externfunctionhead += "return;\r\n";
                            else
                                {
                                string tl = line;
                                if (tl.IndexOf('"') >= 0)
                                    {
                                    //char prevchar = ' ';
                                    bool inquote = false;
                                    string last5 = "";
                                    string nla = "";
                                    for (int uu = 0; uu < tl.Length; uu++)
                                        {
                                        if (tl[uu] == '"')
                                            inquote = !inquote;
                                        nla += tl[uu];

                                        last5 += tl[uu];
                                        if (last5.Length > 6)
                                            last5 = last5.Substring(1, 6);
                                        if (last5.ToLower() == "return")
                                            {
                                            if (inquote)
                                                nla = nla.Substring(0, nla.Length - 6);
                                            }
                                        }
                                    tl = nla;
                                    }

                                if (tl.Contains("return ") || (tl.Contains("return(")))
                                    {
                                    bool cf = false;
                                    if (tl.IndexOf("//", StringComparison.Ordinal) > -1)
                                        {
                                        tl = tl.Substring(0, tl.IndexOf("//", StringComparison.Ordinal));
                                        if (!(tl.Contains("return ") || (tl.Contains("return("))))
                                            {
                                            externfunctionhead += line + "\r\n";
                                            cf = true;
                                            }
                                        }

                                    if (!cf)
                                        {
                                        bool exitsearch = false;
                                        if (tl.IndexOf("return ", StringComparison.Ordinal) > 0)
                                            {
                                            do
                                                {
                                                int bracecount = 0;
                                                for (int o = tl.IndexOf("return ", StringComparison.Ordinal) + 7; o < tl.Length; o++)
                                                    {
                                                    if (tl[o] == '(')
                                                        bracecount++;
                                                    if (tl[o] == ')')
                                                        bracecount--;
                                                    }
                                                if (bracecount != 0)
                                                    {
                                                    i++;
                                                    tl += lines[i];
                                                    }
                                                else
                                                    exitsearch = true;
                                                }
                                            while (!exitsearch);
                                            }

                                        tl = tl.Replace("return ", "{wle_returnObject =") + "\r\n";

                                        externfunctionhead += tl.Replace("return(", "{wle_returnObject =(") + "\r\n";

                                        bool donext = true;

                                        //if the return type can translate to a S32, then just cast it and go on.
                                        if (MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(ed.m_returntype.Trim()))
                                            {
                                            if (MDntcConfig.PreGen_CPP_TypeConv[ed.m_returntype.Trim()] == "S32")
                                                {
                                                externfunctionhead += "return (S32)(wle_returnObject);";
                                                donext = false;
                                                }
                                            }
                                        if (donext)
                                            {
                                            //Add the easy out, if it's an object and its not set, then bail out.
                                            if (MDntcConfig.PreGen_CPP_SimObjectBaseClasses.Contains(ed.m_returntype.Replace("*", "").Trim()) || ed.m_returntype.ToLower().Replace(" ", "").Trim() == "constchar*")
                                                externfunctionhead += "\r\nif (!wle_returnObject) \r\nreturn;\r\n";

                                            if (MDntcConfig.PreGen_CPP_SimObjectBaseClasses.Contains(ed.m_returntype.Replace("*", "").Trim()))
                                                {
                                                //The object is simobject based, so we are just going to return the ID
                                                externfunctionhead += "dSprintf(retval,1024,\"%i\",wle_returnObject->getId());\r\nreturn;\r\n";
                                                }
                                            else
                                                {
                                                if (MDntcConfig.PreGen_CPP_ObjParseDef.ContainsKey(ed.m_returntype.Replace("*", "").Trim()))
                                                    externfunctionhead += MDntcConfig.PreGen_CPP_ObjParseDef[ed.m_returntype.Replace("*", "").Trim()].serializestring.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\"", "\"");
                                                else
                                                    {
                                                    //mLogger.Stage32Log(ed.m_name, "<div style='background-color: red'>Failed</div>", "Missing function Return type " + ed.m_returntype);
                                                    errors.Add("Missing function Return type " + ed.m_returntype);
                                                    }
                                                }
                                            }
                                        externfunctionhead += "}\r\n";
                                        }
                                    }
                                else
                                    externfunctionhead += line + "\r\n";
                                }
                            }
                    }
                        break;
                }

            externfunctionhead += "\r\n}";

            if (errors.Count == 0)
                {
                mLogger.NewEvent("", "(FINISHED) Processing Extern Data for " + ed.m_returntype + " " + ed.m_objecttype + "::" + ed.m_name + "(" + ed.m_params + ").");
                //mLogger.Stage32Log(ed.m_name, "<div style='background-color: green'>Success</div>", "" + ed.m_returntype);
                }
            else
                {
                //mLogger.HTML_Report_Stage_3_Part_2.Clear();
                foreach (string error in errors)
                    {
                    mLogger.NewErrorEvent("", error);
                    //mLogger.Stage32LogError(error);
                    }
                mLogger.NewEvent("", "(FINISHED WITH ERRORS) Processing Extern Data for " + ed.m_returntype + " " + ed.m_objecttype + "::" + ed.m_name + "(" + ed.m_params + ").");
                throw new Exception("Error:  PRocessing Extern Data, event in log.");
                }

            return externfunctionhead.Replace("\r\n\r\n", "\r\n").Replace("\r\n\r\n", "\r\n");
        }
    }
}