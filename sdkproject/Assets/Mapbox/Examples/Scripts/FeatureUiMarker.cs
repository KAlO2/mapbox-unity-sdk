namespace Mapbox.Examples
{
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;
	using UnityEngine.UI;
	using System.Linq;
	using System.Text;

	public class FeatureUiMarker : MonoBehaviour
	{
		[SerializeField]
		private Transform _wrapperMarker;
		[SerializeField]
		private Transform _infoPanel;
		[SerializeField]
		private Text _info;

		private Vector3[] _targetVerts;
		private VectorEntity _selectedFeature;

		void Update()
		{
			Snap();
		}

		internal void Clear()
		{
			gameObject.SetActive(false);
		}

		internal void Show(VectorEntity selectedFeature)
		{
			// Shift + LMB key: unselect feature if selected one
			bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			if (selectedFeature == null || isShiftPressed)
			{
				Clear();
				return;
			}
			_selectedFeature = selectedFeature;
			transform.position = new Vector3(0, 0, 0);
			var mesh = selectedFeature.MeshFilter;

			if (mesh != null)
			{
				_targetVerts = mesh.mesh.vertices;
				Snap();
			}
			gameObject.SetActive(true);
		}

		private void Snap()
		{
			if (_targetVerts == null || _selectedFeature == null)
				return;

			var left = float.MaxValue;
			var right = float.MinValue;
			var top = float.MinValue;
			var bottom = float.MaxValue;
			foreach (var vert in _targetVerts)
			{
				var pos = Camera.main.WorldToScreenPoint(_selectedFeature.Transform.position + (_selectedFeature.Transform.lossyScale.x * vert));
				if (pos.x < left)
					left = pos.x;
				else if (pos.x > right)
					right = pos.x;
				if (pos.y > top)
					top = pos.y;
				else if (pos.y < bottom)
					bottom = pos.y;
			}

			float margin = 10;
			_wrapperMarker.position = new Vector2(left - margin, top + margin);
			(_wrapperMarker as RectTransform).sizeDelta = new Vector2(right - left + margin * 2, top - bottom + margin * 2);
			
			StringBuilder sb = new StringBuilder(256);
			foreach(var property in _selectedFeature.Feature.Properties)
				sb.Append(property.Key).Append(" = ").Append(property.Value).Append(System.Environment.NewLine);
			sb.Append("position = ").Append(_selectedFeature.Transform.position.ToString());
			
			string content = sb.ToString();
			_info.text = content;

			float infoHeight = _info.rectTransform.sizeDelta.y;  // FIXME: not correct
			_infoPanel.position = new Vector2(right + margin, bottom - margin + infoHeight);
		}
	}
}
