using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;
using Winterleaf.SharedServices.Interrogator;
using Winterleaf.SharedServices.Properties;
using Winterleaf.SharedServices.Util;
using Solution = EnvDTE.Solution;

namespace Winterleaf.OmniTools.Windows
{
    /// <summary>
    /// Interaction logic for twStaticCodeGeneratorControl.xaml
    /// </summary>
    public partial class twStaticCodeGeneratorControl : UserControl, INotifyPropertyChanged
    {
        public delegate void progressData(ProgressBar pb, double value);

        private string CPPProjectFilePath = string.Empty;
        private string _ConfigData = "";
        private string _SelectedConfigOption;

        private List<String> _configOptions = new List<string>() {"C++ Constants", //"PreGen_CPP_Constants_cfg",
            "C++ Class/Function Ignores", //"PreGen_CPP_IgnoreClassFunction_cfg",
            "C++ Class pInvoke Serializations", //"PreGen_CPP_ObjParseDef_cfg",
            "C++ SimObject Based Classes", //"PreGen_CPP_SimObjectBaseClasses_cfg",
            "C++ Return Type Casting Overrides", //"PreGen_CPP_TypeConv_cfg",
            "C++ Class/Enum Map To C# Class/Enum", //"PreGen_CS__TypeConvCPPtoCS_cfg",
            "C++ Source Files To Ignore On Interrogation", //"PreGen_IgnoreSourceFiles_cfg",
            "C++ Source Files To Ignore For Enumeration Parsing" //  "PreGen_IgnoreSourceFilesForEnumeration_cfg"
        };

        private DTE2 _mApplicationObject;
        private string _textHasChanged = "";
        private Interrogator gator;
        private string lastHeading = "";
        private string solutionpath = string.Empty;

        public twStaticCodeGeneratorControl(ref DTE2 _applicationObject)
        {
            InitializeComponent();
            DataContext = this;

            SelectedConfigOption = _configOptions[0];

            _mApplicationObject = _applicationObject;

            btnSelect.Click += new RoutedEventHandler(btnSelect_Click);

            cb_CPPDLLProject.DisplayMemberPath = "ProjectName";
            cb_GameLogicProject.DisplayMemberPath = "mName";
            cb_WinterleafOmniProject.DisplayMemberPath = "mName";
            pb_Removal.Minimum = 0;
            pb_Removal.Maximum = 100;
            pb_Removal.Value = 0;
            btnGenerate.IsEnabled = false;
            LoadDropDowns();
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }

        private void LoadDropDowns()
        {
            cb_GameLogicProject.Items.Clear();
            cb_WinterleafOmniProject.Items.Clear();
            Solution s = _mApplicationObject.Solution;
            foreach (Project p in s.Projects)
                {
                plistitem pi = new plistitem {mName = p.Name, mProject = p};
                cb_GameLogicProject.Items.Add(pi);
                cb_WinterleafOmniProject.Items.Add(pi);
                }
        }

        private bool isProjectLoaded(Project project)
        {
            Solution s = _mApplicationObject.Solution;
            return s.Projects.Cast<Project>().Any(p => p == project);
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select the solution file for Omni T3D";
            openFileDialog1.Filter = "VS Solution files (*.sln)|*.sln";
            if (openFileDialog1.ShowDialog() != true)
                return;
            try
                {
                SharedServices.Util.Solution s = new SharedServices.Util.Solution(openFileDialog1.FileName);
                solutionpath = openFileDialog1.FileName;
                foreach (SolutionProject p in s.Projects)
                    cb_CPPDLLProject.Items.Add(p);
                cb_CPPDLLProject.IsEnabled = true;
                }
            catch (Exception)
                {
                }
        }

        public string GetRootNameSpace(Project project)
        {
            return project.Properties.Item("RootNamespace").Value.ToString();
        }

        private void Cb_GameLogicProject_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
                {
                CPPProjectFilePath = Path.GetFullPath(GetProjectPath(((SolutionProject) cb_CPPDLLProject.SelectedItem)));
                txt_RootNamespace.Text = GetRootNameSpace(((plistitem) cb_GameLogicProject.SelectedItem).mProject);
                btnGenerate.IsEnabled = true;
                }
            catch (Exception err)
                {
                btnGenerate.IsEnabled = false;
                }
        }

        private string GetProjectPath(SolutionProject sp)
        {
            string newpath = Path.Combine(Path.GetDirectoryName(solutionpath), sp.RelativePath);
            return Path.GetFullPath(newpath);
        }

