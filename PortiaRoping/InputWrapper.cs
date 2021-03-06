using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using WindowsInput;
using WindowsInput.Native;

namespace PortiaRoping
{
    static class InputWrapper
    {
        private static InputSimulator _sim = new InputSimulator();

        public static void LeftDown()
        {
            _sim.Mouse.LeftButtonDown();
        }

        public static void LeftClick(Point pt)
        {
            Cursor.Position = pt;
            _sim.Mouse.LeftButtonClick();
        }

        public static void LeftClick(int x, int y)
        {
            LeftClick(new Point(x, y));
        }

        public static void RightClick(Point pt)
        {
            Cursor.Position = pt;
            _sim.Mouse.RightButtonClick();
        }

        public static void RightClick(int x, int y)
        {
            RightClick(new Point(x, y));
        }

        public static void ClickAndDrag(Point pt0, Point ptf)
        {
            Cursor.Position = pt0;
            _sim.Mouse.LeftButtonDown();
            Cursor.Position = ptf;
            _sim.Mouse.LeftButtonUp();
        }

        public static void ClickAndDrag(int x0, int y0, int xf, int yf)
        {
            ClickAndDrag(new Point(x0, y0), new Point(xf, yf));
        }

        public static void SendKey(VirtualKeyCode key)
        {
            _sim.Keyboard.KeyPress(key);
        }
    }
}