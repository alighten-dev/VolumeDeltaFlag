//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class VolumeDeltaFlag : Indicator
	{
		private double volumeUp;
		private double volumeDown;
		private bool isFirstBarInUpFlag;
		private bool isFirstBarInDownFlag;
		private List<double> upFlagPeaks;
		private List<double> downFlagPeaks;
		private int MaxArraySize;
		//private double Threshold;
		private double upPeak;
		private double downPeak;
		private double delta;
		private double ratio;
		private NinjaTrader.Gui.Tools.SimpleFont myFont;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Calculate					= Calculate.OnBarClose;
				Description					= @"Sums consecutive volume bars creating volume flags. Subtracts Bull volume flags from Bear volume flags to display Volume imbalance.";
				Name						= "Volume Delta Flags";
				DrawOnPricePanel			= false;
				IsOverlay					= false;
				IsSuspendedWhileInactive	= true;
				

				AddPlot(new Stroke(Brushes.DarkCyan, 2), PlotStyle.Bar, NinjaTrader.Custom.Resource.VolumeUp);
				AddPlot(new Stroke(Brushes.Crimson, 2), PlotStyle.Bar, NinjaTrader.Custom.Resource.VolumeDown);
				AddLine(Brushes.DarkGray, 0, NinjaTrader.Custom.Resource.NinjaScriptIndicatorZeroLine);
				
				isFirstBarInUpFlag = true;
				isFirstBarInDownFlag = true;
				MaxArraySize = 2;
				Threshold = 12;
				ratio = .05;
				myFont = new NinjaTrader.Gui.Tools.SimpleFont("Courier New", 12) { Size = 25, Bold = true };
			}
			else if (State == State.Historical)
			{
				if (Calculate == Calculate.OnPriceChange)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", string.Format(NinjaTrader.Custom.Resource.NinjaScriptOnPriceChangeError, Name), TextPosition.BottomRight);
					Log(string.Format(NinjaTrader.Custom.Resource.NinjaScriptOnPriceChangeError, Name), LogLevel.Error);
				}
			}
			else if (State == State.DataLoaded)
			{
				ClearOutputWindow();
				
				upFlagPeaks	= new List<double>();
				downFlagPeaks	= new List<double>();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1)
                return;
			
			if (Close[0] >= Open[0])
			{		
				if (isFirstBarInUpFlag)
				{
					downFlagPeaks.Add(DownVolume[1]);
					if (downFlagPeaks.Count > MaxArraySize)
						downFlagPeaks.RemoveAt(0);
					
					if ((upFlagPeaks.Count > 1) && (downFlagPeaks.Count > 1))
					{
						downPeak =  Math.Abs(downFlagPeaks[downFlagPeaks.Count - 1]);
						upPeak = Convert.ToDouble(upFlagPeaks[upFlagPeaks.Count - 1]);
						
						double averageDownPeaks = downFlagPeaks.Count > 0 ? Math.Abs(downFlagPeaks.Average()) : 0.0;
						double averageUpPeaks = upFlagPeaks.Count > 0 ? Math.Abs(upFlagPeaks.Average()) : 0.0;
						ratio = 100/averageDownPeaks;
						delta = Math.Round(((downPeak - upPeak)/(100)), 1);
						//delta = Math.Round(((downPeak - upPeak)/(downPeak*ratio)), 1);
						//delta = Math.Round(((downPeak/averageDownPeaks) - (upPeak/averageUpPeaks)), 1);
						//delta = Math.Round((downPeak / upPeak), 1);
						//delta = Math.Round(((downPeak - averageDownPeaks)/100), 1);
						if (delta >= Threshold)
						{
							
//							Print("Down Flag Complete: " + Time[0]);						
//							Print("Latest Down Flag Size: " + downPeak);
//							Print("Average Down Flag Size: " + averageDownPeaks);
//							Print("Previous Up Flag Size: " + upPeak);
//							Print("Delta Up: " + delta);
							
							double top = Close[0] + 10; 
							double bottom = Close[0] - 10;
							Draw.Line(this, CurrentBar + "-VerticalLine", true, 0, bottom, 0, top, Brushes.LimeGreen, DashStyleHelper.Solid, 7); 
							DrawOnPricePanel = true;
							Draw.Line(this, CurrentBar + "-HorizontalLine", true, 7, Close[0], -7, Close[0], Brushes.LimeGreen, DashStyleHelper.Solid, 7);
							DrawOnPricePanel = true;
							Draw.Text(this, CurrentBar + "-DeltaValue", false, delta.ToString(), 0, Low[0], -35, Brushes.LimeGreen, myFont, TextAlignment.Center, Brushes.Transparent, null, 1);
							DrawOnPricePanel = true;

						}
					}
					isFirstBarInUpFlag = false;
					isFirstBarInDownFlag = true;
				}
				volumeUp = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];
				UpVolume[0] = UpVolume[1] + volumeUp;
				DownVolume.Reset();
			}
			else
			{
				if (isFirstBarInDownFlag)
				{
					upFlagPeaks.Add(UpVolume[1]);
					if (upFlagPeaks.Count > MaxArraySize)
						upFlagPeaks.RemoveAt(0);
					
					if ((upFlagPeaks.Count > 1) && (downFlagPeaks.Count > 1))
					{
						downPeak =  Math.Abs(downFlagPeaks[downFlagPeaks.Count - 1]);
						upPeak = Convert.ToDouble(upFlagPeaks[upFlagPeaks.Count - 1]);
						
						
						double averageUpPeaks = upFlagPeaks.Count > 0 ? upFlagPeaks.Average() : 0.0;
						double averageDownPeaks = downFlagPeaks.Count > 0 ? downFlagPeaks.Average() : 0.0;
						ratio = 100/averageUpPeaks;
						delta = Math.Round(((upPeak - downPeak)/(100)), 1);
						//delta = Math.Round(((upPeak - downPeak)/(upPeak*ratio)), 1);
						//delta = Math.Round(((upPeak/averageUpPeaks) - (downPeak/averageDownPeaks)), 1);
						//delta = Math.Round((upPeak / downPeak), 1);
						//delta = Math.Round(((upPeak - averageUpPeaks)/100), 1);
						
						if (delta >= Threshold)
						{
//							Print("Up Flag Complete: " + Time[0]);
//							Print("Latest Up Flag Size: " + upPeak);
//							Print("Average Up Flag Size: " + averageUpPeaks);
//							Print("Previous Down Flag Size: " + downPeak);
//							Print("Delta Up: " + delta);
							
							double top = Close[0] + 10; 
							double bottom = Close[0] - 10;
							Draw.Line(this, CurrentBar + "-VerticalLine", true, 0, bottom, 0, top, Brushes.Crimson, DashStyleHelper.Solid, 7); 
							DrawOnPricePanel = true;
							Draw.Line(this, CurrentBar + "-HorizontalLine", true, 7, Close[0], -7, Close[0], Brushes.Crimson, DashStyleHelper.Solid, 7);
							DrawOnPricePanel = true;
							Draw.Text(this, CurrentBar + "-DeltaValue", false, delta.ToString(), 0, High[0], 35, Brushes.Crimson, myFont, TextAlignment.Center, Brushes.Transparent, null, 1);
							DrawOnPricePanel = true;

						}
					}
					isFirstBarInDownFlag = false;
					isFirstBarInUpFlag = true;
				}
				volumeDown = Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];
				DownVolume[0] = (DownVolume[1] + (-1*volumeDown));
				UpVolume.Reset();		
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Threshold", Order=1, GroupName="Parameters")]
		public double Threshold
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DownVolume
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> UpVolume
		{
			get { return Values[0]; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VolumeDeltaFlag[] cacheVolumeDeltaFlag;
		public VolumeDeltaFlag VolumeDeltaFlag(double threshold)
		{
			return VolumeDeltaFlag(Input, threshold);
		}

		public VolumeDeltaFlag VolumeDeltaFlag(ISeries<double> input, double threshold)
		{
			if (cacheVolumeDeltaFlag != null)
				for (int idx = 0; idx < cacheVolumeDeltaFlag.Length; idx++)
					if (cacheVolumeDeltaFlag[idx] != null && cacheVolumeDeltaFlag[idx].Threshold == threshold && cacheVolumeDeltaFlag[idx].EqualsInput(input))
						return cacheVolumeDeltaFlag[idx];
			return CacheIndicator<VolumeDeltaFlag>(new VolumeDeltaFlag(){ Threshold = threshold }, input, ref cacheVolumeDeltaFlag);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VolumeDeltaFlag VolumeDeltaFlag(double threshold)
		{
			return indicator.VolumeDeltaFlag(Input, threshold);
		}

		public Indicators.VolumeDeltaFlag VolumeDeltaFlag(ISeries<double> input , double threshold)
		{
			return indicator.VolumeDeltaFlag(input, threshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VolumeDeltaFlag VolumeDeltaFlag(double threshold)
		{
			return indicator.VolumeDeltaFlag(Input, threshold);
		}

		public Indicators.VolumeDeltaFlag VolumeDeltaFlag(ISeries<double> input , double threshold)
		{
			return indicator.VolumeDeltaFlag(input, threshold);
		}
	}
}

#endregion
