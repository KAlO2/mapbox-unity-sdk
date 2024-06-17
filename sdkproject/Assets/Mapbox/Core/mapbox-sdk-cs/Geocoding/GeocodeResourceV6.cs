//-----------------------------------------------------------------------
// <copyright file="GeocodeResourceV6.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Geocoding
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Platform;
	using Mapbox.Unity.Map;

	/// <summary> Base geocode class. </summary>
	/// <typeparam name="T"> Type of Query field (either string or LatLng). </typeparam>
	public abstract class GeocodeResourceV6<T> : Resource
	{
		/// <summary> A List of all possible geocoding feature types. </summary>
		public static readonly List<string> FeatureTypes = new List<string>
		{
			"country", "region", "postcode", "place", "locality", "neighborhood", "address", "poi"
		};

		private readonly string apiEndpoint = "search/geocode/v6/";

		private readonly string mode = "reverse";

		public static string COUNTRY = "country";
		public static string LANGUAGE = "language";
		public static string ACCESS_TOKEN = "access_token";

		private Dictionary<string, string> optionalParams;

		// Optional
		private string[] types;

		/// <summary> Gets or sets the query. </summary>
		public abstract T Query { get; set; }

		/// <summary> Gets the API endpoint as a partial URL path. </summary>
		public override string ApiEndpoint
		{
			get
			{
				return this.apiEndpoint;
			}
		}

		/// <summary> Gets the mode. </summary>
		public string Mode
		{
			get
			{
				return this.mode;
			}
		}

		public void SetOptionalParams(Dictionary<string, object> dict)
		{
			object object_;
			if(dict.TryGetValue(LANGUAGE, out object_))
			{
				if(optionalParams == null)
					optionalParams = new Dictionary<string, string>();

				string language = MapLanguageOptions.GetLanguageNameMapbox((Language)object_);
				optionalParams.Add(LANGUAGE, language);
			}

			if(dict.TryGetValue(COUNTRY, out object_))
			{
				if (optionalParams == null)
					optionalParams = new Dictionary<string, string>();

				string country = (string)object_;
				optionalParams.Add(COUNTRY, country);
			}

			if(dict.TryGetValue(ACCESS_TOKEN, out object_))
			{
				if (optionalParams == null)
					optionalParams = new Dictionary<string, string>();

				string accessToken = (string)object_;
				optionalParams.Add(ACCESS_TOKEN, accessToken);
			}
		}

		public Dictionary<string, string> GetOptionalParams()
		{
			return optionalParams;
		}

		/// <summary> Gets or sets which feature types to return results for. </summary>
		public string[] Types
		{
			get
			{
				return this.types;
			}

			set
			{
				if (value == null)
				{
					this.types = value;
					return;
				}

				for (int i = 0; i < value.Length; i++)
				{
					// Validate provided types
					if (!FeatureTypes.Contains(value[i]))
					{
						throw new Exception("Invalid type. Must be \"country\", \"region\", \"postcode\",  \"place\",  \"locality\",  \"neighborhood\",  \"address\", or  \"poi\".");
					}
				}

				this.types = value;
			}
		}
	}
}
