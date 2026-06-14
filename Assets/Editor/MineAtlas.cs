// ASSISTANT-GENERATED CODE

using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "MineAtlas", menuName = "MineTools/MineAtlas")]
public class MineAtlas : ScriptableObject
{
	[SerializeField] private BlockTexture[] blocks;

	private const string savePath = "Assets/Textures/Atlas/";

	public void BuildAtlas()
	{
		var atlas = new Texture2D(512, 512, TextureFormat.RGBA32, false, false);
		atlas.filterMode = FilterMode.Point;
		atlas.wrapMode = TextureWrapMode.Clamp;

		atlas.LoadRawTextureData(new byte[atlas.width * atlas.height * 4]);
		atlas.Apply(false, false);

		for (int i = 0; i < blocks.Length; i++)
		{
			var b = blocks[i];
			if (b == null) continue;

			var pos = new Vector2Int((i & 15) * 32, (i >> 4) * 48);

			atlas.SetPixels32(pos.x, pos.y, 16, 16, b.GetSideData(BlockTexture.BlockSide.Right));

			pos.x += 16;
			atlas.SetPixels32(pos.x, pos.y, 16, 16, b.GetSideData(BlockTexture.BlockSide.Left));

			pos.x -= 16;
			pos.y += 16;
			atlas.SetPixels32(pos.x, pos.y, 16, 16, b.GetSideData(BlockTexture.BlockSide.Up));

			pos.x += 16;
			atlas.SetPixels32(pos.x, pos.y, 16, 16, b.GetSideData(BlockTexture.BlockSide.Down));

			pos.x -= 16;
			pos.y += 16;
			atlas.SetPixels32(pos.x, pos.y, 16, 16, b.GetSideData(BlockTexture.BlockSide.Front));

			pos.x += 16;
			atlas.SetPixels32(pos.x, pos.y, 16, 16, b.GetSideData(BlockTexture.BlockSide.Back));
		}

		atlas.Apply(false, false);

		string fullPath = savePath + name + ".asset";

		var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);

		if (existing != null)
		{
			EditorUtility.CopySerialized(atlas, existing);
			DestroyImmediate(atlas);
			EditorUtility.SetDirty(existing);

			Debug.Log("Atlas updated (no GUID change)");
		}
		else
		{
			AssetDatabase.CreateAsset(atlas, fullPath);
			Debug.Log("Atlas created");
		}

		var flipMap = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
		flipMap.filterMode = FilterMode.Point;
		flipMap.wrapMode = TextureWrapMode.Clamp;
		flipMap.LoadRawTextureData(new byte[flipMap.width * flipMap.height * 4]);
		flipMap.Apply(false, false);

		for (int i = 0; i < blocks.Length; i++)
		{
			var b = blocks[i];
			if (b == null) continue;

			var pos = new Vector2Int((i & 15) * 2, (i >> 4) * 3);
			var s = b.GetSide(BlockTexture.BlockSide.Right);
			flipMap.SetPixels32(pos.x, pos.y, 1, 1, new Color32[] { new Color32(s.randomFlipDiagonal ? byte.MaxValue : byte.MinValue, s.randomFlipHorizontal ? byte.MaxValue : byte.MinValue, s.randomFlipVertical ? byte.MaxValue : byte.MinValue, 0) });
			s = b.GetSide(BlockTexture.BlockSide.Left);
			pos.x += 1;
			flipMap.SetPixels32(pos.x, pos.y, 1, 1, new Color32[] { new Color32(s.randomFlipDiagonal ? byte.MaxValue : byte.MinValue, s.randomFlipHorizontal ? byte.MaxValue : byte.MinValue, s.randomFlipVertical ? byte.MaxValue : byte.MinValue, 1) });
			s = b.GetSide(BlockTexture.BlockSide.Up);
			pos.x -= 1;
			pos.y += 1;
			flipMap.SetPixels32(pos.x, pos.y, 1, 1, new Color32[] { new Color32(s.randomFlipDiagonal ? byte.MaxValue : byte.MinValue, s.randomFlipHorizontal ? byte.MaxValue : byte.MinValue, s.randomFlipVertical ? byte.MaxValue : byte.MinValue, 2) });
			s = b.GetSide(BlockTexture.BlockSide.Down);
			pos.x += 1;
			flipMap.SetPixels32(pos.x, pos.y, 1, 1, new Color32[] { new Color32(s.randomFlipDiagonal ? byte.MaxValue : byte.MinValue, s.randomFlipHorizontal ? byte.MaxValue : byte.MinValue, s.randomFlipVertical ? byte.MaxValue : byte.MinValue, 3) });
			s = b.GetSide(BlockTexture.BlockSide.Front);
			pos.x -= 1;
			pos.y += 1;
			flipMap.SetPixels32(pos.x, pos.y, 1, 1, new Color32[] { new Color32(s.randomFlipDiagonal ? byte.MaxValue : byte.MinValue, s.randomFlipHorizontal ? byte.MaxValue : byte.MinValue, s.randomFlipVertical ? byte.MaxValue : byte.MinValue, 4) });
			s = b.GetSide(BlockTexture.BlockSide.Back);
			pos.x += 1;
			flipMap.SetPixels32(pos.x, pos.y, 1, 1, new Color32[] { new Color32(s.randomFlipDiagonal ? byte.MaxValue : byte.MinValue, s.randomFlipHorizontal ? byte.MaxValue : byte.MinValue, s.randomFlipVertical ? byte.MaxValue : byte.MinValue, 5) });
		}

		flipMap.Apply(false, false);
		fullPath = savePath + name + "_flipmap.asset";
		existing = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
		if (existing != null)
		{
			EditorUtility.CopySerialized(flipMap, existing);
			DestroyImmediate(flipMap);
			EditorUtility.SetDirty(existing);
			Debug.Log("Flipmap updated (no GUID change)");
		}
		else
		{
			AssetDatabase.CreateAsset(flipMap, fullPath);
			Debug.Log("Flipmap created");
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}

// build atlas button
[CustomEditor(typeof(MineAtlas))]
public class MineAtlasEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("Build Atlas"))
		{
			((MineAtlas)target).BuildAtlas();
			Debug.Log("Atlas built");
		}
	}
}