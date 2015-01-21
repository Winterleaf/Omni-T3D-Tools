using System.Windows;
using Winterleaf.SharedServices.GuiParser;

namespace Winterleaf.TSGuiParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "TorqueScript Gui Parser";
        //    base.Content = new GuiParserCtrl();
        }
    }
}