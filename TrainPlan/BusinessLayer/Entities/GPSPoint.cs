using System;
namespace TrainPlan
{
	public class GPSPoint
	{
		public double lat;
		public double lon;
		
		public double convertStringToCoordinate (string input)
		{
			string result;
			
			if (input.Length == 8)
			{
				result = input.Insert (2, ",");
			}
			else
			{
				result = input.Insert (1, ",");
			}
			
			
			return Convert.ToDouble(result);
		}
	}
}

