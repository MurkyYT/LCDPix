using System.Windows.Media;

namespace LCDPix.Classes
{
    internal class ColorChange
    {
        internal PixelInfo pixelInfo { get; }
        internal Color oldColor { get; }
        internal bool firstTime { get; set; }
        public ColorChange(PixelInfo pixelInfo, Color oldColor, bool firstTime)
        {
            this.pixelInfo = pixelInfo;
            this.oldColor = oldColor;
            this.firstTime = firstTime;
        }
        public void UndoAction()
        {
            this.pixelInfo.ChangeColor(oldColor);
        }
    }
}
