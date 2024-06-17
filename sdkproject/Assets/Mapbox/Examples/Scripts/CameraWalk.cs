using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Mapbox.Unity.MeshGeneration;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Interfaces;
using System.Linq;

namespace Mapbox.Examples
{
	public class CameraWalk : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		public KdTreeCollection Collection;

		[SerializeField]
		const float walkSpeed = 10.0f;

		[SerializeField]
		const float runSpeed = 25.0f;

		const float ROTATE_SENSITIVITY = 2.5f; // 0.5 degree per pixel

		private Vector3 _rotation = new Vector3();

		float velocity = 0.0f;
		const float gravity = -9.8f;
		private const float bodyHeight = 1.7f;

		private bool roaming = true;

		private const string CANVAS = "GameUI";
		private GameObject canvas;

		private const string BUILDING = "building";

		private ReplaceFeatureModifier replaceFeatureModifier;
		private ObjectInspectorModifier objectInspectorModifier;

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

			canvas = GameObject.Find(CANVAS);
		}

		private void Start()
		{
			replaceFeatureModifier = FindModifier<ReplaceFeatureModifier>();
			objectInspectorModifier = FindModifier<ObjectInspectorModifier>();
		}

		// https://stackoverflow.com/questions/9808035/how-do-i-make-the-return-type-of-a-method-generic
		private T FindModifier<T>()
		{
			Mapbox.Unity.MeshGeneration.Factories.VectorTileFactory factory = (_map.VectorData as VectorLayer).Factory;
			List<VectorLayerVisualizer> layerVisualizers = factory.FindVectorLayerVisualizer(BUILDING);
			foreach (var layerVisualizer in layerVisualizers)
			{
				VectorSubLayerProperties layerProperties = layerVisualizer.SubLayerProperties;
				for (int i = 0; i < layerProperties.GoModifiers.Count; ++i)
				{
					if (layerProperties.GoModifiers[i] is T)
					{
						return (T)(object)layerProperties.GoModifiers[i];
					}
				}
			}

			return default(T);
		}

		void LateUpdate()
		{
			if (Input.touchSupported && Input.touchCount > 0)
			{
				HandleTouch();
			}
			else
			{
				HandleMouseAndKeyBoard();
			}
		}

		private void HandleTouch()
		{
			switch (Input.touchCount)
			{
				case 1:
				{
					HandleMouseAndKeyBoard();
				}
				break;
			}
		}

		private void HandleMouseAndKeyBoard()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				roaming = !roaming;
				string message = roaming ? "walk" : "stand";
				Debug.Log("enter " + message + " mode, press Esc again to recover");
				canvas.SetActive(false);
			}
			//Cursor.visible = !roaming;
			if (roaming)
				Roam();
			else
				OperateUI();
		}

		private void Roam()
		{ 
			Vector3 position = transform.localPosition + velocity * Time.deltaTime * Vector3.up;
			velocity += gravity * Time.deltaTime;

			Vector3 humanPosition = Camera.main.transform.position;  // role position, not camera position
			Vector2d geoPosition = _map.WorldToGeoPosition(humanPosition);

			float terrainHeight = _map.QueryElevationInMetersAt(geoPosition);
			float height = terrainHeight + bodyHeight;

			if(position.y < height)
			{
				position.y = height;
				velocity = 0.0f;
			}

			if (position.y <= height && Input.GetKeyDown(KeyCode.Space))
			{
				// we are on the ground, we can jump
				velocity = 4;  // 3~4m/s
				//position.y = STAND_HEIGHT;
			}

#if false
			int dx = 0, dy = 0;
			if (Input.GetKeyDown(KeyCode.A))
				dx -= 1;
			if (Input.GetKeyDown(KeyCode.D))
				dx += 1;
			if (Input.GetKeyDown(KeyCode.S))
				dy -= 1;
			if(Input.GetKeyDown(KeyCode.W))
				dy += 1;
#else
			float dx = Input.GetAxisRaw("Horizontal");
			float dy = Input.GetAxisRaw("Vertical");
#endif
			bool run = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
			float speed = run ? runSpeed: walkSpeed;
			Vector3 newPosition = position + transform.rotation * (Vector3.right * dx + Vector3.forward * dy) * (speed * Time.deltaTime);
			transform.localPosition = newPosition;

			
			//transform.rotation.ToAngleAxis
			float mouseX = Input.GetAxis("Mouse X");  // rotate around Y
			float mouseY = Input.GetAxis("Mouse Y");  // rotate around X down +
			//transform.Rotate(-mouseY, mouseX, 0);

			//Vector3 angle = transform.eulerAngles;a
			_rotation.x -= mouseY * ROTATE_SENSITIVITY;
			_rotation.y += mouseX * ROTATE_SENSITIVITY;
			//_rotation.z = _rotation.z;

			float threshold = (float)Mapbox.Utils.Constants.LatitudeMax;
			_rotation.x = Mathf.Clamp(_rotation.x, -threshold, +threshold);
			_rotation.y %= 360.0f;
			if(_rotation.y < 0)  // make y in interval [0, 2 * pi)
				_rotation.y += 360.0f;
			transform.eulerAngles = _rotation;

			if (!(Mathf.Approximately(dx, 0) && Mathf.Approximately(dy, 0) &&
					Mathf.Approximately(mouseX, 0) && Mathf.Approximately(mouseY, 0)))
			{
				_map.UpdateMap();
			}
		}

		private void OperateUI()
		{
			if (Input.GetKeyUp(KeyCode.O))
			{
				bool active = canvas.activeSelf;
				Debug.Log("GameUI canvas change to " + ((!active) ? "visible" : "invisible"));
				canvas.SetActive(!active);
			}

			if (Input.GetKeyUp(KeyCode.P))
			{
				Vector3 position = Camera.main.transform.position + Camera.main.transform.forward * 3;
				Vector2d prefabPosition = _map.WorldToGeoPosition(position);
				spawnPrefab(prefabPosition);
			}

			// https://github.com/mapbox/mapbox-unity-sdk/issues/1694
			// Best way to enable/disable buildings on runtime
			if (Input.GetKeyUp(KeyCode.R))
			{
				VectorEntity selectedFeature = FeatureSelectionDetector.GetSelectedFeature();
				if (replaceFeatureModifier != null && selectedFeature != null)
				{
					// use bounding box center instead of transform.position for better precision
//					Vector3 worldPosition = selectedFeature.GameObject.transform.position;
					Vector3 worldPosition = selectedFeature.Mesh.bounds.center;  

					Vector2d latlong = _map.WorldToGeoPosition(worldPosition);
					string location = string.Format("{0}, {1}", latlong.x, latlong.y);

					if (replaceFeatureModifier.PrefabLocations.Contains(location))
						Debug.Log("PrefabLocations already contains the location:" + location);
					else
					{
						UnityTile tile;
						Map.UnwrappedTileId tileId = Conversions.LatitudeLongitudeToTileId(latlong.x, latlong.y, _map.AbsoluteZoom);
						if (_map.MapVisualizer.ActiveTiles.TryGetValue(tileId, out tile))
						{
							replaceFeatureModifier.PrefabLocations.Add(location);
							selectedFeature.MeshRenderer.enabled = false;  // selectedFeature.GameObject.GetComponent<MeshRenderer>().enabled = false; also works
							replaceFeatureModifier.SpawnPrefab(selectedFeature, tile, latlong);
							//OnReplaceFeatureModifierChanged();
						}
					}
				}
			}

			if (Input.GetKeyUp(KeyCode.I))
			{
				Vector3 humanPosition = Camera.main.transform.position;  // role position, not camera position
				Vector2d geoPosition = _map.WorldToGeoPosition(humanPosition);
				Map.UnwrappedTileId tileId = Conversions.LatitudeLongitudeToTileId(geoPosition.x, geoPosition.y, _map.AbsoluteZoom);

				GameObject prefab = Resources.Load<GameObject>("MapboxPin");
				List<Vector2d> locations = RoadAlgorithm.FindCrossingPoint(_map, tileId);
				bool scaleDownWithWorld = true;
				string locationItemName = "RoadCrossing";
				_map.VectorData.SpawnPrefabAtGeoLocation(prefab, locations.ToArray(), null, scaleDownWithWorld, locationItemName);
			}
		}

		private void OnReplaceFeatureModifierChanged()
		{
			Mapbox.Unity.MeshGeneration.Factories.VectorTileFactory factory = (_map.VectorData as VectorLayer).Factory;
			List<VectorLayerVisualizer> layerVisualizers = factory.FindVectorLayerVisualizer(BUILDING);
			foreach (var layerVisualizer in layerVisualizers)
			{
				VectorSubLayerProperties layerProperties = layerVisualizer.SubLayerProperties;
				if (layerProperties.GoModifiers.Any(_ => _ is ReplaceFeatureCollectionModifier || _ is ReplaceFeatureModifier || _ == objectInspectorModifier))
				{
					layerProperties.SetActive(true);
					break;
				}
			}
		}

		private void spawnPrefab(in Vector2d location)
		{
			string path = "MapboxPin";
			GameObject prefab = Resources.Load<GameObject>(path);
			//prefab.GetComponent<Transform>().localScale = new Vector3(0.3f, 0.3f, 0.3f);

			bool scaleDownWithWorld = true;
			string locationItemName = "Location@" + location.ToString();
			_map.VectorData.SpawnPrefabAtGeoLocation(prefab, location, null, scaleDownWithWorld, locationItemName);
		}
	}
}
