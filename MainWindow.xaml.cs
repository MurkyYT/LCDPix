using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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
        internal class PixelInfo
        {
            public Point TopLeft { get; internal set; }
            public Point TopRight { get; internal set; }
            public Point BottomLeft { get; internal set; }
            public Point BottomRight { get; internal set; }
            public int X { get; internal set; }
            public int Y { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
            public Color FillColor { get; internal set; }
            public Rectangle PixelRect { get; internal set; }
            public PixelInfo(int x, int y, int Width, int Height, Color fillColor)
            {
                this.X = x;
                this.Y = y;
                this.Width = Width;
                this.Height = Height;
                this.FillColor = fillColor;
                this.TopLeft = new Point(x * Width, y * Width);
                this.TopRight = new Point(x * Width + Width, y * Width);
                this.BottomLeft = new Point(x * Width, y * Height + Height);
                this.BottomRight = new Point(x * Width + Width, y * Height + Height);
                this.PixelRect = null;
            }
            public void ChangeColor(Color color)
            {
                this.FillColor = color;
                this.PixelRect.Fill = new SolidColorBrush(color);
            }
        }
        int sizex = 0;
        int sizey = 0;
        double zoom = 1;
        string mode = "Drawing";
        Stack<ColorChange> undoStack = new Stack<ColorChange>();
        List<PixelInfo> ScreenMap = new List<PixelInfo>();
        public MainWindow()
        {
            InitializeComponent();
            SelectedModeText.Text = $"Selected mode: {mode}";
            ZoomInAmount.Text = $"Zoom: ({Math.Round(zoom*100)}%)";
            Mouse.AddMouseWheelHandler(Screen, ZoomIn);
        }
        void ImportLCDPIXFile(string path)
        {
            List<PixelInfo> temp = new List<PixelInfo>();
            try
            {
                if (path.Substring(path.Length - 7) != ".lcdpix")
                {
                    Debug.WriteLine("Not correct file format");
                    return;
                }
                ScreenMap.Clear();
                string[] lines = File.ReadAllLines(path);
                int lineNo = 1;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                bool sizeCheck = false;
                foreach (string line in lines)
                {
                    string cleanLine = line.Replace("[", "").Replace("]", "");
                    if (line == "[SIZE]")
                    {
                        sizeCheck = true;
                        continue;
                    }
                    else if (line == "[END_SIZE]")
                    {
                        sizeCheck = false;
                        continue;
                    }
                    else if (line == "")
                        continue;
                    else if (line.StartsWith("//"))
                        continue;
                    else if (sizeCheck)
                    {
                        sizex = int.Parse(cleanLine.Split(',')[0]);
                        sizey = int.Parse(cleanLine.Split(',')[1]);
                        continue;
                    }
                    else if (line == "[LCDPIX]")
                    {
                        Debug.WriteLine($"Started reading {path}");
                        continue;
                    }
                    else if (line == "[END_LCDPIX]")
                    {
                        Debug.WriteLine($"End of {path}");
                        break;
                    }
                    else
                    {
                        Debug.WriteLine($"-------------LINE {lineNo}-------------");
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
                        temp.Add(new PixelInfo(x, y, width, height, finalColor));
                        lineNo++;
                    }
                }
                ScreenMap = temp;
                int widthCanvas = 0;
                int heightCanvas = 0;
                PixelInfo pixel = ScreenMap[0];
                for (int x = 0; x < sizex; x++)
                {
                    widthCanvas += pixel.Width;
                }
                for (int x = 0; x < sizey; x++)
                {
                    heightCanvas += pixel.Height;
                }
                MapCanvas.Height = heightCanvas * zoom;
                MapCanvas.Width = widthCanvas * zoom;
                Draw();
                Mouse.OverrideCursor = null;
                Title = $"{path} - LCDPix ({sizex},{sizey})";
                undoStack.Clear();
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"An error ocurred while loading LCDPix file ({ex.Message})", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        void ExportLCDPIXFile(string path)
        {
            int lineNo = 1;
            var text = new StringBuilder();
            Debug.WriteLine($"Started writing {path}");
            text.AppendLine("[SIZE]");
            text.AppendLine($"[{sizex},{sizey}]");
            text.AppendLine("[END_SIZE]");
            text.AppendLine("[LCDPIX]");
            foreach (PixelInfo pixelInfo in ScreenMap)
            {
                text.AppendLine($"[{pixelInfo.X},{pixelInfo.Y} | " +
                    $"{pixelInfo.Width},{pixelInfo.Height} | " +
                    $"{pixelInfo.FillColor.R},{pixelInfo.FillColor.G},{pixelInfo.FillColor.B}]");
                Debug.WriteLine($"LINE {lineNo} done");
                lineNo++;
            }
            text.AppendLine("[END_LCDPIX]");
            File.WriteAllText(path, text.ToString());
            Title = $"{path} - LCDPix ({sizex},{sizey})";
            Debug.WriteLine($"Done exporting LCDPIXFile to {path}");
        }
        void Draw()
        {
            Screen.Children.Clear();
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            for (int i = 0; i < ScreenMap.Count; i++)
            {
                PixelInfo pixel = ScreenMap[i];
                int Width = (int)(pixel.Width * zoom);
                int Height = (int)(pixel.Height * zoom);
                int x = pixel.X;
                int y = pixel.Y;
                Color fillColor = pixel.FillColor;
                pixel.PixelRect = DrawRectangle(x, y, Width, Height, fillColor);
            }
            Mouse.OverrideCursor = null;
        }
        Rectangle DrawRectangle(double x, double y, double Width, double Height, Color fiilColor)
        {
            Rectangle rect = new Rectangle()
            {
                Fill = new SolidColorBrush(fiilColor),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1,
                Width = Width + 1,
                Height = Height + 1,
            };
            Canvas.SetLeft(rect, x * Width);
            Canvas.SetTop(rect, y * Width);
            Screen.Children.Add(rect);
            return rect;
        }
        private void Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LeftClick(true, e.MouseDevice.GetPosition(Screen));
        }
        void LeftClick(bool firstTime, Point e)
        {
            switch (mode)
            {
                case "Drawing":
                    SolidColorBrush newBrush = (SolidColorBrush)nowColor.Background;
                    ChangePixelColor(e, Color.FromRgb(newBrush.Color.R, newBrush.Color.G, newBrush.Color.B), firstTime);
                    break;
                case "Eraser":
                    ChangePixelColor(e, Colors.White, firstTime);
                    break;
                case "Pipette":
                    var pixelColor = GetPixelColor(e);
                    if (pixelColor != new Color())
                    {
                        nowColor.Background = new SolidColorBrush(pixelColor);
                        SolidColorBrush nowColorBrush = (SolidColorBrush)nowColor.Background;
                        RedValue.Text = nowColorBrush.Color.R.ToString();
                        GreenValue.Text = nowColorBrush.Color.G.ToString();
                        BlueValue.Text = nowColorBrush.Color.B.ToString();
                    }
                    break;
                case "Fill":
                    newBrush = (SolidColorBrush)nowColor.Background;
                    var color = Color.FromRgb(newBrush.Color.R, newBrush.Color.G, newBrush.Color.B);
                    var fillColor = GetPixelColor(e);
                    var pixel = GetPixel(e);
                    if (pixel != null)
                        FloodFill(pixel.X, pixel.Y, pixel.Width, pixel.Height, color, fillColor,true);
                    break;
            }
            
        }
        void FloodFill(int x,int y,int width, int height,Color fill,Color old,bool firstTime)
        {
            if ((x < 0) || (x >= sizex)) return;
            if ((y < 0) || (y >= sizey)) return;
            if (fill == old) return;
            Point pos = new Point(x * width * zoom + 1, y * height * zoom + 1);
            if (GetPixelColor(pos)== old)
            {
                ChangePixelColor(pos, fill, firstTime);
                FloodFill(x + 1, y, width,height,fill,old,false);
                FloodFill(x, y + 1, width, height, fill, old, false);
                FloodFill(x - 1, y, width, height, fill, old, false);
                FloodFill(x, y - 1, width, height, fill, old, false);
                //FloodFill(x + 1, y - 1, width, height, fill, old, false);
                //FloodFill(x - 1, y - 1, width, height, fill, old, false);
                //FloodFill(x + 1, y + 1, width, height, fill, old, false);
                //FloodFill(x - 1, y + 1, width, height, fill, old, false);
            }
        }
        PixelInfo ChangePixelColor(Point Position, Color color, bool firstTime)
        {
            for (int i = 0; i < ScreenMap.Count; i++)
            {
                PixelInfo pixel = ScreenMap[i];
                if (FindPoint(pixel.BottomLeft, pixel.TopRight, Position) && pixel.FillColor != color)
                {
                    //ScreenMap.Add(new PixelInfo(pixel.x, pixel.y, pixel.Width, pixel.Height, color));
                    //ScreenMap.Remove(pixel);
                    undoStack.Push(new ColorChange(pixel, pixel.FillColor, firstTime));
                    if (Title[Title.Length - 1] != '*')
                        Title += "*";
                    pixel.ChangeColor(color);
                    return pixel;
                    //Draw();
                }
            }
            return null;
        }
        List<PixelInfo> GetNeighbours(PixelInfo pixel)
        {
            List<PixelInfo> neighbours = new List<PixelInfo>();
            if (pixel != null)
            {
                int x = pixel.X;
                int y = pixel.Y;
                foreach (var pixelInfo in ScreenMap)
                {
                    if (pixelInfo.X == x && pixelInfo.Y == y - 1)
                        neighbours.Add(pixelInfo);
                    else if (pixelInfo.X == x && pixelInfo.Y == y + 1)
                        neighbours.Add(pixelInfo);
                    else if (pixelInfo.X == x - 1 && pixelInfo.Y == y)
                        neighbours.Add(pixelInfo);
                    else if (pixelInfo.X == x + 1 && pixelInfo.Y == y)
                        neighbours.Add(pixelInfo);
                }
            }
            return neighbours;
        }
        PixelInfo GetPixel(Point Position)
        {
            for (int i = 0; i < ScreenMap.Count; i++)
            {
                PixelInfo pixel = ScreenMap[i];
                if (FindPoint(pixel.BottomLeft, pixel.TopRight, Position))
                {
                    return pixel;
                }
            }
            return null;
        }
        Color GetPixelColor(Point Position)
        {
            for (int i = 0; i < ScreenMap.Count; i++)
            {
                PixelInfo pixel = ScreenMap[i];
                if (FindPoint(pixel.BottomLeft, pixel.TopRight, Position))
                {
                    return pixel.FillColor;
                }
            }
            return new Color();
        }
        bool FindPoint(Point bottomLeft, Point topRight, Point coords)
        {
            if (coords.X > bottomLeft.X * zoom && coords.X < topRight.X * zoom &&
                coords.Y  < bottomLeft.Y * zoom && coords.Y  > topRight.Y * zoom)
                return true;

            return false;
        }
        public WriteableBitmap SaveAsWriteableBitmap(Canvas surface)
        {
            if (surface == null) return null;
            double tempzoom = zoom;
            // Save current canvas transform
            Transform transform = surface.LayoutTransform;
            // reset current transform (in case it is scaled or rotated)
            zoom = 1;
            Draw();
            foreach (var pixel2 in ScreenMap)
            {
                pixel2.PixelRect.StrokeThickness = 0;
            }
            surface.LayoutTransform = null;
            int width = 0;
            int height = 0;
            PixelInfo pixel = ScreenMap[0];
            for (int x = 0; x < sizex; x++)
            {
                width += pixel.Width;
            }
            for (int x = 0; x < sizey; x++)
            {
                height += pixel.Height;
            }
            // Get the size of canvas
            Size size = new Size(width,height);
            // Measure and arrange the surface
            // VERY IMPORTANT
            surface.Measure(size);
            surface.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
              (int)size.Width,
              (int)size.Height,
              96d,
              96d,
              PixelFormats.Pbgra32);
            renderBitmap.Render(surface);


            //Restore previously saved layout
            surface.LayoutTransform = transform;
            zoom = tempzoom;
            Draw();
            foreach (var pixel3 in ScreenMap)
            {
                pixel3.PixelRect.StrokeThickness = 1;
            }
            //create and return a new WriteableBitmap using the RenderTargetBitmap
            return new WriteableBitmap(renderBitmap);

        }
        private System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            System.Drawing.Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }
        void CheckForChanges()
        {
            if(undoStack.Count != 0)
            {
                string fileName = Title.Split('\\')[Title.Split('\\').Length - 1].Split('.')[0].Replace("*", "");
                MessageBoxResult result = MessageBox.Show($"There are unsaved changes in {fileName} would you like to save them?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if(result == MessageBoxResult.Yes)
                {
                    string fileName2 = Title.Split(new string[] { " - LCDPix" }, StringSplitOptions.None)[0];
                    if (!File.Exists(fileName2))
                    {
                        SaveFile_Click(null, null);
                    }
                    else
                    {
                        undoStack.Clear();
                        ExportLCDPIXFile(fileName2);
                    }
                }
            }
        }
        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            CheckForChanges();
            undoStack.Clear();
            int pixelsize = 0;
            NewFileDialog newFileDialog = new NewFileDialog();
            if (newFileDialog.ShowDialog() == true)
            {
                if (newFileDialog.DialogResult == true)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    ScreenMap.Clear();
                    sizex = (int)newFileDialog.Size.X;
                    sizey = (int)newFileDialog.Size.Y;
                    pixelsize = (int)newFileDialog.PixelSize;
                }
            }
            if (newFileDialog.DialogResult == true)
            {
                Title = $"Blank LCDPix file";
                for (int x = 0; x < sizex; x++)
                {
                    for (int y = 0; y < sizey; y++)
                    {
                        ScreenMap.Add(new PixelInfo(x, y, pixelsize, pixelsize, Colors.White));
                    }
                }
                int width = 0;
                int height = 0;
                PixelInfo pixel = ScreenMap[0];
                for (int x = 0; x < sizex; x++)
                {
                    width += pixel.Width;
                }
                for (int x = 0; x < sizey; x++)
                {
                    height += pixel.Height;
                }
                MapCanvas.Height = height;
                MapCanvas.Width = width;
                Draw();
                Mouse.OverrideCursor = null;
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            CheckForChanges();
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Default", // Default file name
                DefaultExt = ".lcdpix", // Default file extension
                Filter = "LCDPix mapping |*.lcdpix" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ImportLCDPIXFile(filename);
            }
            int width = 0;
            int height = 0;
            PixelInfo pixel = ScreenMap[0];
            for (int x = 0; x < sizex; x++)
            {
                width += pixel.Width;
            }
            for (int x = 0; x < sizey; x++)
            {
                height += pixel.Height;
            }
            MapCanvas.Height = height * zoom;
            MapCanvas.Width = width * zoom;
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Title.Split('\\')[Title.Split('\\').Length - 1].Split('.')[0].Replace("*", ""), // Default file name
                DefaultExt = ".lcdpix", // Default file extension
                Filter = "LCDPix mapping |*.lcdpix" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ExportLCDPIXFile(filename);
                undoStack.Clear();
            }
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void ColorPicker_MouseMove(object sender, MouseEventArgs e)
        {
            Select_Color(sender, e);
        }
        private void Select_Color(object sender, MouseEventArgs e)
        {
            try
            {
                BitmapSource visual_BitmapSource = Get_BitmapSource_of_Element(ColorPicker);
                CroppedBitmap cb = new CroppedBitmap(visual_BitmapSource, new Int32Rect((int)Math.Round(Mouse.GetPosition(ColorPicker).X), (int)Math.Round(Mouse.GetPosition(ColorPicker).Y), 1, 1));
                byte[] pixels = new byte[4];
                try
                {
                    cb.CopyPixels(pixels, 4, 0);
                }
                catch { }
                selectedColor.Background = new SolidColorBrush(Color.FromRgb(pixels[2], pixels[1], pixels[0]));
            }
            catch { }
        }
        public BitmapSource Get_BitmapSource_of_Element(FrameworkElement element)

        {
            if (element == null) { return null; }
            double dpi = 96;
            Double width = element.ActualWidth;
            Double height = element.ActualHeight;
            RenderTargetBitmap bitmap_of_Element = null;
            if (bitmap_of_Element == null)
            {
                try
                {
                    bitmap_of_Element = new RenderTargetBitmap((int)width, (int)height, dpi, dpi, PixelFormats.Default);
                    DrawingVisual visual_area = new DrawingVisual();
                    using (DrawingContext dc = visual_area.RenderOpen())
                    {
                        VisualBrush visual_brush = new VisualBrush(element);
                        dc.DrawRectangle(visual_brush, null, new Rect(new Point(), new Size(width, height)));
                    }
                    bitmap_of_Element.Render(visual_area);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return bitmap_of_Element;
        }

        private void ColorPicker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            nowColor.Background = selectedColor.Background;
            SolidColorBrush newBrush = (SolidColorBrush)nowColor.Background;
            RedValue.Text = newBrush.Color.R.ToString();
            GreenValue.Text = newBrush.Color.G.ToString();
            BlueValue.Text = newBrush.Color.B.ToString();
        }
        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void RedValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            SolidColorBrush newBrush = (SolidColorBrush)nowColor.Background;
            if (RedValue.Text != null)
                nowColor.Background = new SolidColorBrush(Color.FromRgb(byte.Parse(RedValue.Text), newBrush.Color.G, newBrush.Color.B));
        }

        private void GreenValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            SolidColorBrush newBrush = (SolidColorBrush)nowColor.Background;
            if (GreenValue.Text != null)
                nowColor.Background = new SolidColorBrush(Color.FromRgb(newBrush.Color.R, byte.Parse(GreenValue.Text), newBrush.Color.B));
        }

        private void BlueValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            SolidColorBrush newBrush = (SolidColorBrush)nowColor.Background;
            if (BlueValue.Text != null)
                nowColor.Background = new SolidColorBrush(Color.FromRgb(newBrush.Color.R, newBrush.Color.G, byte.Parse(BlueValue.Text)));
        }

        private void BlueValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void GreenValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void RedValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                LeftClick(false, e.MouseDevice.GetPosition(Screen));
            }
        }
        void ExportImage(string path,ImageFormat format)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            System.Drawing.Bitmap bmp = BitmapFromWriteableBitmap(SaveAsWriteableBitmap(Screen));
            System.Drawing.Image img = System.Drawing.Image.FromHbitmap(bmp.GetHbitmap());
            img.Save(path,format);
            foreach (var pixel in ScreenMap)
            {
                pixel.PixelRect.StrokeThickness = 1;
            }
            Mouse.OverrideCursor = null;
            MessageBox.Show($"Done exporting '{path.Split('\\')[path.Split('\\').Length-1].Replace(format.ToString().ToLower(),"")}' as {format.ToString().ToUpper()}","Export Image",MessageBoxButton.OK,MessageBoxImage.Information);
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                while (undoStack.Count != 0)
                {
                    var temp = undoStack.Pop();
                    temp.UndoAction();
                    if (temp.firstTime)
                        break;
                }
                if (undoStack.Count == 0)
                    Title = Title.Replace("*", "");
            }
            if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                string fileName = Title.Split(new string[] { " - LCDPix" }, StringSplitOptions.None)[0];
                if (!File.Exists(fileName))
                {
                    SaveFile_Click(null, null);
                }
                else
                {
                    undoStack.Clear();
                    ExportLCDPIXFile(fileName);
                }
            }
            if (e.Key == Key.N && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                NewFile_Click(null, null);
            }
        }
        private void ZoomIn(object sender, MouseWheelEventArgs e)
        {
            // If the mouse wheel delta is positive, move the box up.
            if (e.Delta > 0 && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                zoom += 0.1;
                e.Handled = true;
                if (zoom <= 0.3)
                    zoom = 0.3;
                int width = 0;
                int height = 0;
                PixelInfo pixel = ScreenMap[0];
                for (int x = 0; x < sizex; x++)
                {
                    width += pixel.Width;
                }
                for (int x = 0; x < sizey; x++)
                {
                    height += pixel.Height;
                }
                MapCanvas.Height = height*zoom;
                MapCanvas.Width = width*zoom;
                Draw();
            }

            // If the mouse wheel delta is negative, move the box down.
            if (e.Delta < 0 && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                zoom -= 0.1;
                e.Handled = true;
                if (zoom <= 0.3)
                    zoom = 0.3;
                int width = 0;
                int height = 0;
                PixelInfo pixel = ScreenMap[0];
                for (int x = 0; x < sizex; x++)
                {
                    width += pixel.Width;
                }
                for (int x = 0; x < sizey; x++)
                {
                    height += pixel.Height;
                }
                MapCanvas.Height = height*zoom;
                MapCanvas.Width = width*zoom;
                Draw();
            }
            ZoomInAmount.Text = $"Zoom: ({Math.Round(zoom*100)}%)";
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CheckForChanges();
        }

        private void ExportAsPNG_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Title.Split('\\')[Title.Split('\\').Length - 1].Split('.')[0].Replace("*", ""), // Default file name
                DefaultExt = ".png", // Default file extension
                Filter = "PNG image |*.png" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ExportImage(filename,ImageFormat.Png);
            }
        }
        private void CurosrSelectButton_Click(object sender, RoutedEventArgs e)
        {
            mode = "Drawing";
            SelectedModeText.Text = $"Selected mode: {mode}";
        }

        private void EraserSelectButton_Click(object sender, RoutedEventArgs e)
        {
            mode = "Eraser";
            SelectedModeText.Text = $"Selected mode: {mode}";
        }

        private void PipetteSelectorButton_Click(object sender, RoutedEventArgs e)
        {
            mode = "Pipette";
            SelectedModeText.Text = $"Selected mode: {mode}";
        }

        private void FillSelectorButton_Click(object sender, RoutedEventArgs e)
        {
            mode = "Fill";
            SelectedModeText.Text = $"Selected mode: {mode}";
        }

        private void ImportPNG_Click(object sender, RoutedEventArgs e)
        {
            CheckForChanges();
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            undoStack.Clear();
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Image", // Default file name
                DefaultExt = ".png", // Default file extension
                Filter = "PNG Image |*.png" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();
            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                var myBitmap = new System.Drawing.Bitmap(filename);
                sizex = myBitmap.Width;
                sizey = myBitmap.Height;
                ScreenMap.Clear();
                for (int x = 0; x < myBitmap.Width; x++) 
                { 
                    for (int y = 0; y < myBitmap.Height; y++)
                    {
                        System.Drawing.Color pixelColor = myBitmap.GetPixel(x, y);
                        ScreenMap.Add(new PixelInfo(x, y, 5, 5, Color.FromRgb(pixelColor.R,pixelColor.G,pixelColor.B)));
                    }
                }
                Title = $"{filename} - LCDPix ({sizex},{sizey})";
            }
            int width = 0;
            int height = 0;
            PixelInfo pixel = ScreenMap[0];
            for (int x = 0; x < sizex; x++)
            {
                width += pixel.Width;
            }
            for (int x = 0; x < sizey; x++)
            {
                height += pixel.Height;
            }
            MapCanvas.Height = height * zoom;
            MapCanvas.Width = width * zoom;
            Draw();
            
        }

        private void ExportAsBMP_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Title.Split('\\')[Title.Split('\\').Length - 1].Split('.')[0].Replace("*", ""), // Default file name
                DefaultExt = ".bmp", // Default file extension
                Filter = "BMP image |*.bmp" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ExportImage(filename, ImageFormat.Bmp);
            }
        }

        private void ExportAsJPG_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Title.Split('\\')[Title.Split('\\').Length - 1].Split('.')[0].Replace("*", ""), // Default file name
                DefaultExt = ".jpg", // Default file extension
                Filter = "JPG image |*.jpg" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ExportImage(filename, ImageFormat.Jpeg);
            }
        }

        private void CloseFile_Click(object sender, RoutedEventArgs e)
        {
            CheckForChanges();
            undoStack.Clear();
            Screen.Children.Clear();
            ScreenMap.Clear();
            Draw();
            Title = "LCDPix";
        }
    }
}