        private void Cb_CPPDLLProject_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDropDowns();
        }

        private void BtnGenerate_OnClick(object sender, RoutedEventArgs e)
        {
            if (!isProjectLoaded(((plistitem) cb_GameLogicProject.SelectedItem).mProject) || (!isProjectLoaded(((plistitem) cb_WinterleafOmniProject.SelectedItem).mProject)))
                {
                LoadDropDowns();
                txt_RootNamespace.Text = "";
                return;
                }

            btnGenerate.IsEnabled = false;
            btnSelect.IsEnabled = false;
            cb_CPPDLLProject.IsEnabled = false;
            cb_GameLogicProject.IsEnabled = false;
            cb_WinterleafOmniProject.IsEnabled = false;

            gator = new Interrogator(NewEvent, Finished, ProgressChange, ProgressSubChange, CPPProjectFilePath, "", "", txt_RootNamespace.Text);

            gator.mCSProject_Engine = ((plistitem) cb_WinterleafOmniProject.SelectedItem).mProject;

            gator.mCSProject_GameLogic = ((plistitem) cb_GameLogicProject.SelectedItem).mProject;

            gator.Start();
        }

        public static void SetProgress(ProgressBar t, double percent)
        {
            if (t.Dispatcher.CheckAccess())
                {
                t.Minimum = 0;
                t.Maximum = 100;
                t.Value = (int) (percent*100.00);
                t.Tag = ((int) (percent*100.00)).ToString("##0") + "%";
                }
            else
                t.Dispatcher.Invoke(new progressData(SetProgress), new object[] {t, percent});
        }

        public static void SetControlPropertyThreadSafe(TextBlock control, string propertyName, object propertyValue)
        {
            if (control.Dispatcher.CheckAccess())
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] {propertyValue});

            else
                control.Dispatcher.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe), new object[] {control, propertyName, propertyValue});
        }

        public static void SetControlPropertyThreadSafeb(Button control, string propertyName, object propertyValue)
        {
            if (control.Dispatcher.CheckAccess())
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] {propertyValue});

            else
                control.Dispatcher.Invoke(new SetControlPropertyThreadSafeDelegateb(SetControlPropertyThreadSafeb), new object[] {control, propertyName, propertyValue});
        }

        public static void SetControlPropertyThreadSafec(ComboBox control, string propertyName, object propertyValue)
        {
            if (control.Dispatcher.CheckAccess())
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] {propertyValue});

            else
                control.Dispatcher.Invoke(new SetControlPropertyThreadSafeDelegatec(SetControlPropertyThreadSafec), new object[] {control, propertyName, propertyValue});
        }

        private void NewEvent(string text)
        {
            //SetAppendData(rtb_Out, text);
        }

        private void Finished(string text)
        {
            SetControlPropertyThreadSafe(lbl_Message, "Text", "Finished");
            SetControlPropertyThreadSafe(lb_Out, "Text", "");
            SetControlPropertyThreadSafeb(btnGenerate, "IsEnabled", true);
            SetControlPropertyThreadSafeb(btnSelect, "IsEnabled", true);

            SetControlPropertyThreadSafec(cb_CPPDLLProject, "IsEnabled", true);
            SetControlPropertyThreadSafec(cb_GameLogicProject, "IsEnabled", true);
            SetControlPropertyThreadSafec(cb_WinterleafOmniProject, "IsEnabled", true);
        }

        public void ProgressChange(double percent, String Text)
        {
            SetProgress(pb_Removal, percent);
            //SetControlPropertyThreadSafe(lb_MainPercent, "Text", (percent * 100.0).ToString("##0.000") + "%");
            SetControlPropertyThreadSafe(lbl_Message, "Text", Text);
            lastHeading = Text;
        }

        public void ProgressSubChange(double percent, String Text)
        {
            SetProgress(pb_sub, percent);
            // SetControlPropertyThreadSafe(lb_Percent, "Text", (percent * 100.0).ToString("##0.000") + "%");
            SetControlPropertyThreadSafe(lb_Out, "Text", lastHeading + " (" + Text + ")");
        }

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

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            TextHasChanged = "* Changed";
        }

        private void Cb_WinterleafOmniProject_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (cb_WinterleafOmniProject.Items.Count == 0)
                LoadDropDowns();
        }

        private delegate void SetControlPropertyThreadSafeDelegate(TextBlock control, string propertyName, object propertyValue);

        private delegate void SetControlPropertyThreadSafeDelegateb(Button control, string propertyName, object propertyValue);

        private delegate void SetControlPropertyThreadSafeDelegatec(ComboBox control, string propertyName, object propertyValue);

        private class plistitem
        {
            public string mName { get; set; }
            public Project mProject { get; set; }
        }
    }
}