namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	public class FeatureSelectionDetector : MonoBehaviour
	{
		private FeatureUiMarker _marker;
		private VectorEntity _feature;

		private static VectorEntity selectedFeature;
		public static VectorEntity GetSelectedFeature() { return selectedFeature; }

		public void OnMouseUpAsButton()
		{
			selectedFeature = _feature;
			_marker.Show(_feature);
		}

		internal void Initialize(FeatureUiMarker marker, VectorEntity ve)
		{
			_marker = marker;
			_feature = ve;
		}
	}
}