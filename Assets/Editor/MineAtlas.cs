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
			var facePixels = new Color32[blockCount * sideCount][];
			for (int slice = 0; slice < facePixels.Length; slice++)
				facePixels[slice] = new Color32[blockTextureSize * blockTextureSize];

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
						int faceRow = y * blockTextureSize;
						for (int x = 0; x < blockTextureSize; x++)
						{
							Color32 pixel = pixels[sourceRow + x];
							if (mask != null && pixel.a == 0)
							{
								// Fill holes in masked foliage faces with a subdued
								// authored leaf color, without biome modulation.
								pixel.r = (byte)(block.leafTint.r * 255f);
								pixel.g = (byte)(block.leafTint.g * 255f * 0.5f);
								pixel.b = (byte)(block.leafTint.b * 255f);
								// Keep the original transparency: this only removes
								// black/incorrect RGB from transparent texels.
								pixel.a = 0;
							}
							else if (mask != null && mask[sourceRow + x].a > 127 && pixel.a != 0)
							{
								// R/B store the authored leaf tint, while G stores
								// the mask strength multiplied by its authored tint.
								var maskPixel = mask[sourceRow + x];
								pixel.r = (byte)(block.leafTint.r * maskPixel.g);
								pixel.g = (byte)(maskPixel.g * block.leafTint.g);
								pixel.b = (byte)(block.leafTint.b * maskPixel.g);
								pixel.a = 128;
							}
							atlasPixels[sideIndex][targetRow + x] = pixel;
							facePixels[blockID * sideCount + sideIndex][faceRow + x] = pixel;
						}
					}

					flipPixels[sideIndex][blockID] = new Color32(
						sideData.randomFlipDiagonal ? byte.MaxValue : byte.MinValue,
						sideData.randomFlipHorizontal ? byte.MaxValue : byte.MinValue,
						sideData.randomFlipVertical ? byte.MaxValue : byte.MinValue,
						byte.MaxValue);
				}
			}

			// Keep Unity's standard mip generation for the first test pass.
			// Point filtering prevents interpolation inside each mip level.
			var atlas = new Texture2DArray(atlasSize, atlasSize, sideCount, TextureFormat.RGBA32, true, false)
			{
				name = name + "_array",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Repeat
			};
			var flipMap = new Texture2DArray(blocksPerAxis, blocksPerAxis, sideCount, TextureFormat.RGBA32, false, true)
			{
				name = name + "_flipmap_array",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp
			};
			var faces = new Texture2DArray(blockTextureSize, blockTextureSize, blockCount * sideCount, TextureFormat.RGBA32, true, false)
			{
				name = name + "_faces_array",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp
			};
			for (int side = 0; side < sideCount; side++)
			{
				atlas.SetPixels32(atlasPixels[side], side);
				flipMap.SetPixels32(flipPixels[side], side);
			}
			for (int slice = 0; slice < facePixels.Length; slice++)
				faces.SetPixels32(facePixels[slice], slice);
			BuildFaceMipmaps(faces, facePixels, blockTextureSize);
			BuildAtlasMipmaps(atlas, atlasPixels);
			atlas.Apply(false, false);
			flipMap.Apply(false, false);
			faces.Apply(false, false);

			SaveOrUpdate(atlas, savePath + name + "_array.asset", "Atlas");
			SaveOrUpdate(flipMap, savePath + name + "_flipmap_array.asset", "Flipmap");
			SaveOrUpdate(faces, savePath + name + "_faces_array.asset", "Faces");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private static void BuildFaceMipmaps(Texture2DArray texture, Color32[][] basePixels, int baseSize)
		{
			for (int slice = 0; slice < basePixels.Length; slice++)
			{
				var previous = basePixels[slice];
				int previousSize = baseSize;

				for (int mip = 1; previousSize > 1; mip++)
				{
					int size = previousSize >> 1;
					var current = new Color32[size * size];

					for (int y = 0; y < size; y++)
					{
						int sourceY = y * 2;
						for (int x = 0; x < size; x++)
						{
							int sourceX = x * 2;
							Color32 a = previous[sourceX + sourceY * previousSize];
							Color32 b = previous[sourceX + 1 + sourceY * previousSize];
							Color32 c = previous[sourceX + (sourceY + 1) * previousSize];
							Color32 d = previous[sourceX + 1 + (sourceY + 1) * previousSize];

							current[x + y * size] = CombineMipPixels(a, b, c, d);
						}
					}

					texture.SetPixels32(current, slice, mip);
					previous = current;
					previousSize = size;
				}
			}
		}

		private static void BuildAtlasMipmaps(Texture2DArray texture, Color32[][] basePixels)
		{
			for (int slice = 0; slice < basePixels.Length; slice++)
			{
				var previous = basePixels[slice];
				int previousSize = atlasSize;
				int previousTileSize = blockTextureSize;

				for (int mip = 1; previousSize > 1 && previousTileSize > 1; mip++)
				{
					int size = previousSize >> 1;
					int tileSize = Mathf.Max(1, previousTileSize >> 1);
					var current = new Color32[size * size];

					for (int tileY = 0; tileY < blocksPerAxis; tileY++)
					for (int tileX = 0; tileX < blocksPerAxis; tileX++)
					for (int y = 0; y < tileSize; y++)
					for (int x = 0; x < tileSize; x++)
					{
						int sourceX = tileX * previousTileSize + x * 2;
						int sourceY = tileY * previousTileSize + y * 2;
						Color32 a = previous[sourceX + sourceY * previousSize];
						Color32 b = previous[sourceX + 1 + sourceY * previousSize];
						Color32 c = previous[sourceX + (sourceY + 1) * previousSize];
						Color32 d = previous[sourceX + 1 + (sourceY + 1) * previousSize];
						int targetX = tileX * tileSize + x;
						int targetY = tileY * tileSize + y;
						current[targetX + targetY * size] = CombineMipPixels(a, b, c, d);
					}

					texture.SetPixels32(current, slice, mip);
					previous = current;
					previousSize = size;
					previousTileSize = tileSize;
				}
			}
		}

		private static Color32 CombineMipPixels(Color32 a, Color32 b, Color32 c, Color32 d)
		{
			int count = 0;
			int red = 0;
			int green = 0;
			int blue = 0;

			if (a.a != 0) { red += a.r; green += a.g; blue += a.b; count++; }
			if (b.a != 0) { red += b.r; green += b.g; blue += b.b; count++; }
			if (c.a != 0) { red += c.r; green += c.g; blue += c.b; count++; }
			if (d.a != 0) { red += d.r; green += d.g; blue += d.b; count++; }

			if (count < 2)
				return new Color32(0, 0, 0, 0);

			return new Color32(
				(byte)(red / count),
				(byte)(green / count),
				(byte)(blue / count),
				255);
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
