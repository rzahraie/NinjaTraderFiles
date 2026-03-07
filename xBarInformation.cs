namespace NinjaTrader.NinjaScript.Indicators
{
	public struct xBarInformation
	{
		public xBarTypesEnum BarType;
		public xBarCloseTypesEnums BarCloseType;
		public xBarBodyTypesEnums BarBodyType;
		public xBarPartTypesEnums BarPartType;
		public int BarNumber;
		
		public xBarInformation(int barnum, xBarTypesEnum bt, xBarCloseTypesEnums bc, xBarBodyTypesEnums bb,
			xBarPartTypesEnums bp)
		{
			BarNumber = barnum;
			BarType = bt;
			BarCloseType = bc;
			BarBodyType = bb;
			BarPartType = bp;
			
		}
		
		public string ToString()
		{
			return(BarNumber.ToString() + "\t\t" + BarType.ToString() + "\t\t" + BarCloseType.ToString()+ "\t\t" + 
				BarBodyType.ToString() + "\t\t" + BarPartType.ToString());	
		}
	}

}
