 namespace Mapbox.Examples
{
	using Mapbox.Geocoding;
	using Mapbox.Unity;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	public class PoiLabelTextSetter : MonoBehaviour, IFeaturePropertySettable
	{
		[SerializeField]
		Text _text;
		[SerializeField]
		Image _background;

		[SerializeField]
		private AbstractMap _map;

		private void Awake()
		{
			//rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
			transform.eulerAngles = Vector3.zero;

			if (_map == null)
			{
				_map = FindObjectOfType<AbstractMap>();
				if (_map == null)
				{
					throw new System.Exception("You must have a reference map assigned!");
				}
			}
		}

		public void Set(Dictionary<string, object> props)
		{
			_text.text = "";

			object value;
			string language = _map.Options.languageOptions.GetLanguageNameMapbox();
			if (props.TryGetValue("name_" + language, out value) ||
				props.TryGetValue("name", out value) ||
				props.TryGetValue("house_num", out value) ||
				props.TryGetValue("type", out value))
			{
				_text.text = value.ToString();
			}

			RefreshBackground();
		}

		public void RefreshBackground()
		{
			RectTransform backgroundRect = _background.GetComponent<RectTransform>();
			LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);
		}
	}
}