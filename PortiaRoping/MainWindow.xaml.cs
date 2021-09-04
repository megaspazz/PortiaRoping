using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WindowsInput.Native;

namespace PortiaRoping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnTakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Console.WriteLine(WindowWrapper.GetClientArea(WindowWrapper.GetForegroundWindow()));
            Bitmap pic = WindowWrapper.TakeClientPicture();
            pic.Save(@"screenshot.png", ImageFormat.Png);
            Bitmap animal = WindowWrapper.ScreenCapture(ANIMAL_RECT);
            animal.Save(@"animal.png", ImageFormat.Png);
        }

        private static readonly int ANIMAL_Y_MIN = 346;
        private static readonly int ANIMAL_Y_MAX = 616;
        private static readonly int ANIMAL_X_MIN = 798;
        private static readonly int ANIMAL_X_MAX = 1622;
        private static readonly Rectangle ANIMAL_RECT = new Rectangle(ANIMAL_X_MIN, ANIMAL_Y_MIN, ANIMAL_X_MAX - ANIMAL_X_MIN, ANIMAL_Y_MAX - ANIMAL_Y_MIN);
        private static readonly int CENTER_OFFSET_X = -40;
        private static readonly int TARGET_X = 960 - ANIMAL_X_MIN + CENTER_OFFSET_X;
        private static readonly TimeSpan FIRE_TIME = TimeSpan.FromMilliseconds(240);
        private static readonly int CENTER_X = 960 - ANIMAL_X_MIN;
        private static readonly int CENTER_Y = 540 - ANIMAL_Y_MIN;
        private static readonly int PLAY_AGAIN_X = 960;
        private static readonly int PLAY_AGAIN_Y = 742;

        private static readonly int CENTER_END_R = 239;
        private static readonly int CENTER_END_G = 166;
        private static readonly int CENTER_END_B = 100;
        private static readonly int[] CENTER_PIXEL = { CENTER_END_R, CENTER_END_G, CENTER_END_B };

        private static readonly VirtualKeyCode INVENTORY_KEY = VirtualKeyCode.VK_V;
        private static readonly int INVENTORY_X = 1096 - ANIMAL_X_MIN;
        private static readonly int INVENTORY_Y = 485 - ANIMAL_Y_MIN;
        private static readonly System.Drawing.Point INVENTORY_POINT = new System.Drawing.Point(INVENTORY_X, INVENTORY_Y);
        private static readonly System.Drawing.Size INVENTORY_SIZE = new System.Drawing.Size(4, 4);
        private static readonly Rectangle INVENTORY_BLUE_RECT = new Rectangle(INVENTORY_POINT, INVENTORY_SIZE);
        private static readonly int[] INVENTORY_PIXEL = { 85, 189, 229 };

        private static readonly int[] STAMINA_PIXEL = { 250, 213, 148 };
        private static readonly int STAMINA_X = 331;
        private static readonly int STAMINA_Y = 578;

        private static readonly int MILK_X = 1207;
        private static readonly int MILK_Y = 358;

        private static readonly VirtualKeyCode INTERACT_KEY = VirtualKeyCode.VK_E;

        private static readonly string GAME_WINDOW_TITLE_TEXT = "My Time at Portia";

        private void btnTrialRun_Click(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Stopwatch swInventory = Stopwatch.StartNew();
            Bitmap24 prev = null;
            //Bitmap24 prev = Bitmap24.FromImage(WindowWrapper.ScreenCapture(ANIMAL_RECT));
            TimeSpan prevTime = sw.Elapsed;
            TimeSpan appearTime = TimeSpan.Zero;
            int appearSteps = -1;
            int appearX = -1;
            int w = ANIMAL_RECT.Width;
            int h = ANIMAL_RECT.Height;
            for (int t = 0; t <= 999999999; ++t)
            {
                if (!inGame())
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    continue;
                }
                TimeSpan beforePicture = sw.Elapsed;
                Bitmap bmp = WindowWrapper.ScreenCapture(ANIMAL_RECT);
                //Bitmap check = WindowWrapper.ScreenCapture(new Rectangle(250, 250, 1, 1));
                TimeSpan afterPicture = sw.Elapsed;
                Bitmap24 curr = Bitmap24.FromImage(bmp);
                TimeSpan currTime = sw.Elapsed;
                Console.WriteLine("t = {0}, take picture time = {1}, convert picture time = {2}", t, afterPicture - beforePicture, currTime - afterPicture);
                //curr.Bitmap.Save(t.ToString().PadLeft(4, '0') + @".png", ImageFormat.Png);
                curr.Lock();
                if (deltaMagnitude(CENTER_PIXEL, curr.GetPixel(CENTER_X, CENTER_Y)) < 8)
                {
                    InputWrapper.LeftClick(PLAY_AGAIN_X, PLAY_AGAIN_Y);
                    prev = null;
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    continue;
                }
                if (swInventory.Elapsed > TimeSpan.FromSeconds(1))
                {
                    if (!checkInventory(curr))
                    {
                        Console.WriteLine("t = {0}, not in inventory", t);
                        InputWrapper.SendKey(INVENTORY_KEY);
                    }
                    else
                    {
                        while (true)
                        {
                            if (!inGame())
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(1));
                                continue;
                            }
                            bool done;
                            using (Bitmap24 b24 = Bitmap24.FromImage(WindowWrapper.TakeClientPicture()))
                            {
                                b24.Lock();
                                done = hasEnoughStamina(b24);
                            }
                            if (done)
                            {
                                InputWrapper.SendKey(INVENTORY_KEY);
                                Stopwatch swInteract = Stopwatch.StartNew();
                                while (swInteract.Elapsed < TimeSpan.FromSeconds(5))
                                {
                                    InputWrapper.SendKey(INTERACT_KEY);
                                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                                }
                                Thread.Sleep(TimeSpan.FromSeconds(1));
                                break;
                            }
                            InputWrapper.RightClick(MILK_X, MILK_Y);
                            Thread.Sleep(TimeSpan.FromMilliseconds(250));
                        }
                    }
                    swInventory.Restart();
                }
                if (prev != null)
                {
                    int minX = int.MaxValue;
                    double sumDeltaX = 0;
                    int countDeltaX = 0;
                    double sumDeltaY = 0;
                    int countDeltaY = 0;
                    for (int x = 0; x < w; ++x)
                    {
                        for (int y = 0; y < h; ++y)
                        {
                            int[] prevPixel = prev.GetPixel(x, y);
                            int[] currPixel = curr.GetPixel(x, y);
                            double deltaMag = deltaMagnitude(prevPixel, currPixel);
                            if (deltaMag > 100)
                            {
                                minX = Math.Min(minX, x);
                                sumDeltaX += x;
                                ++countDeltaX;
                                sumDeltaY += y;
                                ++countDeltaY;
                                //prev.SetPixel(x, y, 255, 0, 0);
                            }
                        }
                    }
                    //prev.Unlock();
                    //prev.Bitmap.Save(@"delta-" + t.ToString().PadLeft(2, '0') + @".png");
                    double avgDeltaX = sumDeltaX / countDeltaX;
                    double avgDeltaY = sumDeltaY / countDeltaY;
                    TimeSpan deltaTime = currTime - prevTime;
                    if (minX != int.MaxValue)
                    {
                        if (appearX < 0)
                        {
                            appearX = minX;
                            appearTime = currTime;
                        }
                        ++appearSteps;
                        if (appearSteps > 0)
                        {
                            TimeSpan timePerStep = new TimeSpan((currTime - appearTime).Ticks / appearSteps);
                            double xPerStep = (0.0 + minX - appearX) / appearSteps;
                            long elapsedTicks = (currTime - appearTime).Ticks;
                            long totalX = appearX - minX;
                            double xPerTick = 1.0 * totalX / elapsedTicks;
                            double aimX = minX - FIRE_TIME.Ticks * xPerTick;
                            Console.WriteLine("t = {0}, elapsedTicks = {1}, totalX = {2}, xPerTick = {3}, aimX = {4}", t, elapsedTicks, totalX, xPerTick, aimX);
                            if (Math.Abs(aimX - TARGET_X) < 15)
                            {
                                InputWrapper.LeftClick(0, 0);
                                curr = null;
                                Console.WriteLine("t = {0}: FIRE!!!", t);
                                Thread.Sleep(TimeSpan.FromSeconds(3));
                            }
                        }
                    }
                    else
                    {
                        appearX = -1;
                        appearSteps = -1;
                    }
                    Console.WriteLine("t = {0}, deltaTime = {1}, avgDeltaX = {2}, avgDeltaY = {3}, minX = {4}, appearX = {5}, appearSteps = {6}", t, deltaTime, avgDeltaX, avgDeltaY, minX, appearX, appearSteps);
                }
                prev = curr;
                prevTime = currTime;
                //Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
        }

        private static bool inGame()
        {
            return WindowWrapper.GetText() == GAME_WINDOW_TITLE_TEXT;
        }

        private static bool hasEnoughStamina(Bitmap24 fullscreen)
        {
            return deltaMagnitude(fullscreen.GetPixel(STAMINA_X, STAMINA_Y), STAMINA_PIXEL) < 16;
        }

        private static bool checkInventory(Bitmap24 img)
        {
            for (int x = INVENTORY_BLUE_RECT.Left; x < INVENTORY_BLUE_RECT.Right; ++x)
            {
                for (int y = INVENTORY_BLUE_RECT.Top; y < INVENTORY_BLUE_RECT.Bottom; ++y)
                {
                    if (deltaMagnitude(img.GetPixel(x, y), INVENTORY_PIXEL) > 24)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static double deltaMagnitude(int[] px1, int[] px2)
        {
            double deltaMagnitude2 = 0;
            for (int i = 0; i < px1.Length; ++i)
            {
                double d = px1[i] - px2[i];
                deltaMagnitude2 += d * d;
            }
            return Math.Sqrt(deltaMagnitude2);
        }

        private void btnFastRecord_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i <= 999; ++i)
            {
                Console.WriteLine("i = {0}, t = {1}", i, sw.Elapsed);
                Bitmap pic = WindowWrapper.TakeClientPicture();
                pic.Save(i.ToString().PadLeft(4, '0') + @".png");
            }
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Directory.GetFiles(".", "*.png");
            foreach (string f in files)
            {
                File.Delete(f);
            }
        }
    }
}
