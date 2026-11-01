using UnityEngine;
using UnityEditor;

namespace VRC_MINE.Tools
{
	[CreateAssetMenu(fileName = "MineBiome", menuName = "MineTools/Structures/MineBiome")]
	public class MineBiome : ScriptableObject
	{
		[SerializeField] private Vector2Int atlasSize = Vector2Int.one;
		[SerializeField, Range(0, 15)] private int biomeId;
		[SerializeField] private Texture2D metadataAtlas;
		[SerializeField] private MineStructure[] structures = new MineStructure[0];
		[SerializeField] private Vector2Int[] structurePositions = new Vector2Int[0];
		[SerializeField] private int[] structureProbabilities = new int[0];

		public Vector2Int AtlasSize
		{
			get { return atlasSize; }
			set { atlasSize = value; }
		}

		public int BiomeId
		{
			get { return biomeId; }
			set { biomeId = Mathf.Clamp(value, 0, 15); }
		}

		public Texture2D MetadataAtlas
		{
			get { return metadataAtlas; }
			set { metadataAtlas = value; }
		}

		public MineStructure[] Structures
		{
			get { return structures; }
			set { structures = value; }
		}

		public Vector2Int[] StructurePositions
		{
			get { return structurePositions; }
			set { structurePositions = value; }
		}

		public int[] StructureProbabilities
		{
			get { return structureProbabilities; }
			set { structureProbabilities = value; }
		}
	}

	[CustomEditor(typeof(MineBiome))]
	public class MineBiomeEditor : Editor
	{
		private Texture2D preview;
		private Texture2D checkerTexture;
		private int lastStructureCount = -1;
		private int draggedStructure = -1;
		private Vector2Int dragOffset;
		private const int snapDistance = 2;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var sizeProperty = serializedObject.FindProperty("atlasSize");
			EditorGUILayout.PropertyField(sizeProperty);
			var biomeIdProperty = serializedObject.FindProperty("biomeId");
			biomeIdProperty.intValue = Mathf.Clamp(biomeIdProperty.intValue, 0, 15);
			EditorGUILayout.IntSlider(biomeIdProperty, 0, 15);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("metadataAtlas"));
			var structuresProperty = serializedObject.FindProperty("structures");
			var positionsProperty = serializedObject.FindProperty("structurePositions");
			if (positionsProperty.arraySize != structuresProperty.arraySize)
				positionsProperty.arraySize = structuresProperty.arraySize;
			var probabilitiesProperty = serializedObject.FindProperty("structureProbabilities");
			if (probabilitiesProperty.arraySize != structuresProperty.arraySize)
			{
				int oldSize = probabilitiesProperty.arraySize;
				probabilitiesProperty.arraySize = structuresProperty.arraySize;
				for (int i = oldSize; i < probabilitiesProperty.arraySize; i++)
					probabilitiesProperty.GetArrayElementAtIndex(i).intValue = 1;
			}
			for (int i = 0; i < probabilitiesProperty.arraySize; i++)
				probabilitiesProperty.GetArrayElementAtIndex(i).intValue = Mathf.Clamp(probabilitiesProperty.GetArrayElementAtIndex(i).intValue, 1, 200);

			var size = sizeProperty.vector2IntValue;
			var clampedSize = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
			if (size != clampedSize)
				sizeProperty.vector2IntValue = clampedSize;
			serializedObject.ApplyModifiedProperties();

			var biome = (MineBiome)target;
			var minimumAtlasSize = GetMinimumAtlasSize(biome);
			var currentAtlasSize = biome.AtlasSize;
			var constrainedAtlasSize = new Vector2Int(
				Mathf.Max(currentAtlasSize.x, minimumAtlasSize.x),
				Mathf.Max(currentAtlasSize.y, minimumAtlasSize.y));
			if (currentAtlasSize != constrainedAtlasSize)
			{
				sizeProperty.vector2IntValue = constrainedAtlasSize;
				serializedObject.ApplyModifiedProperties();
			}
			if (lastStructureCount != biome.Structures.Length)
			{
				PackStructurePositions(biome);
				lastStructureCount = biome.Structures.Length;
			}
			RebuildPreview(biome);
			if (biome.AtlasSize.x <= 0 || biome.AtlasSize.y <= 0)
				return;

