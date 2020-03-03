using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;

namespace OxyPlot
{
	class SciterPlot : SciterEventHandler, IPlotView
	{
		private static readonly OxyRect EmptyRect = new OxyRect();

		private SciterElement _se;
		private GraphicsRenderContext _renderContext;
		private Cursor _cursor;
		private bool _isModelInvalidated;
		private bool _updateDataFlag = true;
		private readonly object _invalidateLock = new object();
		private readonly object _renderingLock = new object();
		private readonly object _modelLock = new object();
		/// <summary>
		/// The current model (holding a reference to this plot view).
		/// </summary>
		private PlotModel _currentModel;
		private PlotModel _model;

		private OxyRect _zoomRectangle;
		private TrackerHitResult _tracker;

		#region Props
		public PlotModel Model
		{
			get
			{
				return _model;
			}

			set
			{
				if(_model != value)
				{
					_model = value;
					OnModelChanged();
				}
			}
		}

		public IPlotController Controller { get; set; }
		#endregion

		public void OnModelChanged()
		{
			lock(_modelLock)
			{
				if(_currentModel != null)
				{
					((IPlotModel)_currentModel).AttachPlotView(null);
					_currentModel = null;
				}

				if(Model != null)
				{
					((IPlotModel)Model).AttachPlotView(this);// this call seems to be the purpouse of OnModelChanged
					_currentModel = this.Model;
				}
			}

			InvalidatePlot(true);
		}

		#region Sciter EVH
		protected override void Attached(SciterElement se)
		{
			_se = se;
			_renderContext = new GraphicsRenderContext(se);
		}

		protected override void Detached(SciterElement se)
		{
			Debug.Assert(se == _se);
			_se = null;
			_renderContext.Dispose();
		}

		protected override bool OnMouse(SciterElement se, SciterXBehaviors.MOUSE_PARAMS prms)
		{
			switch(prms.cmd)
			{
				case SciterXBehaviors.MOUSE_EVENTS.MOUSE_ENTER:
					ActualController.HandleMouseEnter(this, new OxyMouseEventArgs
					{
						Position = new ScreenPoint(prms.pos.X, prms.pos.Y),
						ModifierKeys = prms.alt_state.GetModifiers()
					});
					break;

				case SciterXBehaviors.MOUSE_EVENTS.MOUSE_LEAVE:
					ActualController.HandleMouseLeave(this, new OxyMouseEventArgs
					{
						Position = new ScreenPoint(prms.pos.X, prms.pos.Y),
						ModifierKeys = prms.alt_state.GetModifiers()
					});
					break;

				case SciterXBehaviors.MOUSE_EVENTS.MOUSE_MOVE:
					ActualController.HandleMouseMove(this, new OxyMouseEventArgs
					{
						Position = new ScreenPoint(prms.pos.X, prms.pos.Y),
						ModifierKeys = prms.alt_state.GetModifiers()
					});
					break;

				case SciterXBehaviors.MOUSE_EVENTS.MOUSE_DCLICK:
				case SciterXBehaviors.MOUSE_EVENTS.MOUSE_DOWN:
					OxyMouseButton button = OxyMouseButton.None;
					switch(prms.button_state)
					{
						case (uint) SciterXBehaviors.MOUSE_BUTTONS.MAIN_MOUSE_BUTTON:	button = OxyMouseButton.Left; break;
						case (uint) SciterXBehaviors.MOUSE_BUTTONS.PROP_MOUSE_BUTTON:	button = OxyMouseButton.Right; break;
						case (uint) SciterXBehaviors.MOUSE_BUTTONS.MIDDLE_MOUSE_BUTTON:	button = OxyMouseButton.Middle; break;
					}

					ActualController.HandleMouseDown(this, new OxyMouseDownEventArgs
					{
						ChangedButton = button,
						ClickCount = prms.cmd == SciterXBehaviors.MOUSE_EVENTS.MOUSE_DCLICK ? 2 : 1,
						Position = new ScreenPoint(prms.pos.X, prms.pos.Y),
						ModifierKeys = prms.alt_state.GetModifiers()
					});
					break;

				case SciterXBehaviors.MOUSE_EVENTS.MOUSE_UP:
					ActualController.HandleMouseUp(this, new OxyMouseEventArgs
					{
						Position = new ScreenPoint(prms.pos.X, prms.pos.Y),
						ModifierKeys = prms.alt_state.GetModifiers()
					});
					break;

				case SciterXBehaviors.MOUSE_EVENTS.MOUSE_WHEEL:
					ActualController.HandleMouseWheel(this, new OxyMouseWheelEventArgs
					{
						Delta = (prms.button_state==1 ? 1 : -1) * SystemInformation.MouseWheelScrollDelta,
						Position = new ScreenPoint(prms.pos.X, prms.pos.Y),
						ModifierKeys = prms.alt_state.GetModifiers()
					});
					break;
			}
			return false;
		}

