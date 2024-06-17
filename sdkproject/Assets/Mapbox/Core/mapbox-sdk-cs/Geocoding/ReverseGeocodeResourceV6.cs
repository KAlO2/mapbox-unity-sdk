//-----------------------------------------------------------------------
// <copyright file="ReverseGeocodeResourceV6.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Geocoding
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Mapbox.Utils;

    /// <summary> A reverse geocode request. </summary>
    public sealed class ReverseGeocodeResourceV6 : GeocodeResourceV6<Vector2d>
	{
		// Required
		private Vector2d query;

		/// <summary> Initializes a new instance of the <see cref="ReverseGeocodeResource" /> class.</summary>
		/// <param name="query"> Location to reverse geocode. </param>
		public ReverseGeocodeResourceV6(Vector2d query)
		{
			this.Query = query;
		}

		/// <summary> Gets or sets the location. </summary>
		public override Vector2d Query {
			get {
				return this.query;
			}

			set {
				this.query = value;
			}
		}

		/// <summary> Builds a complete reverse geocode URL string. </summary>
		/// <returns> A complete, valid reverse geocode URL string. </returns>
		/// https://docs.mapbox.com/api/search/geocoding/#reverse-geocoding
		public override string GetUrl()
		{
			Dictionary<string, string> opts = new Dictionary<string, string>();

			if (this.Types != null)
			{
				opts.Add("types", GetUrlQueryFromArray(this.Types));
			}

			StringBuilder sb = new StringBuilder(128);
			sb.Append(Constants.BaseAPI).Append(ApiEndpoint).Append(Mode);
			sb.Append('?');
			sb.Append("longitude=").Append(Query.y);
			sb.Append('&').Append("latitude=").Append(Query.x);

			var optionalParams = GetOptionalParams();
			foreach (KeyValuePair<string, string> entry in optionalParams)
				sb.Append('&').Append(entry.Key).Append('=').Append(entry.Value);
			
			return sb.ToString();
		}
	}
}
