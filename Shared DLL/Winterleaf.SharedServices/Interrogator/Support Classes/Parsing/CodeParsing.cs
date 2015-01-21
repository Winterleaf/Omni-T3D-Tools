using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Winterleaf.SharedServices.Interrogator.Configuration;
using Winterleaf.SharedServices.Interrogator.Containers;

namespace Winterleaf.SharedServices.Interrogator.Parsing
{
    public class CodeParsing
    {
        private readonly List<DefinedValues> _DefinedClasses = new List<DefinedValues>();
        private readonly string _mCPPProjectFile;
        private readonly Dictionary<string, string> _mConsoleTypes = new Dictionary<string, string>();
        private readonly List<DefinedValues> _mDefined = new List<DefinedValues>();

        private readonly List<ImplementGlobalCallback> _mGlobalCallbacks = new List<ImplementGlobalCallback>();
        private readonly List<ImplementCallback> _mImplementCallback = new List<ImplementCallback>();
        private readonly SortedDictionary<string, EnumData> _menumerations = new SortedDictionary<string, EnumData>();
        private ConfigFiles MDntcConfig;
        private List<InitPersistData> _InitPersists = new List<InitPersistData>();
        private List<string> _mClasses = new List<string>();
        private List<Externdata> _mData = new List<Externdata>();
        private List<string> _mFilesToRead = new List<string>();
        private Logger.Logger mLogger; // = new Logger();

        public CodeParsing(ref Logger.Logger logger, ref ConfigFiles configFiles, string CPPProjectFile)
        {
            mLogger = logger;
            MDntcConfig = configFiles;
            _mCPPProjectFile = CPPProjectFile;
        }

        public List<InitPersistData> Data_InitPersists
        {
            get { return _InitPersists; }
            // set { _InitPersists = value; }
        }

        public List<string> Data_Classes
        {
            get { return _mClasses; }
            //  set { _mClasses = value; }
        }

        public Dictionary<string, string> Data_ConsoleTypes
        {
            get { return _mConsoleTypes; }
            //   set { _mConsoleTypes = value; }
        }

        public SortedDictionary<string, EnumData> Data_Enumerations
        {
            get { return _menumerations; }
            //  set { _menumerations = value; }
        }

        public List<ImplementCallback> Data_ImplementCallback
        {
            get { return _mImplementCallback; }
            //  set { _mImplementCallback = value; }
        }

        public List<ImplementGlobalCallback> Data_GlobalCallbacks
        {
            get { return _mGlobalCallbacks; }
            //  set { _mGlobalCallbacks = value; }
        }

        public List<DefinedValues> Data_Defined
        {
            get { return _mDefined; }
            //   set { _mDefined = value; }
        }

        public List<DefinedValues> Data_DefinedClasses
        {
            get { return _DefinedClasses; }
            //  set { _DefinedClasses = value; }
        }

        public List<Externdata> Data_Data
        {
            get { return _mData; }
            set { _mData = value; }
        }

        public void Start()
        {
            mLogger.SectionStart("C++ Code Interrogation");
            ReadCFiles();
            mLogger.SectionEnd();
        }

        private void ReadCFiles()
        {
            string projectfile = _mCPPProjectFile;
            FileReaderCPP codereader = new FileReaderCPP();
            ReadProjectFiles(projectfile);

            codereader.ReadCFiles(_mFilesToRead, ref MDntcConfig, ref mLogger);

            Data_Data.AddRange(codereader._mData);
            Data_Data = Data_Data.OrderBy(x => x.m_name).ToList();

            _InitPersists.AddRange(codereader._midata);
            _InitPersists = _InitPersists.OrderBy(x => x.MName).ToList();

            _mClasses.AddRange(codereader.classes);
            _mClasses = Data_Classes.OrderBy(x => x).ToList();

            Data_ImplementCallback.AddRange(codereader._mImplementCallback);

            Data_GlobalCallbacks.AddRange(codereader._mGlobalCallbacks);

            Data_Defined.AddRange(codereader._mDefined);

            Data_DefinedClasses.AddRange(codereader._DefinedClasses);

            Dictionary<string, string> toadd = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> ct in codereader.mConsoleTypes)
                {
                if (!Data_ConsoleTypes.ContainsKey(ct.Key))
                    {
                    if (!toadd.ContainsKey(ct.Key))
                        toadd.Add(ct.Key, ct.Value);
                    }
                }

            foreach (KeyValuePair<string, string> ct in toadd)
                Data_ConsoleTypes.Add(ct.Key, ct.Value);

            SortedDictionary<string, EnumData> toadd1 = new SortedDictionary<string, EnumData>();

            foreach (KeyValuePair<string, EnumData> ct in codereader._menumerations)
                {
                if (!Data_Enumerations.ContainsKey(ct.Key))
                    {
                    if (!toadd1.ContainsKey(ct.Key))
                        toadd1.Add(ct.Key, ct.Value);
                    }
                }
            foreach (KeyValuePair<string, EnumData> ct in toadd1)
                Data_Enumerations.Add(ct.Key, ct.Value);
        }

        private void ReadProjectFiles(String projectfile)
        {
            _mFilesToRead = new List<string>();

            string filepath = projectfile.Substring(0, projectfile.LastIndexOf("\\"));

            TextReader sr = new StreamReader(projectfile);
            string data = sr.ReadToEnd();
            sr.Close();

            XDocument xdoc = XDocument.Parse(data);

            foreach (XElement ele in xdoc.Root.DescendantNodes().OfType<XElement>().Select(x => x).Distinct())
                {
                if (ele.Name.LocalName == "ClCompile" || ele.Name.LocalName == "ClInclude")
                    {
                    if (ele.Attribute("Include") != null)
                        {
                        if (ele.Attribute("Include").Value.StartsWith("..\\"))
                            {
                            string combined = filepath + "\\" + ele.Attribute("Include").Value;
                            string p = Path.GetFullPath(combined);
                            _mFilesToRead.Add(p);
                            }
                        else
                            _mFilesToRead.Add(ele.Attribute("Include").Value);
                        }
                    }
                if (ele.Name.LocalName == "AdditionalIncludeDirectories")
                    {
                    string[] temp = ele.Value.Split(';');
                    foreach (string t in temp)
                        {
                        if (!t.Contains('('))
                            {
                            string combine = t;
                            if (t.StartsWith("."))
                                combine = filepath + "\\" + t;
                            string p = Path.GetFullPath(combine);
                            if (Directory.Exists(p))
                                {
                                foreach (string f in Directory.GetFiles(p, "*.h", SearchOption.AllDirectories))
                                    {
                                    if (!_mFilesToRead.Contains(p))
                                        _mFilesToRead.Add(f);
                                    }
                                }
                            }
                        }
                    }
                }
        }
    }
}