using System;
using SciterSharp;

namespace OxyPlot
{
	public class Window : SciterWindow
	{
		public Window()
		{
			var wnd = this;
			wnd.CreateMainWindow(1200, 800);
			wnd.CenterTopLevelWindow();
			wnd.Title = "Sciter Bootstrap";
			#if WINDOWS
			wnd.Icon = Properties.Resources.IconMain;
			#endif
		}
	}
}