using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC_MINE.World;
using Random = UnityEngine.Random;

namespace VRC_MINE.Tools
{
	[CustomEditor(typeof(MineStructureEditorComponent))]
	public class MineStructureEditor : Editor
	{
		private SerializedObject serializedComponent;
		private MineStructureEditorComponent targetComponent;
		#region base
		private Vector3Int size;
		private Vector3Int root;
		#endregion
		private List<(Vector2Int pos, Vector2Int size, byte id)>[] zones;
		#region editor
		private int layer;
		private Vector2 blockListScroll;
		private MineAtlas atlas;
		private bool secondVariant;
		private Dictionary<byte, BlockTexture> blockList;
		private byte selectedBlock = 255;
		private bool zoneDrawStarted = false;
		private bool previewRotateStarted = false;
		private Vector2Int zoneDrawStartPos;
		private Texture2D preview;
		private Stage stage;
		private enum Stage
		{
			main,
			random,
			second
		}
		#endregion
		private void ApplyModifiedProperties()
		{
			List<Vector2Int> positions = new List<Vector2Int>();
			List<Vector2Int> sizes = new List<Vector2Int>();
			List<int> IDs = new List<int>();
			for (int i = 0; i < zones.Length; i++)
			{
				var layerZone = zones[i];
				for (int j = 0; j < layerZone.Count; j++)
				{
					var zone = layerZone[j];
					positions.Add(zone.pos);
					sizes.Add(zone.size);
					IDs.Add(zone.id);
				}
			}
			Vector2Int[] layers = new Vector2Int[zones.Length];
			var counter = 0;
			for (int i = 0; i < zones.Length; i++)
			{
				layers[i] = new Vector2Int(counter, counter + zones[i].Count);
				counter += zones[i].Count;
			}

			targetComponent.UpdateProperties(positions.ToArray(), sizes.ToArray(), IDs.ToArray(), layers);
			UpdatePreview();
		}

		#region init
		private void OnEnable()
		{
			Shader shader = Shader.Find("Unlit/Transparent");

			mat = new Material(shader)
			{
				hideFlags = HideFlags.HideAndDontSave,
				color = Color.white
			};
			targetComponent = (MineStructureEditorComponent)target;
			if (EditorPrefs.HasKey("MineStructureEditorAtlas"))
			{
				atlas = AssetDatabase.LoadAssetAtPath<MineAtlas>(AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString("MineStructureEditorAtlas")));
				if (atlas == null) EditorPrefs.DeleteKey("MineStructureEditorAtlas");
			}

