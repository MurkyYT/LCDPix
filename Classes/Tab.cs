using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCDPix.Classes
{
    internal class Tab
    {
        public PixelInfo[] pixels { get; private set; }
        public int index { get; private set; }
        public string name { get; private set; }
        List<Tab> tabs { get; set; }
        public ColorChange[] undoStack {get; private set;}
        public ColorChange[] redoStack { get; private set; }
        public Tab(PixelInfo[] pixels, string name, List<Tab> tabs)
        {
            this.pixels = pixels;
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
