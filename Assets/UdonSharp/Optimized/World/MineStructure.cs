using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace VRC_MINE.World
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class MineStructure : UdonSharpBehaviour
	{
		#region base
		[SerializeField] private Vector3Int size = Vector3Int.one;
		[SerializeField] private Vector3Int root = Vector3Int.zero;
		#endregion

		#region main
		[SerializeField] private Vector2Int[] positions = new Vector2Int[0];
		[SerializeField] private Vector2Int[] sizes = new Vector2Int[0];
		[SerializeField] private int[] IDs = new int[0];
		[SerializeField] private Vector2Int[] layers = new Vector2Int[3];
		#endregion

#if UNITY_EDITOR
		[SerializeField] public int[] blockList;
		[SerializeField] public string structureName = "Structure";

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
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(MineStructure))]
	public class MineStructureEditor : Editor
	{
		private MineStructure targetComponent;
		private void OnEnable()
		{
			targetComponent = (MineStructure)target;
		}

		public override void OnInspectorGUI()
		{

			var newName = EditorGUILayout.TextField("Structure Name", targetComponent.structureName);
			if (newName == targetComponent.structureName) return;
			targetComponent.structureName = newName;
			EditorUtility.SetDirty(target);
		}
	}
#endif
}