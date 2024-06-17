using Mapbox.Examples;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Mapbox.Arena
{
	public class GameUI : MonoBehaviour
	{
		[SerializeField]
		private AbstractMap _map;

		[SerializeField]
		private Dropdown _styleDropdown;

		[SerializeField]
		private Dropdown _languageDropdown;

		[SerializeField]
		private Slider _slider;

		private Text _heightTip;
		private Text _infoText;
		private Text _positionText;

		private ForwardGeocodeUserInput forwardGeocodeUserInput;

		private const string CANVAS = "GameUI";
		private const string HEIGHT_TEXT = "HeightText";
		private const string CITY_TEXT = "CityText";
		private const string STYLE_DROPDOWN = "StyleDropdown";
		private const string LANGUAGE_DROPDOWN = "LanguageDropdown";

		private const string LAYER_BUILDING = "building";
		private const string LAYER_ROAD = "road";
		private const string LAYER_ROAD_LABEL = "road_label";
		private const string LAYER_POI_LABEL = "poi_label";

		void Start()
		{
			if (_map == null)
			{
				_map = FindObjectOfType<AbstractMap>();
				if (_map == null)
				{
					throw new System.Exception("You must have a reference map assigned!");
				}
			}

			GameObject canvas = GameObject.Find(CANVAS);

			Dropdown[] dropdowns = canvas.GetComponentsInChildren<Dropdown>();
			for(int i = 0; i < dropdowns.Length; ++i)
			{
				string name = dropdowns[i].name;
				if (name.Equals(STYLE_DROPDOWN))
					_styleDropdown = dropdowns[i];
				else if (name.Equals(LANGUAGE_DROPDOWN))
					_languageDropdown = dropdowns[i];
			}
			
			_slider = canvas.GetComponentInChildren<Slider>();
			Text[] texts = canvas.GetComponentsInChildren<Text>();
			foreach(Text text in texts)
			{
				if (text.name.Equals(HEIGHT_TEXT))
					_heightTip = text;
				else if (text.name.Equals(CITY_TEXT))
					_infoText = text;
			}

			GameObject UserInput = GameObject.Find("UserInput");
			if(UserInput != null)
			{
				forwardGeocodeUserInput = UserInput.GetComponent<ForwardGeocodeUserInput>();
				if(forwardGeocodeUserInput != null)
					forwardGeocodeUserInput.OnGeocoderResponse += OnGeocoderResponse;
			}

			// widgets' initial value
			Language language = GetLanguage();
			_styleDropdown.value = CastStyleToValue(GetStyle());
			_languageDropdown.value = (int)language;
			float threshold = GetHeightThreshold();
			SetHeightThresholdText(threshold);

			StartCoroutine(QueryLocation());

			// https://docs.unity3d.com/2018.4/Documentation/ScriptReference/UI.Dropdown-onValueChanged.html
			_styleDropdown.onValueChanged.AddListener(_ => OnStyleChanged(_styleDropdown));
			_languageDropdown.onValueChanged.AddListener(_ => OnLanguageChanged(_languageDropdown));
			_slider.onValueChanged.AddListener(_ => OnHeightThresholdChanged(_slider));
		}

		private static int CastStyleToValue(StyleTypes style)
		{
			if (style == StyleTypes.Custom)
				throw new Exception("StyleTypes.Custom is out of list");
			return (int)style - 1;  // The first is StyleTypes.Custom
		}

		private static StyleTypes CastValueToStyle(int value)
		{
			int length = Enum.GetNames(typeof(StyleTypes)).Length;
			// if (!Enum.IsDefined(typeof(StyleTypes), value))
			if (value <= (int)StyleTypes.Custom || value >= length - 1)
				throw new Exception("Slider value is out of range");			
	
			StyleTypes style = (StyleTypes)(value + 1);
			return style;
		}

		private StyleTypes GetStyle()
		{
			StyleTypes style = StyleTypes.Custom;
			IEnumerable<VectorSubLayerProperties> layers = _map.VectorData.GetAllFeatureSubLayers();
			foreach (var layer in layers)
			{
				if (layer.coreOptions.isActive && layer.Key.Equals(LAYER_BUILDING))
				{
					if (style == StyleTypes.Custom)
						style = layer.Texturing.GetStyleType();
					else
						Debug.Log("building with different style exists, return first style");
				}
			}

			return style;
		}

		private void SetStyle(StyleTypes style)
		{
			IEnumerable<VectorSubLayerProperties> layers = _map.VectorData.GetAllFeatureSubLayers();
			foreach (var layer in layers)
			{
				if (layer.coreOptions.isActive && layer.Key.Equals(LAYER_BUILDING))
					layer.Texturing.SetStyleType(style);
			}
		}

		private void OnStyleChanged(Dropdown option)
		{
			StyleTypes style = CastValueToStyle(option.value);
			SetStyle(style);
		}

		private void OnHeightThresholdChanged(Slider option)
		{
			Debug.Log("TextureSideWallModifier height threshold changed to " + option.value);
			SetHeightThresholdText(option.value);
			SetHeightThreshold(option.value);
		}

		private void SetHeightThresholdText(float threshold)
		{
			// https://stackoverflow.com/questions/6356351/formatting-a-float-to-2-decimal-places
			_heightTip.text = "Height: " + threshold.ToString("0.0") + "m";
		}

		private List<VectorLayerVisualizer> GetLayerVisualizer(string layerName)
		{
			IEnumerable<VectorSubLayerProperties> layers = _map.VectorData.GetAllFeatureSubLayers();
			Mapbox.Unity.MeshGeneration.Factories.VectorTileFactory factory = (_map.VectorData as VectorLayer).Factory;
			List<VectorLayerVisualizer> layerVisualizers = factory.FindVectorLayerVisualizer(layerName);

			//buildingLayerVisualizer.Where(_ => _ is VectorLayerVisualizer);
			return layerVisualizers;
		}

		private float GetHeightThreshold()
		{
			float heightThreshold = 0.0f;
#if HEIGHT_BINARY_STYLE
			bool heightThresholdSet = false;

			List<VectorLayerVisualizer> layerVisualizers = GetLayerVisualizer(LAYER_BUILDING);
			foreach (VectorLayerVisualizer layerVisualizer in layerVisualizers)
			{
				List<MeshModifier> meshModifiers = layerVisualizer.DefaultModifierStack.MeshModifiers;
				if(meshModifiers.Any(_ => _ is TextureSideWallModifier))
				{
					VectorSubLayerProperties layerProperties = layerVisualizer.SubLayerProperties;

					if (!heightThresholdSet)
						heightThreshold = layerProperties.extrusionOptions.heightThreshold;
					else if (heightThreshold == layerProperties.extrusionOptions.heightThreshold)
					{
					}
					else
						Debug.Log("multiple TextureSideWallModifier with different height threshold");
				}
			}
#endif
			return heightThreshold;
		}

		private void SetHeightThreshold(float threshold)
		{
			List<VectorLayerVisualizer> layerVisualizers = GetLayerVisualizer(LAYER_BUILDING);
			foreach (VectorLayerVisualizer layerVisualizer in layerVisualizers)
			{
				List<MeshModifier> meshModifiers = layerVisualizer.DefaultModifierStack.MeshModifiers;
				if (meshModifiers.Any(_ => _ is TextureSideWallModifier))
				{
					VectorSubLayerProperties layerProperties = layerVisualizer.SubLayerProperties;
#if HEIGHT_BINARY_STYLE
					layerProperties.extrusionOptions.heightThreshold = threshold;
					layerProperties.extrusionOptions.HasChanged = true;
#endif
				}
			}
		}

		public void OnLanguageChanged(Dropdown option)
		{
			if (!Enum.IsDefined(typeof(Language), option.value))
				throw new ArgumentOutOfRangeException("language is not set");

			Language language = (Language)option.value;
			SetLanguage(language);

			StartCoroutine(QueryLocation());
		}

		private Language GetLanguage()
		{
			return _map.Options.languageOptions.language;
		}

		private void SetLanguage(Language language)
		{
			Debug.Log("change language to " + language.ToString());
			_map.Options.languageOptions.language = language;

			Mapbox.Unity.MeshGeneration.Factories.VectorTileFactory factory = (_map.VectorData as VectorLayer).Factory;
			List<VectorLayerVisualizer> layerVisualizers = factory.FindVectorLayerVisualizer(LAYER_ROAD);
			foreach (VectorLayerVisualizer layerVisualizer in layerVisualizers)
			{
				layerVisualizer.SubLayerProperties.SetActive(true);
			}

			layerVisualizers = factory.FindVectorLayerVisualizer(LAYER_POI_LABEL);
			foreach (VectorLayerVisualizer layerVisualizer in layerVisualizers)
			{
				layerVisualizer.SubLayerProperties.SetActive(true);
			}
		}

		void OnGeocoderResponse(Geocoding.ForwardGeocodeResponse response)
		{
			if (response.Features != null && response.Features.Count > 0)
			{
				Vector2d location = response.Features[0].Center;
				Language language = GetLanguage();
				StartCoroutine(QueryLocation(location, language));
			}
		}

		public IEnumerator QueryLocation()
		{
			Vector2d location = Conversions.StringToLatLon(_map.Options.locationOptions.latitudeLongitude);
			Language language = GetLanguage();
			return QueryLocation(location, language);
		}

		private IEnumerator QueryLocation(Vector2d location, Language language)
		{
			Mapbox.Geocoding.ReverseGeocodeResourceV6 resource = new Mapbox.Geocoding.ReverseGeocodeResourceV6(location);
			Dictionary<string, object> optionalParams = new Dictionary<string, object>();
			optionalParams.Add(Mapbox.Geocoding.GeocodeResourceV6<Vector2d>.LANGUAGE, language);
			optionalParams.Add(Mapbox.Geocoding.GeocodeResourceV6<Vector2d>.ACCESS_TOKEN, MapboxAccess.Instance.Configuration.AccessToken);
			resource.SetOptionalParams(optionalParams);
			string url = resource.GetUrl();

			using (UnityWebRequest request = UnityWebRequest.Get(url))
			{
				yield return request.SendWebRequest();

				if (request.result == UnityWebRequest.Result.Success)
				{
					string jsonString = request.downloadHandler.text;
					FeatureCollection featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(jsonString);
					if (featureCollection.features != null)
					{
						string address = featureCollection.features[0].properties.full_address;
						if(_infoText != null)
							_infoText.text = address;
					}
				}
			}
		}

		private void OnDestroy()
		{
			_styleDropdown.onValueChanged.RemoveAllListeners();
		}
	}

}
