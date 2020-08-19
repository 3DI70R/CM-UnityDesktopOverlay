using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class TransparentWindow : MonoBehaviour
{
	private List<Func<Vector2, bool>> isFocusedHandlers 
		= new List<Func<Vector2, bool>>();

	private int screenWidth;
	private int screenHeight;
	private IntPtr hwnd = IntPtr.Zero;
	private Rectangle margins;
	private Rectangle windowRect;

	public void AddFocusHandler(Func<Vector2, bool> focusHandler)
	{
		isFocusedHandlers.Add(focusHandler);
	}

	public void RemoveFocusHandler(Func<Vector2, bool> focusHandler)
	{
		isFocusedHandlers.Remove(focusHandler);
	}

	private void Awake()
	{
		screenWidth = Screen.currentResolution.width;
		screenHeight = Screen.currentResolution.height;

		Screen.SetResolution(screenWidth, screenHeight, FullScreenMode.FullScreenWindow);
		Application.runInBackground = true;

		if (!Application.isEditor)
		{
			margins = new Rectangle { Left = -1 };
			hwnd = GetActiveWindow();

			UpdateWindowFlags();
			DwmExtendFrameIntoClientArea(hwnd, ref margins);
		}
	}

	private void Update()
	{
		UpdateWindowFlags();
	}

	private void UpdateWindowFlags()
	{
		if (!Application.isEditor)
		{
			GetWindowRect(hwnd, out windowRect);

			var isFocused = isFocusedHandlers.Any(f => f(Input.mousePosition));
			SetWindowLong(hwnd, GWL_STYLE, WindowStyle);
			SetWindowLong (hwnd, GWL_EXSTYLE,  isFocused ? ~WindowExStyle : WindowExStyle);
			SetLayeredWindowAttributes (hwnd, 0, 255, 2);
			SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, 
				screenWidth, screenHeight, 32 | 64);
		}
	}

	public void DragWindow()
	{
		if (!Application.isEditor)
		{
			if (Screen.fullScreenMode != FullScreenMode.Windowed)
			{
				return;
			}
			
			ReleaseCapture ();
			SendMessage(hwnd, WM_SYSCOMMAND, WM_MOUSE_MOVE, 0);
			Input.ResetInputAxes();
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Rectangle
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}
	
	/////////////////////
	//Windows DLL stuff//
	/////////////////////
	
	private const int GWL_STYLE = -16;
	private const int GWL_EXSTYLE = -20;
	private const uint WS_POPUP = 0x80000000;
	private const uint WS_VISIBLE = 0x10000000;
	private const uint WS_EX_LAYERED = 0x00080000;
	private const uint WS_EX_TRANSPARENT = 0x00000020;
	private const int HWND_TOPMOST = -1;
	
	private const uint WindowExStyle = WS_EX_LAYERED | WS_EX_TRANSPARENT;
	private const uint WindowStyle = WS_POPUP | WS_VISIBLE;

	private const int WM_SYSCOMMAND = 0x112;
	private const int WM_MOUSE_MOVE = 0xF012;

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();
	
	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

	[DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
	private static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

	[DllImport("user32.dll", EntryPoint = "GetWindowRect")]
	private static extern bool GetWindowRect(IntPtr hwnd, out Rectangle rect);
	
	[DllImport("user32.dll")]
	private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

	[DllImportAttribute("user32.dll")]
	private static extern bool ReleaseCapture();

	[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
	private static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);

	[DllImport("Dwmapi.dll")]
	private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Rectangle margins);
}