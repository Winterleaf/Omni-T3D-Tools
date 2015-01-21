using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Winterleaf.SharedServices.GuiParser;

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
    /// {}
    [Guid("EFB329CD-837D-4BE2-87FB-2D6FCC74FCE4")]
    public class twAboutWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public twAboutWindow()
        {
            this.Caption = "Omni Tools About";
            base.Content = new Winterleaf.SharedServices.About.About();
        }
    }
}