			if (preview == null || preview.width != biome.AtlasSize.x || preview.height != biome.AtlasSize.y)
			{
				if (preview != null) DestroyImmediate(preview);
				preview = new Texture2D(biome.AtlasSize.x, biome.AtlasSize.y, TextureFormat.RGBA32, false);
				preview.name = "MineBiome Preview";
				preview.hideFlags = HideFlags.HideAndDontSave;
				preview.SetPixels(new Color[biome.AtlasSize.x * biome.AtlasSize.y]);
				preview.Apply(false, false);
			}
			if (checkerTexture == null)
			{
				checkerTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				checkerTexture.hideFlags = HideFlags.HideAndDontSave;
				checkerTexture.filterMode = FilterMode.Point;
				checkerTexture.wrapMode = TextureWrapMode.Repeat;
				checkerTexture.SetPixels(new[]
				{
					new Color(0.28f, 0.28f, 0.28f, 1), new Color(0.36f, 0.36f, 0.36f, 1),
					new Color(0.36f, 0.36f, 0.36f, 1), new Color(0.28f, 0.28f, 0.28f, 1)
				});
				checkerTexture.Apply(false, false);
			}

			EditorGUILayout.Space(8);
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
			var rect = GUILayoutUtility.GetAspectRect((float)biome.AtlasSize.x / biome.AtlasSize.y);
			HandleStructureDragging(biome, rect);
			GUI.DrawTextureWithTexCoords(rect, checkerTexture, new Rect(0, 0, rect.width / 8, rect.height / 8));
			if (preview != null)
				GUI.DrawTextureWithTexCoords(rect, preview, new Rect(0, 0, 1, 1));
			var structureBorderColor = new Color(1, 1, 1, 0.45f);
			var structureLabelStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				fontSize = 9,
				clipping = TextClipping.Clip
			};
			structureLabelStyle.normal.textColor = Color.white;
			for (int i = 0; i < biome.Structures.Length; i++)
			{
				if (biome.Structures[i] == null) continue;
				var structureSize = GetStructureSize(biome.Structures[i]);
				var position = biome.StructurePositions[i];
				var structureRect = new Rect(
					rect.x + (float)position.x / biome.AtlasSize.x * rect.width,
					rect.y + (float)(biome.AtlasSize.y - position.y - structureSize.y) / biome.AtlasSize.y * rect.height,
					(float)structureSize.x / biome.AtlasSize.x * rect.width,
					(float)structureSize.y / biome.AtlasSize.y * rect.height);
				EditorGUI.DrawRect(new Rect(structureRect.x, structureRect.y, structureRect.width, 1), structureBorderColor);
				EditorGUI.DrawRect(new Rect(structureRect.x, structureRect.yMax - 1, structureRect.width, 1), structureBorderColor);
				EditorGUI.DrawRect(new Rect(structureRect.x, structureRect.y, 1, structureRect.height), structureBorderColor);
				EditorGUI.DrawRect(new Rect(structureRect.xMax - 1, structureRect.y, 1, structureRect.height), structureBorderColor);
				int probability = i < biome.StructureProbabilities.Length ? Mathf.Clamp(biome.StructureProbabilities[i], 1, 200) : 1;
				GUI.Label(new Rect(structureRect.x + 2, structureRect.y + 1, structureRect.width - 4, 13), biome.Structures[i].name, structureLabelStyle);
				GUI.Label(new Rect(structureRect.x + 2, structureRect.y + 14, structureRect.width - 4, 13), "duplicates: " + probability, structureLabelStyle);
			}
			var borderColor = new Color(1, 1, 1, 0.45f);
			EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), borderColor);
			EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), borderColor);
			EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), borderColor);
			EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), borderColor);

			GUILayout.Space(10);
			float availableButtonWidth = Mathf.Max(120, EditorGUIUtility.currentViewWidth - 32);
			float buttonWidth = Mathf.Min(360, availableButtonWidth);
			float buttonHeight = buttonWidth / 4;
			var buildButtonStyle = new GUIStyle(GUI.skin.button)
			{
				fontSize = Mathf.Clamp(Mathf.RoundToInt(buttonHeight * 0.35f), 12, 28),
				alignment = TextAnchor.MiddleCenter
			};
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			bool buildClicked = GUILayout.Button("Build", buildButtonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (buildClicked)
				BuildAtlas(biome);
		}

		private void PackStructurePositions(MineBiome biome)
		{
			var structures = biome.Structures;
			var positions = biome.StructurePositions;
			if (structures == null || positions == null || positions.Length != structures.Length)
				return;

			int x = 0;
			int y = 0;
			int rowHeight = 0;
			bool changed = false;
			for (int i = 0; i < structures.Length; i++)
			{
				if (structures[i] == null) continue;
				var structure = new SerializedObject(structures[i]);
				var size = structure.FindProperty("size").vector3IntValue;
				var width = size.x * size.z;
				if (x + width > biome.AtlasSize.x)
				{
					x = 0;
					y += rowHeight;
					rowHeight = 0;
				}
				var position = new Vector2Int(x, y);
				if (positions[i] != position)
				{
					positions[i] = position;
					changed = true;
				}
				x += width;
				rowHeight = Mathf.Max(rowHeight, size.y);
			}
			if (changed) EditorUtility.SetDirty(biome);
		}

		private Vector2Int GetStructureSize(MineStructure structure)
		{
			var serialized = new SerializedObject(structure);
			var size = serialized.FindProperty("size").vector3IntValue;
			return new Vector2Int(size.x * size.z, size.y);
		}

		private Vector2Int GetMinimumAtlasSize(MineBiome biome)
		{
			var minimum = Vector2Int.one;
			for (int i = 0; i < biome.Structures.Length; i++)
			{
				if (biome.Structures[i] == null) continue;
				var size = GetStructureSize(biome.Structures[i]);
				var position = biome.StructurePositions[i];
				minimum.x = Mathf.Max(minimum.x, position.x + size.x);
				minimum.y = Mathf.Max(minimum.y, position.y + size.y);
			}
			return minimum;
		}

		private void HandleStructureDragging(MineBiome biome, Rect rect)
		{
			var current = Event.current;
			Vector2 mouse = current.mousePosition;
			Vector2Int atlasPosition = new Vector2Int(
				Mathf.FloorToInt((mouse.x - rect.x) / rect.width * biome.AtlasSize.x),
				Mathf.FloorToInt((rect.yMax - mouse.y) / rect.height * biome.AtlasSize.y));

			if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(mouse))
			{
				for (int i = biome.Structures.Length - 1; i >= 0; i--)
				{
					if (biome.Structures[i] == null) continue;
					var size = GetStructureSize(biome.Structures[i]);
					var position = biome.StructurePositions[i];
					if (atlasPosition.x >= position.x && atlasPosition.x < position.x + size.x &&
						atlasPosition.y >= position.y && atlasPosition.y < position.y + size.y)
					{
						draggedStructure = i;
						dragOffset = atlasPosition - position;
						current.Use();
						break;
					}
				}
			}

			if (current.type == EventType.ScrollWheel && rect.Contains(mouse))
			{
				for (int i = biome.Structures.Length - 1; i >= 0; i--)
				{
					if (biome.Structures[i] == null) continue;
					var size = GetStructureSize(biome.Structures[i]);
					var position = biome.StructurePositions[i];
					if (atlasPosition.x < position.x || atlasPosition.x >= position.x + size.x ||
						atlasPosition.y < position.y || atlasPosition.y >= position.y + size.y)
						continue;

					if (biome.StructureProbabilities == null || biome.StructureProbabilities.Length != biome.Structures.Length)
					{
						biome.StructureProbabilities = new int[biome.Structures.Length];
						for (int j = 0; j < biome.StructureProbabilities.Length; j++)
							biome.StructureProbabilities[j] = 1;
					}
					int delta = current.delta.y > 0 ? -1 : 1;
					biome.StructureProbabilities[i] = Mathf.Clamp(biome.StructureProbabilities[i] + delta, 1, 200);
					EditorUtility.SetDirty(biome);
					current.Use();
					Repaint();
					break;
				}
			}

			if (draggedStructure >= 0 && current.type == EventType.MouseDrag && rect.Contains(mouse))
			{
				var size = GetStructureSize(biome.Structures[draggedStructure]);
				var position = atlasPosition - dragOffset;
				SnapStructurePosition(biome, draggedStructure, size, ref position);
				position.x = Mathf.Clamp(position.x, 0, biome.AtlasSize.x - size.x);
				position.y = Mathf.Clamp(position.y, 0, biome.AtlasSize.y - size.y);
				if (biome.StructurePositions[draggedStructure] != position)
				{
					biome.StructurePositions[draggedStructure] = position;
					EditorUtility.SetDirty(biome);
					RebuildPreview(biome);
					Repaint();
				}
				current.Use();
			}

			if (current.type == EventType.MouseUp && current.button == 0)
				draggedStructure = -1;
		}

		private void SnapStructurePosition(MineBiome biome, int structureIndex, Vector2Int size, ref Vector2Int position)
		{
			for (int i = 0; i < biome.Structures.Length; i++)
			{
				if (i == structureIndex || biome.Structures[i] == null) continue;
				var otherSize = GetStructureSize(biome.Structures[i]);
				var other = biome.StructurePositions[i];

				int[] xEdges =
				{
					other.x,
					other.x + otherSize.x,
					other.x - size.x,
					other.x + otherSize.x - size.x
				};
				for (int j = 0; j < xEdges.Length; j++)
					if (Mathf.Abs(position.x - xEdges[j]) <= snapDistance)
					{
						position.x = xEdges[j];
						break;
					}

				int[] yEdges =
				{
					other.y,
					other.y + otherSize.y,
					other.y - size.y,
					other.y + otherSize.y - size.y
				};
				for (int j = 0; j < yEdges.Length; j++)
					if (Mathf.Abs(position.y - yEdges[j]) <= snapDistance)
					{
						position.y = yEdges[j];
						break;
					}
			}
		}

		private void RebuildPreview(MineBiome biome)
		{
			if (preview == null || preview.width != biome.AtlasSize.x || preview.height != biome.AtlasSize.y)
			{
				if (preview != null) DestroyImmediate(preview);
				preview = new Texture2D(biome.AtlasSize.x, biome.AtlasSize.y, TextureFormat.RGBA32, false);
				preview.hideFlags = HideFlags.HideAndDontSave;
				preview.filterMode = FilterMode.Point;
				preview.wrapMode = TextureWrapMode.Clamp;
			}

			var pixels = new Color32[biome.AtlasSize.x * biome.AtlasSize.y];
			var structures = biome.Structures;
			var placements = biome.StructurePositions;
			for (int i = 0; i < structures.Length; i++)
			{
				if (structures[i] == null) continue;
				var structure = new SerializedObject(structures[i]);
				var size = structure.FindProperty("size").vector3IntValue;
				var positions = structure.FindProperty("positions");
				var sizes = structure.FindProperty("sizes");
				var IDs = structure.FindProperty("IDs");
				var layers = structure.FindProperty("layers");
				for (int layer = 0; layer < layers.arraySize; layer++)
				{
					int stage = layer / size.z;
					var range = layers.GetArrayElementAtIndex(layer).vector2IntValue;
					for (int zone = range.x; zone < range.y; zone++)
					{
						var position = positions.GetArrayElementAtIndex(zone).vector2IntValue;
						var zoneSize = sizes.GetArrayElementAtIndex(zone).vector2IntValue;
						var id = (byte)IDs.GetArrayElementAtIndex(zone).intValue;
						for (int y = 0; y < zoneSize.y; y++)
							for (int x = 0; x < zoneSize.x; x++)
							{
								int atlasX = placements[i].x + position.x + (layer % size.z) * size.x + x;
								int atlasY = placements[i].y + position.y + y;
								if (atlasX >= 0 && atlasX < biome.AtlasSize.x && atlasY >= 0 && atlasY < biome.AtlasSize.y)
								{
									int pixelIndex = atlasX + atlasY * biome.AtlasSize.x;
									var pixel = pixels[pixelIndex];
									if (stage == 1)
										pixel.g = id;
									else
									{
										pixel.r = id;
										if (stage == 2) pixel.b = byte.MaxValue;
									}
									pixel.a = byte.MaxValue;
									pixels[pixelIndex] = pixel;
								}
							}
					}
				}
			}
			preview.SetPixels32(pixels);
			preview.Apply(false, false);
		}

		private void BuildAtlas(MineBiome biome)
		{
			RebuildPreview(biome);
			var folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(biome)).Replace('\\', '/');
			var atlasFolder = folder + "/BiomeAtlases";
			if (!AssetDatabase.IsValidFolder(atlasFolder))
				AssetDatabase.CreateFolder(folder, "BiomeAtlases");

			BuildStructureAtlas(biome, atlasFolder);
			BuildMetadataAtlas(biome, atlasFolder);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void BuildStructureAtlas(MineBiome biome, string atlasFolder)
		{
			var path = atlasFolder + "/" + biome.name + ".asset";
			var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			bool recreateAtlas = atlas == null ||
				atlas.width != biome.AtlasSize.x ||
				atlas.height != biome.AtlasSize.y;

			if (recreateAtlas)
			{
				var replacement = new Texture2D(
					biome.AtlasSize.x,
					biome.AtlasSize.y,
					TextureFormat.RGBA32,
					false);
				replacement.filterMode = FilterMode.Point;
				replacement.wrapMode = TextureWrapMode.Clamp;

				if (atlas != null)
				{
					EditorUtility.CopySerialized(replacement, atlas);
					DestroyImmediate(replacement);
					EditorUtility.SetDirty(atlas);
				}
				else
				{
					atlas = replacement;
					AssetDatabase.CreateAsset(atlas, path);
				}
			}

			atlas.filterMode = FilterMode.Point;
			atlas.wrapMode = TextureWrapMode.Clamp;
			atlas.SetPixels32(preview.GetPixels32());
			atlas.Apply(false, false);
			EditorUtility.SetDirty(atlas);
		}

		private void BuildMetadataAtlas(MineBiome biome, string atlasFolder)
		{
			const int atlasSize = 64;
			const int tileSize = 16;
			const int maxMetadataEntries = tileSize * tileSize;

			var metadataAtlas = biome.MetadataAtlas;
			if (metadataAtlas == null)
			{
				var path = atlasFolder + "/BiomeStructureMetadataAtlas.asset";
				metadataAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (metadataAtlas == null)
				{
					metadataAtlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);
					metadataAtlas.filterMode = FilterMode.Point;
					metadataAtlas.wrapMode = TextureWrapMode.Clamp;
					metadataAtlas.SetPixels32(new Color32[atlasSize * atlasSize]);
					metadataAtlas.Apply(false, false);
					AssetDatabase.CreateAsset(metadataAtlas, path);
				}

				biome.MetadataAtlas = metadataAtlas;
				EditorUtility.SetDirty(biome);
			}

			if (metadataAtlas.width != atlasSize || metadataAtlas.height != atlasSize)
			{
				Debug.LogError("Biome structure metadata atlas must be 64x64.", metadataAtlas);
				return;
			}

			var pixels = metadataAtlas.GetPixels32();
			int tileX = (biome.BiomeId & 3) * tileSize;
			int tileY = (biome.BiomeId >> 2) * tileSize;

			for (int y = 0; y < tileSize; y++)
				for (int x = 0; x < tileSize; x++)
					pixels[tileX + x + (tileY + y) * atlasSize] = new Color32(0, 0, 0, 0);

			int metadataIndex = 0;
			for (int i = 0; i < biome.Structures.Length; i++)
			{
				if (biome.Structures[i] == null) continue;
				int duplicates = i < biome.StructureProbabilities.Length
					? Mathf.Clamp(biome.StructureProbabilities[i], 1, 200)
					: 1;
				if (metadataIndex + duplicates > maxMetadataEntries) break;

				for (int copy = 0; copy < duplicates; copy++)
				{
					int localX = metadataIndex & 15;
					int localY = metadataIndex >> 4;
					pixels[tileX + localX + (tileY + localY) * atlasSize] =
						new Color32((byte)i, 0, 0, byte.MaxValue);
					metadataIndex++;
				}
			}

			metadataAtlas.SetPixels32(pixels);
			metadataAtlas.Apply(false, false);
			EditorUtility.SetDirty(metadataAtlas);
		}

		private void OnDisable()
		{
			if (preview != null) DestroyImmediate(preview);
			if (checkerTexture != null) DestroyImmediate(checkerTexture);
		}
	}
}
