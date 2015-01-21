using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using System.Windows.Input;

using Winterleaf.SharedServices.Interrogator;
using Winterleaf.SharedServices.Util;
using Winterleaf.SharedServices.Properties;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Forms;
using Winterleaf.SharedServices.About;

namespace Winterleaf.StaticCodeGenerator.Controls
    {
    /// <summary>
    /// Interaction logic for SolutionSelectorCtrl.xaml
    /// </summary>
    public partial class SolutionSelectorCtrl : UserControl, INotifyPropertyChanged
        {
        private List<SolutionProject> AvailProjectsCPP = new List<SolutionProject>();
        private List<SolutionProject> AvailProjectsCS = new List<SolutionProject>();
        private string _LogData = string.Empty;
        private bool _btn_Execute_isEnabled = false;
        private bool _btn_SelectOmniFrameworkSolutionFile_isEnabled = false;
        private bool _btn_SelectOmniT3DSolutionFile_IsEnabled = true;
        private bool _cb_CSharpLogicProject_isEnabled = false;

        private bool _cb_OmniCPPDLLProject_isEnabled = false;
        private bool _cb_WinterleafEngineProject_isEnabled = false;
        private string _m_Data_LocationCPPDLLL = string.Empty;
        private string _m_Data_LocationOfGameLogic = string.Empty;
        private string _m_Data_LocationOfGameLogicProject = string.Empty;
        private string _m_Data_LocationOfWinterleafEngineOmni = string.Empty;
        private string _m_Data_RootNamespace = string.Empty;
        private double _pb_Main = 0;
        private string _pb_Main_Tag = "";
        private double _pb_Sub = 0;
        private string _pb_Sub_Tag = "";
        private string _txt_MainMessage = "Begin!";
        private string _txt_SubMessage = "Begin!";
        private Interrogator gator;
        private string lastHeading = "";
        private string solutionpath = string.Empty;

        public SolutionSelectorCtrl()
            {
            InitializeComponent();
            }

        public string LogData
            {
            get { return _LogData; }
            set
                {
                _LogData = value;
                OnPropertyChanged("LogData");
                }
            }

        public Boolean btn_SelectOmniT3DSolutionFile_IsEnabled
            {
            get { return _btn_SelectOmniT3DSolutionFile_IsEnabled; }
            set
                {
                _btn_SelectOmniT3DSolutionFile_IsEnabled = value;
                OnPropertyChanged("btn_SelectOmniT3DSolutionFile_IsEnabled");
                }
            }

        public bool cb_OmniCPPDLLProject_isEnabled
            {
            get { return _cb_OmniCPPDLLProject_isEnabled; }
            set
                {
                _cb_OmniCPPDLLProject_isEnabled = value;
                OnPropertyChanged("cb_OmniCPPDLLProject_isEnabled");
                }
            }

        public bool btn_SelectOmniFrameworkSolutionFile_isEnabled
            {
            get { return _btn_SelectOmniFrameworkSolutionFile_isEnabled; }
            set
                {
                _btn_SelectOmniFrameworkSolutionFile_isEnabled = value;
                OnPropertyChanged("btn_SelectOmniFrameworkSolutionFile_isEnabled");
                }
            }

        public bool cb_WinterleafEngineProject_isEnabled
            {
            get { return _cb_WinterleafEngineProject_isEnabled; }
            set
                {
                _cb_WinterleafEngineProject_isEnabled = value;
                OnPropertyChanged("cb_WinterleafEngineProject_isEnabled");
                }
            }

        public bool cb_CSharpLogicProject_isEnabled
            {
            get { return _cb_CSharpLogicProject_isEnabled; }
            set
                {
                _cb_CSharpLogicProject_isEnabled = value;
                OnPropertyChanged("cb_CSharpLogicProject_isEnabled");
                }
            }

        public bool btn_Execute_isEnabled
            {
            get { return _btn_Execute_isEnabled; }
            set
                {
                _btn_Execute_isEnabled = value;
                OnPropertyChanged("btn_Execute_isEnabled");
                }
            }

        public double pb_Main
            {
            get { return _pb_Main; }
            set
                {
                _pb_Main = value;
                OnPropertyChanged("pb_Main");
                }
            }

        public double pb_Sub
            {
            get { return _pb_Sub; }
            set
                {
                _pb_Sub = value;
                OnPropertyChanged("pb_Sub");
                }
            }

        public string PbMainTag
            {
            get { return _pb_Main_Tag; }
            set
                {
                _pb_Main_Tag = value;
                OnPropertyChanged("PbMainTag");
                }
            }

        public string PbSubTag
            {
            get { return _pb_Sub_Tag; }
            set
                {
                _pb_Sub_Tag = value;
                OnPropertyChanged("PbSubTag");
                }
            }

        public string TxtMainMessage
            {
            get { return _txt_MainMessage; }
            set
                {
                _txt_MainMessage = value;
                OnPropertyChanged("TxtMainMessage");
                }
            }

        public string TxtSubMessage
            {
            get { return _txt_SubMessage; }
            set
                {
                _txt_SubMessage = value;
                OnPropertyChanged("TxtSubMessage");
                }
            }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
            {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
            }

        private void Btn_SelectOmniT3DSolutionFile_OnClick(object sender, RoutedEventArgs e)
            {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select the solution file for Omni T3D";
            openFileDialog1.Filter = "VS Solution files (*.sln)|*.sln";
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr != DialogResult.OK)
                return;

            btn_SelectOmniT3DSolutionFile.Content = "Selected OMNI T3D Solution File (" + Path.GetFileName(openFileDialog1.FileName) + ")";

            try
                {
                Winterleaf.SharedServices.Util.Solution s = new Winterleaf.SharedServices.Util.Solution(openFileDialog1.FileName);
                solutionpath = openFileDialog1.FileName;
                AvailProjectsCPP = s.Projects;
                cb_OmniCPPDLLProject.Items.Clear();
                foreach (SolutionProject p in s.Projects)
                    cb_OmniCPPDLLProject.Items.Add(p);
                cb_OmniCPPDLLProject_isEnabled = true;
                }
            catch (Exception)
                {
                }
            }

        private void Btn_SelectOmniFrameworkSolutionFile_OnClick(object sender, RoutedEventArgs e)
            {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select the solution file for Omni Framework";
            openFileDialog1.Filter = "VS Solution files (*.sln)|*.sln";
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr != DialogResult.OK)
                return;

            btn_SelectOmniFrameworkSolutionFile.Content = "Selected Omni Framework Solution File (" + Path.GetFileName(openFileDialog1.FileName) + ")";
            try
                {
                Winterleaf.SharedServices.Util.Solution s = new Winterleaf.SharedServices.Util.Solution(openFileDialog1.FileName);
                solutionpath = openFileDialog1.FileName;
                AvailProjectsCS = s.Projects;
                foreach (SolutionProject p in s.Projects)
                    cb_WinterleafEngineProject.Items.Add(p);
                cb_WinterleafEngineProject_isEnabled = true;
                cb_CSharpLogicProject_isEnabled = false;
                txt_Namespace.Text = "";
                }
            catch (Exception)
                {
                }
            }

        private void Cb_WinterleafEngineProject_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
            cb_CSharpLogicProject.Items.Clear();
            foreach (SolutionProject p in AvailProjectsCS)
                {
                if (p != cb_CSharpLogicProject.SelectedItem)
                    cb_CSharpLogicProject.Items.Add(p);
                }
            cb_CSharpLogicProject_isEnabled = true;
            }

        private void Cb_CSharpLogicProject_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
            try
                {
                _m_Data_LocationOfWinterleafEngineOmni = Path.GetDirectoryName(GetProjectPath(((SolutionProject)cb_WinterleafEngineProject.SelectedItem)));
                _m_Data_LocationCPPDLLL = Path.GetFullPath(GetProjectPath(((SolutionProject)cb_OmniCPPDLLProject.SelectedItem)));
                _m_Data_LocationOfGameLogic = Path.GetDirectoryName(GetProjectPath(((SolutionProject)cb_CSharpLogicProject.SelectedItem)));
                _m_Data_LocationOfGameLogicProject = Path.GetFullPath(GetProjectPath(((SolutionProject)cb_CSharpLogicProject.SelectedItem)));
                _m_Data_RootNamespace = FindRootNamespace(_m_Data_LocationOfGameLogicProject);
                txt_Namespace.Text = _m_Data_RootNamespace;
                btn_Execute_isEnabled = true;
                }
            catch (Exception)
                {
                }
            }

        private string GetProjectPath(SolutionProject sp)
            {
            string newpath = Path.Combine(Path.GetDirectoryName(solutionpath), sp.RelativePath);
            return Path.GetFullPath(newpath);
            }

        private string FindRootNamespace(string path)
            {
            string data = File.ReadAllText(path);
            string RootNamespace;
            if (GetCapture(data, "RootNamespace", "<RootNamespace>(?<RootNamespace>.*)</RootNamespace>", out RootNamespace))
                return RootNamespace;
            return "";
            }

        public static bool GetCapture(string text, string GroupName, string Regex, out string value)
            {
            value = "";
            Match match = System.Text.RegularExpressions.Regex.Match(text, Regex);
            if (match.Groups[GroupName].Captures.Count > 0)
                {
                value = match.Groups[GroupName].Captures[0].Value;
                return true;
                }
            return false;
            }

        private void NewEvent(string text)
            {
            //SetAppendData(rtb_Out, text);
            }

        private void Finished(string text)
            {
            btn_Execute_isEnabled = true;
            btn_SelectOmniFrameworkSolutionFile_isEnabled = true;
            btn_SelectOmniT3DSolutionFile_IsEnabled = true;

            cb_CSharpLogicProject_isEnabled = true;
            cb_OmniCPPDLLProject_isEnabled = true;
            cb_WinterleafEngineProject_isEnabled = true;

            TxtMainMessage = "Finished.";

            // LogData = gator.mLogger._mLog.ToString();
            }

        public void ProgressChange(double percent, String Text)
            {
            pb_Main = percent * 100.0;
            PbMainTag = (percent * 100.0).ToString("##") + "%";
            TxtMainMessage = Text;
            lastHeading = Text;
            }

        public void ProgressSubChange(double percent, String Text)
            {
            pb_Sub = percent * 100.0;
            PbSubTag = (percent * 100.0).ToString("##.###") + "%";
            TxtSubMessage = lastHeading + " (" + Text + ")";
            }

        private void Btn_Execute_OnClick(object sender, RoutedEventArgs e)
            {
            btn_Execute_isEnabled = false;
            btn_SelectOmniFrameworkSolutionFile_isEnabled = false;
            btn_SelectOmniT3DSolutionFile_IsEnabled = false;

            cb_CSharpLogicProject_isEnabled = false;
            cb_OmniCPPDLLProject_isEnabled = false;
            cb_WinterleafEngineProject_isEnabled = false;

            string _mCPPDLLProjectFile = _m_Data_LocationCPPDLLL;
            string _mRootNamespace = _m_Data_RootNamespace;
            string _mWinterleafEngineOmnifolder = _m_Data_LocationOfWinterleafEngineOmni;
            string _m_mCSGameLogicFolder = _m_Data_LocationOfGameLogic;

            gator = new Interrogator(NewEvent, Finished, ProgressChange, ProgressSubChange, _mCPPDLLProjectFile, _mWinterleafEngineOmnifolder, _m_mCSGameLogicFolder, _mRootNamespace);
            gator.Start();
            }

        private void Cb_OmniCPPDLLProject_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
            btn_SelectOmniFrameworkSolutionFile_isEnabled = true;
            }



        #region Configs
        private string _ConfigData = "";
        private string _SelectedConfigOption;
        private string _textHasChanged = "";

      

        public string ConfigData
            {
            get { return _ConfigData; }
            set
                {
                _ConfigData = value;
                OnPropertyChanged("ConfigData");
                }
            }

        public List<string> ConfigOptions
            {
            get
                {
                _configOptions.Sort();
                return _configOptions;
                }
            set { _configOptions = value; }
            }

        public string SelectedConfigOption
            {
            get { return _SelectedConfigOption; }
            set
                {
                _SelectedConfigOption = value;
                OnPropertyChanged("SelectedConfigOption");
                }
            }

        public string TextHasChanged
            {
            get { return _textHasChanged; }
            set
                {
                _textHasChanged = value;
                OnPropertyChanged("TextHasChanged");
                }
            }

        private List<String> _configOptions = new List<string>() {"C++ Constants", //"PreGen_CPP_Constants_cfg",
            "C++ Class/Function Ignores", //"PreGen_CPP_IgnoreClassFunction_cfg",
            "C++ Class pInvoke Serializations", //"PreGen_CPP_ObjParseDef_cfg",
            "C++ SimObject Based Classes", //"PreGen_CPP_SimObjectBaseClasses_cfg",
            "C++ Return Type Casting Overrides", //"PreGen_CPP_TypeConv_cfg",
            "C++ Class/Enum Map To C# Class/Enum", //"PreGen_CS__TypeConvCPPtoCS_cfg",
            "C++ Source Files To Ignore On Interrogation", //"PreGen_IgnoreSourceFiles_cfg",
            "C++ Source Files To Ignore For Enumeration Parsing" //  "PreGen_IgnoreSourceFilesForEnumeration_cfg"
        };
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
            TextHasChanged = "";
            switch (_SelectedConfigOption)
                {
                case "C++ Constants":
                    ConfigData = Settings.Default.PreGen_CPP_Constants_cfg;
                    break;
                case "C++ Class/Function Ignores":
                    ConfigData = Settings.Default.PreGen_CPP_IgnoreClassFunction_cfg;
                    break;
                case "C++ Class pInvoke Serializations":
                    ConfigData = Settings.Default.PreGen_CPP_ObjParseDef_cfg;
                    break;
                case "C++ SimObject Based Classes":
                    ConfigData = Settings.Default.PreGen_CPP_SimObjectBaseClasses_cfg;
                    break;
                case "C++ Return Type Casting Overrides":
                    ConfigData = Settings.Default.PreGen_CPP_TypeConv_cfg;
                    break;
                case "C++ Class/Enum Map To C# Class/Enum":
                    ConfigData = Settings.Default.PreGen_CS__TypeConvCPPtoCS_cfg;
                    break;
                case "C++ Source Files To Ignore On Interrogation":
                    ConfigData = Settings.Default.PreGen_IgnoreSourceFiles_cfg;
                    break;
                case "C++ Source Files To Ignore For Enumeration Parsing":
                    ConfigData = Settings.Default.PreGen_IgnoreSourceFilesForEnumeration_cfg;
                    break;
                }
            }

        private void Save_OnClick(object sender, RoutedEventArgs e)
            {
            TextHasChanged = "";
            switch (_SelectedConfigOption)
                {
                case "C++ Constants":
                    Settings.Default.PreGen_CPP_Constants_cfg = ConfigData;
                    break;
                case "C++ Class/Function Ignores":
                    Settings.Default.PreGen_CPP_IgnoreClassFunction_cfg = ConfigData;
                    break;
                case "C++ Class pInvoke Serializations":
                    Settings.Default.PreGen_CPP_ObjParseDef_cfg = ConfigData;
                    break;
                case "C++ SimObject Based Classes":
                    Settings.Default.PreGen_CPP_SimObjectBaseClasses_cfg = ConfigData;
                    break;
                case "C++ Return Type Casting Overrides":
                    Settings.Default.PreGen_CPP_TypeConv_cfg = ConfigData;
                    break;
                case "C++ Class/Enum Map To C# Class/Enum":
                    Settings.Default.PreGen_CS__TypeConvCPPtoCS_cfg = ConfigData;
                    break;
                case "C++ Source Files To Ignore On Interrogation":
                    Settings.Default.PreGen_IgnoreSourceFiles_cfg = ConfigData;
                    break;
                case "C++ Source Files To Ignore For Enumeration Parsing":
                    Settings.Default.PreGen_IgnoreSourceFilesForEnumeration_cfg = ConfigData;
                    break;
                }
            Settings.Default.Save();
            }

        private void UIElement_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
            {
            TextHasChanged = "* Changed";
            }


        private delegate void SetControlPropertyThreadSafeDelegate(System.Windows.Controls.TextBlock control, string propertyName, object propertyValue);

        private delegate void SetControlPropertyThreadSafeDelegateb(System.Windows.Controls.Button control, string propertyName, object propertyValue);

        private delegate void SetControlPropertyThreadSafeDelegatec(System.Windows.Controls.ComboBox control, string propertyName, object propertyValue);

        private class plistitem
            {
            public string mName { get; set; }
            public Project mProject { get; set; }
            }
        #endregion
        }
    }