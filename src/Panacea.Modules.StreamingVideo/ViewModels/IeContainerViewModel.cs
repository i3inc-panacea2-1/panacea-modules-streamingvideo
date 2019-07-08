using Panacea.Core;
using Panacea.Modularity.UiManager;
using Panacea.Modules.StreamingVideo.Views;
using Panacea.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Panacea.Modules.StreamingVideo.ViewModels
{
    [View(typeof(IeContainer))]
    public class IeContainerViewModel : ViewModelBase
    {
        private PanaceaServices _core;
        private Process _p;
        DispatcherTimer t;
        public IeContainerViewModel(PanaceaServices core, Process p)
        {
            _core = core;
            _p = p;
            t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(1);
            t.Tick += T_Tick;
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
            if (View.IsVisible && View.IsLoaded)
            {
                t.Start();
            }
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_SHOWWINDOW = 0x0040;

        // Call this way:

        public override async void Activate()
        {
            if (_core.TryGetUiManager(out IUiManager ui))
            {
                ui.Paused += Host_Paused;
            }
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

                //TODO:
                //SetParent(hWnd, Cont.Handle);

                ////SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                Move();
                t.Start();

            }
            catch { }
        }
        public override void Deactivate()
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
            //TODO:
            //PresentationSource source =
            //    PresentationSource.FromVisual(Sizer);
            //var height = (int)Sizer.ActualHeight;
            //int threshold = 8000;
            //if (height > threshold) height = threshold;
            //MoveWindow(_p.MainWindowHandle,
            //       0,
            //       0,
            //       (int)Sizer.ActualWidth,
            //       height,
            //       false);
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
    }


    public static class MyEnumWindows
    {
        public static List<IntPtr> GetRootWindowsOfProcess(int pid)
        {
            List<IntPtr> rootWindows = GetChildWindows(IntPtr.Zero);
            List<IntPtr> dsProcRootWindows = new List<IntPtr>();
            foreach (IntPtr hWnd in rootWindows)
            {
                uint lpdwProcessId;
                GetWindowThreadProcessId(hWnd, out lpdwProcessId);
                if (lpdwProcessId == pid)
                    dsProcRootWindows.Add(hWnd);
            }
            return dsProcRootWindows;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                Win32Callback childProc = new Win32Callback(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }

        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);
    }
}
