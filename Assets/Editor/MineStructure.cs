using UnityEngine;

namespace VRC_MINE.Tools
{
	[CreateAssetMenu(fileName = "MineStructure", menuName = "MineTools/Structures/MineStructure")]
	public class MineStructure : ScriptableObject
	{
		#region base
		[SerializeField] private Vector3Int size = new Vector3Int(3, 3, 3);
		[SerializeField] private Vector3Int root = new Vector3Int(1, 0, 1);
		[SerializeField] private int rootOffsetMask;
		[SerializeField] private int variableHeight;
		#endregion

		#region main
		[SerializeField] private Vector2Int[] positions = new Vector2Int[0];
		[SerializeField] private Vector2Int[] sizes = new Vector2Int[0];
		[SerializeField] private int[] IDs = new int[0];
		[SerializeField] private Vector2Int[] layers = new Vector2Int[9];
		#endregion

		[SerializeField] public int[] blockList = new int[0];

		private void OnValidate()
		{
			size = new Vector3Int(
				Mathf.Clamp(size.x, 2, 9),
				Mathf.Clamp(size.y, 3, 34),
				Mathf.Clamp(size.z, 2, 9));
			rootOffsetMask &= 3;
			if ((size.x & 1) != 0) rootOffsetMask &= ~1;
			if ((size.z & 1) != 0) rootOffsetMask &= ~2;
			root = new Vector3Int(
				(size.x - 1) / 2 + ((rootOffsetMask & 1) != 0 ? 1 : 0),
				0,
				(size.z - 1) / 2 + ((rootOffsetMask & 2) != 0 ? 1 : 0));
			variableHeight = Mathf.Clamp(variableHeight, 0, 7);
		}

		public void ResetProperties(Vector3Int newSize)
		{
			size = newSize;
			rootOffsetMask = 0;
			root = new Vector3Int((newSize.x - 1) / 2, 0, (newSize.z - 1) / 2);
			positions = new Vector2Int[0];
			sizes = new Vector2Int[0];
			IDs = new int[0];
			layers = new Vector2Int[newSize.z * 3];
		}

		public void UpdateProperties(Vector2Int[] positions, Vector2Int[] sizes, int[] IDs, Vector2Int[] layers)
		{
			this.positions = positions;
			this.sizes = sizes;
			this.IDs = IDs;
			this.layers = layers;
		}
	}

}
