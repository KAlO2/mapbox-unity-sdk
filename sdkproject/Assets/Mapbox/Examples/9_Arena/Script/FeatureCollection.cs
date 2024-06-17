using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.Arena
{
	[Serializable]
	public class Coordinate
	{
		double longitude { get; set; }
		double latitude { get; set; }
	}

	[Serializable]
	public class Geometry
	{
		public string type { get; set; }
		public double[] coordinates { get; set; }
	}


	[Serializable]
	public class Properties
	{
		public string mapbox_id { get; set; }
		public string feature_type { get; set; }
		public string full_address { get; set; }
		public string name { get; set; }
		public string name_preferred { get; set; }
		public Coordinate coordinates { get; set; }
		public string place_formatted { get; set; }
		public double[] bbox { get; set; }
		// Dictionary<string, object>
		public object context { get; set; }
	}

	[Serializable]
	public class Feature
	{
		public string type { get; set; }
		public string id { get; set; }
		public Geometry geometry { get; set; }
		public Properties properties { get; set; }
	}

	[Serializable]
	public class FeatureCollection
	{
		public string type { get; set; }
		public Feature[] features { get; set; }
		public string attribution { get; set; }
	}

}
