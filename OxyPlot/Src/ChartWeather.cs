using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using SciterSharp;
using SciterSharp.Interop;

namespace SciterBootstrap
{
	class ChartWeather : ChartBase
	{
		private CandleStickSeries _serie_candle;

		public ChartWeather()
		{
			_model = new PlotModel();

			var magnitudeAxis = new LinearAxis()
			{
				Minimum = 0,
				Maximum = 40,
				Title = "Temperature Cº",
				MajorGridlineThickness = 1,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineColor = OxyColor.Parse("#E9ECEF"),
				CropGridlines = true,
				TickStyle = TickStyle.None,
			};
			_model.Axes.Add(magnitudeAxis);

			var dateAxis = new DateTimeAxis()
			{
				StringFormat = "MMM dd",
				TickStyle = TickStyle.None,
				MaximumPadding = 0.05,// adds padding inside the view
				MinimumPadding = 0.05,
			};
			_model.Axes.Add(dateAxis);

			_serie_candle = new CandleStickSeries
			{
				IncreasingColor = OxyColors.BlueViolet,
				DecreasingColor = OxyColors.BlueViolet,
				CandleWidth = 0.2,
			};

			_model.Series.Add(_serie_candle);
		}

		protected override bool OnScriptCall(SciterElement se, string name, SciterValue[] args, out SciterValue result)
		{
			switch(name)
			{
				case "PlotForecastData":
					var l = new List<HighLowItem>();
					foreach(var item in args[0].AsEnumerable())
					{
						var dt = DateTime.Parse(item["date"].Get(""));
						int high = int.Parse(item["high"].Get(""));
						int low = int.Parse(item["low"].Get(""));
						Debug.Assert(high > low);
						var entry = new HighLowItem(DateTimeAxis.ToDouble(dt), high, low, high, low);
						l.Add(entry);
					}
					_serie_candle.ItemsSource = l;
					break;
			}

			result = null;
			return true;
		}
	}
}