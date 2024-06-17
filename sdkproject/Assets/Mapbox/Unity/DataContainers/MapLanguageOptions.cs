namespace Mapbox.Unity.Map
{
	using Mapbox.VectorTile.ExtensionMethods;
	using System;
	using System.ComponentModel;

	// https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo?view=net-8.0
	// the languages that Mapbox supports currently in mapbox.mapbox-streets-v8 data
	public enum Language
	{
		[Description("ar")]
		Arabic,
		[Description("de")]
		German,
		[Description("en")]
		English,
//		[Description("eo")]
//		Esperanto,
		[Description("es")]
		Spanish,
		[Description("fr")]
		French,
		[Description("jp")]
		Japanese,
//		[Description("id")]
//		Indonesian,
		[Description("it")]
		Italian,
		[Description("kr")]
		Korean,
//		[Description("nl")]
//		Dutch,
//		[Description("pl")]
//		Polish,
		[Description("pt")]
		Portugal,
//		[Description("ro")]
//		Romanian,
		[Description("ru")]
		Russian,
/*
		[Description("sv")]
		Swedish,
		[Description("tr")]
		Turkish,
		[Description("uk")]
		Ukrainian,
*/
		[Description("vi")]
		Vietnamese,
		[Description("zh-Hans")]
		ChineseSimplified,
		[Description("zh-Hant")]
		ChineseTraditional,
	}

	[Serializable]
	public class MapLanguageOptions : MapboxDataProperty
	{
		public Language language { get; set; } = Language.English;

		// System.Globalization.CultureInfo.TwoLetterISOLanguageName
		public static string GetLanguageName(Language language)
		{
			string description = language.Description();
			return description;
		}

		// mapbox.mapbox-streets-v8.json uses "ja", not "jp" for Japanese, "ko", not "kr" for Korean.
		// This method is tweaked for Mapbox HTTP request.
		public static string GetLanguageNameMapbox(Language language)
		{
			switch(language)
			{
				case Language.Japanese: return "ja";
				case Language.Korean:   return "ko";
				default: return GetLanguageName(language);
			}
		}
		public string GetLanguageNameMapbox()
		{
			return GetLanguageNameMapbox(language);
		}

	}


}
