using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC_MINE.Data;

internal static class SmartChunkGizmos
{
	[DrawGizmo(GizmoType.Selected)]
	private static void Draw(Transform selectedTransform, GizmoType gizmoType)
	{
		if (!Application.isPlaying) return;

		Color previousColor = Gizmos.color;
		Gizmos.color = Color.cyan;
		foreach (SmartChunk chunk in selectedTransform.GetComponents<SmartChunk>())
		{
			var behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(chunk);
			if (behaviour.GetProgramVariable(nameof(SmartChunk.hasData)) is not true) continue;

			var position = (Vector2Int)behaviour.GetProgramVariable(nameof(SmartChunk.chunkPosition));
			var data = (byte[])behaviour.GetProgramVariable(nameof(SmartChunk.blockData));
			for (int z = 0; z < 16; z++)
			for (int x = 0; x < 16; x++)
			{
				int height = data[(128 << 8) + x + (z << 4)];
				Vector3 center = new Vector3(position.x * 16 + x + 0.5f, height + 0.5f, position.y * 16 + z + 0.5f);
				Gizmos.DrawWireCube(center, Vector3.one);
			}
		}
		Gizmos.color = previousColor;
	}
}
