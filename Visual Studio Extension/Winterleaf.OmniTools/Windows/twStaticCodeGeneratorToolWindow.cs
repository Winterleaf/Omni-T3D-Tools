using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace Winterleaf.OmniTools.Windows
{
    /// <summary>
    /// This class implements the tool window twStaticCodeGeneratorToolWindow exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("83f22d5c-afb1-4f5f-b3cc-43954a2a2afc")]
    public class twStaticCodeGeneratorToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public twStaticCodeGeneratorToolWindow()
        {
            DTE2 dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof (DTE)) as DTE2;
            this.Caption = "Omni Static Code Generator";
            base.Content = new twStaticCodeGeneratorControl(ref dte);
        }
    }
}