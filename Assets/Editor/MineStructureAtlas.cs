using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRC_MINE.Tools
{
	[CreateAssetMenu(fileName = "MineStructureAtlas", menuName = "MineTools/Structures/MineStructureAtlas")]
	public class MineStructureAtlas : ScriptableObject
	{
		[SerializeField] private Vector2Int atlasSize = Vector2Int.one;
		[SerializeField] private MineStructure[] structures = new MineStructure[0];
		[SerializeField] private Vector2Int[] structurePositions = new Vector2Int[0];
		[SerializeField] private string[] biomeNames = new string[0];
		[SerializeField] private int[] biomeProbabilities = new int[0];

		private void OnValidate()
		{
			atlasSize = new Vector2Int(256, Mathf.Max(1, atlasSize.y));
			if (biomeProbabilities != null)
				for (int i = 0; i < biomeProbabilities.Length; i++)
					biomeProbabilities[i] = Mathf.Clamp(biomeProbabilities[i], 0, 256);
		}

		public Vector2Int AtlasSize
		{
			get { return atlasSize; }
			set { atlasSize = new Vector2Int(256, Mathf.Max(1, value.y)); }
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

		public string[] BiomeNames
		{
			get { return biomeNames; }
			set { biomeNames = value ?? new string[0]; }
		}

		public int[] BiomeProbabilities
		{
			get { return biomeProbabilities; }
			set { biomeProbabilities = value ?? new int[0]; }
		}
	}

	[CustomEditor(typeof(MineStructureAtlas))]
	public class MineStructureAtlasEditor : Editor
	{
		private Texture2D preview;
		private Texture2D checkerTexture;
		private int lastBiomeCount = -1;
		private int lastProbabilityStructureCount = -1;
		private int draggedStructure = -1;
		private Vector2Int dragOffset;
		private int selectedBioIndex;
		private string hoveredStructureName;
		private GUIStyle structureHoverTooltipStyle;
		private const int snapDistance = 2;

		private void SyncBiomeProbabilities(
			SerializedProperty biomeNamesProperty,
			SerializedProperty structuresProperty,
			SerializedProperty probabilitiesProperty)
		{
			int biomeCount = biomeNamesProperty.arraySize;
			int structureCount = structuresProperty.arraySize;
			int oldBiomeCount = lastBiomeCount >= 0 ? lastBiomeCount : biomeCount;
			int oldStructureCount = lastProbabilityStructureCount >= 0
				? lastProbabilityStructureCount
				: (oldBiomeCount > 0 && probabilitiesProperty.arraySize % oldBiomeCount == 0
					? probabilitiesProperty.arraySize / oldBiomeCount
					: 0);
			int[] oldValues = new int[probabilitiesProperty.arraySize];
			for (int i = 0; i < oldValues.Length; i++)
				oldValues[i] = probabilitiesProperty.GetArrayElementAtIndex(i).intValue;

			probabilitiesProperty.arraySize = biomeCount * structureCount;
			for (int biomeIndex = 0; biomeIndex < biomeCount; biomeIndex++)
				for (int structureIndex = 0; structureIndex < structureCount; structureIndex++)
				{
					int oldIndex = biomeIndex * oldStructureCount + structureIndex;
					int value = oldBiomeCount > biomeIndex && structureIndex < oldStructureCount &&
						oldIndex >= 0 && oldIndex < oldValues.Length
						? oldValues[oldIndex]
						: 0;
					probabilitiesProperty
						.GetArrayElementAtIndex(biomeIndex * structureCount + structureIndex)
						.intValue = Mathf.Clamp(value, 0, 256);
				}

			lastBiomeCount = biomeCount;
			lastProbabilityStructureCount = structureCount;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var sizeProperty = serializedObject.FindProperty("atlasSize");
			var atlasSize = sizeProperty.vector2IntValue;
			atlasSize.x = 256;
			atlasSize.y = Mathf.Max(1, EditorGUILayout.IntField("Atlas Height", Mathf.Max(1, atlasSize.y)));
			sizeProperty.vector2IntValue = atlasSize;
			var structuresProperty = serializedObject.FindProperty("structures");
			EditorGUILayout.PropertyField(structuresProperty, new GUIContent("Structures"), true);
			var biomeNamesProperty = serializedObject.FindProperty("biomeNames");
			EditorGUILayout.PropertyField(biomeNamesProperty, new GUIContent("Biomes"), true);
			var biomeProbabilitiesProperty = serializedObject.FindProperty("biomeProbabilities");
			SyncBiomeProbabilities(biomeNamesProperty, structuresProperty, biomeProbabilitiesProperty);
			if (biomeNamesProperty.arraySize > 0)
			{
				selectedBioIndex = Mathf.Clamp(selectedBioIndex, 0, biomeNamesProperty.arraySize - 1);
				var biomeNames = new string[biomeNamesProperty.arraySize];
				for (int i = 0; i < biomeNames.Length; i++)
					biomeNames[i] = biomeNamesProperty.GetArrayElementAtIndex(i).stringValue;
				selectedBioIndex = EditorGUILayout.Popup("Selected Biome", selectedBioIndex, biomeNames);
			}
			var positionsProperty = serializedObject.FindProperty("structurePositions");
			if (positionsProperty.arraySize != structuresProperty.arraySize)
				positionsProperty.arraySize = structuresProperty.arraySize;

			var size = sizeProperty.vector2IntValue;
			var clampedSize = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
			if (size != clampedSize)
				sizeProperty.vector2IntValue = clampedSize;
			serializedObject.ApplyModifiedProperties();

			var biome = (MineStructureAtlas)target;
			var minimumAtlasSize = GetMinimumAtlasSize(biome);
			var currentAtlasSize = biome.AtlasSize;
			var constrainedAtlasSize = new Vector2Int(
				256,
				Mathf.Max(currentAtlasSize.y, minimumAtlasSize.y));
			if (currentAtlasSize != constrainedAtlasSize)
			{
				sizeProperty.vector2IntValue = constrainedAtlasSize;
				serializedObject.ApplyModifiedProperties();
			}
			RebuildPreview(biome);
			if (biome.AtlasSize.x <= 0 || biome.AtlasSize.y <= 0)
				return;

			if (preview == null || preview.width != biome.AtlasSize.x || preview.height != biome.AtlasSize.y)
			{
				if (preview != null) DestroyImmediate(preview);
				preview = new Texture2D(biome.AtlasSize.x, biome.AtlasSize.y, TextureFormat.RGBA32, false);
				preview.name = "MineStructureAtlas Preview";
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
			var layerBorderColor = new Color(1, 1, 1, 0.22f);
			var usedStructureColor = new Color(0f, 1f, 0f, 0.10f);
			var unusedStructureColor = new Color(1f, 0f, 0f, 0.10f);
			hoveredStructureName = null;
			var structureLabelStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				fontSize = 14,
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
				int probability = GetSelectedProbability(i, biome.Structures.Length);
				EditorGUI.DrawRect(
					structureRect,
					probability > 0 ? usedStructureColor : unusedStructureColor);
				EditorGUI.DrawRect(new Rect(structureRect.x, structureRect.y, structureRect.width, 1), structureBorderColor);
				EditorGUI.DrawRect(new Rect(structureRect.x, structureRect.yMax - 1, structureRect.width, 1), structureBorderColor);
				EditorGUI.DrawRect(new Rect(structureRect.x, structureRect.y, 1, structureRect.height), structureBorderColor);
				EditorGUI.DrawRect(new Rect(structureRect.xMax - 1, structureRect.y, 1, structureRect.height), structureBorderColor);

				var structureSerialized = new SerializedObject(biome.Structures[i]);
				var structureDimensions = structureSerialized.FindProperty("size").vector3IntValue;
				for (int layer = 1; layer < structureDimensions.z; layer++)
				{
					float layerX = rect.x +
						(float)(position.x + layer * structureDimensions.x) /
						biome.AtlasSize.x * rect.width;
					EditorGUI.DrawRect(
						new Rect(layerX, structureRect.y, 1f, structureRect.height),
						layerBorderColor);
				}

				if (structureRect.Contains(Event.current.mousePosition))
					hoveredStructureName = biome.Structures[i].name;

				var probabilityRect = new Rect(
					structureRect.x + 2,
					structureRect.y + 1,
					Mathf.Max(0, structureRect.width - 4),
					17);
				GUI.Label(
					probabilityRect,
					probability.ToString(),
					structureLabelStyle);
			}
			DrawStructureHoverTooltip(rect);
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

		private void DrawStructureHoverTooltip(Rect previewRect)
		{
			if (string.IsNullOrEmpty(hoveredStructureName))
				return;

			if (Event.current.type == EventType.MouseMove)
				Repaint();

			Vector2 mouse = Event.current.mousePosition;
			if (structureHoverTooltipStyle == null)
			{
				structureHoverTooltipStyle = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.MiddleLeft,
					padding = new RectOffset(4, 4, 2, 2),
					clipping = TextClipping.Clip
				};
				structureHoverTooltipStyle.normal.textColor = Color.white;
			}

			Vector2 size = structureHoverTooltipStyle.CalcSize(new GUIContent(hoveredStructureName));
			float tooltipWidth = Mathf.Min(size.x + 8f, previewRect.width);
			Rect tooltipRect = new Rect(
				mouse.x + 12,
				mouse.y - size.y - 18,
				tooltipWidth,
				size.y + 6);

			if (tooltipRect.xMax > previewRect.xMax)
				tooltipRect.x = mouse.x - tooltipRect.width - 12;

			if (tooltipRect.y < previewRect.y)
				tooltipRect.y = mouse.y + 18;

			if (tooltipRect.x >= mouse.x && tooltipRect.y >= mouse.y)
				tooltipRect.x += 20f;

			if (tooltipRect.yMax > previewRect.yMax)
				tooltipRect.y = mouse.y - tooltipRect.height - 18;

			tooltipRect.x = Mathf.Clamp(
				tooltipRect.x,
				previewRect.x,
				Mathf.Max(previewRect.x, previewRect.xMax - tooltipRect.width));
			tooltipRect.y = Mathf.Clamp(
				tooltipRect.y,
				previewRect.y,
				Mathf.Max(previewRect.y, previewRect.yMax - tooltipRect.height));

			EditorGUI.DrawRect(tooltipRect, new Color(0.035f, 0.035f, 0.035f, 0.96f));
			GUI.Label(tooltipRect, hoveredStructureName, structureHoverTooltipStyle);
		}

		private Vector2Int GetStructureSize(MineStructure structure)
		{
			var serialized = new SerializedObject(structure);
			var size = serialized.FindProperty("size").vector3IntValue;
			return new Vector2Int(size.x * size.z, size.y);
		}

		private Vector2Int GetMinimumAtlasSize(MineStructureAtlas biome)
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

		private void HandleStructureDragging(MineStructureAtlas biome, Rect rect)
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

					int delta = current.delta.y > 0 ? -1 : 1;
					ChangeSelectedProbability(i, biome.Structures.Length, delta);
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
					var positionsProperty = serializedObject.FindProperty("structurePositions");
					if (draggedStructure < positionsProperty.arraySize)
					{
						positionsProperty
							.GetArrayElementAtIndex(draggedStructure)
							.vector2IntValue = position;
						serializedObject.ApplyModifiedProperties();
					}
					biome.StructurePositions[draggedStructure] = position;
					EditorUtility.SetDirty(target);
					RebuildPreview(biome);
					Repaint();
				}
				current.Use();
			}

			if (current.type == EventType.MouseUp && current.button == 0)
				draggedStructure = -1;
		}

		private struct SnapCandidate
		{
			public int target;
			public int distance;

			public SnapCandidate(int target, int distance)
			{
				this.target = target;
				this.distance = distance;
			}
		}

		private void SnapStructurePosition(MineStructureAtlas biome, int structureIndex, Vector2Int size, ref Vector2Int position)
		{
			var xCandidates = new List<SnapCandidate>();
			var yCandidates = new List<SnapCandidate>();

			for (int i = 0; i < biome.Structures.Length; i++)
			{
				if (i == structureIndex || biome.Structures[i] == null) continue;

				var otherSize = GetStructureSize(biome.Structures[i]);
				var other = biome.StructurePositions[i];

				bool verticalRangesMeet = RangesMeet(
					position.y,
					position.y + size.y,
					other.y,
					other.y + otherSize.y);
				if (verticalRangesMeet)
				{
					AddCandidate(xCandidates, position.x, other.x);
					AddCandidate(xCandidates, position.x, other.x + otherSize.x);
					AddCandidate(xCandidates, position.x, other.x - size.x);
					AddCandidate(xCandidates, position.x, other.x + otherSize.x - size.x);
				}

				bool horizontalRangesMeet = RangesMeet(
					position.x,
					position.x + size.x,
					other.x,
					other.x + otherSize.x);
				if (horizontalRangesMeet)
				{
					AddCandidate(yCandidates, position.y, other.y);
					AddCandidate(yCandidates, position.y, other.y + otherSize.y);
					AddCandidate(yCandidates, position.y, other.y - size.y);
					AddCandidate(yCandidates, position.y, other.y + otherSize.y - size.y);
				}
			}

			if (xCandidates.Count > 0)
				position.x = GetClosestCandidate(xCandidates).target;
			if (yCandidates.Count > 0)
				position.y = GetClosestCandidate(yCandidates).target;
		}

		private void AddCandidate(List<SnapCandidate> candidates, int current, int target)
		{
			int distance = Mathf.Abs(current - target);
			if (distance <= snapDistance)
				candidates.Add(new SnapCandidate(target, distance));
		}

		private SnapCandidate GetClosestCandidate(List<SnapCandidate> candidates)
		{
			SnapCandidate closest = candidates[0];
			for (int i = 1; i < candidates.Count; i++)
				if (candidates[i].distance < closest.distance)
					closest = candidates[i];
			return closest;
		}

		private bool RangesMeet(int firstMin, int firstMax, int secondMin, int secondMax)
		{
			return firstMin <= secondMax + snapDistance &&
				secondMin <= firstMax + snapDistance;
		}

		private int GetSelectedProbability(int structureIndex, int structureCount)
		{
			var biomeNamesProperty = serializedObject.FindProperty("biomeNames");
			var probabilitiesProperty = serializedObject.FindProperty("biomeProbabilities");
			if (selectedBioIndex < 0 || selectedBioIndex >= biomeNamesProperty.arraySize || structureCount <= 0)
				return 0;

			int index = selectedBioIndex * structureCount + structureIndex;
			if (index < 0 || index >= probabilitiesProperty.arraySize)
				return 0;
			return Mathf.Clamp(probabilitiesProperty.GetArrayElementAtIndex(index).intValue, 0, 256);
		}

		private void ChangeSelectedProbability(int structureIndex, int structureCount, int delta)
		{
			var biomeNamesProperty = serializedObject.FindProperty("biomeNames");
			var probabilitiesProperty = serializedObject.FindProperty("biomeProbabilities");
			if (selectedBioIndex < 0 || selectedBioIndex >= biomeNamesProperty.arraySize || structureCount <= 0)
				return;

			int index = selectedBioIndex * structureCount + structureIndex;
			if (index < 0 || index >= probabilitiesProperty.arraySize)
				return;

			var probability = probabilitiesProperty.GetArrayElementAtIndex(index);
			probability.intValue = Mathf.Clamp(probability.intValue + delta, 0, 256);
			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(target);
		}

		private void RebuildPreview(MineStructureAtlas biome)
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
				var redIDs = structure.FindProperty("redIDs");
				var greenIDs = structure.FindProperty("greenIDs");
				var blueIDs = structure.FindProperty("blueIDs");
				var settings = structure.FindProperty("settings");
				var layers = structure.FindProperty("layers");
				for (int layer = 0; layer < layers.arraySize; layer++)
				{
					var range = layers.GetArrayElementAtIndex(layer).vector2IntValue;
					for (int zone = range.x; zone < range.y; zone++)
					{
						var position = positions.GetArrayElementAtIndex(zone).vector2IntValue;
						var zoneSize = sizes.GetArrayElementAtIndex(zone).vector2IntValue;
						var pixel = new Color32(
							(byte)redIDs.GetArrayElementAtIndex(zone).intValue,
							(byte)greenIDs.GetArrayElementAtIndex(zone).intValue,
							(byte)blueIDs.GetArrayElementAtIndex(zone).intValue,
							(byte)settings.GetArrayElementAtIndex(zone).intValue);
						for (int y = 0; y < zoneSize.y; y++)
							for (int x = 0; x < zoneSize.x; x++)
							{
								int atlasX = placements[i].x + position.x + layer * size.x + x;
								int atlasY = placements[i].y + position.y + y;
								if (atlasX >= 0 && atlasX < biome.AtlasSize.x && atlasY >= 0 && atlasY < biome.AtlasSize.y)
									pixels[atlasX + atlasY * biome.AtlasSize.x] = pixel;
							}
					}
				}
			}
			preview.SetPixels32(pixels);
			preview.Apply(false, false);
		}

		private void BuildAtlas(MineStructureAtlas biome)
		{
			RebuildPreview(biome);
			var folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(biome)).Replace('\\', '/');
			var atlasFolder = folder + "/BiomeAtlases";
			if (!AssetDatabase.IsValidFolder(atlasFolder))
				AssetDatabase.CreateFolder(folder, "BiomeAtlases");

			BuildStructureAtlas(biome, atlasFolder);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void BuildStructureAtlas(MineStructureAtlas biome, string atlasFolder)
		{
			int metadataRows = biome.BiomeNames.Length;
			int atlasWidth = biome.AtlasSize.x;
			int atlasHeight = biome.AtlasSize.y + metadataRows;
			var path = atlasFolder + "/" + biome.name + ".asset";
			var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			bool recreateAtlas = atlas == null ||
				atlas.width != atlasWidth ||
				atlas.height != atlasHeight ||
				atlas.isDataSRGB;

			if (recreateAtlas)
			{
				var replacement = new Texture2D(
					atlasWidth,
					atlasHeight,
					TextureFormat.RGBA32,
					false,
					true);
				replacement.name = biome.name;
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

			atlas.name = biome.name;
			atlas.filterMode = FilterMode.Point;
			atlas.wrapMode = TextureWrapMode.Clamp;

			var atlasPixels = new Color32[atlasWidth * atlasHeight];
			var previewPixels = preview.GetPixels32();
			for (int y = 0; y < biome.AtlasSize.y; y++)
				Array.Copy(
					previewPixels,
					y * atlasWidth,
					atlasPixels,
					(y + metadataRows) * atlasWidth,
					atlasWidth);

			for (int biomeIndex = 0; biomeIndex < metadataRows; biomeIndex++)
			{
				int metadataX = 0;
				for (int structureIndex = 0;
					structureIndex < biome.Structures.Length && metadataX < atlasWidth;
					structureIndex++)
				{
					var structure = biome.Structures[structureIndex];
					if (structure == null) continue;

					int probabilityIndex = biomeIndex * biome.Structures.Length + structureIndex;
					int probability = probabilityIndex < biome.BiomeProbabilities.Length
						? Mathf.Clamp(biome.BiomeProbabilities[probabilityIndex], 0, atlasWidth)
						: 0;
					if (probability == 0) continue;

					var structureData = new SerializedObject(structure);
					var structureSize = structureData.FindProperty("size").vector3IntValue;
					int rootOffsetMask = structureData.FindProperty("rootOffsetMask").intValue & 3;
					int variableHeight = structureData.FindProperty("variableHeight").intValue & 7;
					var structurePosition = biome.StructurePositions[structureIndex];

					byte packedSize = (byte)(
						((structureSize.x - 2) & 7) |
						(((structureSize.z - 2) & 7) << 3) |
						((rootOffsetMask & 1) << 6) |
						((rootOffsetMask & 2) << 6));
					byte packedHeight = (byte)(
						((structureSize.y - 3) & 31) |
						(variableHeight << 5));
					var metadata = new Color32(
						(byte)structurePosition.x,
						(byte)(structurePosition.y + metadataRows),
						packedSize,
						packedHeight);

					int writeCount = Mathf.Min(probability, atlasWidth - metadataX);
					for (int copy = 0; copy < writeCount; copy++)
						atlasPixels[biomeIndex * atlasWidth + metadataX++] = metadata;
				}
			}

			atlas.SetPixels32(atlasPixels);
			atlas.Apply(false, false);
			EditorUtility.SetDirty(atlas);
		}

		private void OnDisable()
		{
			if (preview != null) DestroyImmediate(preview);
			if (checkerTexture != null) DestroyImmediate(checkerTexture);
		}
	}

}

