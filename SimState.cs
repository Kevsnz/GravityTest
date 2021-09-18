using System;
using System.Collections.Generic;

namespace GravityTest
{
	[Serializable]
	internal class SimState
	{
		public double SimTime { get; set; }
		public List <IObject> ObjectList { get; set; }
	}
}
