
using UdonSharp;
using UnityEngine;

namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class ItemData : UdonSharpBehaviour
	{
		private Mesh[] _iconsMeshes = new Mesh[256];

		[SerializeField] private string[] _names;
		[SerializeField] private int[] _maxCounts;
		[SerializeField] private Material[] _materials;
		[SerializeField] private IconType[] _iconTypes;
		[SerializeField] private int[] _iconIndexes;

		[SerializeField] private Mesh[] _iconSamples;
		public int MaxCount(int iconIndex) { return _maxCounts[iconIndex]; }
		public Mesh GetMesh(int iconIndex)
		{
			if (_iconsMeshes[iconIndex] == null)
			{
				var s = _iconSamples[(int)_iconTypes[iconIndex]];
				var uv = s.uv;
				switch (_iconTypes[iconIndex])
				{
					case IconType.block:
						for (int i = 0; i < uv.Length; i++) uv[i] += new Vector2(0.0625f * (_iconIndexes[iconIndex] & 15), 0.09375f * (_iconIndexes[iconIndex] >> 4));
						break;
				}

				_iconsMeshes[iconIndex] = new Mesh();
				_iconsMeshes[iconIndex].vertices = s.vertices;
				_iconsMeshes[iconIndex].triangles = s.triangles;
				_iconsMeshes[iconIndex].uv = uv;
				_iconsMeshes[iconIndex].RecalculateNormals();
			}

			return _iconsMeshes[iconIndex];
		}
		public Material GetMaterial(int iconIndex)
		{
			return _materials[iconIndex];
		}
	}

	public enum IconType
	{
		block
	}
}