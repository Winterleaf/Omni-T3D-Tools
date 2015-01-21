using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Winterleaf.SharedServices.Properties;
using Clipboard = System.Windows.Clipboard;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using UserControl = System.Windows.Controls.UserControl;

namespace Winterleaf.SharedServices.GuiParser
    {
    /// <summary>
    /// Interaction logic for GuiParserCtrl.xaml
    /// </summary>
    public partial class GuiParserCtrl : UserControl, INotifyPropertyChanged
        {
        private string _mTorqueScriptBody = "";
        private string _mcSharpScriptBody = "";
        private int counter = 1;
        private int objectcounter = 0;

        public GuiParserCtrl()
            {
            InitializeComponent();
            if (Settings.Default.WorkingFolder == "")
                return;
            FileNode rootnode = new FileNode(Settings.Default.WorkingFolder, ImageFolder.Source);
            treeView1.Items.Add(rootnode);
            FillChildNodes(rootnode);
            }

        public string TorqueScriptBody
            {
            get { return _mTorqueScriptBody; }
            set
                {
                _mTorqueScriptBody = value;
                OnPropertyChanged("TorqueScriptBody");
                }
            }

        public string cSharpScriptBody
            {
            get { return _mcSharpScriptBody; }
            set
                {
                _mcSharpScriptBody = value;
                OnPropertyChanged("cSharpScriptBody");
                }
            }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ParseMe(string data, int start, ref ScriptObject parent, bool initcall = false)
            {
            for (int i = start; i < data.Length; i++)
                {
                if (data.Length > i + "datablock ".Length)
                    {
                    ScriptObject so = new ScriptObject();
                    bool isnew = false;
                    if (data.Substring(i, 4).ToLower() == "new ")
                        {
                        if (i == 0)
                            {
                            isnew = true;
                            so.objectType = ScriptObject.T3DObjectType.Object;
                            }
                        else
                            {
                            for (int q = i - 1; q > 0; q--)
                                {
                                if (data[q] != ' ' && data[q] != '\r' && data[q] != '\n' && data[q] != '\t' && data[q] != ';' && data[q] != '=')
                                    continue;

                                if (data[q] != ';' && data[q] != '=')
                                    continue;

                                isnew = true;
                                so.objectType = ScriptObject.T3DObjectType.Object;
                                //so.isSingleton = false;
                                break;
                                }
                            }
                        }
                    if (data.Substring(i, "singleton ".Length) == "singleton ")
                        {
                        if (i == 0)
                            {
                            isnew = true;
                            //so.isSingleton = true;
                            so.objectType = ScriptObject.T3DObjectType.Singleton;
                            }
                        else
                            {
                            for (int q = i - 1; q > 0; q--)
                                {
                                if (data[q] != ' ' && data[q] != '\r' && data[q] != '\n' && data[q] != '\t' && data[q] != ';' && data[q] != '=')
                                    continue;

                                if (data[q] != ';' && data[q] != '=')
                                    continue;

                                isnew = true;
                                //so.isSingleton = true;
                                so.objectType = ScriptObject.T3DObjectType.Singleton;
                                break;
                                }
                            }
                        }
                    else if (data.Substring(i, "datablock ".Length) == "datablock ")
                        {
                        if (i == 0)
                            {
                            isnew = true;
                            //so.isSingleton = true;
                            so.objectType = ScriptObject.T3DObjectType.Datablock;
                            }
                        else
                            {
                            for (int q = i - 1; q > 0; q--)
                                {
                                if (data[q] != ' ' && data[q] != '\r' && data[q] != '\n' && data[q] != '\t' && data[q] != ';' && data[q] != '=')
                                    continue;

                                if (data[q] != ';' && data[q] != '=')
                                    continue;

                                isnew = true;
                                //so.isSingleton = true;
                                so.objectType = ScriptObject.T3DObjectType.Datablock;
                                break;
                                }
                            }
                        }

                    if (!isnew)
                        continue;

                  //  Console.WriteLine("Object Type " + so.objectType + data.Substring(i,20));

                    switch (so.objectType)
                        {
                        case ScriptObject.T3DObjectType.Singleton:
                            i += "singleton ".Length;
                            break;
                        case ScriptObject.T3DObjectType.Datablock:
                            i += "datablock ".Length;
                            break;
                        case ScriptObject.T3DObjectType.Object:
                            i += "new ".Length;
                            break;
                        }

                    //if (so.objectType == ScriptObject.T3DObjectType.Singleton)
                    //    i += "singleton ".Length;
                    //else
                    //    i += "new ".Length;

                    so.classname = "";
                    so.instancename = "";
                    so.body = "";

                    #region Classname

                    while (data[i] != '(')
                        {
                        so.classname += data[i];
                        i++;
                        }

                    #endregion

                    i++;

                    #region InstanceName

                    while (data[i] != ')')
                        {
                        so.instancename += data[i];
                        i++;
                        }

                    #endregion

                    i++;

                    #region Parse Brace

                    while (data[i] != '{')
                        i++;
                    i++;

                    int bracketcount = 1;

                    int startp = i;

                    while (i < data.Length)
                        {
                        if (data[i] == '{')
                            bracketcount++;

                        if (data[i] == '}')
                            bracketcount--;

                        if (bracketcount == 0)
                            break;

                        i++;
                        }

                    #endregion

                    so.RawBody = data.Substring(startp, i - startp);

                    bool checkagain = true;
                    int firstnew = -1;

                    firstnew = so.RawBody.IndexOf("new ", StringComparison.Ordinal);
                    while (checkagain)
                        {
                        bool inquote = false;
                        for (int ic = 0; ic < firstnew; ic++)
                            {
                            if (so.RawBody[ic] == '"' && !inquote)
                                inquote = true;
                            else if (so.RawBody[ic] == '"' && inquote)
                                inquote = false;
                            }
                        if (inquote == false)
                            checkagain = false;
                        else
                            {
                            firstnew = so.RawBody.IndexOf("new ", firstnew + 1, StringComparison.Ordinal);
                            if (firstnew == -1)
                                break;
                            }
                        }

                    if (firstnew != -1)
                        {
                        so.body = so.RawBody.Substring(0, firstnew);
                        ParseMe(so.RawBody.Substring(firstnew), 0, ref so);
                        }
                    else
                        so.body = so.RawBody;
                    objectcounter++;
                    parent.properties.Add("#Newobject" + objectcounter.ToString("0000#"), so);
                    if (initcall)
                        Console.WriteLine();
                    }
                }
            }

        public string removeComments(string data)
            {
            bool inquotes = false;
            bool inMacro = false;
            StringBuilder newcode = new StringBuilder();
            int i = 0;
            while (i < data.Length)
                {
                if (data.Length < i + 2)
                    {
                    newcode.Append(data[i]);
                    break;
                    }

                if (data[i] == '"' && data[i - 1] != '\\' && !inquotes)
                    {
                    inquotes = true;
                    newcode.Append(data[i]);
                    i++;
                    continue;
                    }
                if (data[i] == '"' && data[i - 1] != '\\' && inquotes)
                    {
                    inquotes = false;
                    newcode.Append(data[i]);
                    i++;
                    continue;
                    }
                if (inquotes)
                    {
                    newcode.Append(data[i]);
                    i++;
                    continue;
                    }

                if (data.Substring(i, 2) == "/*")
                    {
                    i = i + 2;
                    while (true)
                        {
                        if (data.Length < i + 2)
                            break;
                        if (data.Substring(i, 2) == "*/")
                            {
                            i = i + 2;
                            break;
                            }
                        i++;
                        }
                    continue;
                    }

                if (data.Substring(i, 2) == "//")
                    {
                    i = i + 2;
                    while (true)
                        {
                        if (i >= data.Length)
                            break;
                        if (data[i] == '\n')
                            {
                            i++;
                            break;
                            }
                        i++;
                        }
                    continue;
                    }
                //#ifndef
                //#if
                //#else
                //#endif
                bool domacrocheck = false;
                if ((i == 0) && data[i] == '#')
                    domacrocheck = true;
                else if (data[i] == '#' && data[i - 1] == '\n')
                    domacrocheck = true;

                if (domacrocheck)
                    {
                    if (i + "#ifndef".Length < data.Length)
                        {
                        if (data.Substring(i, "#ifndef".Length) == "#ifndef")
                            {
                            inMacro = true;
                            newcode.Append("\r\n");
                            }
                        }
                    if (i + "#if".Length < data.Length)
                        {
                        if (data.Substring(i, "#if".Length) == "#if")
                            {
                            inMacro = true;
                            newcode.Append("\r\n");
                            }
                        }
                    if (i + "#endif".Length < data.Length)
                        {
                        if (data.Substring(i, "#endif".Length) == "#endif")
                            {
                            inMacro = false;
                            newcode.Append("\r\n");
                            }
                        }
                    }

                newcode.Append(data[i]);
                i++;
                }

            string y = newcode.ToString();
            return newcode.ToString();
            }

        protected void OnPropertyChanged(string name)
            {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
            }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
            {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.MyComputer;

            if (fbd.ShowDialog() != DialogResult.OK)
                return;
            Settings.Default.WorkingFolder = fbd.SelectedPath;
            Settings.Default.Save();
            treeView1.Items.Clear();
            FileNode rootnode = new FileNode(fbd.SelectedPath, ImageFolder.Source);
            treeView1.Items.Add(rootnode);
            FillChildNodes(rootnode);
            }

        private void FillChildNodes(FileNode node)
            {
            try
                {
                DirectoryInfo dirs = new DirectoryInfo(node.FullPath);
                foreach (DirectoryInfo dir in dirs.GetDirectories())
                    {
                    FileNode newnode = new FileNode(dir.Name, ImageFolder.Source);
                    newnode.FullPath = dir.FullName;
                    newnode.Expanding += new RoutedEventHandler(newnode_Expanding);
                    newnode.isDirectory = true;
                    newnode.Image = ImageFolder;
                    node.Items.Add(newnode);

                    newnode.Items.Add("*");
                    }
                foreach (FileInfo file in dirs.GetFiles())
                    {
                    FileNode newnode = new FileNode(file.Name, ImageDocument.Source);
                    newnode.IsExpanded = true;
                    newnode.path = file.Directory + "\\" + file.Name;
                    newnode.isDirectory = false;
                    //newnode.ImageIndex = 0;
                    //newnode.SelectedImageIndex = 0;
                    node.Items.Add(newnode);
                    }
                }
            catch (Exception ex)
                {
                MessageBox.Show(ex.Message.ToString());
                }
            }

        private void newnode_Expanding(object sender, RoutedEventArgs e)
            {
            FileNode fn = (FileNode)e.Source;
            if (fn.isDirectory)
                {
                fn.Items.Clear();
                FillChildNodes(fn);
                }
            //Console.WriteLine("");
            //e.Node.Nodes.Clear();
            //    FillChildNodes((FileNode) e.Node);
            }

        //public class FileNode : TreeViewItem
        //    {
        //    public FileNode(string text)
        //        {
        //        this.Header = text;
        //        }

        //    }

        private void TreeView1_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
            {
            FileNode fn = (FileNode)e.NewValue;
            if (fn == null)
                return;
            if (fn.isDirectory)
                return;
            objectcounter = 0;
            string filepath = fn.path;
				if (fn.path == null)
					return;

            if (!((filepath.ToLower().EndsWith(".cs")) || (filepath.ToLower().EndsWith(".gui"))))
                {
                TorqueScriptBody = "Cannot Parse file: " + filepath;
                cSharpScriptBody = "Cannot Parse file: " + filepath;
                return;
                }

            if (filepath == null)
                return;
            if (filepath == "")
                return;

            try
                {
                Clipboard.SetText(Path.GetFileNameWithoutExtension(filepath));
                }
            catch (Exception)
                {
                }
            TextReader sr = new StreamReader(filepath);
            string data = sr.ReadToEnd();

            TorqueScriptBody = data;

            if (data.Contains("//--- OBJECT WRITE END ---"))
                data = data.Substring(0, data.IndexOf("//--- OBJECT WRITE END ---"));

            data = removeComments(data);

            /*
            @       (concatenates two strings)
            TAB     (concatenation with tab)
            SPC     (concatenation with space)
            NL      (newline)
            */

            sr.Close();

            ScriptObject t = new ScriptObject();
            t.body = "";
            if (data.StartsWith("\r\n"))
                data = data.Substring(2);

            data = data.Trim();

            bool inquotes = false;
            string newdata = "";

            #region Hideme

            if (1 == 0)
                {
                for (int i = 0; i < data.Length; i++)
                    {
                    if (data[i] == '"')
                        {
                        if (i > 0)
                            {
                            if (data[i - 1] != '\\')
                                inquotes = !inquotes;
                            }
                        }
                    if (inquotes)
                        newdata += data[i];
                    else
                        {
                        if (data[i] == '@')
                            {
                            newdata += " + ";
                            continue;
                            }
                        if (data.Length > i + 1)
                            {
                            if (data.Substring(i, 2) == "NL")
                                {
                                newdata += @" \\""\\r\\n\\""";
                                continue;
                                }
                            }
                        if (data.Length > i + 2)
                            {
                            if (data.Substring(i, 2) == "TAB")
                                {
                                newdata += @" \\""\\t\\""";
                                continue;
                                }
                            if (data.Substring(i, 2) == "SPC")
                                {
                                newdata += @" \\"" \\""";
                                continue;
                                }
                            }
                        newdata += data[i];
                        }
                    }
                data = newdata;
                }

            #endregion

            try
                {
                ParseMe(data, 0, ref t, true);
                counter = 1;
                _VarDef = new List<string>();
                cSharpScriptBody = GenText(t, "rootctrl", 0).Trim();
                foreach (string vardef in _VarDef.OrderBy(q => q).ToList())
                    {
                    cSharpScriptBody = vardef + cSharpScriptBody;
                    }
                }
            catch (Exception)
                {
                cSharpScriptBody = "ERROR: FAILED TO PARSE.";
                }
            }



        private string GenText(ScriptObject so, string name, int depth)
            {
            counter++;
            string result = string.Empty;

            if (name != "rootctrl" && so.objectType == ScriptObject.T3DObjectType.Object) //so.isSingleton == false)
                {
                //result += "{\r\n";
                result += "#region " + so.classname + " (" + so.instancename + ")        oc_" + name + "\r\n";
                //result += genspacing(depth) + "ObjectCreator oc_" + name + " = new ObjectCreator(\"" + so.classname + "\", \"" + so.instancename + "\");\r\n";
                result += genspacing(depth) + "oc_" + name + " = new ObjectCreator(\"" + so.classname + "\", \"" + so.instancename + "\");\r\n";
                _VarDef.Add("ObjectCreator oc_" + name + ";\r\n");
                }
            else if (name != "rootctrl" && so.objectType == ScriptObject.T3DObjectType.Singleton) //so.isSingleton == true)
                {
                //result += "{\r\n";
                _VarDef.Add("SingletonCreator oc_" + name + ";\r\n");
                result += "#region " + so.classname + " (" + so.instancename + ")        oc_" + name + "\r\n";
                //result += genspacing(depth) + "SingletonCreator oc_" + name + " = new SingletonCreator(\"" + so.classname + "\", \"" + so.instancename + "\");\r\n";
                result += genspacing(depth) + "oc_" + name + " = new SingletonCreator(\"" + so.classname + "\", \"" + so.instancename + "\");\r\n";
                }
            else if (name != "rootctrl" && so.objectType == ScriptObject.T3DObjectType.Datablock) //so.isSingleton == true)
                {
                // result += "{\r\n";
                _VarDef.Add("DatablockCreator oc_" + name + ";\r\n");
                result += "#region " + so.classname + " (" + so.instancename + ")        oc_" + name + "\r\n";
                //result += genspacing(depth) + "DatablockCreator oc_" + name + " = new DatablockCreator(\"" + so.classname + "\", \"" + so.instancename + "\");\r\n";
                result += genspacing(depth) + "oc_" + name + " = new DatablockCreator(\"" + so.classname + "\", \"" + so.instancename + "\");\r\n";
                }

            so.name = "oc_" + name;

            string paramName = string.Empty;
            string paramValue = string.Empty;
            int readstage = 0;
            bool inQuote = false;

            for (int i = 0; i < so.body.Length; i++)
                {
                if (so.body[i] == '\r')
                    continue;
                if (so.body[i] == '\n')
                    continue;

                if (readstage == 1 && so.body[i] == '"' && !inQuote)
                    inQuote = true;
                else if (readstage == 1 && so.body[i] == '"' && inQuote)
                    inQuote = false;
                if (so.body[i] == '=' && readstage == 0)
                    {
                    readstage = 1;
                    continue;
                    }

                if (so.body[i] == ';' && !inQuote)
                    {
                    string cleanup = paramValue.Trim();

                    bool noQuote = false;

                    if (cleanup.StartsWith("\"") && cleanup.EndsWith("\""))
                        cleanup = cleanup.Substring(1, cleanup.Length - 2);
                    else
                        noQuote = true;

                    string a = '"' + "";
                    string b = @"\\""";

                    if (noQuote)
                        result += genspacing(depth) + "oc_" + name + "[\"" + paramName.Trim() + "\"] = new ObjectCreator.StringNoQuote (\"" + cleanup.Replace("\"", "\\\"") + "\") ;\r\n";
                    else
                        result += genspacing(depth) + "oc_" + name + "[\"" + paramName.Trim() + "\"] = \"" + cleanup.Replace(a, b) + "\" ;\r\n";
                    paramName = "";
                    paramValue = "";
                    readstage = 0;
                    continue;
                    }

                if (readstage == 0)
                    paramName += so.body[i];

                if (readstage == 1)
                    paramValue += so.body[i];
                }

            bool added1 = false;
            result += "if (true)\r\n";
            result += "{\r\n";
            foreach (KeyValuePair<string, object> kvp in so.properties)
                {
                if (kvp.Key.StartsWith("#"))
                    {
                    result += "\r\n" + GenText((ScriptObject)kvp.Value, kvp.Key.Replace("#", ""), depth + 1);
                    if (name != "rootctrl")
                        result += "\r\n" + genspacing(depth) + "oc_" + name + "[\"" + kvp.Key + "\"] = oc_" + kvp.Key.Replace("#", "") + ";\r\n";
                    result += "\r\n";
                    added1 = true;
                    }
                }
            if (!added1)
                {
                result = result.Substring(0, result.LastIndexOf("if (true)\r\n"));
                }
            else
                result += "}\r\n";

            if (name != "rootctrl")
                {
                result += "#endregion\r\n";
                // result += "}\r\n";
                }

            if (depth == 1)
                {
                result += so.name + ".Create();\r\n";

                }


            while (result.IndexOf("\r\n\r\n\r\n") >= 0)
                result = result.Replace("\r\n\r\n\r\n", "\r\n\r\n");
            return result;
            }

        private List<string> _VarDef;

        private string genspacing(int depth)
            {
            return "";
            //string result = "";
            //for (int i = 0; i < depth; i++)
            //    result += "    ";
            //return result;
            }

        private void BtnCopyToClipboard_OnClick(object sender, RoutedEventArgs e)
            {
            Clipboard.SetText(cSharpScriptBody);
            }

        public class FileNode : TreeViewItem, INotifyPropertyChanged
            {
            public static readonly RoutedEvent CollapsingEvent = EventManager.RegisterRoutedEvent("Collapsing", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FileNode));

            public static readonly RoutedEvent ExpandingEvent = EventManager.RegisterRoutedEvent("Expanding", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FileNode));
            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
                {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(name));
                }

            public event RoutedEventHandler Collapsing
                {
                add { AddHandler(CollapsingEvent, value); }

                remove { RemoveHandler(CollapsingEvent, value); }
                }

            public event RoutedEventHandler Expanding
                {
                add { AddHandler(ExpandingEvent, value); }

                remove { RemoveHandler(ExpandingEvent, value); }
                }

            protected override void OnExpanded(RoutedEventArgs e)
                {
                OnExpanding(new RoutedEventArgs(ExpandingEvent, this));

                base.OnExpanded(e);
                }

            protected override void OnCollapsed(RoutedEventArgs e)
                {
                OnCollapsing(new RoutedEventArgs(CollapsingEvent, this));

                base.OnCollapsed(e);
                }

            protected virtual void OnCollapsing(RoutedEventArgs e)
                {
                RaiseEvent(e);
                }

            protected virtual void OnExpanding(RoutedEventArgs e)
                {
                RaiseEvent(e);
                }

            #region Data Member

            private Image _image = null;
            private Uri _imageUrl = null;
            private TextBlock _textBlock = null;

            public string path { get; set; }
            public string FullPath { get; set; }

            public bool isDirectory { get; set; }

            #endregion

            #region Properties

            public string Text
                {
                get { return _textBlock.Text; }
                set { _textBlock.Text = value; }
                }

            public Image Image
                {
                get { return _image; }
                set
                    {
                    _image = value;
                    OnPropertyChanged("Image");
                    }
                }

            #endregion

            #region Constructor

            public FileNode(string text, ImageSource source)
                {
                CreateTreeViewItemTemplate(source);
                Text = text;
                FullPath = text;
                }

            #endregion

            #region Private Methods

            private void CreateTreeViewItemTemplate(ImageSource source)
                {
                StackPanel stack = new StackPanel();
                stack.Orientation = Orientation.Horizontal;
                stack.Margin = new Thickness(0);

                Image = new Image();
                Image.HorizontalAlignment = HorizontalAlignment.Left;
                Image.VerticalAlignment = VerticalAlignment.Center;
                Image.Width = 16;
                Image.Height = 16;
                Image.Margin = new Thickness(0);
                Image.Source = source;
                stack.Children.Add(Image);

                TextBlock spacer = new TextBlock();
                spacer.Width = 10;
                stack.Children.Add(spacer);

                _textBlock = new TextBlock();
                _textBlock.Margin = new Thickness(0);
                _textBlock.VerticalAlignment = VerticalAlignment.Center;
                _textBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                //_textBlock.LineHeight = 5; // Or some other value you fancy.

                stack.Children.Add(_textBlock);

                Header = stack;
                }

            #endregion
            }

        private class ScriptObject
            {
            public enum T3DObjectType
                {
                Uninitialized,
                Datablock,
                Singleton,
                Object
                }

            //public bool isSingleton { get; set; }
            public T3DObjectType objectType;
            public Dictionary<string, object> properties = new Dictionary<string, object>();
            public string classname { get; set; }
            public string instancename { get; set; }
            public string body { get; set; }
            public string RawBody { get; set; }
            public string name { get; set; }
            }
        }
    }