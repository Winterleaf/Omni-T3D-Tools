using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Winterleaf.OmniTools.Windows;
using Winterleaf.SharedServices;

namespace Winterleaf.OmniTools
    {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidOmniToolsPkgString)]
    [ProvideToolWindow(typeof(twStaticCodeGeneratorToolWindow), Orientation = ToolWindowOrientation.Right, Style = VsDockStyle.Float, MultiInstances = false, Transient = false, PositionX = 100, PositionY = 100, Width = 400, Height = 700)]
    [ProvideToolWindow(typeof(twGuiParserControlWindow), Orientation = ToolWindowOrientation.Right, Style = VsDockStyle.Float, MultiInstances = false, Transient = false, PositionX = 100, PositionY = 100, Width = 400, Height = 700)]
    [ProvideToolWindow(typeof(twAboutWindow), Orientation = ToolWindowOrientation.Right, Style = VsDockStyle.Float, MultiInstances = false, Transient = false, PositionX = 100, PositionY = 100, Width = 300, Height = 100)]
    public sealed class OmniToolsPackage : Package
        {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public OmniToolsPackage()
            {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
            {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
                {
                // Create the command for the menu item.
                mcs.AddCommand(new MenuCommand(MenuItemCallback, new CommandID(GuidList.guidOmniToolsCmdSet, (int)PkgCmdIDList.TopLevelMenu)));
                mcs.AddCommand(new MenuCommand(MenuItemCallback, new CommandID(GuidList.guidStaticCodeGenerationCmdSet, (int)PkgCmdIDList.SubLevelMenu)));


                mcs.AddCommand(new MenuCommand(cmdStaticCodeGeneratorCallback, new CommandID(GuidList.guidStaticCodeGenerationCmdSet, (int)PkgCmdIDList.cmdStaticCodeGenerator)));
                mcs.AddCommand(new MenuCommand(cmdGuiParserCallback, new CommandID(GuidList.guidOmniToolsCmdSet, (int)PkgCmdIDList.cmdT3DGuiToCSharp)));
                mcs.AddCommand(new MenuCommand(cmdOmniToolsAbout_MenuItemCallback, new CommandID(GuidList.guidOmniToolsAboutCmdSet, (int)PkgCmdIDList.cmdOmniToolsAbout)));

                OleMenuCommand menuItem = new OleMenuCommand(cmdidAutoGenConverterCallback, new CommandID(GuidList.guidAutoGenConverterCmdSet, (int)PkgCmdIDList.cmdidAutoGenConverter));
                //menuItem.BeforeQueryStatus += new EventHandler(menuItem_BeforeQueryStatus);

                mcs.AddCommand(menuItem);
                }
            }

        private void menuItem_BeforeQueryStatus(object sender, EventArgs e)
            {
            OleMenuCommand cmd = sender as OleMenuCommand;
            if (null != cmd)
                {
                bool enable = false;
                IWpfTextView view = GetActiveTextView();
                if ((null != view) && !view.Selection.IsEmpty)
                    enable = (view.Selection.SelectedSpans.Count > 0);
                cmd.Enabled = enable;
                }
            }

        private IWpfTextView GetActiveTextView()
            {
            IWpfTextView view = null;
            IVsTextView vTextView = null;

            IVsTextManager txtMgr = (IVsTextManager)GetService(typeof(SVsTextManager));
            int mustHaveFocus = 1;
            txtMgr.GetActiveView(mustHaveFocus, null, out vTextView);

            IVsUserData userData = vTextView as IVsUserData;
            if (null != userData)
                {
                IWpfTextViewHost viewHost;
                object holder;
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out holder);
                viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
                }

            return view;
            }

        #endregion

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
            {
            MessageBox.Show("Got!");
            //DTE2 dte = GetGlobalService(typeof (DTE)) as DTE2;
            }

        private void cmdOmniToolsAbout_MenuItemCallback(object sender, EventArgs e)
            {
            ToolWindowPane window = this.FindToolWindow(typeof(twAboutWindow), 0, true);
            if ((null == window) || (null == window.Frame))
                throw new NotSupportedException(String.Format("Can not create Toolwindow: twAboutWindow"));
            Guid m = Guid.Empty;

            ((IVsWindowFrame)window.Frame).SetFramePos(VSSETFRAMEPOS.SFP_fSize, ref m, 0, 0, 300, 100);
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }

        private void cmdidAutoGenConverterCallback(object sender, EventArgs e)
            {
            GetCodeElementAtCursor();
            }

        public void GetCodeElementAtCursor()
            {
            DTE2 dte = GetGlobalService(typeof(DTE)) as DTE2;

            CodeElement objCodeElement = default(CodeElement);
            TextPoint objCursorTextPoint = default(TextPoint);

            try
                {
                objCursorTextPoint = GetCursorTextPoint();

                if ((objCursorTextPoint != null))
                    {
                    // Get the class at the cursor
                    objCodeElement = GetCodeElementAtTextPoint(vsCMElement.vsCMElementClass, dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements, objCursorTextPoint);
                    }

                if (objCodeElement == null)
                    MessageBox.Show("No class found at the cursor!");
                else
                    {

                    EditPoint ep = objCursorTextPoint.CreateEditPoint();
                    UndoContext uc = dte.UndoContext;
                    uc.Open("Code Region");
                    ep.Insert(CodeTemplates.ConversionCode.Replace("#@!#CLASSNAME#@!#", objCodeElement.Name));
                    uc.Close();
                    }
                }
            catch (Exception ex)
                {
                MessageBox.Show(ex.ToString());
                }
            }

        private TextPoint GetCursorTextPoint()
            {
            DTE2 dte = GetGlobalService(typeof(DTE)) as DTE2;
            TextDocument objTextDocument = default(TextDocument);
            TextPoint objCursorTextPoint = default(TextPoint);

            try
                {
                objTextDocument = (TextDocument)dte.ActiveDocument.Object("TextDocument");
                objCursorTextPoint = objTextDocument.Selection.ActivePoint;
                }
            catch (Exception ex)
                {
                }

            return objCursorTextPoint;
            }

        private CodeElement GetCodeElementAtTextPoint(vsCMElement eRequestedCodeElementKind, CodeElements colCodeElements, TextPoint objTextPoint)
            {
            CodeElement objResultCodeElement = default(CodeElement);
            CodeElements colCodeElementMembers = default(CodeElements);
            CodeElement objMemberCodeElement = default(CodeElement);

            if ((colCodeElements != null))
                {
                foreach (CodeElement objCodeElement in colCodeElements)
                    {
                    if (objCodeElement.StartPoint.GreaterThan(objTextPoint))
                        {
                        // The code element starts beyond the point
                        }
                    else if (objCodeElement.EndPoint.LessThan(objTextPoint))
                        {
                        // The code element ends before the point

                        // The code element contains the point
                        }
                    else
                        {
                        if (objCodeElement.Kind == eRequestedCodeElementKind)
                            {
                            // Found
                            objResultCodeElement = objCodeElement;
                            }

                        // We enter in recursion, just in case there is an inner code element that also 
                        // satisfies the conditions, for example, if we are searching a namespace or a class
                        colCodeElementMembers = GetCodeElementMembers(objCodeElement);

                        objMemberCodeElement = GetCodeElementAtTextPoint(eRequestedCodeElementKind, colCodeElementMembers, objTextPoint);

                        if ((objMemberCodeElement != null))
                            {
                            // A nested code element also satisfies the conditions
                            objResultCodeElement = objMemberCodeElement;
                            }

                        break; // TODO: might not be correct. Was : Exit For
                        }
                    }
                }

            return objResultCodeElement;
            }

        private CodeElements GetCodeElementMembers(CodeElement objCodeElement)
            {
            CodeElements colCodeElements = default(CodeElements);

            if (objCodeElement is CodeNamespace)
                colCodeElements = ((CodeNamespace)objCodeElement).Members;
            else if (objCodeElement is CodeType)
                colCodeElements = ((CodeType)objCodeElement).Members;
            else if (objCodeElement is CodeFunction)
                colCodeElements = ((CodeFunction)objCodeElement).Parameters;

            return colCodeElements;
            }

        private void cmdStaticCodeGeneratorCallback(object sender, EventArgs e)
            {
            ToolWindowPane window = this.FindToolWindow(typeof(twStaticCodeGeneratorToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
                throw new NotSupportedException(String.Format("Can not create Toolwindow: twStaticCodeGenerator"));
            Guid m = Guid.Empty;
            ((IVsWindowFrame)window.Frame).SetFramePos(VSSETFRAMEPOS.SFP_fSize, ref m, 0, 0, 700, 400);
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }

        private void cmdGuiParserCallback(object sender, EventArgs e)
            {
            ToolWindowPane window = this.FindToolWindow(typeof(twGuiParserControlWindow), 0, true);
            if ((null == window) || (null == window.Frame))
                throw new NotSupportedException(String.Format("Can not create Toolwindow: twGuiParserControlWindow"));
            Guid m = Guid.Empty;

            ((IVsWindowFrame)window.Frame).SetFramePos(VSSETFRAMEPOS.SFP_fSize, ref m, 0, 0, 1000, 400);
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
        }
    }