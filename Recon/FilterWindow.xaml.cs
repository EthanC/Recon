using AdonisUI.Controls;
using System.Windows;

namespace Recon
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : AdonisWindow
    {
        public FilterWindow()
        {
            InitializeComponent();
        }

        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
