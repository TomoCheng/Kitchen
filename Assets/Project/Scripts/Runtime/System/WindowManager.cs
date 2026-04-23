using System;
using System.Runtime.InteropServices;
using Tomo.Core;

namespace Kitchen.System
{
	public class WindowManager : Singleton<WindowManager>
	{
		[DllImport("user32.dll")]
		private static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll")]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

		private const uint SWP_NOSIZE = 0x0001;
		private const uint SWP_NOMOVE = 0x0002;
		private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

		void Start()
		{
			// 預設啟動時就置頂
			SetAlwaysOnTop(true);
		}

		public void SetAlwaysOnTop(bool top)
		{
			// 注意：這只在 Windows Build 版本有效，編輯器內無效
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        IntPtr hWnd = GetActiveWindow();
        SetWindowPos(hWnd, top ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
        Debug.Log(top ? "視窗已設為最上層" : "視窗已取消置頂");
#endif
		}
	}
}