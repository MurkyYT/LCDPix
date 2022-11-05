using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace LCDPix
{
    internal class ColorChange
    {
        internal MainWindow.PixelInfo pixelInfo { get; }
        internal Color oldColor { get; }
        internal bool firstTime { get; set; }
        public ColorChange(MainWindow.PixelInfo pixelInfo, Color oldColor, bool firstTime)
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
