using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(CustomRenderTexture), true)]
public class CompactCustomRenderTextureEditor : Editor
{
	private const int zonesPerPage = 16;

	private int updateZonePage;
	private Editor unityCustomRenderTextureEditor;
	private ReorderableList unityUpdateZoneList;
	private ReorderableList savedZoneList;
	private ReorderableList.HeaderCallbackDelegate savedHeaderCallback;
	private ReorderableList.ElementCallbackDelegate savedElementCallback;
	private ReorderableList.ElementHeightCallbackDelegate savedElementHeightCallback;
	private ReorderableList.AddCallbackDelegate savedAddCallback;
	private ReorderableList.AddDropdownCallbackDelegate savedAddDropdownCallback;
	private float savedHeaderHeight;
	private bool savedDraggable;
	private bool hasExplicitZoneSelection;

	private void CreateUnityEditor()
	{
		if (unityCustomRenderTextureEditor != null)
			return;

		Type editorType = typeof(Editor).Assembly.GetType("UnityEditor.CustomRenderTextureEditor");
		if (editorType == null)
			return;

		try
		{
			unityCustomRenderTextureEditor = Editor.CreateEditor(target, editorType);
		}
		catch
		{
			unityCustomRenderTextureEditor = null;
		}
	}

	private ReorderableList GetUnityUpdateZoneList()
	{
		if (unityCustomRenderTextureEditor == null)
			return null;

		FieldInfo listField = unityCustomRenderTextureEditor.GetType().GetField(
			"m_RectList",
			BindingFlags.Instance | BindingFlags.NonPublic);
		return listField == null
			? null
			: listField.GetValue(unityCustomRenderTextureEditor) as ReorderableList;
	}

	private SerializedProperty GetUnityUpdateZones()
	{
		if (unityCustomRenderTextureEditor == null)
			return null;

		return unityCustomRenderTextureEditor.serializedObject.FindProperty("m_UpdateZones");
	}

	private void SaveOriginalZoneCallbacks(ReorderableList list)
	{
		if (savedZoneList == list)
			return;

		savedZoneList = list;
		hasExplicitZoneSelection = false;
		savedHeaderCallback = list.drawHeaderCallback;
		savedElementCallback = list.drawElementCallback;
		savedElementHeightCallback = list.elementHeightCallback;
		savedAddCallback = list.onAddCallback;
		savedAddDropdownCallback = list.onAddDropdownCallback;
		savedHeaderHeight = list.headerHeight;
		savedDraggable = list.draggable;
	}

	private void RestoreOriginalZoneCallbacks()
	{
		if (savedZoneList == null)
			return;

		savedZoneList.drawHeaderCallback = savedHeaderCallback;
		savedZoneList.drawElementCallback = savedElementCallback;
		savedZoneList.elementHeightCallback = savedElementHeightCallback;
		savedZoneList.onAddCallback = savedAddCallback;
		savedZoneList.onAddDropdownCallback = savedAddDropdownCallback;
		savedZoneList.headerHeight = savedHeaderHeight;
		savedZoneList.draggable = savedDraggable;
		savedZoneList = null;
	}

	private int GetPageCount(SerializedProperty updateZones)
	{
		return Mathf.Max(1, Mathf.CeilToInt((float)updateZones.arraySize / zonesPerPage));
	}

	private bool IsZoneVisible(int zoneIndex, SerializedProperty updateZones)
	{
		int pageCount = GetPageCount(updateZones);
		updateZonePage = Mathf.Clamp(updateZonePage, 0, pageCount - 1);

		int firstZone = updateZonePage * zonesPerPage;
		int lastZone = Mathf.Min(firstZone + zonesPerPage, updateZones.arraySize);
		return zoneIndex >= firstZone && zoneIndex < lastZone;
	}

	private float GetZoneTabWidth()
	{
		return Mathf.Max(
			56,
			EditorStyles.toolbarButton.CalcSize(new GUIContent("000–999")).x + 8);
	}

	private int GetTabsPerRow(float width)
	{
		return Mathf.Max(1, Mathf.FloorToInt(width / GetZoneTabWidth()));
	}

	private int GetHeaderRowCount(float width, int pageCount)
	{
		int tabsPerRow = GetTabsPerRow(width);
		return Mathf.Max(1, Mathf.CeilToInt((float)pageCount / tabsPerRow));
	}

