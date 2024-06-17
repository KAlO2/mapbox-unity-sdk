namespace Mapbox.Examples
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Unity;
	using Mapbox.Unity.Map;

	public class MakiHelper : MonoBehaviour, IFeaturePropertySettable
	{
		public static RectTransform Parent;
		public static GameObject UiPrefab;

		private GameObject _uiObject;

		public void Set(Dictionary<string, object> props)
		{
			if (Parent == null)
			{
				var canv = GameObject.Find("PoiCanvas");
				var ob = new GameObject("PoiContainer");
				ob.transform.SetParent(canv.transform);
				Parent = ob.AddComponent<RectTransform>();
				UiPrefab = Resources.Load<GameObject>("MakiUiPrefab");
			}

			if (props.ContainsKey("maki"))
			{
				_uiObject = Instantiate(UiPrefab);
				_uiObject.transform.SetParent(Parent);
				_uiObject.transform.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>("maki/" + props["maki"].ToString() + "-15");

				AbstractMap map = FindObjectOfType<AbstractMap>();
				string language = map.Options.languageOptions.GetLanguageNameMapbox();

				object value = null;
				if (props.TryGetValue("name_" + language, out value) ||
						props.TryGetValue("name", out value))
				{
					_uiObject.GetComponentInChildren<Text>().text = value.ToString();
				}
			}
		}

		public void LateUpdate()
		{
			if (_uiObject)
				_uiObject.transform.position = Camera.main.WorldToScreenPoint(transform.position);
		}
	}
}