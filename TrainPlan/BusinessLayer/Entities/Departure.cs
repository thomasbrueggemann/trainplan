using System;

namespace TrainPlan.BusinessLayer
{
	public class Departure
	{
		
		/* MEMBER VARS */
		public string name;
		public string category;
		public string product;
		public string direction;
		public string scheduledTime;
		public string scheduledPlatform;
		public string actualTime;
		public int trainID;
		
		public Station station;
	}
}

