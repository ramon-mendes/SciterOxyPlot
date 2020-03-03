using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace OxyPlot
{
	class LineChartGraphics : SciterPlot
	{
		public LineChartGraphics()
		{
			Model = Host._model;
		}
	}
}