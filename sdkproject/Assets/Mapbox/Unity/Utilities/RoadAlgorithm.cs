namespace Mapbox.Utils
{
	using Mapbox.Map;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Unity.Utilities;
	using Mapbox.VectorTile.Geometry;
	using NUnit.Framework.Constraints;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public class RoadAlgorithm
	{
#if false
		public static UnityTile FindUnityTile(UnwrappedTileId tileId)
		{
			UnityTile unityTile = null;
			foreach (KeyValuePair<UnityTile, List<ulong>> entry in layerVisualizer._idPool)
			{
				if (entry.Key.UnwrappedTileId.Equals(tileId))
				{
					unityTile = entry.Key;
					break;
				}
			}

			return unityTile;
		}
#endif

		private static bool ContainsPoint(in Dictionary<Vector3, int> points, in Vector3 point, out int index, in float tolerance = 0)
		{
			if (points.TryGetValue(point, out index))
				return true;

			if (tolerance > 0)
			{
				foreach (var pair in points)
				{
					float dx = pair.Key.x - point.x;
					if (Mathf.Abs(dx) > tolerance)
						continue;
					float dy = pair.Key.y - point.y;
					if (Mathf.Abs(dy) > tolerance)
						continue;
					if (dx * dx + dy * dy > tolerance * tolerance)
						continue;

					index = pair.Value;
					return true;
				}
			}

			//index = 0;
			return false;
		}

		/// <summary>
		/// Collect all the road features from a UnityTile.
		/// </summary>
		/// <param name="unityTile"></param>
		/// <param name="lines"></param>
		/// <returns></returns>
		private static Dictionary<Vector3, int> CollectRoadFeatures(in UnityTile unityTile, out List<List<int>> lines)
		{
			Dictionary<Vector3, int> points = new Dictionary<Vector3, int>();
			lines = new List<List<int>>();

			Mapbox.VectorTile.VectorTileLayer roadLayer = (unityTile != null && unityTile.VectorData != null) ? unityTile.VectorData.GetLayer("road") : null;
			if (roadLayer != null)
			{
				int featureCount = roadLayer.FeatureCount();
				for (int i = 0; i < featureCount; ++i)
				{
					Mapbox.VectorTile.VectorTileFeature feature = roadLayer.GetFeature(i);
					bool isPolygon = feature.GeometryType == GeomType.POLYGON;
					if (feature.GeometryType != GeomType.LINESTRING && !isPolygon)
						continue;

					bool buildingsWithUniqueIds = true;  // set it to true for all the tiles' feature
					float layerExtent = roadLayer.Extent;
					VectorFeatureUnity vectorFeatureUnity = new VectorFeatureUnity(feature, unityTile, layerExtent, buildingsWithUniqueIds);
					float tolerance = 1 / layerExtent;

					foreach (List<Vector3> polyline in vectorFeatureUnity.Points)
					{
						if (polyline.Count < 2)
							continue;

						List<int> line = new List<int>(polyline.Count);
						foreach (Vector3 point in polyline)
						{
							if(ContainsPoint(points, point, out int index, tolerance))
							{
								line.Add(index);
							}
							else
							{
								int newIndex = points.Count;
								points.Add(point, newIndex);
								line.Add(newIndex);
							}
						}


						if (isPolygon)  // add first index to form a loop
						{
							if(ContainsPoint(points, polyline[0], out int firstPointIndex, tolerance))
								line.Add(firstPointIndex);
							else
								Debug.Assert(false);  // You should not be here.
						}

						lines.Add(line);
					}
				}
			}
			return points;
		}

		private static List<int> FindCrossingIndices(in List<List<int>> lines)
		{
			Dictionary<int, SortedSet<int>> connectivities = new Dictionary<int, SortedSet<int>>();
			foreach (List<int> line in lines)
			{
				int firstIndex = line.First();
				for (int i = 1; i < line.Count; ++i)
				{
					int nextIndex = line[i];

					// A -> B
					SortedSet<int> connectivity;
					if (connectivities.TryGetValue(firstIndex, out connectivity))
						connectivity.Add(nextIndex);
					else
					{
						connectivity = new SortedSet<int>();
						connectivity.Add(nextIndex);
						connectivities.Add(firstIndex, connectivity);
					}

					// B -> A
					if (connectivities.TryGetValue(nextIndex, out connectivity))
						connectivity.Add(firstIndex);
					else
					{
						connectivity = new SortedSet<int>();
						connectivity.Add(firstIndex);
						connectivities.Add(nextIndex, connectivity);
					}
				}
			}

			List<int> indices = new List<int>();
			foreach (var entry in connectivities)
			{
				if (entry.Value.Count >= 3)  // at least 3 roads converge at the same point, that's the intersection.
					indices.Add(entry.Key);
			}

			return indices;
		}

		public static List<Vector2d> FindCrossingPoint(AbstractMap map, UnwrappedTileId tileId)
		{
			UnityTile unityTile;
			bool foundTile = map.MapVisualizer.ActiveTiles.TryGetValue(tileId, out unityTile);
			if (!foundTile)
				return new List<Vector2d>{};

			List<List<int>> lines;
			Dictionary<Vector3, int> points = CollectRoadFeatures(unityTile, out lines);

			List<int> indices = FindCrossingIndices(lines);

			Vector2dBounds rect = Conversions.TileIdToBounds(tileId.X, tileId.Y, tileId.Z);
			Vector2d tileLatLonCenter = rect.Center;
			Vector2d tileCenter = Conversions.LatLonToMeters(tileLatLonCenter);
			float scale = (float)(map.WorldRelativeScale * Mathd.Pow(2, (map.InitialZoom - map.AbsoluteZoom)));

			Dictionary<int, Vector2d> reversePoints = new Dictionary<int, Vector2d>();
			foreach (var entry in points)
			{
				Vector3 position = entry.Key;

				//Vector2d latlong = _map.WorldToGeoPosition(position);
				// The line above is wrong. After one day's struggling, I finally found the correct line below. Damn it!
				Vector2d latlong = VectorExtensions.GetGeoPosition(position, tileCenter, scale);
				reversePoints.Add(entry.Value, latlong);
			}

			List<Vector2d> locations = new List<Vector2d>(indices.Count);
			foreach (int index in indices)
			{
				Vector2d location = reversePoints[index];
				locations.Add(location);
			}

			return locations;
		}
	}

}
