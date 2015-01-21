using System;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using Winterleaf.SharedServices.Interrogator.Configuration;
using Winterleaf.SharedServices.Interrogator.cSharp_Generators;
using Winterleaf.SharedServices.Interrogator.Parsing;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Winterleaf.SharedServices.Interrogator
{
    public class Interrogator
    {
        private static Interrogator _self;

        private readonly string _mCPPProjectFile;
        private readonly string _mCSharpProjectFile;
        private readonly Logger.Logger.NewEventRecorded _mFinished;
        private readonly string _mUserNamespace;
        private readonly string _mUsercSharp;
        private CodeParsing _mCodeParsing;
        private ConfigFiles _mConfigFiles;
        private Project _mEngineProject;
        private Enumerations _mEnumerations;
        private Project _mGameLogic;
        private Generator_Auto _mGenerator_Auto;
        //private Generator_GlobalCallbacks _mGenerator_GlobalCallbacks;
        private Generator_ProxyClasses _mGenerator_ProxyClasses;
        private Generator_SafeNativeMethods _mGenerator_SafeNativeMethods;
        private Generator_CPP _mGenerator_cpp;
        private Generator_pInvokes _mGenerator_pInvokes;
        private Logger.Logger.NewEventRecorded _mNewEventRecorded;
        private Logger.Logger.ProgressChange _mProgressChange;
        private Logger.Logger.ProgressChange _mProgressSubChange;
        private bool _misRunning;
        private Logger.Logger _mlogger;
        private ProjectItem piProxyObjectsBase = null;
        private ProjectItem piuserObjectsProxyObjects = null;

        public Interrogator(Logger.Logger.NewEventRecorded cbNewEvent, Logger.Logger.NewEventRecorded cbfinished, Logger.Logger.ProgressChange cbProgress, Logger.Logger.ProgressChange cbSubProgress, string cppProjectFile, string cSharpProjectfile, string userCSharp, string UserNamespace)
        {
            _self = this;
            _mNewEventRecorded = cbNewEvent;
            _mFinished = cbfinished;
            _mCPPProjectFile = cppProjectFile;
            _mCSharpProjectFile = cSharpProjectfile;
            _mProgressChange = cbProgress;
            _mProgressSubChange = cbSubProgress;
            _mUsercSharp = userCSharp;
            _mUserNamespace = UserNamespace;
        }

        public static Interrogator self
        {
            get { return _self; }
        }

        public Project mCSProject_Engine
        {
            get { return _mEngineProject; }
            set { _mEngineProject = value; }
        }

        public Project mCSProject_GameLogic
        {
            get { return _mGameLogic; }
            set { _mGameLogic = value; }
        }

        public Logger.Logger mLogger
        {
            get { return _mlogger; }
            set { _mlogger = value; }
        }

        public ConfigFiles mConfigFiles
        {
            get { return _mConfigFiles; }
            set { _mConfigFiles = value; }
        }

        public bool mIsRunning
        {
            get { return _misRunning; }
            set { _misRunning = value; }
        }

        internal bool findProjectItem(ProjectItems items, string filename, out ProjectItem item)
        {
            foreach (ProjectItem pi in items)
                {
                string path = pi.Properties.Item("FullPath").Value.ToString();

                if (path.ToLower().EndsWith(filename.ToLower()))
                    {
                    item = pi;
                    return true;
                    }
                if (pi.ProjectItems.Count > 0)
                    {
                    ProjectItem piout;
                    if (findProjectItem(pi.ProjectItems, filename, out piout))
                        {
                        item = piout;
                        return true;
                        }
                    }
                }
            item = null;
            return false;
        }

        private void onmFinished()
        {
            if (_mFinished != null)
                _mFinished("");
        }

        public void Start()
        {
            if (!mIsRunning)
                {
                mIsRunning = true;
                Thread thread = new Thread(_Start);
                thread.Start();
                }
        }

        private void _Start()
        {
            try
                {
                mLogger = new Logger.Logger(ref _mNewEventRecorded, ref _mProgressChange, ref _mProgressSubChange);
                if (mCSProject_GameLogic != null)
                    {
                    try
                        {
                        ProjectItem pi = null;
                        if (findProjectItem(mCSProject_GameLogic.ProjectItems, constants.fileLocations.ProxyObjects_Base, out pi))
                            {
                            mLogger.onProgressSubChange(1, "Deleteing old proxy base files.");
                            int totalItems = pi.ProjectItems.Count;
                            int counter = 1;
                            while (pi.ProjectItems.Count > 0)
                                {
                                mLogger.onProgressChange(((float) counter)/((float) totalItems), "Removing file (\"" + pi.ProjectItems.Item(1).Name + "\")");
                                pi.ProjectItems.Item(1).Delete();
                                counter++;
                                }
                            }

                        if (findProjectItem(mCSProject_GameLogic.ProjectItems, constants.fileLocations.userObjects_ProxyObjects, out pi))
                            {
                            mLogger.onProgressSubChange(1, "Deleteing old user proxy files.");
                            int totalItems = pi.ProjectItems.Count;
                            int counter = 1;
                            while (pi.ProjectItems.Count > 0)
                                {
                                mLogger.onProgressChange(((float) counter)/((float) totalItems), "Removing file (\"" + pi.ProjectItems.Item(1).Name + "\")");
                                pi.ProjectItems.Item(1).Delete();
                                counter++;
                                }
                            }
                        }
                    catch (Exception er)
                        {
                        mLogger.onProgressSubChange(1, "");
                        mLogger.onProgressChange(1, "A Error has occurred, Please check the log.");
                        mLogger._mLog.Append(er.Message);

                        onmFinished();
                        }
                    }

                mLogger.onProgressChange(.0, "Loading Configuration Files...");

                _mConfigFiles = new ConfigFiles(ref _mlogger);
                _mConfigFiles.LoadConfig();
                mLogger.onProgressChange(.10, "Parsing C++ Files...");

                _mCodeParsing = new CodeParsing(ref _mlogger, ref _mConfigFiles, _mCPPProjectFile);
                _mCodeParsing.Start();
                mLogger.onProgressChange(.20, "Generating Enumerations....");

                _mEnumerations = new Enumerations(ref _mCodeParsing, ref _mConfigFiles, ref _mlogger);
                _mEnumerations.Start(_mCSharpProjectFile);

                mLogger.onProgressChange(.30, "Pruning Classes not needed...");

                _mConfigFiles.pruneparentclasses();

                if (self.mCSProject_Engine != null)
                    {
                    string newfilename = "ClassInheritanceLog" + DateTime.Now.ToString("MMddyyyymmss") + ".txt";
                    newfilename = Path.Combine(Path.GetDirectoryName(mCSProject_Engine.FullName), newfilename);
                    using (StreamWriter file = new StreamWriter(newfilename, false))
                        file.WriteLine(_mConfigFiles.DumpClassInheritance());
                    ProjectItem ni = mCSProject_Engine.ProjectItems.AddFromFile(newfilename);
                    ni.Open();
                    // System.Diagnostics.Process.Start("notepad.exe", newfilename);
                    }
                else
                    {
                    if (!Directory.Exists(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Logs")))
                        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Logs"));
                    string timestamp = DateTime.Now.ToString("yyyy MMMM dd mm ss");
                    string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Logs\\ClassStructure" + timestamp + ".txt");

                    using (StreamWriter file = new StreamWriter(path, false))
                        file.WriteLine(_mConfigFiles.DumpClassInheritance());
                    Process.Start("notepad.exe", path);
                    }

                mLogger.onProgressChange(.40, "Generating C++ (Source)...");

                _mGenerator_cpp = new Generator_CPP(ref _mlogger, ref _mConfigFiles, ref _mCodeParsing);
                _mGenerator_cpp.Start();

                mLogger.onProgressChange(.60, "Generating C# (SafeNativeMethods)...");

                _mGenerator_SafeNativeMethods = new Generator_SafeNativeMethods(_mCSharpProjectFile, ref _mlogger, ref _mCodeParsing, ref _mConfigFiles);
                _mGenerator_SafeNativeMethods.Start();
                mLogger.onProgressChange(.70, "Generating C# (Auto's)...");

                _mGenerator_Auto = new Generator_Auto(_mCSharpProjectFile, ref _mlogger, ref _mCodeParsing, ref _mConfigFiles);
                _mGenerator_Auto.Start();
                mLogger.onProgressChange(.80, "Generating C# (pInvokes)...");

                _mGenerator_pInvokes = new Generator_pInvokes(_mCSharpProjectFile, ref _mlogger, ref _mCodeParsing, ref _mConfigFiles);
                _mGenerator_pInvokes.Start();

                mLogger.onProgressChange(.90, "Generating C# (Model Classes)....");
                _mGenerator_ProxyClasses = new Generator_ProxyClasses(_mCSharpProjectFile, ref _mlogger, ref _mConfigFiles, ref _mCodeParsing, _mUsercSharp, _mUserNamespace);
                _mGenerator_ProxyClasses.Start();

                //mLogger.onProgressChange(.95, "Generating C# (Global Callbacks)...");
                //_mGenerator_GlobalCallbacks = new Generator_GlobalCallbacks(_mCSharpProjectFile, ref _mlogger, ref _mCodeParsing, ref _mConfigFiles);
                //_mGenerator_GlobalCallbacks.Start();

                mLogger.onProgressChange(1);

                mIsRunning = false;

                onmFinished();
                }
            catch (Exception er)
                {
                mLogger.onProgressSubChange(1, "A Error has occurred, Please check the log.");
                mLogger.onProgressChange(1, "A Error has occurred, Please check the log.");
                mLogger._mLog.Append(er.Message);

                onmFinished();
                }
            finally
                {
                if (self.mCSProject_Engine != null)
                    {
                    string newfilename = "StaticCodeAnalyserRunLog" + DateTime.Now.ToString("MMddyyyymmss") + ".txt";
                    newfilename = Path.Combine(Path.GetDirectoryName(mCSProject_Engine.FullName), newfilename);
                    using (StreamWriter file = new StreamWriter(newfilename, false))
                        file.WriteLine(mLogger._mLog.ToString());
                    ProjectItem nii = mCSProject_Engine.ProjectItems.AddFromFile(newfilename);
                    Window win;
                    win = nii.Open();
                    win.Visible = true;

                    // System.Diagnostics.Process.Start("notepad.exe", newfilename);
                    }
                else
                    {
                    string timestamp = DateTime.Now.ToString("yyyy MMMM dd mm ss");

                    string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Logs\\StaticCodeAnalyserRunLog" + timestamp + ".txt");

                    using (StreamWriter file = new StreamWriter(path, false))
                        file.WriteLine(mLogger._mLog.ToString());
                    Process.Start("notepad.exe", path);
                    }
                }
        }

        internal static class constants
        {
            internal static class fileLocations
            {
                public static string ProxyObjects_Base = "\\Models.Base\\";
                public static string userObjects_ProxyObjects = "\\Models.User\\Extendable\\";
            }

            internal static class general
            {
                public static string classPrefix = ""; //"co";
            }

            internal static class namespaces
            {
                public static string ProxyObjects_Base = "Models.Base";
                public static string userObjects_ProxyObjects = "Models.User.Extendable";
            }

        }
    }
}