	private void DrawCustomZoneHeader(Rect rect)
	{
		SerializedProperty updateZones = GetUnityUpdateZones();
		if (updateZones == null)
			return;

		int pageCount = GetPageCount(updateZones);
		int tabsPerRow = GetTabsPerRow(rect.width);
		float lineHeight = EditorGUIUtility.singleLineHeight;
		float tabWidth = GetZoneTabWidth();

		EditorGUI.LabelField(
			new Rect(rect.x, rect.y, rect.width, lineHeight),
			"Update Zones (" + updateZones.arraySize + ")",
			EditorStyles.boldLabel);

		for (int page = 0; page < pageCount; page++)
		{
			int row = page / tabsPerRow;
			int column = page % tabsPerRow;
			int firstZone = page * zonesPerPage;
			int lastZone = Mathf.Min(firstZone + zonesPerPage, updateZones.arraySize) - 1;
			Rect tabRect = new Rect(
				rect.x + column * tabWidth,
				rect.y + lineHeight + row * (lineHeight + 2),
				tabWidth - 2,
				lineHeight);
			bool selected = updateZonePage == page;
			bool pressed = GUI.Toggle(
				tabRect,
				selected,
				updateZones.arraySize == 0 ? "Empty" : firstZone + "–" + lastZone,
				EditorStyles.toolbarButton);

			if (pressed && !selected)
				updateZonePage = page;
		}
	}

	private float GetCustomZoneHeaderHeight(float width, int pageCount)
	{
		int rows = GetHeaderRowCount(width, pageCount);
		float lineHeight = EditorGUIUtility.singleLineHeight;
		return lineHeight + rows * (lineHeight + 2) + 4;
	}

	private static void DrawZoneProperty(
		Rect rect,
		SerializedProperty zone,
		string propertyName)
	{
		SerializedProperty property = zone.FindPropertyRelative(propertyName);
		if (property != null)
			EditorGUI.PropertyField(rect, property, new GUIContent(property.displayName), true);
	}

	private void DrawCustomZoneElement(
		Rect rect,
		int index,
		bool active,
		bool focused)
	{
		SerializedProperty updateZones = GetUnityUpdateZones();
		if (updateZones == null)
			return;
		if (active || focused)
			hasExplicitZoneSelection = true;

		if (!IsZoneVisible(index, updateZones))
		{
			// ReorderableList still draws its drag handle for zero-height hidden
			// elements. Cover that tiny leftover handle without disabling dragging.
			if (Event.current.type == EventType.Repaint)
			{
				Color background = EditorGUIUtility.isProSkin
					? new Color(0.21f, 0.21f, 0.21f, 1f)
					: new Color(0.76f, 0.76f, 0.76f, 1f);
				EditorGUI.DrawRect(
					new Rect(rect.x - 20f, rect.y, rect.width + 20f, 16f),
					background);
			}
			return;
		}

		SerializedProperty zone = updateZones.GetArrayElementAtIndex(index);
		Rect boxRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
		GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

		float lineHeight = EditorGUIUtility.singleLineHeight;
		float y = rect.y + 5;
		EditorGUI.LabelField(
			new Rect(rect.x + 8, y, rect.width - 16, lineHeight),
			"Zone " + index,
			EditorStyles.boldLabel);
		y += lineHeight + 2;

		DrawZoneProperty(new Rect(rect.x + 8, y, rect.width - 16, lineHeight), zone, "updateZoneCenter");
		y += lineHeight;
		DrawZoneProperty(new Rect(rect.x + 8, y, rect.width - 16, lineHeight), zone, "updateZoneSize");
		y += lineHeight;
		DrawZoneProperty(new Rect(rect.x + 8, y, rect.width - 16, lineHeight), zone, "rotation");
		y += lineHeight;
		DrawZoneProperty(new Rect(rect.x + 8, y, rect.width - 16, lineHeight), zone, "passIndex");
		y += lineHeight;
		DrawZoneProperty(new Rect(rect.x + 8, y, rect.width - 16, lineHeight), zone, "needSwap");
	}

	private float GetCustomZoneElementHeight(int index)
	{
		SerializedProperty updateZones = GetUnityUpdateZones();
		if (updateZones == null || !IsZoneVisible(index, updateZones))
			return 0;

		return EditorGUIUtility.singleLineHeight * 6 + 12;
	}

	private static int ReadInt(SerializedObject serializedObject, string propertyName, int fallback)
	{
		SerializedProperty property = serializedObject.FindProperty(propertyName);
		return property == null ? fallback : property.intValue;
	}

