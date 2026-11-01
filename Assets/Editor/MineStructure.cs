using UnityEngine;

namespace VRC_MINE.Tools
{
	[CreateAssetMenu(fileName = "MineStructure", menuName = "MineTools/Structures/MineStructure")]
	public class MineStructure : ScriptableObject
	{
		#region base
		[SerializeField] private Vector3Int size = new Vector3Int(3, 3, 3);
		[SerializeField] private Vector3Int root = Vector3Int.zero;
		#endregion

		#region main
		[SerializeField] private Vector2Int[] positions = new Vector2Int[0];
		[SerializeField] private Vector2Int[] sizes = new Vector2Int[0];
		[SerializeField] private int[] IDs = new int[0];
		[SerializeField] private Vector2Int[] layers = new Vector2Int[9];
		#endregion

		[SerializeField] public int[] blockList = new int[0];

		public void ResetProperties(Vector3Int newSize)
		{
			size = newSize;
			root = Vector3Int.zero;
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
