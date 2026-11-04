using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRC_MINE.Tools
{
	[CustomEditor(typeof(MineStructure))]
	public class MineStructureEditor : Editor
	{
		private SerializedObject serializedComponent;
		private MineStructure targetComponent;
		#region base
		private Vector3Int size;
		private Vector3Int root;
		private int rootOffsetMask;
		private int variableHeight;
		#endregion
		private struct ZoneData
		{
			public Vector2Int pos;
			public Vector2Int size;
			public byte red;
			public byte green;
			public byte blue;
			public byte settings;

			public ZoneData(Vector2Int pos, Vector2Int size, byte red, byte green, byte blue, byte settings)
			{
				this.pos = pos;
				this.size = size;
				this.red = red;
				this.green = green;
				this.blue = blue;
				this.settings = settings;
			}
		}

		private List<ZoneData>[] zones;
		#region editor
		private int layer;
		private readonly Vector2[] blockListScroll = new Vector2[4];
		private MineAtlas atlas;
		private Dictionary<byte, BlockTexture> blockList;
		private readonly byte[] selectedBlocks = { 255, 255, 255 };
		private int appearanceProbability = 3;
		private bool appearanceXYZ;
		private bool variantXYZ;
		private bool useBlueVariant = true;
		private bool protectedPriority;
		private bool mandatoryPriority;
		private bool zoneDrawStarted = false;
		private bool previewRotateStarted = false;
		private Vector2Int zoneDrawStartPos;
		private Texture2D preview;
		private readonly Texture2D[,] probabilityPatterns = new Texture2D[2, 4];
		private bool hasStructureChanges;
		#endregion

		private byte BrushSettings
		{
			get
			{
				int value = appearanceProbability & 3;
				if (appearanceXYZ) value |= 1 << 2;
				if (variantXYZ) value |= 1 << 3;
				if (protectedPriority) value |= 1 << 4;
				if (mandatoryPriority) value |= 1 << 5;
				if (useBlueVariant) value |= 1 << 6;
				return (byte)value;
			}
		}
		private void MarkDirty()
		{
			hasStructureChanges = true;
			EditorUtility.SetDirty(targetComponent);
		}

		private void OnDisable()
		{
			if (hasStructureChanges && targetComponent != null && !EditorApplication.isCompiling)
			{
				AssetDatabase.SaveAssetIfDirty(targetComponent);
				hasStructureChanges = false;
			}

			for (int dimensions = 0; dimensions < 2; dimensions++)
				for (int probability = 0; probability < 4; probability++)
					if (probabilityPatterns[dimensions, probability] != null)
						DestroyImmediate(probabilityPatterns[dimensions, probability]);
			if (mat != null) DestroyImmediate(mat);
			if (preview != null) DestroyImmediate(preview);
		}

		private void ApplyModifiedProperties()
		{
			List<Vector2Int> positions = new List<Vector2Int>();
			List<Vector2Int> sizes = new List<Vector2Int>();
			List<int> redIDs = new List<int>();
			List<int> greenIDs = new List<int>();
			List<int> blueIDs = new List<int>();
			List<int> settings = new List<int>();
			for (int i = 0; i < zones.Length; i++)
			{
				var layerZone = zones[i];
				for (int j = 0; j < layerZone.Count; j++)
				{
					var zone = layerZone[j];
					positions.Add(zone.pos);
					sizes.Add(zone.size);
					redIDs.Add(zone.red);
					greenIDs.Add(zone.green);
					blueIDs.Add(zone.blue);
					settings.Add(zone.settings);
				}
			}
			Vector2Int[] layers = new Vector2Int[zones.Length];
			var counter = 0;
			for (int i = 0; i < zones.Length; i++)
			{
				layers[i] = new Vector2Int(counter, counter + zones[i].Count);
				counter += zones[i].Count;
			}

			targetComponent.UpdateProperties(
				positions.ToArray(),
				sizes.ToArray(),
				redIDs.ToArray(),
				greenIDs.ToArray(),
				blueIDs.ToArray(),
				settings.ToArray(),
				layers);
			MarkDirty();
			UpdatePreview();
		}

		#region init
		private void OnEnable()
		{
			CreateProbabilityPatterns();
			Shader shader = Shader.Find("Unlit/Transparent");

			mat = new Material(shader)
			{
				hideFlags = HideFlags.HideAndDontSave,
				color = Color.white
			};
			targetComponent = (MineStructure)target;
			if (EditorPrefs.HasKey("MineStructureEditorAtlas"))
			{
				atlas = AssetDatabase.LoadAssetAtPath<MineAtlas>(AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString("MineStructureEditorAtlas")));
				if (atlas == null) EditorPrefs.DeleteKey("MineStructureEditorAtlas");
			}

			ReLoad();
		}

		private void CreateProbabilityPatterns()
		{
			for (int dimensions = 0; dimensions < 2; dimensions++)
			{
				for (int probability = 0; probability < 4; probability++)
				{
					var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false, true)
					{
						name = $"Structure probability {(probability + 1) * 25}% {(dimensions == 0 ? "XZ" : "XYZ")}",
						filterMode = FilterMode.Point,
						wrapMode = TextureWrapMode.Repeat,
						hideFlags = HideFlags.HideAndDontSave
					};
					var pixels = new Color32[16 * 16];
					for (int y = 0; y < 16; y++)
					{
						for (int x = 0; x < 16; x++)
						{
							bool enabled;
							if (dimensions == 0)
							{
								int sample = ((x & 1) << 1) | ((x >> 1) & 1);
								enabled = sample <= probability;
							}
							else
							{
								enabled = probability == 3 ||
									(probability == 2 && !((x & 1) != 0 && (y & 1) != 0)) ||
									(probability == 1 && ((x + y) & 1) == 0) ||
									(probability == 0 && (x & 1) == 0 && (y & 1) == 0);
							}
							pixels[x + y * 16] = enabled
								? new Color32(255, 255, 255, 190)
								: new Color32(255, 255, 255, 0);
						}
					}
					texture.SetPixels32(pixels);
					texture.Apply(false, true);
					probabilityPatterns[dimensions, probability] = texture;
				}
			}
		}
		private void ReLoad()
		{
			serializedComponent = new SerializedObject(targetComponent);
			serializedComponent.Update();
			#region base
			size = serializedComponent.FindProperty("size").vector3IntValue;
			var rootProperty = serializedComponent.FindProperty("root");
			var rootOffsetProperty = serializedComponent.FindProperty("rootOffsetMask");
			rootOffsetMask = rootOffsetProperty.intValue & 3;
			if ((size.x & 1) != 0) rootOffsetMask &= ~1;
			if ((size.z & 1) != 0) rootOffsetMask &= ~2;
			root = CalculateRoot(size, rootOffsetMask);
			if (rootProperty.vector3IntValue != root || rootOffsetProperty.intValue != rootOffsetMask)
			{
				rootOffsetProperty.intValue = rootOffsetMask;
				rootProperty.vector3IntValue = root;
				serializedComponent.ApplyModifiedProperties();
				MarkDirty();
			}
			variableHeight = Mathf.Clamp(serializedComponent.FindProperty("variableHeight").intValue, 0, 7);
			#endregion
			#region main
			var positions = GetVector2IntArrayProperty("positions");
			var sizes = GetVector2IntArrayProperty("sizes");
			var redIDs = GetIntArrayProperty("redIDs");
			var greenIDs = GetIntArrayProperty("greenIDs");
			var blueIDs = GetIntArrayProperty("blueIDs");
			var settings = GetIntArrayProperty("settings");
			var layersProperty = serializedComponent.FindProperty("layers");
			if (layersProperty.arraySize != size.z)
			{
				layersProperty.arraySize = size.z;
				serializedComponent.ApplyModifiedProperties();
			}
			var layers = GetVector2IntArrayProperty("layers");

			zones = new List<ZoneData>[layers.Length];
			for (int i = 0; i < zones.Length; i++)
			{
				zones[i] = new List<ZoneData>();
				for (int j = layers[i].x; j < layers[i].y; j++)
				{
					zones[i].Add(new ZoneData(
						positions[j],
						sizes[j],
						(byte)redIDs[j],
						(byte)greenIDs[j],
						(byte)blueIDs[j],
						(byte)settings[j]));
				}
			}
			#endregion
			#region blockList
			var prop = serializedComponent.FindProperty("blockList");
			blockList = new Dictionary<byte, BlockTexture>();
			if (atlas != null && prop != null)
			{
				var atlasProp = new SerializedObject(atlas).FindProperty("blocks");
				for (int i = 0; i < prop.arraySize; i++)
				{
					var id = (byte)prop.GetArrayElementAtIndex(i).intValue;
					var bt = atlasProp.GetArrayElementAtIndex(id).objectReferenceValue as BlockTexture;
					blockList.Add(id, bt);
				}
			}
			#endregion
			UpdatePreview();
		}
		private int lastRandomTick = 0;

		private static uint PreviewHash(int x, int y, int z, int salt)
		{
			uint h = (uint)(x * 374761393 ^ z * 668265263 ^ salt * 1274126177);
			h ^= (uint)(y * 1446658333);
			h ^= h >> 13;
			h *= 1274126177u;
			return h ^ (h >> 16);
		}

		private void UpdatePreview()
		{
			if (preview != null) DestroyImmediate(preview);
			preview = new Texture2D(size.x * size.z, size.y, TextureFormat.R8, false, true);
			preview.LoadRawTextureData(new byte[preview.width * preview.height]);
			if (lastRandomTick != DateTime.Now.Second)
			{
				lastRandomTick = DateTime.Now.Second;
				Repaint();
			}
			for (int i = 0; i < zones.Length; i++)
			{
				var layerZones = zones[i];
				foreach (var zone in layerZones)
				{
					for (int y = 0; y < zone.size.y; y++)
					{
						for (int x = 0; x < zone.size.x; x++)
						{
							int px = zone.pos.x + x;
							int py = zone.pos.y + y;
							bool appearanceUsesY = (zone.settings & (1 << 2)) != 0;
							uint appearanceHash = PreviewHash(px, appearanceUsesY ? py : 0, i, lastRandomTick);
							int probability = (zone.settings & 3) + 1;
							if ((appearanceHash & 3u) >= probability) continue;

							bool variantUsesY = (zone.settings & (1 << 3)) != 0;
							uint variantHash = PreviewHash(px, variantUsesY ? py : 0, i, lastRandomTick + 7919);
							int variantCount = (zone.settings & (1 << 6)) != 0 ? 3 : 2;
							int variant = (int)(variantHash % (uint)variantCount);
							byte id = variant == 0 ? zone.red : variant == 1 ? zone.green : zone.blue;
							preview.SetPixel(px + i * size.x, py, (Color)new Color32(id, 0, 0, 255));
						}
					}
				}
			}
			preview.Apply();
		}
		#endregion

		#region get array property
		private Vector2Int[] GetVector2IntArrayProperty(string propertyName)
		{
			var prop = serializedComponent.FindProperty(propertyName);
			var array = new Vector2Int[prop.arraySize];
			for (int i = 0; i < prop.arraySize; i++)
			{
				array[i] = prop.GetArrayElementAtIndex(i).vector2IntValue;
			}
			return array;
		}
		private int[] GetIntArrayProperty(string propertyName)
		{
			var prop = serializedComponent.FindProperty(propertyName);
			var array = new int[prop.arraySize];
			for (int i = 0; i < prop.arraySize; i++)
			{
				array[i] = prop.GetArrayElementAtIndex(i).intValue;
			}
			return array;
		}
		#endregion
		private static Vector3Int CalculateRoot(Vector3Int structureSize, int offsetMask)
		{
			return new Vector3Int(
				(structureSize.x - 1) / 2 + ((offsetMask & 1) != 0 ? 1 : 0),
				0,
				(structureSize.z - 1) / 2 + ((offsetMask & 2) != 0 ? 1 : 0));
		}

		private void SetRootOffsetMask(int offsetMask)
		{
			rootOffsetMask = offsetMask & 3;
			if ((size.x & 1) != 0) rootOffsetMask &= ~1;
			if ((size.z & 1) != 0) rootOffsetMask &= ~2;
			root = CalculateRoot(size, rootOffsetMask);
			serializedComponent.FindProperty("rootOffsetMask").intValue = rootOffsetMask;
			serializedComponent.FindProperty("root").vector3IntValue = root;
			serializedComponent.ApplyModifiedProperties();
			MarkDirty();
			Repaint();
		}

		private bool HasPlacedData()
		{
			if (zones == null) return false;
			for (int i = 0; i < zones.Length; i++)
				if (zones[i] != null && zones[i].Count > 0) return true;
			return false;
		}

		private bool IsBlockUsed(byte id)
		{
			if (zones == null) return false;
			for (int i = 0; i < zones.Length; i++)
				if (zones[i] != null)
					for (int j = 0; j < zones[i].Count; j++)
						if (zones[i][j].red == id || zones[i][j].green == id || zones[i][j].blue == id)
							return true;
			return false;
		}

		private void SyncBlockList()
		{
			targetComponent.blockList = new int[blockList.Count];
			int i = 0;
			foreach (var entry in blockList) targetComponent.blockList[i++] = entry.Key;
			MarkDirty();
		}

		private byte DrawBlockPanel(Rect viewport, int panel, Color tint, bool library)
		{
			EditorGUI.DrawRect(viewport, tint);
			GUI.Label(new Rect(viewport.x + 4, viewport.y + 1, viewport.width - 8, 17), library ? "Blocks" : "RGB"[panel - 1].ToString(), EditorStyles.miniBoldLabel);
			Rect scrollRect = new Rect(viewport.x, viewport.y + 18, viewport.width, viewport.height - 18);
			float cell = Mathf.Max(24, (scrollRect.width - 13) / 2);
			int count = blockList.Count + (library ? 1 : 0);
			Rect contentRect = new Rect(0, 0, scrollRect.width - 13, Mathf.Max(scrollRect.height + .1f, Mathf.Ceil(count / 2f) * cell));
			blockListScroll[panel] = GUI.BeginScrollView(scrollRect, blockListScroll[panel], contentRect);
			Rect button = new Rect(0, 0, cell - 2, cell - 2);
			GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(cell / 3) };
			byte delete = 255;
			int i = 0;
			foreach (var entry in blockList)
			{
				button.x = (i % 2) * cell + 1;
				button.y = (i / 2) * cell + 1;
				if (GUI.Button(button, new GUIContent("", entry.Value == null ? "" : entry.Value.name), style))
				{
					if (library && Event.current.button == 1) delete = entry.Key;
					else if (!library && Event.current.button == 0) selectedBlocks[panel - 1] = entry.Key;
				}
				if (!library && entry.Key == selectedBlocks[panel - 1])
					Handles.DrawSolidRectangleWithOutline(button, Color.clear, Color.white);
				i++;
			}

			rot = Vector2.one * .3f;
			CacheCubeRotation();
			Rect glRect = new Rect(blockListScroll[panel].x, blockListScroll[panel].y, contentRect.width, scrollRect.height);
			i = 0;
			foreach (var entry in blockList)
			{
				button.x = (i % 2) * cell + 1;
				button.y = (i / 2) * cell + 1;
				DrawCubeInRect(glRect, entry.Value, new Rect(button.x + 4, button.y + 4, cell - 8, cell - 8));
				i++;
			}

			if (library)
			{
				button.x = (i % 2) * cell + 1;
				button.y = (i / 2) * cell + 1;
				EditorGUI.BeginDisabledGroup(atlas == null);
				if (GUI.Button(button, new GUIContent("+", atlas == null ? "Select a MineAtlas first." : "Add a block."), style))
				{
					MineStructureBlockCreator.Open(block =>
					{
						if (blockList.ContainsKey(block.id)) return;
						blockList.Add(block.id, block.blockTex);
						SyncBlockList();
					});
				}
				EditorGUI.EndDisabledGroup();
			}
			GUI.EndScrollView();
			return delete;
		}

		private void DrawZoneVisualization(Rect zoneRect, ZoneData zone, Color outlineColor)
		{
			int probability = zone.settings & 3;
			int appearanceDimensions = (zone.settings & (1 << 2)) != 0 ? 1 : 0;
			Color previousGUIColor = GUI.color;
			GUI.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, previousGUIColor.a * .55f);
			GUI.DrawTextureWithTexCoords(
				zoneRect,
				probabilityPatterns[appearanceDimensions, probability],
				new Rect(0, 0, zone.size.x, zone.size.y),
				true);
			GUI.color = previousGUIColor;

			Handles.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, .85f);
			float blockWidth = zoneRect.width / zone.size.x;
			float blockHeight = zoneRect.height / zone.size.y;
			for (int blockX = 0; blockX < zone.size.x; blockX++)
			{
				for (int division = 1; division <= 3; division++)
				{
					float x = zoneRect.xMin + (blockX + division * .25f) * blockWidth;
					Handles.DrawAAPolyLine(1f, new Vector3(x, zoneRect.yMin), new Vector3(x, zoneRect.yMax));
				}
			}
			if ((zone.settings & (1 << 3)) != 0)
				for (int blockY = 0; blockY < zone.size.y; blockY++)
					for (int division = 1; division <= 3; division++)
					{
						float y = zoneRect.yMin + (blockY + division * .25f) * blockHeight;
						Handles.DrawAAPolyLine(1f, new Vector3(zoneRect.xMin, y), new Vector3(zoneRect.xMax, y));
					}
			Handles.color = Color.white;
		}

		public override void OnInspectorGUI()
		{
			if (target == null) return;
			#region base
			var newSize = EditorGUILayout.Vector3IntField("size", size);
			newSize = new Vector3Int(
				Mathf.Clamp(newSize.x, 2, 9),
				Mathf.Clamp(newSize.y, 3, 34),
				Mathf.Clamp(newSize.z, 2, 9));
			if (newSize != size)
			{
				if (!HasPlacedData() || EditorUtility.DisplayDialog(
					"Resize Structure",
					"Resizing the structure will delete all placed data.\r\n\r\nContinue?",
					"Yes",
					"No"
				))
				{
					size = newSize;
					targetComponent.ResetProperties(size);
					MarkDirty();
					ReLoad();
				}
			}
			var newVariableHeight = EditorGUILayout.IntSlider("variable height", variableHeight, 0, 7);
			if (newVariableHeight != variableHeight)
			{
				variableHeight = newVariableHeight;
				serializedComponent.FindProperty("variableHeight").intValue = variableHeight;
				serializedComponent.ApplyModifiedProperties();
				MarkDirty();
			}
			EditorGUILayout.LabelField("root", root.ToString());
			int newRootOffsetMask = rootOffsetMask;
			EditorGUI.BeginDisabledGroup((size.x & 1) != 0);
			if (EditorGUILayout.Toggle("root offset X (+1)", (rootOffsetMask & 1) != 0))
				newRootOffsetMask |= 1;
			else
				newRootOffsetMask &= ~1;
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup((size.z & 1) != 0);
			if (EditorGUILayout.Toggle("root offset Z (+1)", (rootOffsetMask & 2) != 0))
				newRootOffsetMask |= 2;
			else
				newRootOffsetMask &= ~2;
			EditorGUI.EndDisabledGroup();
			if (newRootOffsetMask != rootOffsetMask)
				SetRootOffsetMask(newRootOffsetMask);

			var newAtlas = (MineAtlas)EditorGUILayout.ObjectField("atlas", atlas, typeof(MineAtlas), false);
			if (newAtlas != atlas)
			{
				atlas = newAtlas;
				if (atlas != null)
					EditorPrefs.SetString("MineStructureEditorAtlas", AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(atlas)));
				else
					EditorPrefs.DeleteKey("MineStructureEditorAtlas");
			}
			#endregion
			#region brush
			GUILayout.Label("brush", EditorStyles.boldLabel);
			int probabilityStep = EditorGUILayout.IntSlider("appearance probability", appearanceProbability + 1, 1, 4);
			appearanceProbability = probabilityStep - 1;
			EditorGUILayout.LabelField("", (probabilityStep * 25) + "%", GUILayout.Width(45));

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("appearance hash", GUILayout.Width(EditorGUIUtility.labelWidth));
			appearanceXYZ = GUILayout.Toolbar(appearanceXYZ ? 1 : 0, new[] { "XZ", "XYZ" }) == 1;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("variant hash", GUILayout.Width(EditorGUIUtility.labelWidth));
			variantXYZ = GUILayout.Toolbar(variantXYZ ? 1 : 0, new[] { "XZ", "XYZ" }) == 1;
			EditorGUILayout.EndHorizontal();

			useBlueVariant = EditorGUILayout.Toggle("use blue variant", useBlueVariant);
			bool newProtected = EditorGUILayout.Toggle("protected", protectedPriority);
			bool newMandatory = EditorGUILayout.Toggle("mandatory", mandatoryPriority);
			if (newProtected && !protectedPriority) newMandatory = false;
			if (newMandatory && !mandatoryPriority) newProtected = false;
			protectedPriority = newProtected;
			mandatoryPriority = newMandatory;
			#endregion

			layer = EditorGUILayout.IntSlider("layer", layer, 0, size.z - 1);
			GUILayout.Space(4);

			EditorGUILayout.BeginHorizontal();
			Rect editorRect = GUILayoutUtility.GetAspectRect((size.x * 1.5f) / size.y);
			GUILayout.Space(2);
			EditorGUILayout.EndHorizontal();

			Rect blockListViewportRect = new Rect(editorRect.min.x, editorRect.min.y, editorRect.width - editorRect.height / size.y * size.x - 5, editorRect.height);
			Rect gridRect = new Rect(editorRect.max.x - editorRect.height / size.y * size.x, editorRect.min.y, editorRect.height / size.y * size.x, editorRect.height);

			#region editor
			Handles.BeginGUI();
			Handles.DrawSolidRectangleWithOutline(
				gridRect,
				Color.clear,
				Color.white
			);

			for (int i = 1; i < size.x; i++)
			{
				for (int j = 1; j < size.y; j++)
				{
					var pos = gridRect.min + new Vector2(i, j) / size.x * gridRect.width;
					Handles.DrawLine(pos + Vector2.up * 3, pos + Vector2.down * 3);
					Handles.DrawLine(pos + Vector2.right * 3, pos + Vector2.left * 3);
				}
			}
			Handles.EndGUI();

			bool brushReady = blockList.ContainsKey(selectedBlocks[0]) &&
				blockList.ContainsKey(selectedBlocks[1]) &&
				(!useBlueVariant || blockList.ContainsKey(selectedBlocks[2]));
			if (brushReady && Event.current.type == EventType.MouseDown && Event.current.button == 0 && gridRect.Contains(Event.current.mousePosition))
			{
				zoneDrawStarted = true;
				zoneDrawStartPos = new Vector2Int(Mathf.FloorToInt((Event.current.mousePosition.x - gridRect.min.x) / gridRect.width * size.x), Mathf.FloorToInt((Event.current.mousePosition.y - gridRect.min.y) / gridRect.height * size.y));
				zoneDrawStartPos.y = size.y - zoneDrawStartPos.y - 1;
				zones[layer].Add(new ZoneData(
					zoneDrawStartPos,
					new Vector2Int(1, 1),
					selectedBlocks[0],
					selectedBlocks[1],
					useBlueVariant ? selectedBlocks[2] : selectedBlocks[1],
					BrushSettings));
				Repaint();
			}

			if (zoneDrawStarted)
			{
				if (Event.current.type == EventType.MouseDrag)
				{
					var zoneDrawCurrentPos = new Vector2Int(Mathf.FloorToInt((Event.current.mousePosition.x - gridRect.min.x) / gridRect.width * size.x), Mathf.FloorToInt((Event.current.mousePosition.y - gridRect.min.y) / gridRect.height * size.y));
					zoneDrawCurrentPos.y = size.y - zoneDrawCurrentPos.y - 1;
					zoneDrawCurrentPos = Vector2Int.Max(zoneDrawCurrentPos, Vector2Int.zero);
					zoneDrawCurrentPos = Vector2Int.Min(zoneDrawCurrentPos, (Vector2Int)size - Vector2Int.one);
					var zoneDrawSize = zoneDrawCurrentPos - zoneDrawStartPos;
					zoneDrawSize = new Vector2Int(Mathf.Abs(zoneDrawSize.x), Mathf.Abs(zoneDrawSize.y)) + Vector2Int.one;
					int editedZone = zones[layer].Count - 1;
					ZoneData zone = zones[layer][editedZone];
					zone.pos = Vector2Int.Min(zoneDrawStartPos, zoneDrawCurrentPos);
					zone.size = zoneDrawSize;
					zones[layer][editedZone] = zone;
					Repaint();
				}
			}

			if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && zoneDrawStarted)
			{
				zoneDrawStarted = false;
				ApplyModifiedProperties();
			}

			int zoneToDelete = -1;
			{
				var layerZone = zones[layer];
				float gridCellSize = editorRect.height / size.y;
				for (byte i = 0; i < layerZone.Count; i++)
				{
					var zone = layerZone[i];
					var zoneRect = new Rect(gridRect.min + new Vector2(zone.pos.x, size.y - zone.pos.y - zone.size.y) * gridCellSize, (Vector2)zone.size * gridCellSize);
					var displayRect = new Rect(
						zoneRect.x,
						zoneRect.y,
						Mathf.Max(0, zoneRect.width - 2),
						Mathf.Max(0, zoneRect.height - 2));
					var b = blockList.ContainsKey(zone.red) ? blockList[zone.red] : null;
					var color = b == null ? Color.magenta : b.previewColor;
					var fillColor = new Color(color.r, color.g, color.b, 0.2f);
					EditorGUI.DrawRect(displayRect, fillColor);
					DrawZoneVisualization(displayRect, zone, color);
					Color zoneOutline = (zone.settings & (1 << 5)) != 0
						? Color.black
						: (zone.settings & (1 << 4)) != 0 ? Color.white : color;
					Handles.DrawSolidRectangleWithOutline(displayRect, Color.clear, zoneOutline);
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && zoneRect.Contains(Event.current.mousePosition))
					{
						zoneToDelete = i;
					}
				}
				if (zoneToDelete >= 0 && zoneToDelete < layerZone.Count)
				{
					zones[layer].RemoveAt(zoneToDelete);
					ApplyModifiedProperties();
				}
			}
			#endregion
			#region block list
			float panelGap = 3;
			float panelWidth = (blockListViewportRect.width - panelGap) * .5f;
			float panelHeight = (blockListViewportRect.height - panelGap) * .5f;
			Rect[] panels =
			{
				new Rect(blockListViewportRect.x, blockListViewportRect.y, panelWidth, panelHeight),
				new Rect(blockListViewportRect.x + panelWidth + panelGap, blockListViewportRect.y, panelWidth, panelHeight),
				new Rect(blockListViewportRect.x, blockListViewportRect.y + panelHeight + panelGap, panelWidth, panelHeight),
				new Rect(blockListViewportRect.x + panelWidth + panelGap, blockListViewportRect.y + panelHeight + panelGap, panelWidth, panelHeight)
			};
			byte delete = DrawBlockPanel(panels[0], 0, new Color(.12f, .12f, .12f, .45f), true);
			DrawBlockPanel(panels[1], 1, new Color(.35f, .05f, .05f, .28f), false);
			DrawBlockPanel(panels[2], 2, new Color(.05f, .35f, .05f, .28f), false);
			DrawBlockPanel(panels[3], 3, new Color(.05f, .12f, .4f, .28f), false);
			if (blockList.ContainsKey(delete) && (!IsBlockUsed(delete) || EditorUtility.DisplayDialog(
				"Delete Block", "Deleting this block may remove all placed structure parts that use it.\r\n\r\nContinue?", "Yes", "No")))
			{
				blockList.Remove(delete);
				for (int i = 0; i < selectedBlocks.Length; i++) if (selectedBlocks[i] == delete) selectedBlocks[i] = 255;
				SyncBlockList();
			}
			#endregion
			#region preview
			GUILayout.Space(4);
			EditorGUILayout.BeginHorizontal();
			Rect previewRect = GUILayoutUtility.GetAspectRect(1);
			GUILayout.Space(2);
			EditorGUILayout.EndHorizontal();
			//draw black rect
			Handles.DrawSolidRectangleWithOutline(new Rect(previewRect.min, new Vector2(previewRect.width, previewRect.height / (2 + previewRot.y * 2))), new Color(0, 1, 1, 1), Color.clear);
			//draw black rect
			Handles.DrawSolidRectangleWithOutline(new Rect(previewRect.min + Vector2.up * previewRect.height / (2 + previewRot.y * 2), new Vector2(previewRect.width, previewRect.height - (previewRect.height / (2 + previewRot.y * 2)))), new Color(0, 0.5f, 0, 1), Color.clear);
			rot = previewRot;
			CacheCubeRotation();
			var xFor = rot.x > 3.14159265358979f ? new Vector2Int(size.x - 1, -1) : new Vector2Int(0, 1);
			var yFor = rot.y > 0 ? new Vector2Int(0, 1) : new Vector2Int(size.y - 1, -1);
			var zFor = Mathf.Abs(rot.x - 3.14159265358979f) < 3.14159265358979f / 2 ? new Vector2Int(0, 1) : new Vector2Int(size.z - 1, -1);
			for (int k = zFor.x; k < size.z && k > -1; k += zFor.y)
			{
				for (int i = xFor.x; i < size.x && i > -1; i += xFor.y)
				{
					for (int j = yFor.x; j < size.y && j > -1; j += yFor.y)
					{
						var blockId = ((Color32)preview.GetPixel(i + k * size.x, j)).r;
						if (blockId == 0) continue;
						var blockTex = blockList.ContainsKey(blockId) ? blockList[blockId] : null;
						DrawCubeInSpace(previewRect, blockTex, new Vector3Int(i, j, k));
					}
				}
			}

			if (!previewRotateStarted)
			{
				//draw black rect
				Handles.DrawSolidRectangleWithOutline(new Rect(previewRect.min, new Vector2(previewRect.width, previewRect.height / (2 + previewRot.y * 2))), new Color(0, 0.25f, 0.25f, 0.3f), Color.clear);
				//draw black rect
				Handles.DrawSolidRectangleWithOutline(new Rect(previewRect.min + Vector2.up * previewRect.height / (2 + previewRot.y * 2), new Vector2(previewRect.width, previewRect.height - (previewRect.height / (2 + previewRot.y * 2)))), new Color(0, 0.125f, 0, 0.3f), Color.clear);
				var k = layer;
				for (int i = xFor.x; i < size.x && i > -1; i += xFor.y)
				{
					for (int j = yFor.x; j < size.y && j > -1; j += yFor.y)
					{
						var blockId = ((Color32)preview.GetPixel(i + k * size.x, j)).r;
						if (blockId == 0) continue;
						var blockTex = blockList.ContainsKey(blockId) ? blockList[blockId] : null;
						DrawCubeInSpace(previewRect, blockTex, new Vector3Int(i, j, k));
					}
				}
			}
			Handles.color = Color.black;
			Vector2 axisOrigin = GetPreviewPosition(previewRect, root);
			Handles.DrawAAPolyLine(9f, axisOrigin, axisOrigin + (p[3] - p[0]) * 1.52f);
			Handles.DrawAAPolyLine(9f, axisOrigin, axisOrigin + (p[0] - p[4]) * 1.52f);
			Handles.DrawAAPolyLine(9f, axisOrigin, axisOrigin + (p[1] - p[0]) * 1.52f);
			Handles.color = Color.red;
			Handles.DrawAAPolyLine(5f, axisOrigin, axisOrigin + (p[3] - p[0]) * 1.5f);
			Handles.color = Color.green;
			Handles.DrawAAPolyLine(5f, axisOrigin, axisOrigin + (p[0] - p[4]) * 1.5f);
			Handles.color = Color.blue;
			Handles.DrawAAPolyLine(5f, axisOrigin, axisOrigin + (p[1] - p[0]) * 1.5f);

			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && previewRect.Contains(Event.current.mousePosition))
			{
				previewRotateStarted = true;
				Repaint();
			}

			if (previewRotateStarted && Event.current.type == EventType.MouseDrag)
			{
				Vector2 delta = Event.current.delta;

				previewRot.x -= 0.006f * delta.x;
				previewRot.y += 0.009f * delta.y;
				previewRot.x = (previewRot.x + (Mathf.PI * 2)) % (Mathf.PI * 2);
				previewRot.y = Mathf.Clamp(previewRot.y, -0.6f, 0.6f);

				Repaint();
			}

			if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && previewRotateStarted)
			{
				previewRotateStarted = false;
				Repaint();
			}
			#endregion
		}

		private static readonly Vector3[] cubeVertices =
		{
			new Vector3(-0.5f,  0.5f, -0.5f),
			new Vector3(-0.5f,  0.5f,  0.5f),
			new Vector3( 0.5f,  0.5f,  0.5f),
			new Vector3( 0.5f,  0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f,  0.5f),
			new Vector3( 0.5f, -0.5f,  0.5f),
			new Vector3( 0.5f, -0.5f, -0.5f)
		};

		private readonly Vector3[] rotatedCubeVertices = new Vector3[8];
		private readonly Vector2[] p = new Vector2[8];

		private Material mat;
		Vector2 previewRot = Vector2.one * 0.3f;
		Vector2 rot = Vector2.one * 0.3f;
		private void CacheCubeRotation()
		{
			float sx = Mathf.Sin(rot.x);
			float cx = Mathf.Cos(rot.x);

			float sy = -Mathf.Sin(rot.y);
			float cy = Mathf.Cos(rot.y);

			for (int i = 0; i < cubeVertices.Length; i++)
			{
				Vector3 v = cubeVertices[i];

				// Поворот вокруг Y
				float x = v.x * cx + v.z * sx;
				float z = -v.x * sx + v.z * cx;

				// Поворот вокруг X
				float y = v.y * cy - z * sy;
				z = v.y * sy + z * cy;

				rotatedCubeVertices[i] = new Vector3(x, y, z);
			}

			cubeAxisX = rotatedCubeVertices[3] - rotatedCubeVertices[0];
			cubeAxisY = rotatedCubeVertices[0] - rotatedCubeVertices[4];
			cubeAxisZ = rotatedCubeVertices[1] - rotatedCubeVertices[0];
		}

		private Vector3 cubeAxisX;
		private Vector3 cubeAxisY;
		private Vector3 cubeAxisZ;

		private void DrawCubeInSpace(Rect viewPort, BlockTexture blockTex, Vector3Int position)
		{
			if (blockTex == null)
				return;

			int maxSize = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
			float scale = Mathf.Min(viewPort.width, viewPort.height) / maxSize * 0.5f;

			Vector3 structureCenter = (Vector3)(size - Vector3Int.one) * 0.5f;
			Vector3 localPosition = (Vector3)position - structureCenter;

			Vector3 offset =
				cubeAxisX * localPosition.x +
				cubeAxisY * localPosition.y +
				cubeAxisZ * localPosition.z;

			Vector2 center = viewPort.center;

			for (int i = 0; i < p.Length; i++)
			{
				Vector3 v = rotatedCubeVertices[i] + offset;

				p[i] = new Vector2(
					center.x + v.x * scale,
					center.y - v.y * scale
				);
			}

			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Left), 5, 1, 0, 4);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Right), 7, 3, 2, 6);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Down), 5, 4, 7, 6);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Up), 0, 1, 2, 3);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Back), 4, 0, 3, 7);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Front), 6, 2, 1, 5);
		}

		private Vector2 GetPreviewPosition(Rect viewPort, Vector3Int position)
		{
			int maxSize = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
			float scale = Mathf.Min(viewPort.width, viewPort.height) / maxSize * 0.5f;
			Vector3 structureCenter = (Vector3)(size - Vector3Int.one) * 0.5f;
			Vector3 localPosition = (Vector3)position - structureCenter;
			Vector3 offset =
				cubeAxisX * localPosition.x +
				cubeAxisY * localPosition.y +
				cubeAxisZ * localPosition.z;

			return new Vector2(
				viewPort.center.x + offset.x * scale,
				viewPort.center.y - offset.y * scale);
		}

		private void DrawCubeInRect(Rect viewPort, BlockTexture blockTex, Rect rect)
		{
			if (blockTex == null)
				return;

			float scale = Mathf.Min(rect.width, rect.height) * 0.72f;
			Vector2 center = rect.center;

			for (int i = 0; i < p.Length; i++)
			{
				p[i] = new Vector2(
					center.x + rotatedCubeVertices[i].x * scale,
					center.y - rotatedCubeVertices[i].y * scale
				);
			}

			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Left), 5, 1, 0, 4);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Right), 7, 3, 2, 6);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Down), 5, 4, 7, 6);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Up), 0, 1, 2, 3);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Back), 4, 0, 3, 7);
			DrawSide(viewPort, blockTex.GetSide(BlockTexture.BlockSide.Front), 6, 2, 1, 5);
		}

		private void DrawSide(
			Rect viewPort,
			BlockTexture.SideData s,
			int a,
			int b,
			int c,
			int d)
		{
			float cross =
				(p[b].x - p[a].x) * (p[c].y - p[a].y) -
				(p[b].y - p[a].y) * (p[c].x - p[a].x);

			if (s.texture == null)
				return;

			if (cross < 0f)
				return;

			Vector2 uv0 = new Vector2(0f, 0f);
			Vector2 uv1 = new Vector2(0f, 1f);
			Vector2 uv2 = new Vector2(1f, 1f);
			Vector2 uv3 = new Vector2(1f, 0f);

			if (s.flipHorizontal)
			{
				Vector2 t = uv0;
				uv0 = uv3;
				uv3 = t;

				t = uv1;
				uv1 = uv2;
				uv2 = t;
			}

			if (s.flipVertical)
			{
				Vector2 t = uv0;
				uv0 = uv1;
				uv1 = t;

				t = uv3;
				uv3 = uv2;
				uv2 = t;
			}

			if (s.flipDiagonal)
			{
				Vector2 t = uv1;
				uv1 = uv3;
				uv3 = t;
			}

			mat.mainTexture = s.texture;

			if (!mat.SetPass(0))
				return;

			//GL.LoadPixelMatrix();
			GL.Begin(GL.TRIANGLES);

			DrawTriangle(
				viewPort,
				new V(p[a], uv0),
				new V(p[b], uv1),
				new V(p[c], uv2));

			DrawTriangle(
				viewPort,
				new V(p[c], uv2),
				new V(p[d], uv3),
				new V(p[a], uv0));

			GL.End();
		}
		struct V
		{
			public Vector2 p;
			public Vector2 uv;

			public V(Vector2 p, Vector2 uv)
			{
				this.p = p;
				this.uv = uv;
			}
		}

		void DrawTriangle(Rect clip, V v0, V v1, V v2)
		{
			V[] inV = { v0, v1, v2, new V(), new V(), new V() };
			V[] outV = new V[6];

			int count = 3;

			for (int pass = 0; pass < 2; pass++)
			{
				float y = pass == 0 ? clip.yMin : clip.yMax;
				bool bottom = pass == 0;

				int outCount = 0;

				for (int i = 0; i < count; i++)
				{
					V a = inV[i];
					V b = inV[(i + 1) % count];

					bool ina = bottom ? a.p.y >= y : a.p.y <= y;
					bool inb = bottom ? b.p.y >= y : b.p.y <= y;

					if (ina && inb)
					{
						outV[outCount++] = b;
					}
					else if (ina != inb)
					{
						float t = (y - a.p.y) / (b.p.y - a.p.y);

						V c = new V(
							Vector2.LerpUnclamped(a.p, b.p, t),
							Vector2.LerpUnclamped(a.uv, b.uv, t));

						c.p.y = y;

						outV[outCount++] = c;

						if (inb)
							outV[outCount++] = b;
					}
				}

				if (outCount < 3)
					return;

				count = outCount;

				var tmp = inV;
				inV = outV;
				outV = tmp;
			}

			for (int i = 1; i < count - 1; i++)
			{
				GL.TexCoord2(inV[0].uv.x, inV[0].uv.y);
				GL.Vertex3(inV[0].p.x, inV[0].p.y, 0);

				GL.TexCoord2(inV[i].uv.x, inV[i].uv.y);
				GL.Vertex3(inV[i].p.x, inV[i].p.y, 0);

				GL.TexCoord2(inV[i + 1].uv.x, inV[i + 1].uv.y);
				GL.Vertex3(inV[i + 1].p.x, inV[i + 1].p.y, 0);
			}
		}
	}

	public class MineStructureBlockCreator : EditorWindow
	{
		private static System.Action<(byte id, BlockTexture blockTex)> onSelect;
		private MineAtlas atlas;

		private string hoveredName = "";
		private GUIStyle hoverTooltipStyle;

		public static void Open(System.Action<(byte id, BlockTexture blockTex)> callback)
		{
			onSelect = callback;

			var window = GetWindow<MineStructureBlockCreator>("Block Picker");
			window.minSize = new Vector2(300, 400);

			window.LoadAtlas();
			window.ShowUtility();
		}

		private void LoadAtlas()
		{
			mat = new Material(Shader.Find("Unlit/Transparent"));
			atlas = AssetDatabase.LoadAssetAtPath<MineAtlas>(AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString("MineStructureEditorAtlas")));
		}

		private readonly Vector3[] rotatedVertices = new Vector3[8];

		private void OnGUI()
		{
			if (atlas == null)
			{
				Close();
				return;
			}

			int size = 32;
			int columns = Mathf.Max(1, (int)(position.width - 3) / (size + 3));

			Event e = Event.current;
			hoveredName = "";

			CacheCubeRotation();

			//get "blocks" property from atlas
			var blocks = new SerializedObject(atlas).FindProperty("blocks");
			for (byte i = 0; i < blocks.arraySize; i++)
			{
				if (i % columns == 0)
					EditorGUILayout.BeginHorizontal();

				Rect r = GUILayoutUtility.GetRect(size, size);

				var block = blocks.GetArrayElementAtIndex(i).objectReferenceValue as BlockTexture;



				if (GUI.Button(r, ""))
				{
					onSelect?.Invoke((i, block));
					Close();
				}

				if (block != null)
				{
					DrawCube(block, r);
					if (r.Contains(e.mousePosition))
					{
						hoveredName = block.name;
					}
				}

				bool isLastInRow = (i % columns == columns - 1);
				bool isLastItem = (i == blocks.arraySize - 1);

				if (isLastInRow || isLastItem)
					EditorGUILayout.EndHorizontal();
			}

			DrawHoverTooltip();
			Repaint();
		}

		private static readonly Vector3[] cubeVertices =
		{
			new Vector3(-0.5f,  0.5f, -0.5f),
			new Vector3(-0.5f,  0.5f,  0.5f),
			new Vector3( 0.5f,  0.5f,  0.5f),
			new Vector3( 0.5f,  0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f,  0.5f),
			new Vector3( 0.5f, -0.5f,  0.5f),
			new Vector3( 0.5f, -0.5f, -0.5f)
		};

		private readonly Vector3[] rotatedCubeVertices = new Vector3[8];
		private readonly Vector2[] p = new Vector2[8];

		private Material mat;
		Vector2 rot = Vector2.up * 0.3f;
		private void CacheCubeRotation()
		{
			rot.x += Time.deltaTime / 3;

			float sx = Mathf.Sin(rot.x);
			float cx = Mathf.Cos(rot.x);

			float sy = -Mathf.Sin(rot.y);
			float cy = Mathf.Cos(rot.y);

			for (int i = 0; i < cubeVertices.Length; i++)
			{
				Vector3 v = cubeVertices[i];

				// Поворот вокруг Y
				float x = v.x * cx + v.z * sx;
				float z = -v.x * sx + v.z * cx;

				// Поворот вокруг X
				float y = v.y * cy - z * sy;
				z = v.y * sy + z * cy;

				rotatedCubeVertices[i] = new Vector3(x, y, z);
			}
		}

		private void DrawCube(BlockTexture blockTex, Rect rect)
		{
			if (blockTex == null)
				return;

			if (mat == null)
			{
				Shader shader = Shader.Find("Unlit/Transparent");

				if (shader == null)
					return;

				mat = new Material(shader)
				{
					hideFlags = HideFlags.HideAndDontSave,
					color = Color.white
				};
			}

			float scale = Mathf.Min(rect.width, rect.height) * 0.72f;
			Vector2 center = rect.center;

			for (int i = 0; i < p.Length; i++)
			{
				p[i] = new Vector2(
					center.x + rotatedCubeVertices[i].x * scale,
					center.y - rotatedCubeVertices[i].y * scale
				);
			}

			DrawSide(blockTex.GetSide(BlockTexture.BlockSide.Left), 5, 1, 0, 4);
			DrawSide(blockTex.GetSide(BlockTexture.BlockSide.Right), 7, 3, 2, 6);
			DrawSide(blockTex.GetSide(BlockTexture.BlockSide.Down), 5, 4, 7, 6);
			DrawSide(blockTex.GetSide(BlockTexture.BlockSide.Up), 0, 1, 2, 3);
			DrawSide(blockTex.GetSide(BlockTexture.BlockSide.Back), 4, 0, 3, 7);
			DrawSide(blockTex.GetSide(BlockTexture.BlockSide.Front), 6, 2, 1, 5);
		}

		private void DrawSide(
			BlockTexture.SideData s,
			int a,
			int b,
			int c,
			int d)
		{
			float cross =
				(p[b].x - p[a].x) * (p[c].y - p[a].y) -
				(p[b].y - p[a].y) * (p[c].x - p[a].x);

			if (s.texture == null)
				return;

			if (cross < 0f)
				return;

			Vector2 uv0 = new Vector2(0f, 0f);
			Vector2 uv1 = new Vector2(0f, 1f);
			Vector2 uv2 = new Vector2(1f, 1f);
			Vector2 uv3 = new Vector2(1f, 0f);

			if (s.flipHorizontal)
			{
				Vector2 t = uv0;
				uv0 = uv3;
				uv3 = t;

				t = uv1;
				uv1 = uv2;
				uv2 = t;
			}

			if (s.flipVertical)
			{
				Vector2 t = uv0;
				uv0 = uv1;
				uv1 = t;

				t = uv3;
				uv3 = uv2;
				uv2 = t;
			}

			if (s.flipDiagonal)
			{
				Vector2 t = uv1;
				uv1 = uv3;
				uv3 = t;
			}

			mat.mainTexture = s.texture;

			if (!mat.SetPass(0))
				return;

			GL.LoadPixelMatrix();
			GL.Begin(GL.TRIANGLES);

			GL.TexCoord2(uv0.x, uv0.y);
			GL.Vertex3(p[a].x, p[a].y, 0f);

			GL.TexCoord2(uv1.x, uv1.y);
			GL.Vertex3(p[b].x, p[b].y, 0f);

			GL.TexCoord2(uv2.x, uv2.y);
			GL.Vertex3(p[c].x, p[c].y, 0f);

			GL.TexCoord2(uv2.x, uv2.y);
			GL.Vertex3(p[c].x, p[c].y, 0f);

			GL.TexCoord2(uv3.x, uv3.y);
			GL.Vertex3(p[d].x, p[d].y, 0f);

			GL.TexCoord2(uv0.x, uv0.y);
			GL.Vertex3(p[a].x, p[a].y, 0f);

			GL.End();
		}

		private void DrawHoverTooltip()
		{
			if (string.IsNullOrEmpty(hoveredName))
				return;

			Vector2 mouse = Event.current.mousePosition;
			Vector2 size = GUI.skin.box.CalcSize(new GUIContent(hoveredName));

			Rect rect = new Rect(
				mouse.x + 12,
				mouse.y - size.y - 18,
				size.x + 8,
				size.y + 6
			);

			// Если не помещается справа — показываем слева
			if (rect.xMax > position.width)
				rect.x = mouse.x - rect.width - 12;

			// Если не помещается сверху — показываем снизу
			if (rect.y < 0)
				rect.y = mouse.y + 18;

			// Если подсказка оказалась справа и снизу от курсора,
			// дополнительно отводим её вправо, чтобы мышь не закрывала текст.
			if (rect.x >= mouse.x && rect.y >= mouse.y)
				rect.x += 20f;

			// Если всё равно вылезает — прижимаем к границам
			rect.x = Mathf.Clamp(rect.x, 0, position.width - rect.width);
			rect.y = Mathf.Clamp(rect.y, 0, position.height - rect.height);

			if (hoverTooltipStyle == null)
			{
				hoverTooltipStyle = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.MiddleLeft,
					padding = new RectOffset(4, 4, 2, 2)
				};
				hoverTooltipStyle.normal.textColor = Color.white;
			}

			EditorGUI.DrawRect(rect, new Color(0.035f, 0.035f, 0.035f, 0.96f));
			GUI.Label(rect, hoveredName, hoverTooltipStyle);
		}
	}
}
