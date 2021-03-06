using SciterSharp;
using SciterSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace OxyPlot
{
	class Host : BaseHost
	{
		public static PlotModel _model;
		public static LineSeries _serie;
		public static int _ipoint;

		static Host()
		{
			_model = new PlotModel()
			{
				PlotAreaBorderColor = OxyColors.Blue
			};

			_model.Axes.Add(new LinearAxis
			{
				Key = "xAxis",
				Position = AxisPosition.Bottom,
				MajorGridlineStyle = LineStyle.Solid,
				MinorGridlineStyle = LineStyle.Dash,
				MinimumRange = 20,
			});
			_model.Axes.Add(new LinearAxis
			{
				Key = "yAxis",
				Position = AxisPosition.Left,
				MinimumRange = 0,
				MaximumRange = 200,
			});


			_serie = new LineSeries
			{
				MarkerType = MarkerType.Circle,
				MarkerSize = 4,
				MarkerStroke = OxyColors.White,
			};
			_model.Series.Add(_serie);

			var rnd = new Random();
			for(_ipoint = 0; _ipoint< 20; _ipoint++)
				_serie.Points.Add(new DataPoint(_ipoint, rnd.Next(200)));
		}

		public Host(SciterWindow wnd)
		{
			var host = this;
			host.Setup(wnd);
			host.AttachEvh(new HostEvh());
			host.RegisterBehaviorHandler(typeof(LineChartGraphics));
			host.RegisterBehaviorHandler(typeof(LineChartPNG));
			host.SetupPage("index.html");
			wnd.Show();
		}
	}

	class HostEvh : SciterEventHandler
	{
		public bool Host_HelloWorld(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			result = new SciterValue("Hello World! (from native side)");
			return true;
		}
	}

	// This base class overrides OnLoadData and does the resource loading strategy
	// explained at http://misoftware.rs/Bootstrap/Dev
	//
	// - in DEBUG mode: resources loaded directly from the file system
	// - in RELEASE mode: resources loaded from by a SciterArchive (packed binary data contained as C# code in ArchiveResource.cs)
	class BaseHost : SciterHost
	{
		protected static SciterX.ISciterAPI _api = SciterX.API;
		protected SciterArchive _archive = new SciterArchive();
		protected SciterWindow _wnd;

		public BaseHost()
		{
		#if !DEBUG
			_archive.Open(SciterAppResource.ArchiveResource.resources);
		#endif
		}

		public void Setup(SciterWindow wnd)
		{
			_wnd = wnd;
			SetupWindow(wnd._hwnd);
		}

		public void SetupPage(string page_from_res_folder)
		{
		#if DEBUG
			string cwd = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ).Replace('\\', '/');

			#if OSX
			Environment.CurrentDirectory = cwd + "/../../../../..";
			#else
			Environment.CurrentDirectory = cwd + "/../..";
			#endif

			string path = Environment.CurrentDirectory + "/res/" + page_from_res_folder;
			Debug.Assert(File.Exists(path));

			string url = "file://" + path;
		#else
			string url = "archive://app/" + page_from_res_folder;
		#endif

			bool res = _wnd.LoadPage(url);
			Debug.Assert(res);
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
			if(sld.uri.StartsWith("archive://app/"))
			{
				// load resource from SciterArchive
				string path = sld.uri.Substring(14);
				byte[] data = _archive.Get(path);
				if(data!=null)
					_api.SciterDataReady(_wnd._hwnd, sld.uri, data, (uint) data.Length);
			}
			return SciterXDef.LoadResult.LOAD_OK;
		}
	}
}