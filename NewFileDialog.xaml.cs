using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace LCDPix
{
    /// <summary>
    /// Interaction logic for NewFileDialog.xaml
    /// </summary>
    public partial class NewFileDialog : Window
    {
        //double CmToPixel = 37.7952755906;
        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        public NewFileDialog()
        {
            InitializeComponent();
        }
        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

        }

        public Point Size
        {
            get { return new Point(double.Parse(SizeX.Text), double.Parse(SizeY.Text)); }
        }
        public double PixelSize
        {
            get { return double.Parse(PixelSizeText.Text.Replace(".",",")); }
        }

        private void PixelSizeText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void SizeY_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void SizeX_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
    }
}
