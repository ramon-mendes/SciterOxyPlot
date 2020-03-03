using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot.WindowsForms;
using SciterSharp;
using SciterSharp.Interop;

namespace OxyPlot
{
	class LineChartPNG : SciterEventHandler
	{
		protected override bool OnDraw(SciterElement se, SciterXBehaviors.DRAW_PARAMS prms)
		{
			if(prms.cmd == SciterXBehaviors.DRAW_EVENTS.DRAW_CONTENT)
			{
				var pngExporter = new PngExporter()
				{
					Width = prms.area.Width,
					Height = prms.area.Height,
					Background = OxyColors.Transparent
				};

				using(var ms = new MemoryStream())
				{
					var bmp = pngExporter.ExportToBitmap(Host._model);

					using(var img = new SciterImage(bmp))
					{
						new SciterGraphics(prms.gfx).BlendImage(img, prms.area.left, prms.area.top);
					}
				}

				return true;
			}
			return false;
		}
	}
}