#region Using declarations
using System;
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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class EntryRisk : Indicator
	{
		private double EmaValue = 0.0;
		private int BoxStartBarnum = 0;
		private int BoxLength = 0;
		private double HighestHigh = 0.0;
		private double LowestLow = 0.0;
		private string TopName = "TopBox";
		private string BottomName = "BottomBox";
		
		// buttons 
		private System.Windows.Controls.Button mySellButton;
		private System.Windows.Controls.Grid   myGrid; 
		private int ToggleNum = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "EntryRisk";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				LineColor					= Brushes.DarkGray;
				BoxColor					= Brushes.Gainsboro;
				TextColor					= Brushes.DarkGray;
				EmaLength					= 9;
				ShowBox						= true;
				ShowRisk					= true;
				ShowTarget					= true;
				RiskInPoints				= 4;
				Bearish						= true;
				MaxBoxSize 					= 20;
			}
			else if (State == State.Configure)
			{
			}
			// Once the NinjaScript object has reached State.Historical, our custom control can now be added to the chart
			else if (State == State.Historical)
			  {
				Print("State.Historical"); 
			    ChartControl.Dispatcher.InvokeAsync((() =>
			    { 
			        if (UserControlCollection.Contains(myGrid))
			          return; 
			        myGrid = new System.Windows.Controls.Grid
			        {
			          Name = "MyCustomGrid", 
			          HorizontalAlignment = HorizontalAlignment.Right,
			          VerticalAlignment = VerticalAlignment.Bottom,
			        }; 
			        System.Windows.Controls.ColumnDefinition column1 = new System.Windows.Controls.ColumnDefinition(); 
			        myGrid.ColumnDefinitions.Add(column1); 
			        mySellButton = new System.Windows.Controls.Button
			        {
			          Name = "MySellButton",
			          Content = "O F F",
			          Foreground = Brushes.Black,
			          Background = Brushes.Gainsboro
			        }; 
			        mySellButton.Click += OnMyButtonClick; 
			        System.Windows.Controls.Grid.SetColumn(mySellButton, 1); 
			        myGrid.Children.Add(mySellButton); 
			        UserControlCollection.Add(myGrid);
			 
			    }));
			  }
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 20 ) { return; }
			EmaValue = EMA(EmaLength)[0];
			
			//  if toping area
			if ( ToggleNum == 1 ) { //  top area
				// if Ema 9 > Ema 18 -> 
				if ( IsFirstTickOfBar && CrossAbove(EMA(EmaLength), EMA(EmaLength * 2), 1) ) {
					// start counting to have box length
					BoxStartBarnum = CurrentBar;
					Draw.ArrowUp(this, "up"+CurrentBar, false, 1, Low[1], Brushes.Green);
				}
				
				BoxLength = CurrentBar - BoxStartBarnum;
				if ( BoxLength > MaxBoxSize ) { BoxLength = MaxBoxSize; }
				// find highest 
				if (BoxLength > 0 ) {
					HighestHigh = MAX(High, BoxLength)[0] + TickSize;
					//Draw.Dot(this, "HH" + CurrentBar, false, 0, HighestHigh, Brushes.Blue);
				}
				
				// draw a 4 pt box 
				double TargetLevel = HighestHigh - 8.0;
				BoxConstructor( BoxLength: BoxLength , BoxTopPrice: HighestHigh, TargetLevel: TargetLevel, BoxName: TopName);
			}
			
			//  if bottoming area
			if ( ToggleNum == 2 ) {		// bottom area
				RemoveDrawObject(TopName);
				// if Ema 9 > Ema 18 -> 
				if ( IsFirstTickOfBar && CrossBelow(EMA(EmaLength), EMA(EmaLength * 2), 1) ) {
					// start counting to have box length
					BoxStartBarnum = CurrentBar;
					Draw.ArrowDown(this, "dnp"+CurrentBar, false, 1, High[1], Brushes.Red);
				}
				
				BoxLength = CurrentBar - BoxStartBarnum;
				if ( BoxLength > MaxBoxSize ) { BoxLength = MaxBoxSize; }
				// find lowest 
				if (BoxLength > 0 ) {
					LowestLow = MIN(Low, BoxLength)[0] - TickSize;
					//Draw.Dot(this, "HH" + CurrentBar, false, 0, HighestHigh, Brushes.Blue);
				}
				
				// draw a 4 pt box 
				double TargetLevel = LowestLow + 8.0;
				BoxConstructor( BoxLength: BoxLength , BoxTopPrice: LowestLow + RiskInPoints , TargetLevel: TargetLevel, BoxName: BottomName);
			}
			
			// if off
			if ( ToggleNum == 0 ) {		// bottom area
				RemoveDrawObject(BottomName);
				RemoveDrawObject(BottomName + "priceLine");
				RemoveDrawObject(BottomName + "RiskHere");
				RemoveDrawObject(BottomName + "TargetLevel"); 
			}
		}
		
		private void BoxConstructor(int BoxLength, double BoxTopPrice, double TargetLevel, string BoxName) {
			
			if ( BoxName == TopName) {
				RemoveDrawObject(BottomName);
				RemoveDrawObject(BottomName + "priceLine");
				RemoveDrawObject(BottomName + "RiskHere");
				RemoveDrawObject(BottomName + "TargetLevel");
			} else {
				RemoveDrawObject(TopName);
				RemoveDrawObject(TopName + "priceLine");
				RemoveDrawObject(TopName + "RiskHere");
				RemoveDrawObject(TopName + "TargetLevel");
			}
			if ( ShowBox ) {				 ;
				Draw.Rectangle(this, BoxName, false, BoxLength, BoxTopPrice - RiskInPoints, -2, BoxTopPrice, BoxColor, Brushes.Transparent, 100);
			}
			// draw a line at close with risk
			if ( ShowRisk ) {
				double RiskHere = BoxTopPrice - Close[0];
				if ( BoxName == BottomName) {
					RiskHere = Close[0] - (BoxTopPrice - RiskInPoints);
				}
				//Draw.Line(this, BoxName + "priceLine", 0, Close[0], -2, Close[0], LineColor); 
				// show risk
				Draw.Text(this, BoxName + "RiskHere", RiskHere.ToString("N2"), -5, Close[0], TextColor);
			}
			if ( ShowTarget ) {
				Draw.Line(this, BoxName + "TargetLevel", BoxLength, TargetLevel, -2, TargetLevel, LineColor);
			}
		}

		#region Button Logic
		// Define a custom event method to handle our custom task when the button is clicked
		private void OnMyButtonClick(object sender, RoutedEventArgs rea)
		{
		  System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
		  if (button != null) {

			  ToggleNum +=1;
			  if( ToggleNum == 3 ) { ToggleNum = 0; }
			  
			  switch (ToggleNum) {
			  	case 0:
			  		//Print(button.Name + " Clicked and ToggleNum is " + ToggleNum);
					  mySellButton.Content = "O F F";
			  		break;
			  	case 1:
			  		mySellButton.Content = "H I G H S";
			  		break;
				case 2:
			  		mySellButton.Content = "L O W S";
			  		break;
			  	default:
			  		
			  		break;
			  }
			  
			  Print(button.Name + " Clicked and ToggleNum is " + ToggleNum);
	
		  }
		}
		#endregion
		
		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Line Color", Order=1, GroupName="Parameters")]
		public Brush LineColor
		{ get; set; }

		[Browsable(false)]
		public string LineColorSerializable
		{
			get { return Serialize.BrushToString(LineColor); }
			set { LineColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Box Color", Order=2, GroupName="Parameters")]
		public Brush BoxColor
		{ get; set; }

		[Browsable(false)]
		public string BoxColorSerializable
		{
			get { return Serialize.BrushToString(BoxColor); }
			set { BoxColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Text Color", Order=3, GroupName="Parameters")]
		public Brush TextColor
		{ get; set; }

		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Ema Length", Order=4, GroupName="Parameters")]
		public int EmaLength
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Box", Order=5, GroupName="Parameters")]
		public bool ShowBox
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Risk", Order=6, GroupName="Parameters")]
		public bool ShowRisk
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Target", Order=6, GroupName="Parameters")]
		public bool ShowTarget
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Risk In Points", Order=7, GroupName="Parameters")]
		public double RiskInPoints
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Bearish", Order=8, GroupName="Parameters")]
		public bool Bearish
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MaxBox Size", Order=9, GroupName="Parameters")]
		public int MaxBoxSize
		{ get; set; }
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EntryRisk[] cacheEntryRisk;
		public EntryRisk EntryRisk(Brush lineColor, Brush boxColor, Brush textColor, int emaLength, bool showBox, bool showRisk, bool showTarget, double riskInPoints, bool bearish, int maxBoxSize)
		{
			return EntryRisk(Input, lineColor, boxColor, textColor, emaLength, showBox, showRisk, showTarget, riskInPoints, bearish, maxBoxSize);
		}

		public EntryRisk EntryRisk(ISeries<double> input, Brush lineColor, Brush boxColor, Brush textColor, int emaLength, bool showBox, bool showRisk, bool showTarget, double riskInPoints, bool bearish, int maxBoxSize)
		{
			if (cacheEntryRisk != null)
				for (int idx = 0; idx < cacheEntryRisk.Length; idx++)
					if (cacheEntryRisk[idx] != null && cacheEntryRisk[idx].LineColor == lineColor && cacheEntryRisk[idx].BoxColor == boxColor && cacheEntryRisk[idx].TextColor == textColor && cacheEntryRisk[idx].EmaLength == emaLength && cacheEntryRisk[idx].ShowBox == showBox && cacheEntryRisk[idx].ShowRisk == showRisk && cacheEntryRisk[idx].ShowTarget == showTarget && cacheEntryRisk[idx].RiskInPoints == riskInPoints && cacheEntryRisk[idx].Bearish == bearish && cacheEntryRisk[idx].MaxBoxSize == maxBoxSize && cacheEntryRisk[idx].EqualsInput(input))
						return cacheEntryRisk[idx];
			return CacheIndicator<EntryRisk>(new EntryRisk(){ LineColor = lineColor, BoxColor = boxColor, TextColor = textColor, EmaLength = emaLength, ShowBox = showBox, ShowRisk = showRisk, ShowTarget = showTarget, RiskInPoints = riskInPoints, Bearish = bearish, MaxBoxSize = maxBoxSize }, input, ref cacheEntryRisk);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EntryRisk EntryRisk(Brush lineColor, Brush boxColor, Brush textColor, int emaLength, bool showBox, bool showRisk, bool showTarget, double riskInPoints, bool bearish, int maxBoxSize)
		{
			return indicator.EntryRisk(Input, lineColor, boxColor, textColor, emaLength, showBox, showRisk, showTarget, riskInPoints, bearish, maxBoxSize);
		}

		public Indicators.EntryRisk EntryRisk(ISeries<double> input , Brush lineColor, Brush boxColor, Brush textColor, int emaLength, bool showBox, bool showRisk, bool showTarget, double riskInPoints, bool bearish, int maxBoxSize)
		{
			return indicator.EntryRisk(input, lineColor, boxColor, textColor, emaLength, showBox, showRisk, showTarget, riskInPoints, bearish, maxBoxSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EntryRisk EntryRisk(Brush lineColor, Brush boxColor, Brush textColor, int emaLength, bool showBox, bool showRisk, bool showTarget, double riskInPoints, bool bearish, int maxBoxSize)
		{
			return indicator.EntryRisk(Input, lineColor, boxColor, textColor, emaLength, showBox, showRisk, showTarget, riskInPoints, bearish, maxBoxSize);
		}

		public Indicators.EntryRisk EntryRisk(ISeries<double> input , Brush lineColor, Brush boxColor, Brush textColor, int emaLength, bool showBox, bool showRisk, bool showTarget, double riskInPoints, bool bearish, int maxBoxSize)
		{
			return indicator.EntryRisk(input, lineColor, boxColor, textColor, emaLength, showBox, showRisk, showTarget, riskInPoints, bearish, maxBoxSize);
		}
	}
}

#endregion
