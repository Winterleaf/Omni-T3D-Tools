using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SharedServices.Interrogator.Support_Classes.Configuration.Data_Classes;
using Winterleaf.SharedServices.Properties;

namespace Winterleaf.SharedServices.Interrogator.Configuration
{
    public class ConfigFiles
    {
        public static int BufferReturn_Character = 16384; //see PreGen_CPP_ObjParseDef.cfg line 110 and 33
        public static int BufferReturn_Object = 1024;

        public Dictionary<string, string> ObjectParent = new Dictionary<string, string>();
        //public Dictionary<string, string> PosGen_CPP_FindReplace = new Dictionary<string, string>();
        public Dictionary<string, string> PreGen_CPP_Constants = new Dictionary<string, string>();

        public Dictionary<string, CPPObjectFunctions> PreGen_CPP_IgnoreClassFunction = new Dictionary<string, CPPObjectFunctions>();

        public Dictionary<string, CPPObjectSerializeDeserializeDef> PreGen_CPP_ObjParseDef = new Dictionary<string, CPPObjectSerializeDeserializeDef>();

        public List<string> PreGen_CPP_SimObjectBaseClasses = new List<string>();
        public Dictionary<string, string> PreGen_CPP_TypeConv = new Dictionary<string, string>();

        public Dictionary<string, CPPToCSharpClassMap> PreGen_CS__TypeConvCPPtoCS = new Dictionary<string, CPPToCSharpClassMap>();

        public List<string> PreGen_IgnoreSourceFiles = new List<string>();
        public List<string> PreGen_IgnoreSourceFilesForEnumeration = new List<string>();
        private Logger.Logger _mLogger;

        public ConfigFiles(ref Logger.Logger log)
        {
            _mLogger = log;
        }

        private string ReadFileRemoveComments(string filename, bool preservelinebreaks)
        {
            StringBuilder filecontents = new StringBuilder();
            using (TextReader sr = new StreamReader(filename))
                {
                string line;
                while ((line = sr.ReadLine()) != null)
                    {
                    if (!line.StartsWith("///"))
                        {
                        if (preservelinebreaks)
                            filecontents.Append(line + "\r\n");
                        else
                            filecontents.Append(line);
                        }
                    }
                }
            return filecontents.ToString().Replace("\t", " ");
        }

        private string RemoveComments(string text, bool preservelinebreaks)
        {
            StringBuilder filecontents = new StringBuilder();
            foreach (string line in Regex.Split(text, "\r\n"))
                {
                if (!line.StartsWith("///"))
                    {
                    if (preservelinebreaks)
                        filecontents.Append(line + "\r\n");
                    else
                        filecontents.Append(line);
                    }
                }

            //var filecontents = new StringBuilder();
            //using (TextReader sr = new StreamReader(filename))
            //    {
            //    string line;
            //    while ((line = sr.ReadLine()) != null)
            //        {
            //        if (!line.StartsWith("///"))
            //            if (preservelinebreaks)
            //                filecontents.Append(line + "\r\n");
            //            else
            //                filecontents.Append(line);
            //        }
            //    }
            return filecontents.ToString().Replace("\t", " ");
        }

        public void LoadConfig()
        {
            _mLogger.SectionStart("Reading Configuration Files");

            _mLogger.onProgressSubChange(0, "C++ Class pInvoke Serializations");
            _mLogger.SubSectionStart("C++ Class pInvoke Serializations");
            LoadPreGen_CPP_ObjParseDef(ref _mLogger);

            _mLogger.onProgressSubChange(.1, "C++ Class/Enum Map To C# Class/Enum");

            _mLogger.SubSectionStart("C++ Class/Enum Map To C# Class/Enum");
            LoadPreGen_CS__TypeConvCPPtoCS(ref _mLogger);
            _mLogger.onProgressSubChange(.2, "C++ Return Type Casting Overrides");

            _mLogger.SubSectionStart("C++ Return Type Casting Overrides");
            LoadPreGen_CPP_TypeConv(ref _mLogger);
            _mLogger.onProgressSubChange(.3, "C++ SimObject Based Classes");

            _mLogger.SubSectionStart("C++ SimObject Based Classes");
            LoadPreGen_CPP_SimObjectBaseClasses(ref _mLogger);
            //_mLogger.onProgressSubChange(.4, "PosGen_CPP_FindReplace");

            //_mLogger.SubSectionStart("PosGen_CPP_FindReplace");
            //LoadPosGen_CPP_FindReplace(ref _mLogger);
            _mLogger.onProgressSubChange(.5, "C++ Class/Function Ignores");

            _mLogger.SubSectionStart("C++ Class/Function Ignores");
            LoadPreGen_CPP_IgnoreClassFunction(ref _mLogger);
            _mLogger.onProgressSubChange(.6, "C++ Source Files To Ignore On Interrogation");

            _mLogger.SubSectionStart("C++ Source Files To Ignore On Interrogation");
            LoadPreGen_IgnoreSourceFiles(ref _mLogger);
            _mLogger.onProgressSubChange(.7, "C++ Source Files To Ignore For Enumeration Parsing");

            _mLogger.SubSectionStart("C++ Source Files To Ignore For Enumeration Parsing");
            LoadIgnoreSourceFilesForEnumeration(ref _mLogger);
            _mLogger.onProgressSubChange(.8, "C++ Constants");

            LoadPreGen_CPP_Constants(ref _mLogger);
            _mLogger.onProgressSubChange(.9, "Finished");

            _mLogger.SectionEnd();
        }

