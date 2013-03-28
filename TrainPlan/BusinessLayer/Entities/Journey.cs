using System;
using System.Collections.Generic;

namespace TrainPlan.BusinessLayer 
{
	public class Journey
	{
		public int trainID;
		public string name;
		public string category;
		public string direction;
		
		public List<BasicStop> passList = new List<BasicStop>();
	}
}

