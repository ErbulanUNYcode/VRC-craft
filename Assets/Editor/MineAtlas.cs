// ASSISTANT-GENERATED CODE

using UnityEditor;
using UnityEngine;

namespace VRC_MINE.Tools
{
	[CreateAssetMenu(fileName = "MineAtlas", menuName = "MineTools/Blocks/MineAtlas")]
	public class MineAtlas : ScriptableObject
	{
		[SerializeField] private BlockTexture[] blocks;

		private const string savePath = "Assets/Textures/Atlas/";
		private const int blockTextureSize = 32;
		private const int blocksPerAxis = 16;
		private const int atlasSize = blockTextureSize * blocksPerAxis;
		private const int sideCount = 6;

		public void BuildAtlas()
		{
			var atlasPixels = new Color32[sideCount][];
			var flipPixels = new Color32[sideCount][];
			for (int side = 0; side < sideCount; side++)
			{
				atlasPixels[side] = new Color32[atlasSize * atlasSize];
				flipPixels[side] = new Color32[blocksPerAxis * blocksPerAxis];
			}

			int blockCount = Mathf.Min(blocks == null ? 0 : blocks.Length, 256);
			for (int blockID = 0; blockID < blockCount; blockID++)
			{
				var block = blocks[blockID];
				if (block == null) continue;
				int blockX = (blockID & 15) * blockTextureSize;
				int blockY = (blockID >> 4) * blockTextureSize;

				for (int sideIndex = 0; sideIndex < sideCount; sideIndex++)
				{
					var side = (BlockTexture.BlockSide)sideIndex;
					var sideData = block.GetSide(side);
					var pixels = block.GetSideData(side);
					if (pixels == null) continue;
					if (sideData.texture.width != blockTextureSize || sideData.texture.height != blockTextureSize)
					{
						Debug.LogError($"{block.name}, {side}: expected a 32x32 texture.", block);
						continue;
					}

					var maskData = block.GetLeave(side);
					var mask = block.GetLeaveData(side);
					if (mask != null && (maskData.texture.width != blockTextureSize || maskData.texture.height != blockTextureSize))
					{
						Debug.LogError($"{block.name}, {side}: expected a 32x32 biome mask.", block);
						mask = null;
					}
					for (int y = 0; y < blockTextureSize; y++)
					{
						int sourceRow = y * blockTextureSize;
						int targetRow = (blockY + y) * atlasSize + blockX;
						for (int x = 0; x < blockTextureSize; x++)
						{
							Color32 pixel = pixels[sourceRow + x];
							if (mask != null && mask[sourceRow + x].a != 0 && pixel.a != 0)
								pixel.a = 128;
							atlasPixels[sideIndex][targetRow + x] = pixel;
						}
					}

					flipPixels[sideIndex][blockID] = new Color32(
						sideData.randomFlipDiagonal ? byte.MaxValue : byte.MinValue,
						sideData.randomFlipHorizontal ? byte.MaxValue : byte.MinValue,
						sideData.randomFlipVertical ? byte.MaxValue : byte.MinValue,
						byte.MaxValue);
				}
			}

			var atlas = new Texture2DArray(atlasSize, atlasSize, sideCount, TextureFormat.RGBA32, false, false)
			{
				name = name + "_array",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp
			};
			var flipMap = new Texture2DArray(blocksPerAxis, blocksPerAxis, sideCount, TextureFormat.RGBA32, false, true)
			{
				name = name + "_flipmap_array",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp
			};
			for (int side = 0; side < sideCount; side++)
			{
				atlas.SetPixels32(atlasPixels[side], side);
				flipMap.SetPixels32(flipPixels[side], side);
			}
			atlas.Apply(false, false);
			flipMap.Apply(false, false);

			SaveOrUpdate(atlas, savePath + name + "_array.asset", "Atlas");
			SaveOrUpdate(flipMap, savePath + name + "_flipmap_array.asset", "Flipmap");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private static void SaveOrUpdate(Texture2DArray generated, string path, string label)
		{
			var existing = AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
			if (existing == null)
			{
				AssetDatabase.CreateAsset(generated, path);
				Debug.Log(label + " array created");
				return;
			}

			EditorUtility.CopySerialized(generated, existing);
			DestroyImmediate(generated);
			EditorUtility.SetDirty(existing);
			Debug.Log(label + " array updated (no GUID change)");
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
}
