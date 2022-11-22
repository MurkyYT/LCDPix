using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LCDPix.Classes
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
        public double Opacity { get; internal set; }
        public PixelInfo(int x, int y, int Width, int Height, Color fillColor)
        {
            this.X = x;
            this.Y = y;
            this.Width = Width;
            this.Height = Height;
            this.FillColor = fillColor;
            this.TopLeft = new Point(x * Width, y * Height);
            this.TopRight = new Point(x * Width + Width, y * Height);
            this.BottomLeft = new Point(x * Width, y * Height + Height);
            this.BottomRight = new Point(x * Width + Width, y * Height + Height);
            this.PixelRect = null;
            this.Opacity = 1;
        }
        public void ChangeColor(Color color)
        {
            this.FillColor = color;
            this.PixelRect.Fill = new SolidColorBrush(color);
        }
    }
}