        public void pruneparentclasses()
        {
            _mLogger.SectionStart("Pruning Garbage Parent Class Information");
            List<string> invalidclasses = ObjectParent.Keys.Where(cppclass => !IsParentSimObject(ObjectParent[cppclass])).ToList();

            double total = invalidclasses.Count;
            double pos = 0;

            foreach (string invalidclass in invalidclasses)
                {
                pos += 1;
                _mLogger.onProgressSubChange(pos/total, "invalidclass");
                ObjectParent.Remove(invalidclass);
                _mLogger.Stage14Log("", "Removing classname '" + invalidclass + "' from Parent Tree.");
                }
            ObjectParent.Add("SimObject", "");
            _mLogger.SectionEnd();
        }

        private bool IsParentSimObject(String Parent)
        {
            if (!ObjectParent.ContainsKey(Parent))
                return false;
            if (Parent == ObjectParent[Parent])
                return false;
            if (Parent.ToLower() == "simobject")
                return true;
            return ObjectParent.ContainsKey(Parent) && IsParentSimObject(ObjectParent[Parent]);
        }

        public string DumpClassInheritance()
        {
            return DumpClassInheritanceRecurse("");
        }

        private string DumpClassInheritanceRecurse(string toSearchFor, int depth = 0)
        {
            string result = "\r\n";
            for (int i = 0; i < depth; i++)
                result += "--";
            result += ">" + toSearchFor;

            List<string> members = new List<string>();
            foreach (KeyValuePair<string, string> klass in ObjectParent)
                {
                if (klass.Value == toSearchFor)
                    members.Add(klass.Key);
                }

            foreach (string m in members)
                result += DumpClassInheritanceRecurse(m, depth + 1);
            return result;
        }

        #region Load Text Configuration Files