		protected override bool OnKey(SciterElement se, SciterXBehaviors.KEY_PARAMS prms)
		{
			if(prms.cmd == (uint)SciterXBehaviors.KEY_EVENTS.KEY_DOWN)
			{
				Keys key = (Keys) prms.key_code;

				var args = new OxyKeyEventArgs { ModifierKeys = prms.alt_state.GetModifiers(), Key = key.Convert() };
				return ActualController.HandleKeyDown(this, args);
			}
			return false;
		}

		protected override bool OnDraw(SciterElement se, SciterXBehaviors.DRAW_PARAMS prms)
		{
			if(prms.cmd == SciterXBehaviors.DRAW_EVENTS.DRAW_CONTENT)
			{
				lock(_invalidateLock)
				{
					if(_isModelInvalidated)
					{
						if(_model != null)
						{
                            ((IPlotModel) _model).Update(this._updateDataFlag);
							_updateDataFlag = false;
						}

						_isModelInvalidated = false;
					}
				}

				lock(_renderingLock)
				{
					using(var gfx = new SciterGraphics(prms.gfx))
					{
						_renderContext.SetGraphicsTarget(gfx);

						gfx.StateSave();
						gfx.Translate(prms.area.left + 0.5f, prms.area.top + 0.5f);

						if(_model != null)
						{
							var size = _se.SizePadding;
							((IPlotModel)_model).Render(_renderContext, size.cx, size.cy);
						}

						// zoomRectangle, empty?
						if(_zoomRectangle.Width != 0 && _zoomRectangle.Height != 0)
						{
							gfx.FillColor = new RGBAColor(0xFF, 0xFF, 0x00, 0x40);
							gfx.Rectangle((float)_zoomRectangle.Left, (float)_zoomRectangle.Top, (float)_zoomRectangle.Right, (float)_zoomRectangle.Bottom);
						}

						if(_tracker != null)
						{
							Debug.Assert(_tracker.Item != null);
							Debug.Assert(_tracker.Text != null);
							gfx.FillColor = new RGBAColor(255, 0, 0);
							gfx.Ellipse((float) _tracker.Position.X, (float)_tracker.Position.Y, 10, 10);
						}

						gfx.StateRestore();
					}
				}
			}
			return false;
		}
		#endregion

		#region IPlotView
		private IPlotController defaultController;

		public IController ActualController
		{
			get
			{
				return this.Controller ?? (this.defaultController ?? (this.defaultController = new PlotController()));
			}
		}

		public OxyRect ClientArea
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public PlotModel ActualModel
		{
			get
			{
				return this.Model;
			}
		}

		Model IView.ActualModel
		{
			get
			{
				return this.Model;
			}
		}

		

		public void InvalidatePlot(bool updateData)
		{
			lock(_invalidateLock)
			{
				_isModelInvalidated = true;
				_updateDataFlag = _updateDataFlag || updateData;
			}

			if(_se != null)
				_se.Refresh();
		}

		public void SetClipboardText(string text)
		{
			try
			{
				// todo: can't get the following solution to work
				// http://stackoverflow.com/questions/5707990/requested-clipboard-operation-did-not-succeed
				Clipboard.SetText(text);
			}
			catch(ExternalException ee)
			{
				// Requested Clipboard operation did not succeed.
				SciterSharp.MessageBox.Show(IntPtr.Zero, ee.Message, "OxyPlot");
			}
		}

		public void SetCursorType(CursorType cursorType)
		{
			switch(cursorType)
			{
				case CursorType.Pan:
					_cursor = Cursors.Hand;
					break;
				case CursorType.ZoomRectangle:
					_cursor = Cursors.SizeNWSE;
					break;
				case CursorType.ZoomHorizontal:
					_cursor = Cursors.SizeWE;
					break;
				case CursorType.ZoomVertical:
					_cursor = Cursors.SizeNS;
					break;
				default:
					_cursor = Cursors.Arrow;
					break;
			}
		}

		public void ShowTracker(TrackerHitResult trackerHitResult)
		{
			_tracker = trackerHitResult;
			_se.Refresh();
		}

		public void HideTracker()
		{
			_tracker = null;
			_se.Refresh();
		}

		public void ShowZoomRectangle(OxyRect rectangle)
		{
			_zoomRectangle = rectangle;
			_se.Refresh();
		}

		public void HideZoomRectangle()
		{
			_zoomRectangle = EmptyRect;
			_se.Refresh();
		}
		#endregion
	}
}