
using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace VRC_MINE.World
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class MineBiome : UdonSharpBehaviour
	{
		[SerializeField] private MineStructure[] structures;
#if UNITY_EDITOR
		[SerializeField] public string biomeName = "Biome";

		public MineStructure[] _structures { get { return structures; } set { structures = value; } }
#endif
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(MineBiome))]
	public class MineBiomeEditor : Editor
	{
		private MineBiome targetComponent;
		private void OnEnable()
		{
			targetComponent = (MineBiome)target;
		}

		public override void OnInspectorGUI()
		{
			var newName = EditorGUILayout.TextField("Biome Name", targetComponent.biomeName);
			if (newName != targetComponent.biomeName)
			{
				targetComponent.biomeName = newName;
				EditorUtility.SetDirty(target);
			}
			if (GUILayout.Button("edit structure list"))
			{
				MineBiomeStructureSelector.Open(targetComponent.GetComponentsInChildren<MineStructure>(), targetComponent._structures, (inputData) =>
				{
					targetComponent._structures = inputData;
					EditorUtility.SetDirty(target);
				});
			}
		}
	}

	public class MineBiomeStructureSelector : EditorWindow
	{
		private static System.Action<MineStructure[]> onSelect;
		private static MineStructure[] allStructures;
		private static List<MineStructure> structures = new List<MineStructure>();
		private static List<int> probabilityes = new List<int>();

		public static void Open(MineStructure[] _allStructures, MineStructure[] _structures, System.Action<MineStructure[]> callback)
		{
			structures.Clear();
			probabilityes.Clear();
			onSelect = callback;
			allStructures = _allStructures;
			MineStructure lastStructure = null;
			foreach (var structure in _structures)
			{
				if (structure == lastStructure)
				{
					probabilityes[probabilityes.Count - 1]++;
				}
				else
				{
					structures.Add(structure);
					probabilityes.Add(1);
					lastStructure = structure;
				}
			}
			var window = GetWindow<MineBiomeStructureSelector>("Structure Selector");
			window.minSize = new Vector2(500, 300);
			window.ShowUtility();
		}

		private static Vector2 scroll1 = Vector2.zero;
		private static Vector2 scroll2 = Vector2.zero;

		private void OnGUI()
		{
			if (GUILayout.Button("Enter"))
			{
				var _structures = new List<MineStructure>();
				for (int i = 0; i < structures.Count; i++)
				{
					for (int j = 0; j < probabilityes[i]; j++)
					{
						_structures.Add(structures[i]);
					}
				}
				onSelect?.Invoke(_structures.ToArray());
				Close();
			}
			var btn = GUILayoutUtility.GetLastRect();
			var fullRect = new Rect(0, btn.height + 5, position.width, position.height - btn.height - 5);
			var viewPort1 = new Rect(fullRect.x, fullRect.y, fullRect.width / 2 - 30, fullRect.height);
			var count = 0;
			foreach (var structure in allStructures)
			{
				if (!structures.Contains(structure)) count++;
			}
			var contentRect1 = new Rect(0, 0, viewPort1.width - 13, Mathf.Max(count * (btn.height + 4), viewPort1.height + 0.1f));
			EditorGUI.BeginDisabledGroup(contentRect1.height < viewPort1.height + 0.2f);
			scroll1 = GUI.BeginScrollView(viewPort1, scroll1, contentRect1);
			EditorGUI.EndDisabledGroup();

			var listRect = new Rect(0, 0, contentRect1.width, btn.height + 4);
			count = 0;
			for (int i = 0; i < allStructures.Length; i++)
			{
				if (structures.Contains(allStructures[i])) continue;
				listRect.y = count * listRect.height;
				count++;
				GUI.Label(listRect, " " + allStructures[i].structureName, EditorStyles.objectField);
				var btnRect = new Rect(listRect.width - listRect.height + 1, 2 + listRect.y, listRect.height - 4, listRect.height);
				if (GUI.Button(btnRect, ">", EditorStyles.miniButton))
				{
					structures.Add(allStructures[i]);
					probabilityes.Add(1);
					GUI.FocusControl(null);
				}
			}
			GUI.EndScrollView();
			var viewPort2 = new Rect(fullRect.x + fullRect.width / 2 - 30, fullRect.y, fullRect.width / 2 + 30, fullRect.height);
			var contentRect2 = new Rect(0, 0, viewPort2.width - 13, Mathf.Max(structures.Count * (btn.height + 4), viewPort2.height + 0.1f));
			EditorGUI.BeginDisabledGroup(contentRect2.height < viewPort2.height + 0.2f);
			scroll2 = GUI.BeginScrollView(viewPort2, scroll2, contentRect2);
			EditorGUI.EndDisabledGroup();
			int destroyed = -1;
			listRect.width = contentRect2.width;
			for (int i = 0; i < structures.Count; i++)
			{
				listRect.y = i * listRect.height;
				GUI.Label(listRect, "       " + structures[i].structureName, EditorStyles.objectField);
				var btnRect = new Rect(2, 2 + listRect.y, listRect.height - 4, listRect.height);

				probabilityes[i] = Mathf.Clamp(EditorGUI.IntField(new Rect(listRect.width - 60 - 2, btnRect.y, 60, btnRect.height - 4), probabilityes[i]), 1, 500);

				if (GUI.Button(btnRect, "<", EditorStyles.miniButton))
				{
					destroyed = i;
					GUI.FocusControl(null);
				}
			}
			if (destroyed != -1)
			{
				structures.RemoveAt(destroyed);
				probabilityes.RemoveAt(destroyed);
			}
			GUI.EndScrollView();
			if (Event.current.type == EventType.MouseDown &&
	Event.current.button == 0 &&
	fullRect.Contains(Event.current.mousePosition))
			{
				GUIUtility.keyboardControl = 0;
				GUI.FocusControl(null);
			}
		}
	}
#endif
}
