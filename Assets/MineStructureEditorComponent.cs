#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC_MINE.World;

namespace VRC_MINE.Tools
{
	public class MineStructureEditorComponent : MonoBehaviour
	{
		public MineStructure current { get; set; }
		public int[] blockList
		{
			get { return current.blockList; }
			set { current.blockList = value; }
		}

		public void UpdateProperties(Vector2Int[] positions, Vector2Int[] sizes, int[] IDs, Vector2Int[] layers)
		{
			current.UpdateProperties(positions, sizes, IDs, layers);

			EditorUtility.SetDirty(current);
		}

		public void ResetProperties(Vector3Int size)
		{
			current.ResetProperties(size);

			EditorUtility.SetDirty(current);
		}
	}
}
#endif