        private void LoadPreGen_CPP_ObjParseDef(ref Logger.Logger logger)
        {
            #region PreGen_CPP_ObjParseDef

            //string filename = Path.GetDirectoryName(Application.ExecutablePath) + "\\Templates\\Configuration\\PreGen_CPP_ObjParseDef.cfg";
        logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ Class pInvoke Serializations");
            try
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ Class pInvoke Serializations");
                //string data = ReadFileRemoveComments(filename, true);

                string data = RemoveComments(Settings.Default.PreGen_CPP_ObjParseDef_cfg, true);

                string[] defs = Regex.Split(data, "#ObjectType#=");
                foreach (string def in defs)
                    {
                    if (def.Length < 10)
                        continue;
                    string typename = def.Substring(0, def.IndexOf("\r\n", StringComparison.Ordinal));
                    string deserializestring = Regex.Split(Regex.Split(def, "#DeserializeString#=")[1], "#SerializeString#")[0];
                    string serializestring = Regex.Split(Regex.Split(def, "#SerializeString#=")[1], "#IsObject#")[0];
                    string isobject = Regex.Split(def, "#IsObject#=")[1].Substring(0, 1);
                    PreGen_CPP_ObjParseDef.Add(typename, new CPPObjectSerializeDeserializeDef(deserializestring, serializestring, isobject != "0"));
                    logger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, "C++ Class pInvoke Serializations", "Loaded Configuration data for: '" + typename + "'");
                    }
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Class pInvoke Serializations");
                logger.NewConfigEvent(Logger.Logger.EventStatus.SUCCESS, "C++ Class pInvoke Serializations");
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Class pInvoke Serializations");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ Class pInvoke Serializations", err.Message + err.StackTrace);
                throw;
                }

            #endregion
        }

        private void LoadPreGen_CS__TypeConvCPPtoCS(ref Logger.Logger logger)
        {
            //string filename = Path.GetDirectoryName(Application.ExecutablePath) + "\\Templates\\Configuration\\PreGen_CS__TypeConvCPPtoCS.cfg";
        logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ Class/Enum Map To C# Class/Enum");
            try
                {
                //data = ReadFileRemoveComments(filename, false);
                string data = RemoveComments(Settings.Default.PreGen_CS__TypeConvCPPtoCS_cfg, false);
                foreach (string line in data.Split(';').Where(line => line.Trim() != ""))
                    {
                    CPPEntityType ti;
                    switch (line.Split(' ')[2].ToLower())
                        {
                            case "class":
                                ti = CPPEntityType.Class;
                                break;
                            case "enum":
                                ti = CPPEntityType.Enum;
                                break;
                            default:
                                throw new Exception("Unknow Type");
                                break;
                        }

                    PreGen_CS__TypeConvCPPtoCS.Add(line.Split(' ')[0], new CPPToCSharpClassMap(line.Split(' ')[1], ti));
                    logger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, "C++ Class/Enum Map To C# Class/Enum", "Loaded Configuration data for: '" + line.Split(' ')[0] + "'");
                    }
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Class/Enum Map To C# Class/Enum");
                logger.NewConfigEvent(Logger.Logger.EventStatus.SUCCESS, "C++ Class/Enum Map To C# Class/Enum");
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Class/Enum Map To C# Class/Enum");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ Class/Enum Map To C# Class/Enum", err.Message + err.StackTrace);
                throw;
                }
        }

        private void LoadPreGen_CPP_TypeConv(ref Logger.Logger logger)
        {
            //string filename = Path.GetDirectoryName(Application.ExecutablePath) +"\\Templates\\Configuration\\PreGen_CPP_TypeConv.cfg";
        logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ Return Type Casting Overrides");
            try
                {
                //data = ReadFileRemoveComments(filename, false);
                string data = RemoveComments(Settings.Default.PreGen_CPP_TypeConv_cfg, false);
                foreach (string line in data.Split(';').Where(line => line.Trim() != ""))
                    {
                    PreGen_CPP_TypeConv.Add(line.Split(' ')[0].Trim(), line.Split(' ')[1].Trim());
                    logger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, "C++ Return Type Casting Overrides", "Loaded Configuration data for: '" + line.Split(' ')[0] + "'");
                    }
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Return Type Casting Overrides");
                logger.NewConfigEvent(Logger.Logger.EventStatus.SUCCESS, "C++ Return Type Casting Overrides");
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Return Type Casting Overrides");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ Return Type Casting Overrides", err.Message + err.StackTrace);
                throw;
                }
        }

        private void LoadPreGen_CPP_SimObjectBaseClasses(ref Logger.Logger logger)
        {
            //string filename = Path.GetDirectoryName(Application.ExecutablePath) +"\\Templates\\Configuration\\PreGen_CPP_SimObjectBaseClasses.cfg";
        logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ SimObject Based Classes");
            try
                {
                //data = ReadFileRemoveComments(filename, false);
                string data = RemoveComments(Settings.Default.PreGen_CPP_SimObjectBaseClasses_cfg, false);
                foreach (string line in data.Split(' ').Where(line => line.Trim() != ""))
                    {
                    PreGen_CPP_SimObjectBaseClasses.Add(line.Replace("\n", ""));
                    logger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, "C++ SimObject Based Classes", "Loaded Configuration data for: '" + line.Replace("\n", "") + "'");
                    }
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ SimObject Based Classes");
                logger.NewConfigEvent(Logger.Logger.EventStatus.SUCCESS, "C++ SimObject Based Classes");
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ SimObject Based Classes");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ SimObject Based Classes", err.Message + err.StackTrace);
                throw;
                }
        }

        private void LoadPreGen_CPP_IgnoreClassFunction(ref Logger.Logger logger)
        {
            string data = "";
            //string filename = Path.GetDirectoryName(Application.ExecutablePath) +
            //                  "\\Templates\\Configuration\\PreGen_CPP_IgnoreClassFunction.cfg";
            try
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ Class/Function Ignores");
                //data = ReadFileRemoveComments(filename, false);
                data = RemoveComments(Settings.Default.PreGen_CPP_IgnoreClassFunction_cfg, false);
                foreach (string VARIABLE in data.Split(';'))
                    {
                    if (VARIABLE.Trim() == "")
                        continue;

                    string aclass = VARIABLE.Split(':')[0].Trim().ToLower();
                    string afunction = VARIABLE.Split(':')[1].Trim().ToLower();
                    if (!PreGen_CPP_IgnoreClassFunction.ContainsKey(aclass))
                        PreGen_CPP_IgnoreClassFunction.Add(aclass, new CPPObjectFunctions());
                    if (!PreGen_CPP_IgnoreClassFunction[aclass].functions.Contains(afunction))
                        {
                        PreGen_CPP_IgnoreClassFunction[aclass].functions.Add(afunction);
                        logger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, "C++ Class/Function Ignores", "Adding Ignore for Class: '" + aclass + "' Function: '" + afunction + "'");
                        }
                    }
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Class/Function Ignores");
                logger.NewConfigEvent(Logger.Logger.EventStatus.SUCCESS, "C++ Class/Function Ignores");
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Class/Function Ignores");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ Class/Function Ignores", err.Message + err.StackTrace);
                throw;
                }
        }

        private void LoadPreGen_IgnoreSourceFiles(ref Logger.Logger logger)
        {
            string data = "";
            //string filename = Path.GetDirectoryName(Application.ExecutablePath) +
            //                  "\\Templates\\Configuration\\PreGen_IgnoreSourceFiles.cfg";
            try
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ Source Files To Ignore On Interrogation");
                //data = ReadFileRemoveComments(filename, false);
                data = RemoveComments(Settings.Default.PreGen_IgnoreSourceFiles_cfg, false);
                foreach (string VARIABLE in data.Split(';'))
                    {
                    if (VARIABLE.Trim() == "")
                        continue;
                    string namepart = VARIABLE.Replace("\r", "").Replace("\n", "");
                    if (!PreGen_IgnoreSourceFiles.Contains(namepart))
                        {
                        PreGen_IgnoreSourceFiles.Add(namepart.ToLower());
                        logger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, "C++ Source Files To Ignore On Interrogation", "Adding Ignore for any FILE WITH: '" + namepart + "' in it.");
                        }
                    }
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Source Files To Ignore On Interrogation");
                logger.NewConfigEvent(Logger.Logger.EventStatus.SUCCESS, "C++ Source Files To Ignore On Interrogation");
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Source Files To Ignore On Interrogation");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ Source Files To Ignore On Interrogation", err.Message + err.StackTrace);
                throw;
                }
        }

        private void LoadIgnoreSourceFilesForEnumeration(ref Logger.Logger logger)
        {
            string data = "";
            //string filename = Path.GetDirectoryName(Application.ExecutablePath) +
            //                  "\\Templates\\Configuration\\PreGen_IgnoreSourceFilesForEnumeration.cfg";
            try
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.START, "C++ Source Files To Ignore For Enumeration Parsing");
                //data = ReadFileRemoveComments(filename, false);
                data = RemoveComments(Settings.Default.PreGen_IgnoreSourceFilesForEnumeration_cfg, false);
                foreach (string VARIABLE in data.Split(';'))
                    {
                    if (VARIABLE.Trim() == "")
                        continue;
                    string namepart = VARIABLE.Replace("\r", "").Replace("\n", "");
                    if (!PreGen_IgnoreSourceFiles.Contains(namepart))
                        {
                        PreGen_IgnoreSourceFilesForEnumeration.Add(namepart.ToLower());
                        logger.NewConfigEvent(Logger.Logger.EventStatus.DETAIL, "C++ Source Files To Ignore For Enumeration Parsing", "(ENUMERATIONS) Adding Ignore for any FILE WITH: '" + namepart + "' in it.");
                        }
                    }
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Source Files To Ignore For Enumeration Parsing");
                logger.NewConfigEvent(Logger.Logger.EventStatus.SUCCESS, "C++ Source Files To Ignore For Enumeration Parsing");
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Source Files To Ignore For Enumeration Parsing");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ Source Files To Ignore For Enumeration Parsing", err.Message + err.StackTrace);
                throw;
                }
        }

        private void LoadPreGen_CPP_Constants(ref Logger.Logger logger)
        {
            string data = "";
            //string filename = Path.GetDirectoryName(Application.ExecutablePath) +
            //                  "\\Templates\\Configuration\\PreGen_CPP_Constants.cfg";
            try
                {
                data = RemoveComments(Settings.Default.PreGen_CPP_Constants_cfg, false);
                //data = ReadFileRemoveComments(filename, false);
                foreach (string sline in Regex.Split(data, "##END##"))
                    {
                    string line = sline.Trim();
                    if (line == "")
                        continue;
                    string tag = line.Substring(0, line.IndexOf("##BEGIN##")).Trim();
                    string value = line.Substring(line.IndexOf("##BEGIN##") + "##BEGIN##".Length).Trim();
                    PreGen_CPP_Constants.Add(tag, value);
                    }
                }
            catch (Exception err)
                {
                logger.NewConfigEvent(Logger.Logger.EventStatus.END, "C++ Constants");
                logger.NewConfigEvent(Logger.Logger.EventStatus.ERROR, "C++ Constants", err.Message + err.StackTrace);
                throw;
                }
        }

        #endregion
    }
}