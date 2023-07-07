using System.Collections.Generic;
using System.Drawing;

namespace LCDPix.Classes
{
    internal class Tab
    {
        public PixelInfo[] pixels { get; private set; }
        public int index { get; private set; }
        public string name { get; private set; }
        public Point size { get; private set; }
        List<Tab> tabs { get; set; }
        public ColorChange[] undoStack { get; private set; }
        public ColorChange[] redoStack { get; private set; }
        public Tab(PixelInfo[] pixels, string name, List<Tab> tabs, int sizex, int sizey)
        {
            this.pixels = pixels;
            this.size = new Point(sizex, sizey);
            this.index = tabs.Count;
            this.name = name;
            this.tabs = tabs;
        }
        public void UpdateStacks(Stack<ColorChange> undoStack, Stack<ColorChange> redoStack)
        {
            this.undoStack = new ColorChange[undoStack.Count];
            this.redoStack = new ColorChange[redoStack.Count];
            undoStack.CopyTo(this.undoStack, 0);
            redoStack.CopyTo(this.redoStack, 0);
        }
    }
}
