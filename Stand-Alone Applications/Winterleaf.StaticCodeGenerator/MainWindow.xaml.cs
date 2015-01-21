using System.Windows;
using Winterleaf.StaticCodeGenerator.Controls;

namespace OMNIStaticCodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Content = new SolutionSelectorCtrl();
        }
    }
}