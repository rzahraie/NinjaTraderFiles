using System.Drawing;
using System.Windows.Media;

namespace NinjaTrader.NinjaScript.Indicators
{
	public struct xVolumeBarInformation
	{
		//@@
		public int BarNumber;
		//@@
		public xVolumeColorStateEnums VolumeColorState;
		public string VolumeColorStateString 
		{ 
			get 
			{
				return (xEnumDescription.GetEnumDescription(VolumeColorState));		
			}
		}
		
		//@@
		public string VolumeColorSeries;
		
		public Brush VolumeBarColor;
		
		public xVolumeBarInformation(int barnum, xVolumeColorStateEnums vc, string vs, Brush volumecolor)
		{
			BarNumber = barnum;
			VolumeColorState = vc;
			VolumeColorSeries = vs;
			VolumeBarColor = volumecolor;
		}
		
		public override string ToString()
		{
			return(BarNumber.ToString() + "\t\t" + VolumeColorStateString + "\t\t" + VolumeColorSeries.ToString());	
		}
	}

}