	private static Vector3 GetTextureSize(SerializedObject serializedObject)
	{
		int width = Mathf.Max(1, ReadInt(serializedObject, "m_Width", 1));
		int height = Mathf.Max(1, ReadInt(serializedObject, "m_Height", 1));
		int depth = Mathf.Max(1, ReadInt(serializedObject, "m_VolumeDepth", 1));

		return new Vector3(width, height, depth);
	}

	private static void SetZoneVector3(
		SerializedProperty zone,
		string propertyName,
		Vector3 value)
	{
		SerializedProperty property = zone.FindPropertyRelative(propertyName);
		if (property != null && property.propertyType == SerializedPropertyType.Vector3)
			property.vector3Value = value;
	}

	private static void SetZoneFloat(
		SerializedProperty zone,
		string propertyName,
		float value)
	{
		SerializedProperty property = zone.FindPropertyRelative(propertyName);
		if (property != null && property.propertyType == SerializedPropertyType.Float)
			property.floatValue = value;
	}

	private static void CopyZoneValue(
		SerializedProperty sourceZone,
		SerializedProperty destinationZone,
		string propertyName)
	{
		SerializedProperty source = sourceZone.FindPropertyRelative(propertyName);
		SerializedProperty destination = destinationZone.FindPropertyRelative(propertyName);
		if (source == null || destination == null)
			return;

		switch (source.propertyType)
		{
			case SerializedPropertyType.Vector3:
				destination.vector3Value = source.vector3Value;
				break;
			case SerializedPropertyType.Float:
				destination.floatValue = source.floatValue;
				break;
			case SerializedPropertyType.Integer:
				destination.intValue = source.intValue;
				break;
			case SerializedPropertyType.Boolean:
				destination.boolValue = source.boolValue;
				break;
		}
	}

	private static void CopyZoneValues(
		SerializedProperty sourceZone,
		SerializedProperty destinationZone)
	{
		CopyZoneValue(sourceZone, destinationZone, "updateZoneCenter");
		CopyZoneValue(sourceZone, destinationZone, "updateZoneSize");
		CopyZoneValue(sourceZone, destinationZone, "rotation");
		CopyZoneValue(sourceZone, destinationZone, "passIndex");
		CopyZoneValue(sourceZone, destinationZone, "needSwap");
	}

	private void AddEmptyZone(ReorderableList list)
	{
		if (unityCustomRenderTextureEditor == null)
			return;

		SerializedObject serializedObject = unityCustomRenderTextureEditor.serializedObject;
		serializedObject.Update();

		if (savedAddCallback != null)
		{
			savedAddCallback(list);
		}
		else
		{
			SerializedProperty updateZones = serializedObject.FindProperty("m_UpdateZones");
			if (updateZones != null && updateZones.isArray)
			{
				int newIndex = updateZones.arraySize;
				updateZones.InsertArrayElementAtIndex(newIndex);
				list.index = newIndex;
			}
		}

		serializedObject.ApplyModifiedProperties();
		SerializedProperty zonesAfterAdd = serializedObject.FindProperty("m_UpdateZones");
		if (zonesAfterAdd != null && zonesAfterAdd.isArray && list.index >= 0)
		{
			hasExplicitZoneSelection = true;
			updateZonePage = list.index / zonesPerPage;
		}
	}

	private void DuplicateSelectedZone(ReorderableList list)
	{
		if (unityCustomRenderTextureEditor == null)
			return;

		SerializedObject serializedObject = unityCustomRenderTextureEditor.serializedObject;
		serializedObject.Update();
		SerializedProperty updateZones = serializedObject.FindProperty("m_UpdateZones");
		int sourceIndex = list.index;
		if (updateZones == null || !updateZones.isArray ||
			sourceIndex < 0 || sourceIndex >= updateZones.arraySize)
		{
			serializedObject.ApplyModifiedProperties();
			return;
		}

		int newIndex = sourceIndex + 1;
		SerializedProperty sourceZone = updateZones.GetArrayElementAtIndex(sourceIndex);
		updateZones.InsertArrayElementAtIndex(newIndex);
		SerializedProperty newZone = updateZones.GetArrayElementAtIndex(newIndex);
		CopyZoneValues(sourceZone, newZone);
		list.index = newIndex;
		updateZonePage = newIndex / zonesPerPage;
		serializedObject.ApplyModifiedProperties();
	}

