
using System;
using UdonSharp;
using UnityEngine;

public class StructureDataManager : UdonSharpBehaviour
{
	[SerializeField] private Structure[] structures;
	[SerializeField] private GameObject structuresData;
	public int Length => structures.Length;
	public Structure this[int id]
	{
		get { return structures[id]; }
	}
#if UNITY_EDITOR
	public void ResetStructures()
	{
		structures = structuresData.GetComponents<Structure>();
		int count = 0;

		for (int i = 0; i < structures.Length; i++)
		{
			if (structures[i].isAnalog) continue;
			structures[count] = structures[i];
			count++;
		}

		var filteredStructures = new Structure[count];
		Array.Copy(structures, 0, filteredStructures, 0, count);

		structures = filteredStructures;
	}
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(StructureDataManager))]
public class StructureDataManagerEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		var manager = (StructureDataManager)target;
		var tooltip = "";

		for (int i = 0; i < manager.Length; i++)
		{
			var structure = manager[i];
			if (structure == null)
			{
				tooltip += "<color=red>Missing</color>\n";
			}
			else
			{
				tooltip += $"{i}  <color=green>{structure.Name}</color>\n";
				for (int j = 0; j < structure.length; j++)
				{
					var analog = structure[j];
					if (analog == null)
					{
						tooltip += $"  {j}  <color=red>Missing</color>\n";
					}
					else
					{
						tooltip += $"  {j}  <color=green>{analog.Name}</color>\n";
					}
				}
			}
		}

		tooltip = tooltip.TrimEnd('\n');

		if (GUILayout.Button(new GUIContent("Reset All Structures", tooltip)))
		{
			manager.ResetStructures();

			for (int i = 0; i < manager.Length; i++)
			{
				var structure = manager[i];
				if (structure != null)
				{
					for (int j = 0; j < structure.length; j++)
					{
						var analog = structure[j];
						if (analog != null)
						{
							analog.UpdateStartsEnds();
						}
					}
				}
			}
		}
	}
}
#endif