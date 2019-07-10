using Panacea.Core;
using Panacea.Modularity.UiManager;
using Panacea.Modules.StreamingVideo.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Threading;
using static Panacea.Modules.StreamingVideo.ViewModels.IeContainerViewModel;

namespace Panacea.Modules.StreamingVideo.Views
{
    /// <summary>
    /// Interaction logic for IeContainer.xaml
    /// </summary>
    public partial class IeContainer : UserControl
    {
        private readonly Process _p;
        private readonly PanaceaServices _core;
        DispatcherTimer t;
        public IeContainer()
        {
            InitializeComponent();
        }
        public IeContainer(PanaceaServices core, Process p)
        {
            _p = p;
            _core = core;
            t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(1);
            t.Tick += T_Tick;
            InitializeComponent();
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        const UInt32 WM_CLOSE = 0x0010;
        private async void T_Tick(object sender, EventArgs e)
        {
            t.Stop();
            try
            {
                Move();
                await Task.Run(() =>
                {
                    foreach (var win in MyEnumWindows.GetRootWindowsOfProcess(_p.Id)
                   .Where(ptr => ptr != _p.MainWindowHandle))
                    {
                        try
                        {
                            SendMessage(win, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        }
                        catch { }
                    }
                });
            }
            catch
            {
            }
            if (IsLoaded && IsVisible)
                t.Start();
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_SHOWWINDOW = 0x0040;

        // Call this way:


        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_core.TryGetUiManager(out IUiManager ui))
            {
                ui.Paused += Host_Paused;
                //MouseRightClickDisable.HookInput();
                try
                {
                    _p.WaitForInputIdle();
                    while (_p.MainWindowHandle == IntPtr.Zero)
                    {
                        await Task.Delay(100);
                    }
                    IntPtr hWnd = _p.MainWindowHandle;
                    //
                    long style = GetWindowLongA(hWnd, -16);

                    long WS_CHILD = 0x40000000L;
                    long WS_POPUP = 0x80000000L;

                    style &= ~WS_POPUP;    //remove the POPUP style

                    style |= WS_CHILD;      //Add the Child style

                    var x = SetWindowLongPtr(hWnd, -16, (int)style);

                    SetParent(hWnd, Cont.Handle);

                    //SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    Move();
                    t.Start();

                }
                catch { }
            }
        }

        private void Host_Paused(object sender, EventArgs e)
        {
            if (_core.TryGetUiManager(out IUiManager ui))
            {
                ui.GoHome();
            }
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, long dwNewLong);


        [DllImport("User32", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern long GetWindowLongA(IntPtr hWnd, int nIndex);
        void Move()
        {
            PresentationSource source =
                PresentationSource.FromVisual(Sizer);
            var height = (int)Sizer.ActualHeight;
            int threshold = 8000;
            if (height > threshold) height = threshold;
            MoveWindow(_p.MainWindowHandle,
                   0,
                   0,
                   (int)Sizer.ActualWidth,
                   height,
                   false);
        }

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, int ProcessId);

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_core.TryGetUiManager(out IUiManager ui))
            {
                ui.Paused -= Host_Paused;
                t.Stop();
                try
                {
                    _p?.Kill();
                }
                catch { }
            }
            //MouseRightClickDisable.Stop();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            Move();

        }
    }
}
