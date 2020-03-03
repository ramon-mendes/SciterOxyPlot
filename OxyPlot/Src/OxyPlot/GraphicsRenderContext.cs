using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;

namespace OxyPlot
{
	class GraphicsRenderContext : RenderContextBase, IDisposable
	{
		private SciterElement _se;
		private SciterGraphics _g;

		public GraphicsRenderContext(SciterElement se)
		{
			_se = se;
		}

		public void SetGraphicsTarget(SciterGraphics graphics)
		{
			_g = graphics;
		}

		public override void DrawEllipse(OxyRect extents, OxyColor fill, OxyColor stroke, double thickness = 1)
		{
			_g.FillColor = new RGBAColor(fill.R, fill.G, fill.B, fill.A);
			_g.LineColor = new RGBAColor(stroke.R, stroke.G, stroke.B, stroke.A);
			_g.LineWidth = (float)thickness;
			_g.Ellipse((float)extents.Center.X, (float)extents.Center.Y, (float)extents.Width/2, (float)extents.Height/2);
		}

		public override void DrawLine(IList<ScreenPoint> points, OxyColor stroke, double thickness, double[] dashArray, LineJoin lineJoin, bool aliased)
		{
			_g.LineColor = new RGBAColor(stroke.R, stroke.G, stroke.B, stroke.A);
			_g.LineWidth = (float) thickness;
			switch(lineJoin)
			{
				case LineJoin.Miter:
					_g.LineJoin = SciterXGraphics.SCITER_LINE_JOIN_TYPE.SCITER_JOIN_MITER;
					break;
				case LineJoin.Round:
					_g.LineJoin = SciterXGraphics.SCITER_LINE_JOIN_TYPE.SCITER_JOIN_ROUND;
					break;
				case LineJoin.Bevel:
					_g.LineJoin = SciterXGraphics.SCITER_LINE_JOIN_TYPE.SCITER_JOIN_BEVEL;
					break;
			}

			var gpoints = points.Select(p => Tuple.Create((float)p.X, (float)p.Y)).ToList();
			_g.Polyline(gpoints);
		}

		public override void DrawPolygon(IList<ScreenPoint> points, OxyColor fill, OxyColor stroke, double thickness, double[] dashArray, LineJoin lineJoin, bool aliased)
		{
			_g.FillColor = new RGBAColor(fill.R, fill.G, fill.B, fill.A);
			_g.LineColor = new RGBAColor(stroke.R, stroke.G, stroke.B, stroke.A);
			_g.LineWidth = (float)thickness;

			/*switch(lineJoin)
			{
				case LineJoin.Miter:
					_g.LineJoin = SciterXGraphics.SCITER_LINE_JOIN_TYPE.SCITER_JOIN_MITER;
					break;
				case LineJoin.Round:
					_g.LineJoin = SciterXGraphics.SCITER_LINE_JOIN_TYPE.SCITER_JOIN_ROUND;
					break;
				case LineJoin.Bevel:
					_g.LineJoin = SciterXGraphics.SCITER_LINE_JOIN_TYPE.SCITER_JOIN_BEVEL;
					break;
			}*/

			var gpoints = points.Select(p => Tuple.Create((float)p.X, (float)p.Y)).ToList();
			_g.Polygon(gpoints);
		}

		public override void DrawText(ScreenPoint p, string text, OxyColor fill, string fontFamily, double fontSize, double fontWeight, double rotate, HorizontalAlignment halign, VerticalAlignment valign, OxySize? maxSize)
		{
			/*SciterXGraphics.SCITER_TEXT_ALIGNMENT line_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_DEFAULT;
			switch(halign)
			{
				case HorizontalAlignment.Left:		line_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_START; break;
				case HorizontalAlignment.Center:	line_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_CENTER; break;
				case HorizontalAlignment.Right:		line_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_END; break;
			}

			SciterXGraphics.SCITER_TEXT_ALIGNMENT text_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_DEFAULT;
			switch(valign)
			{
				case VerticalAlignment.Top:		text_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_START; break;
				case VerticalAlignment.Middle:	text_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_CENTER; break;
				case VerticalAlignment.Bottom:	text_alg = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_END; break;
			}

			var txt = SciterText.Create(text, new SciterXGraphics.SCITER_TEXT_FORMAT
			{
				fontFamily = fontFamily,
				fontSize = (float) fontSize,
				fontWeight = (uint) fontWeight,
				lineAlignment = line_alg,
				textAlignment = text_alg
			});*/

			var txt = SciterText.CreateWithStyle(text, _se._he, "color: red;");

			if(maxSize.HasValue)
			{
				Debug.Assert(false);
				txt.SetBox((float) maxSize.Value.Width, (float) maxSize.Value.Height);
			}

			if(rotate != 0)
			{
				Debug.Assert(false);
				_g.Rotate((float) rotate, 0, 0);
			}

			_g.DrawText(txt, (float) p.X, (float) p.Y, 5);
		}

		public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight)
		{
			return new OxySize();

			/*if(text != null)
			{
				var txt = SciterText.Create(text, new SciterXGraphics.SCITER_TEXT_FORMAT
				{
					fontFamily = fontFamily,
					fontSize = (float)fontSize,
					fontWeight = (uint)fontWeight,
					lineAlignment = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_DEFAULT,
					textAlignment = SciterXGraphics.SCITER_TEXT_ALIGNMENT.TEXT_ALIGN_DEFAULT
				});
				var metrics = txt.Metrics;
				return new OxySize(metrics.minWidth, metrics.maxWidth);
			}
			return new OxySize();*/
		}

		public override void ResetClip()
		{
			_g.PopClip();
		}
		public override bool SetClip(OxyRect rect)
		{
			_g.PushClipBox((float) rect.Left, (float) rect.Top, (float) rect.Right, (float) rect.Bottom);
			return true;
		}

		public override void DrawImage(OxyImage source, double srcX, double srcY, double srcWidth, double srcHeight, double destX, double destY, double destWidth, double destHeight, double opacity, bool interpolate)
		{
			Debug.Assert(source.Format == ImageFormat.Unknown);

			/*byte[] data = source.GetData();
			GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
			IntPtr pointer = pinnedArray.AddrOfPinnedObject();

			var img = new SciterImage(data, source.Width, source.Height);
			_g.BlendImage();

			pinnedArray.Free();*/
		}

		public void Dispose()
		{
		}
	}
}