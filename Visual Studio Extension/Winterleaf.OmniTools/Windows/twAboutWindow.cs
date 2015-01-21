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
    [Guid("B976CDAA-E383-46FC-A403-5B19F6CFA99E")]
    public class twGuiParserControlWindow : ToolWindowPane
        {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public twGuiParserControlWindow()
            {
            this.Caption = "TorqueScript Gui Parser";
            base.Content = new GuiParserCtrl();
            }
        }
    }