			if (targetComponent.current != null) ReLoad();
		}
		private void ReLoad()
		{
			serializedComponent = new SerializedObject(targetComponent.current);
			serializedComponent.Update();
			#region base
			size = serializedComponent.FindProperty("size").vector3IntValue;
			root = serializedComponent.FindProperty("root").vector3IntValue;
			#endregion
			#region main
			var positions = GetVector2IntArrayProperty("positions");
			var sizes = GetVector2IntArrayProperty("sizes");
			var IDs = GetIntArrayProperty("IDs");
			var layers = GetVector2IntArrayProperty("layers");

			zones = new List<(Vector2Int pos, Vector2Int size, byte id)>[layers.Length];
			for (int i = 0; i < zones.Length; i++)
			{
				zones[i] = new List<(Vector2Int pos, Vector2Int size, byte id)>();
				for (int j = layers[i].x; j < layers[i].y; j++)
				{
					zones[i].Add((positions[j], sizes[j], (byte)IDs[j]));
				}
			}
			#endregion
			#region blockList
			var atlasProp = new SerializedObject(atlas).FindProperty("blocks");
			var prop = serializedComponent.FindProperty("blockList");
			blockList = new Dictionary<byte, BlockTexture>();
			for (int i = 0; i < prop.arraySize; i++)
			{
				var id = (byte)prop.GetArrayElementAtIndex(i).intValue;
				var bt = atlasProp.GetArrayElementAtIndex(id).objectReferenceValue as BlockTexture;
				blockList.Add(id, bt);
			}
			#endregion
			UpdatePreview();
		}
		private int lastRandomTick = 0;
		private void UpdatePreview()
		{
			preview = new Texture2D(size.x * size.z, size.y, TextureFormat.R8, false, true);
			preview.LoadRawTextureData(new byte[preview.width * preview.height]);
			if (lastRandomTick != DateTime.Now.Second)
			{
				lastRandomTick = DateTime.Now.Second;
				Repaint();
			}
			Random.InitState(DateTime.Now.Second);
			for (int i = 0; i < zones.Length; i++)
			{
				var isRandom = i / size.z == 1;
				var layerZones = zones[i];
				foreach (var zone in layerZones)
				{
					if (isRandom && Random.Range(0f, 1f) < 0.5f) continue;
					var colors = new Color[zone.size.x * zone.size.y];
					for (int j = 0; j < colors.Length; j++)
					{
						colors[j] = (Color)(new Color32(zone.id, 0, 0, 0));
					}
					preview.SetPixels(zone.pos.x + (i % size.z) * size.x, zone.pos.y, zone.size.x, zone.size.y, colors);
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

		public override void OnInspectorGUI()
		{
			var allStructures = targetComponent.GetComponents<MineStructure>();
			if (allStructures == null || allStructures.Length == 0) return;
			var names = new string[allStructures.Length + 1];
			names[0] = "None";
			var selected = targetComponent.current == null ? 0 : Array.IndexOf(allStructures, targetComponent.current) + 1;

			for (int i = 0; i < allStructures.Length; i++)
			{
				names[i + 1] = allStructures[i].structureName;
			}

			var newSelected = EditorGUILayout.Popup("Structure", selected, names);

			if (newSelected != selected)
			{
				if (newSelected == 0)
				{
					targetComponent.current = null;
				}
				else
				{
					targetComponent.current = allStructures[newSelected - 1];
					ReLoad();
				}
			}

			if (targetComponent.current == null) return;
			#region base
			var newSize = EditorGUILayout.Vector3IntField("size", size);
			if (newSize != size)
			{
				if (EditorUtility.DisplayDialog(
					"Resize Structure",
					"Resizing the structure will delete all placed data.\r\n\r\nContinue?",
					"Yes",
					"No"
				))
				{
					size = newSize;
					targetComponent.ResetProperties(size);
					ReLoad();
				}
			}
			var newRoot = EditorGUILayout.Vector3IntField("root", root);
			if (newRoot != root)
			{
				root = newRoot;
				serializedObject.FindProperty("root").vector3IntValue = root;
				serializedObject.ApplyModifiedProperties();
			}

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
			#region stage
			GUILayout.Label("stage");

			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginDisabledGroup(stage == Stage.main);
			if (GUILayout.Button("main")) stage = Stage.main;
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(stage == Stage.random);
			if (GUILayout.Button("random")) stage = Stage.random;
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(stage == Stage.second);
			if (GUILayout.Button("second")) stage = Stage.second;
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();
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

			if (blockList.ContainsKey(selectedBlock) && Event.current.type == EventType.MouseDown && Event.current.button == 0 && gridRect.Contains(Event.current.mousePosition))
			{
				zoneDrawStarted = true;
				zoneDrawStartPos = new Vector2Int(Mathf.FloorToInt((Event.current.mousePosition.x - gridRect.min.x) / gridRect.width * size.x), Mathf.FloorToInt((Event.current.mousePosition.y - gridRect.min.y) / gridRect.height * size.y));
				zoneDrawStartPos.y = size.y - zoneDrawStartPos.y - 1;
				zones[layer + (int)stage * size.x].Add((zoneDrawStartPos, new Vector2Int(1, 1), selectedBlock));
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
					zones[layer + (int)stage * size.x][zones[layer + (int)stage * size.x].Count - 1] = (Vector2Int.Min(zoneDrawStartPos, zoneDrawCurrentPos), zoneDrawSize, selectedBlock);
					Repaint();
				}
			}

			if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && zoneDrawStarted)
			{
				zoneDrawStarted = false;
				ApplyModifiedProperties();
			}

			byte delete = 255;
			{
				var layerZone = zones[layer + (int)stage * size.x];
				float gridCellSize = editorRect.height / size.y;
				for (byte i = 0; i < layerZone.Count; i++)
				{
					var zone = layerZone[i];
					var zoneRect = new Rect(gridRect.min + new Vector2(zone.pos.x, size.y - zone.pos.y - zone.size.y) * gridCellSize, (Vector2)zone.size * gridCellSize);
					var b = blockList.ContainsKey(zone.id) ? blockList[zone.id] : null;
					var color = b == null ? Color.magenta : b.previewColor;
					var fillColor = new Color(color.r, color.g, color.b, 0.2f);
					Handles.DrawSolidRectangleWithOutline(zoneRect, fillColor, color);
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && zoneRect.Contains(Event.current.mousePosition))
					{
						delete = i;
					}
				}
				if (delete < zones.Length)
				{
					zones[layer + (int)stage * size.x].RemoveAt(delete);
					ApplyModifiedProperties();
				}
			}
			#endregion
			#region block list
			float blockSize = (blockListViewportRect.width - 13) / 2;
			Rect blockListContentRect = new Rect(0, 0, blockListViewportRect.width - 13, Mathf.Max(blockListViewportRect.height + 0.1f, Mathf.Ceil((targetComponent.blockList.Length + 1f) / 2) * blockSize));
			EditorGUI.BeginDisabledGroup(blockListContentRect.height < blockListViewportRect.height + 1);
			blockListScroll = GUI.BeginScrollView(
				blockListViewportRect,
				blockListScroll,
				blockListContentRect
			);
			EditorGUI.EndDisabledGroup();

			Rect block = new Rect(0, 0, blockSize - 2, blockSize - 2);
			GUIStyle blockStyle = new GUIStyle(GUI.skin.button);
			blockStyle.alignment = TextAnchor.MiddleCenter;
			blockStyle.fontSize = Mathf.RoundToInt(blockSize / 3);
			GUIContent content = null;
			delete = 255;
			var _i = 0;
			foreach (var blockTex in blockList)
			{
				content = new GUIContent("", blockTex.Value == null ? "" : blockTex.Value.name);
				block.x = (_i % 2) * blockSize + 1;
				block.y = (_i / 2) * blockSize + 1;
				if (GUI.Button(block, content, blockStyle))
				{
					switch (Event.current.button)
					{
						case 0:
							selectedBlock = blockTex.Key;
							break;

						case 1:
							delete = blockTex.Key;
							break;
					}
				}
				if (blockTex.Key == selectedBlock) Handles.DrawSolidRectangleWithOutline(
					block,
					Color.clear,
					Color.white
				);
				_i++;
			}
			rot = Vector2.one * 0.3f;
			CacheCubeRotation();
			Rect GLViewRect = new Rect(blockListScroll.x, blockListScroll.y, blockListContentRect.width, blockListViewportRect.height);
			_i = 0;
			foreach (var blockTex in blockList)
			{
				block.x = (_i % 2) * blockSize + 1;
				block.y = (_i / 2) * blockSize + 1;
				DrawCubeInRect(GLViewRect, blockTex.Value, new Rect(block.x + 4, block.y + 4, blockSize - 8, blockSize - 8));
				_i++;
			}

			if (blockList.ContainsKey(delete) &&
				EditorUtility.DisplayDialog(
					"Delete Block",
					"Deleting this block may remove all placed structure parts that use it.\r\n\r\nContinue?",
					"Yes",
					"No")
			)
			{
				blockList.Remove(delete);
				targetComponent.blockList = new int[blockList.Count];
				for (int i = 0; i < blockList.Count; i++) targetComponent.blockList[i] = i;
			}

			block.x = (targetComponent.blockList.Length % 2) * blockSize + 1;
			block.y = (targetComponent.blockList.Length / 2) * blockSize + 1;

			content = new GUIContent(
				"+",
				atlas == null
					? "Select a MineAtlas first."
					: "Add a block."
			);

			EditorGUI.BeginDisabledGroup(atlas == null);
			if (GUI.Button(block, content, blockStyle))
			{
				MineStructureBlockCreator.Open((block) =>
				{
					if (blockList.ContainsKey(block.id)) return;
					blockList.Add(block.id, block.blockTex);
					targetComponent.blockList = new int[blockList.Count];
					_i = 0;
					foreach (var blockTex in blockList)
					{
						targetComponent.blockList[_i] = blockTex.Key;
						_i++;
					}
				});
			}
			EditorGUI.EndDisabledGroup();
			GUI.EndScrollView();
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
				Handles.DrawSolidRectangleWithOutline(new Rect(previewRect.min, new Vector2(previewRect.width, previewRect.height / (2 + previewRot.y * 2))), new Color(0, 1, 1, 0.3f), Color.clear);
				//draw black rect
				Handles.DrawSolidRectangleWithOutline(new Rect(previewRect.min + Vector2.up * previewRect.height / (2 + previewRot.y * 2), new Vector2(previewRect.width, previewRect.height - (previewRect.height / (2 + previewRot.y * 2)))), new Color(0, 0.5f, 0, 0.3f), Color.clear);
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
			Handles.DrawAAPolyLine(9f, previewRect.center, previewRect.center + (p[3] - p[0]) * 1.52f);
			Handles.DrawAAPolyLine(9f, previewRect.center, previewRect.center + (p[0] - p[4]) * 1.52f);
			Handles.DrawAAPolyLine(9f, previewRect.center, previewRect.center + (p[1] - p[0]) * 1.52f);
			Handles.color = Color.red;
			Handles.DrawAAPolyLine(5f, previewRect.center, previewRect.center + (p[3] - p[0]) * 1.5f);
			Handles.color = Color.green;
			Handles.DrawAAPolyLine(5f, previewRect.center, previewRect.center + (p[0] - p[4]) * 1.5f);
			Handles.color = Color.blue;
			Handles.DrawAAPolyLine(5f, previewRect.center, previewRect.center + (p[1] - p[0]) * 1.5f);

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

			// Если всё равно вылезает — прижимаем к границам
			rect.x = Mathf.Clamp(rect.x, 0, position.width - rect.width);
			rect.y = Mathf.Clamp(rect.y, 0, position.height - rect.height);

			GUI.Box(rect, hoveredName);
		}
	}
}