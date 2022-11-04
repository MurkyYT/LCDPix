using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LCDPix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double CmToPixel = 37.7952755906;
        struct PixelInfo
        {
            public Point TopLeft { get; internal set; }
            public Point TopRight { get; internal set; }
            public Point BottomLeft { get; internal set; }
            public Point BottomRight { get; internal set; }
            public int x { get; internal set; }
            public int y { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
            public Color fillColor { get; set; }
            public PixelInfo(int x,int y,int Width,int Height,Color fillColor)
            {
                this.x = x;
                this.y = y;
                this.Width = Width;
                this.Height = Height;
                this.fillColor = fillColor;
                this.TopLeft = new Point(x * Width, y * Width);
                this.TopRight = new Point(x * Width + Width, y * Width);
                this.BottomLeft = new Point(x * Width, y * Height + Height);
                this.BottomRight = new Point(x * Width + Width, y * Height + Height);
            }
        }
        List<PixelInfo> ScreenMap = new List<PixelInfo>();
        public MainWindow()
        {
            InitializeComponent();
            ImportLCDPIXFile("f:\\Visual Studio Projects\\Personal\\LCDPix\\Default.lcdpix");
        }
        void ImportLCDPIXFile(string path)
        {
            if (path.Substring(path.Length - 7) != ".lcdpix") 
            {
                Debug.WriteLine("Not correct file format");
                return;
            }
            string[] lines = File.ReadAllLines(path);
            int lineNo = 1;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            foreach (string line in lines)
            {
                if (line == "[LCDPIX]")
                {
                    Debug.WriteLine($"Started reaing {path}");
                    continue;
                }
                else if (line == "[END_LCDPIX]")
                {
                    Debug.WriteLine($"End of {path}");
                    break;
                }
                else if (line == "")
                    continue;
                else if (line.StartsWith("//"))
                    continue;
                Debug.WriteLine($"-------------LINE {lineNo}-------------");
                string cleanLine = line.Replace("[", "").Replace("]", "");
                string[] sections = cleanLine.Split('|');
                string[] coords = sections[0].Split(',');
                string[] size = sections[1].Split(',');
                string[] color = sections[2].Split(',');

                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);
                int width = int.Parse(size[0]);
                int height = int.Parse(size[1]);
                Color finalColor = Color.FromRgb(byte.Parse(color[0]), byte.Parse(color[1]), byte.Parse(color[2]));
                Debug.WriteLine($"Coords:({x},{y}) Size:({width}X{height}) Color:({finalColor.R},{finalColor.G},{finalColor.B})");
                ScreenMap.Add(new PixelInfo(x, y, width, height, finalColor));
                lineNo++;
            }
            Draw();
            Mouse.OverrideCursor = null;
        }
        void ExportLCDPIXFile(string path)
        {
            int lineNo = 1;
            var text = new StringBuilder();
            Debug.WriteLine($"Started writing {path}");
            text.AppendLine("[LCDPIX]");
            foreach (PixelInfo pixelInfo in ScreenMap)
            {
                
                text.AppendLine($"[{pixelInfo.x},{pixelInfo.y} | " +
                    $"{pixelInfo.Width},{pixelInfo.Height} | " +
                    $"{pixelInfo.fillColor.R},{pixelInfo.fillColor.G},{pixelInfo.fillColor.B}]");
                Debug.WriteLine($"LINE {lineNo} done");
                lineNo++;
            }
            text.AppendLine("[END_LCDPIX]");
            File.WriteAllText(path, text.ToString());
            Debug.WriteLine($"Done exporting LCDPIXFile to {path}");
        }
        void Draw()
        {
            Screen.Children.Clear();
            foreach (PixelInfo pixel in ScreenMap)
            {
                int Width = pixel.Width;
                int Height = pixel.Height;
                int x = pixel.x;
                int y = pixel.y;
                Color fillColor = pixel.fillColor;
                DrawRectangle(x, y, Width, Height, fillColor);
            }
        }
        void DrawRectangle(double x, double y, double Width, double Height, Color fiilColor)
        {
            Rectangle rect = new Rectangle()
            {
                Fill = new SolidColorBrush(fiilColor),
                Stroke = new SolidColorBrush(Colors.Black),
                Width = Width,
                Height = Height,
            };
            Canvas.SetLeft(rect, x*Width);
            Canvas.SetTop(rect, y*Width);
            Screen.Children.Add(rect);
        }
        private void Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangePixelColor(e.MouseDevice.GetPosition(Screen),Colors.Black);
        }
        void ChangePixelColor(Point Position,Color color)
        {
            for (int i = 0; i < ScreenMap.Count; i++)
            {
                PixelInfo pixel = ScreenMap[i];
                if (FindPoint(pixel.BottomLeft, pixel.TopRight, Position) && pixel.fillColor != color)
                {
                    ScreenMap.Add(new PixelInfo(pixel.x, pixel.y, pixel.Width, pixel.Height, color));
                    ScreenMap.Remove(pixel);
                    Draw();
                }
            }
        }
        static bool FindPoint(Point bottomLeft, Point topRight, Point coords)
        {
            if (coords.X > bottomLeft.X && coords.X < topRight.X &&
                coords.Y < bottomLeft.Y && coords.Y > topRight.Y)
                return true;

            return false;
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            ScreenMap.Clear();
            for (int x = 0; x < 25; x++)
            {
                for(int y = 0; y < 15; y++)
                {
                    ScreenMap.Add(new PixelInfo(x, y, 20, 20, Colors.White));
                }
            }
            Draw();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Default"; // Default file name
            dialog.DefaultExt = ".lcdpix"; // Default file extension
            dialog.Filter = "LCDPix mapping (.lcdpix)|*.lcdpix"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ImportLCDPIXFile(filename);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "Default"; // Default file name
            dialog.DefaultExt = ".lcdpix"; // Default file extension
            dialog.Filter = "LCDPix mapping (.lcdpix)|*.lcdpix"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ExportLCDPIXFile(filename);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
