
using UdonSharp;
using UnityEditor;
using UnityEngine;

public class ItemDataManager : UdonSharpBehaviour
{
	[SerializeField] private Item[] items;
	[SerializeField] private Sprite[] icons;
	[SerializeField] private DebugConsole debugConsole;
	private string language = "";

	public override void OnLanguageChanged(string language)
	{
		if (language != "") debugConsole.Message($"Language changed to: {language}");
		this.language = language;
	}

	public int Length => items.Length;

	public string Language
	{
		get => language;
	}

	public Item this[int id]
	{
		get
		{
			if (id < 0 || id >= items.Length)
			{
				Debug.LogError($"Item ID {id} is out of range.");
				return null;
			}
			return items[id];
		}
	}

	public Sprite Icon(int id)
	{
		return icons[items[id].iconId];
	}

	public void ResetItems()
	{
		items = GetComponents<Item>();
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(ItemDataManager))]
public class ItemDataManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		var manager = (ItemDataManager)target;

		var tooltip = "";

		for (var i = 0; i < manager.Length; i++)
		{
			if (manager[i] == null)
			{
				tooltip += $"Item {i} is null\n";
			}
			else
			{
				tooltip += $"Item {i}: {manager[i]._name}\n";
			}
		}

		tooltip = tooltip.TrimEnd('\n');

		if (GUILayout.Button(new GUIContent("Reset All Items", tooltip)))
		{
			Undo.RecordObject(manager, "Reset All Items");
			manager.ResetItems();
			EditorUtility.SetDirty(manager);
		}
	}
}
#endif