	private void SetFullTextureZone(SerializedProperty zone, SerializedObject serializedObject)
	{
		SerializedProperty updateZoneSpace = serializedObject.FindProperty("m_UpdateZoneSpace");
		bool normalized = updateZoneSpace == null || updateZoneSpace.enumValueIndex == 0;
		Vector3 textureSize = GetTextureSize(serializedObject);

		SetZoneVector3(
			zone,
			"updateZoneCenter",
			normalized ? new Vector3(0.5f, 0.5f, 0.5f) : textureSize * 0.5f);
		SetZoneVector3(
			zone,
			"updateZoneSize",
			normalized ? Vector3.one : textureSize);
		SetZoneFloat(zone, "rotation", 0);
	}

	private void AddFullTextureZone(ReorderableList list)
	{
		AddEmptyZone(list);
		if (unityCustomRenderTextureEditor == null || list.index < 0)
			return;

		SerializedObject serializedObject = unityCustomRenderTextureEditor.serializedObject;
		serializedObject.Update();
		SerializedProperty updateZones = serializedObject.FindProperty("m_UpdateZones");
		if (updateZones != null && updateZones.isArray && list.index < updateZones.arraySize)
			SetFullTextureZone(updateZones.GetArrayElementAtIndex(list.index), serializedObject);
		serializedObject.ApplyModifiedProperties();
	}

	private void ShowAddZoneMenu(Rect buttonRect, ReorderableList list)
	{
		var menu = new GenericMenu();
		bool canDuplicate = hasExplicitZoneSelection && list.index >= 0 &&
			GetUnityUpdateZones() != null &&
			list.index < GetUnityUpdateZones().arraySize;

		if (canDuplicate)
			menu.AddItem(
				new GUIContent("Duplicate Selected Zone"),
				false,
				() => DuplicateSelectedZone(list));
		else
			menu.AddDisabledItem(new GUIContent("Duplicate Selected Zone"));

		menu.AddItem(
			new GUIContent("Add Empty Zone"),
			false,
			() => AddEmptyZone(list));
		menu.AddItem(
			new GUIContent("Add Full Texture Zone"),
			false,
			() => AddFullTextureZone(list));
		menu.DropDown(buttonRect);
	}

	private void ConfigureCustomZoneList()
	{
		unityUpdateZoneList = GetUnityUpdateZoneList();
		if (unityUpdateZoneList == null)
			return;

		SaveOriginalZoneCallbacks(unityUpdateZoneList);
		SerializedProperty updateZones = GetUnityUpdateZones();
		int pageCount = updateZones == null ? 1 : GetPageCount(updateZones);
		float width = Mathf.Max(1, EditorGUIUtility.currentViewWidth - 32);

		unityUpdateZoneList.drawHeaderCallback = DrawCustomZoneHeader;
		unityUpdateZoneList.drawElementCallback = DrawCustomZoneElement;
		unityUpdateZoneList.elementHeightCallback = GetCustomZoneElementHeight;
		unityUpdateZoneList.onAddCallback = null;
		unityUpdateZoneList.onAddDropdownCallback = ShowAddZoneMenu;
		unityUpdateZoneList.headerHeight = GetCustomZoneHeaderHeight(width, pageCount);
	}

	private void DrawUnityInspector()
	{
		CreateUnityEditor();
		if (unityCustomRenderTextureEditor == null)
		{
			DrawDefaultInspector();
			return;
		}

		ConfigureCustomZoneList();
		try
		{
			unityCustomRenderTextureEditor.OnInspectorGUI();
		}
		finally
		{
			RestoreOriginalZoneCallbacks();
			unityUpdateZoneList = null;
		}
	}

	private void OnEnable()
	{
		CreateUnityEditor();
	}

	private void OnDisable()
	{
		RestoreOriginalZoneCallbacks();
		if (unityCustomRenderTextureEditor != null)
			DestroyImmediate(unityCustomRenderTextureEditor);
		unityCustomRenderTextureEditor = null;
	}

	public override void OnInspectorGUI()
	{
		DrawUnityInspector();
	}

	// Keep Unity's native Custom Render Texture preview while replacing only
	// the inspector presentation of update zones.
	public override bool HasPreviewGUI()
	{
		CreateUnityEditor();
		return unityCustomRenderTextureEditor != null &&
			unityCustomRenderTextureEditor.HasPreviewGUI();
	}

	public override void OnPreviewSettings()
	{
		CreateUnityEditor();
		if (unityCustomRenderTextureEditor != null)
			unityCustomRenderTextureEditor.OnPreviewSettings();
	}

	public override void OnPreviewGUI(Rect rect, GUIStyle background)
	{
		CreateUnityEditor();
		if (unityCustomRenderTextureEditor != null)
			unityCustomRenderTextureEditor.OnPreviewGUI(rect, background);
	}
}
