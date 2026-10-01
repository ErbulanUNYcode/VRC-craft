#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshMaker : MonoBehaviour
{
	[NonSerialized]
	public MeshFilter meshFilter;

	[ContextMenu("Create base")]
	private void CreateBase()
	{
		var meshF = GetComponent<MeshFilter>();
		var mesh = new Mesh();
		var points = new Vector3[3 + 6 + 12 + 24 * 10 + 25 * 2 + 12 + 6 + 3];
		var uv = new Vector2[points.Length];
		for (int i = 0; i < uv.Length; i++) uv[i] = Vector2.right * 2;
		var id = 0;
		for (int i = 0; i < 25; i++)
		{
			var a = (float)Math.PI * 2 * i / 24;
			uv[id] = new Vector2((float)i / 24, 0);
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), -0.1f);
			uv[id] = new Vector2((float)i / 24, 1);
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), -0.2f);
		}
		for (int i = 0; i < 3; i++)
		{
			float a = (float)Math.PI * 2 * i / 3;
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.1f);
		}
		for (int i = 0; i < 6; i++)
		{
			float a = (float)Math.PI * 2 * i / 6;
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.2f);
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.3f);
		}
		for (int i = 0; i < 12; i++)
		{
			float a = (float)Math.PI * 2 * i / 12;
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.4f);
			points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.5f);
		}
		for (int i = 0; i < 24; i++)
		{
			var a = (float)Math.PI * 2 * i / 24;
			for (int j = 0; j < 10; j++)
			{
				points[id++] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.6f + j * 0.1f);
			}
		}

		mesh.vertices = points;
		mesh.uv = uv;
		mesh.name = meshF.name;
		meshF.sharedMesh = mesh;
		//save new mesh in assets
		AssetDatabase.CreateAsset(mesh, "Assets/" + mesh.name + ".asset");
	}
}

[CustomEditor(typeof(MeshMaker))]
public class MeshMakerEditor : Editor
{
	private Mesh mesh;
	private bool editMode = false;
	private Vector3[] positions;
	private bool[] selected;
	private List<int> selects = new List<int>();
	private int[] triangles;
	private Color[] colors;
	private Vector2[] uv;
	private Texture2D uvRefTex;
	private bool showUVEditor;
	private int currentUV = 1;

	private void OnEnable() { UpdateData(); }
	private void UpdateData()
	{
		uvRefTex = new Texture2D(32, 32);
		uvRefTex.wrapMode = TextureWrapMode.Clamp;
		for (int i = 0; i < uvRefTex.width; i++)
		{
			for (int j = 0; j < uvRefTex.height; j++)
			{
				uvRefTex.SetPixel(i, j, new Color(i / (float)uvRefTex.width, j / (float)uvRefTex.height, 0, 1));
			}
		}
		uvRefTex.Apply();

		var maker = (MeshMaker)target;
		maker.meshFilter = maker.GetComponent<MeshFilter>();

		mesh = maker.meshFilter.sharedMesh;
		if (mesh == null)
		{
			mesh = new Mesh();
			mesh.name = maker.name;
			maker.meshFilter.sharedMesh = mesh;
			AssetDatabase.CreateAsset(mesh, "Assets/" + mesh.name + ".asset");
		}
		positions = mesh.vertices;
		triangles = mesh.triangles;
		colors = mesh.colors;
		uv = mesh.uv;
		if (colors.Length != positions.Length)
		{
			colors = new Color[positions.Length];
			for (int i = 0; i < positions.Length; i++) colors[i] = Color.white;
			mesh.colors = colors.ToArray();
		}
		if (uv.Length != positions.Length) uv = new Vector2[positions.Length];
		selected = new bool[positions.Length];
	}

	private void OnDisable()
	{
		Tools.hidden = false;
	}

	public override void OnInspectorGUI()
	{
		var maker = (MeshMaker)target;
		#region EDIT MODE TOGGLE
		//toggle button for edit mode
		GUIContent editIcon = EditorGUIUtility.IconContent("EditCollider");
		var oldEditMode = editMode;
		editMode = GUILayout.Toggle(editMode, editIcon, "Button", GUILayout.Height(30), GUILayout.Width(30));
		if (editMode != oldEditMode)
		{
			Tools.hidden = editMode;
			selects.Clear();
			selected = new bool[positions.Length];
			//update scene view
			SceneView.RepaintAll();
		}
		#endregion

		if (!editMode) return;
		#region UV maker
		GUILayout.BeginHorizontal();
		showUVEditor = EditorGUILayout.Foldout(showUVEditor, "UV Editor", true);
		EditorGUI.BeginChangeCheck();
		if (selects.Count == 1) uv[selects[0]] = EditorGUILayout.Vector2Field("", uv[selects[0]]);
		if (EditorGUI.EndChangeCheck()) mesh.uv = uv;
		GUILayout.EndHorizontal();
		if (showUVEditor)
		{
			//space 5
			GUILayout.Space(5);
			float size = EditorGUIUtility.currentViewWidth - 22;
			Rect rect = GUILayoutUtility.GetRect(size, size);
			GUILayout.Space(5);
			Texture tex = null;
			var renderer = maker.GetComponent<MeshRenderer>();
			if (renderer != null)
			{
				var mat = renderer.sharedMaterial;
				if (mat != null)
				{
					tex = mat.GetTexture("_MainTex");
				}
			}


			GUI.Box(rect, GUIContent.none);
			GUI.DrawTexture(rect, tex != null ? tex : uvRefTex, ScaleMode.StretchToFill);

			Handles.color = new Color(1, 1, 1, 0.5f);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				var p1 = uv[triangles[i]] * rect.size;
				p1.y = 1 - p1.y + rect.yMax;
				p1.x += rect.xMin;
				var p2 = uv[triangles[i + 1]] * rect.size;
				p2.y = 1 - p2.y + rect.yMax;
				p2.x += rect.xMin;
				var p3 = uv[triangles[i + 2]] * rect.size;
				p3.y = 1 - p3.y + rect.yMax;
				p3.x += rect.xMin;

				Handles.DrawLine(p1, p2);
				Handles.DrawLine(p2, p3);
				Handles.DrawLine(p3, p1);
			}
			EditorGUI.BeginChangeCheck();
			Handles.color = Color.blue;
			for (int i = 0; i < selects.Count; i++)
			{
				uv[selects[i]] = Drag2D.Handle(rect, uv[selects[i]], 4);
			}
			if (EditorGUI.EndChangeCheck()) mesh.uv = uv;
		}
		#endregion

		#region ADD/DELETE POINTS
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(selects.Count == 0 ? "Add Point" : selects.Count == 1 ? "Copy point" : "Copy points"))
		{
			if (selects.Count == 0)
			{
				selects.Add(selected.Length);

				var posList = positions.ToList();
				posList.Add(Vector3.zero);
				positions = posList.ToArray();

				var selList = selected.ToList();
				selList.Add(true);
				selected = selList.ToArray();

				var colList = colors.ToList();
				colList.Add(Color.white);
				colors = colList.ToArray();

				var uvList = uv.ToList();
				uvList.Add(Vector2.zero);
				uv = uvList.ToArray();
			}
			else
			{
				var posList = positions.ToList();
				var colList = colors.ToList();
				var uvList = uv.ToList();
				for (int i = 0; i < selects.Count; i++)
				{
					posList.Add(positions[selects[i]]);

					colList.Add(colors[selects[i]]);

					uvList.Add(uv[selects[i]]);
				}
				positions = posList.ToArray();
				colors = colList.ToArray();
				uv = uvList.ToArray();
				selected = new bool[positions.Length];
				//select last added points
				for (int i = 0; i < selects.Count; i++)
				{
					var id = positions.Length - selects.Count + i;
					selects[i] = id;
					selected[id] = true;
				}
			}
			mesh.vertices = positions.ToArray();
			mesh.colors = colors.ToArray();
			mesh.uv = uv.ToArray();
			mesh.RecalculateNormals();
			SceneView.RepaintAll();
		}

		if (selects.Count == 0) GUI.enabled = false;

		if (GUILayout.Button("Delete Point" + (selects.Count > 1 ? "s" : "")))
		{
			var triList = triangles.ToList();
			for (int i = triangles.Length - 3; i >= 0; i -= 3)
			{
				if (selects.Contains(triangles[i]) || selects.Contains(triangles[i + 1]) || selects.Contains(triangles[i + 2]))
				{
					triList.RemoveAt(i + 2);
					triList.RemoveAt(i + 1);
					triList.RemoveAt(i);
				}
			}

			for (int i = 0; i < triList.Count; i++)
			{
				int offset = 0;
				for (int j = 0; j < selects.Count; j++)
				{
					if (triList[i] >= selects[j]) offset++;
				}
				triList[i] -= offset;
			}
			if (triList.Count != triangles.Length)
			{
				triangles = triList.ToArray();
			}
			mesh.triangles = triangles;

			//invert sort of list
			var posList = positions.ToList();
			var colList = colors.ToList();
			var uvList = uv.ToList();
			selects.Sort((a, b) => b.CompareTo(a));
			foreach (var i in selects)
			{
				posList.RemoveAt(i);
				colList.RemoveAt(i);
				uvList.RemoveAt(i);
			}
			positions = posList.ToArray();
			colors = colList.ToArray();
			uv = uvList.ToArray();

			selected = new bool[positions.Length];
			selects.Clear();

			mesh.vertices = positions.ToArray();
			mesh.uv = uv.ToArray();
			mesh.colors = colors.ToArray();

			SceneView.RepaintAll();
		}

		GUI.enabled = true;
		GUILayout.EndHorizontal();
		#endregion

		#region POINTS
		if (selects.Count == 0) return;

		(bool x, bool y, bool z) vectorData = (true, true, true);
		bool colorData = true;

		//vector x
		for (int i = 0; i < selects.Count - 1; i++)
		{
			var v1 = positions[selects[i]];
			var v2 = positions[selects[i + 1]];
			if (v1.x != v2.x) vectorData.x = false;
			if (v1.y != v2.y) vectorData.y = false;
			if (v1.z != v2.z) vectorData.z = false;

			if (colors[selects[i]] != colors[selects[i + 1]]) colorData = false;
		}

		var vector = positions[selects[0]];
		GUILayout.BeginHorizontal();
		GUILayout.Label("Pos");
		//x
		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = !vectorData.x;
		vector.x = EditorGUILayout.FloatField(vector.x);
		vectorData.x = EditorGUI.EndChangeCheck();

		//y
		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = !vectorData.y;
		vector.y = EditorGUILayout.FloatField(vector.y);
		vectorData.y = EditorGUI.EndChangeCheck();

		//z
		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = !vectorData.z;
		vector.z = EditorGUILayout.FloatField(vector.z);
		vectorData.z = EditorGUI.EndChangeCheck();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Color");
		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = !colorData;
		var color = EditorGUILayout.ColorField(colors[selects[0]]);
		colorData = EditorGUI.EndChangeCheck();
		GUILayout.EndHorizontal();


		for (int i = 0; i < selects.Count; i++)
		{
			var v = positions[selects[i]];
			var c = colors[selects[i]];

			if (vectorData.x) v.x = vector.x;
			if (vectorData.y) v.y = vector.y;
			if (vectorData.z) v.z = vector.z;

			if (colorData) c = color;

			positions[selects[i]] = v;
			colors[selects[i]] = c;
		}

		if (vectorData.x || vectorData.y || vectorData.z) mesh.vertices = positions.ToArray();
		if (colorData) mesh.colors = colors.ToArray();
		#endregion
	}
	private void OnSceneGUI()
	{
		if (!editMode) return;
		//target component
		var maker = (MeshMaker)target;
		if (maker.meshFilter.sharedMesh == null) UpdateData();
		if (maker.meshFilter.sharedMesh != mesh) UpdateData();
		Event e = Event.current;

		if (e.shift)
		{
			if (TriangleControl(maker.transform))
			{
				mesh.triangles = triangles.ToArray();
				mesh.RecalculateBounds();
				mesh.RecalculateNormals();

				return;
			}
			TryDropSelects(e);
			return;
		}
		else triangle = new List<int>();

		if (e.control)
		{
			if (SelectControl(maker.transform)) return;
			TryDropSelects(e);
			return;
		}

		if (MoveControl(maker.transform))
		{
			mesh.vertices = positions.ToArray();
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			Repaint();
			return;
		}

		TryDropSelects(e);
	}

	private void TryDropSelects(Event e)
	{
		if (e.type == EventType.MouseDown && e.button == 0)
		{
			selects.Clear();
			selected = new bool[positions.Length];
			e.Use();
		}
	}

	private List<int> triangle = new List<int>();

	private bool TriangleControl(Transform transform)
	{
		var ret = false;

		Event e = Event.current;

		int clickedVertex = -1;

		// -----------------------------
		// VERTEX PASS
		// -----------------------------
		for (int i = 0; i < positions.Length; i++)
		{
			var pos = transform.TransformPoint(positions[i]);
			float size = HandleUtility.GetHandleSize(pos);

			bool isSelected = triangle.Contains(i);

			Handles.color = colors[i];
			Handles.DotHandleCap(0, pos, Quaternion.identity, 0.075f * size, EventType.Repaint);

			Handles.color = isSelected ? Color.yellow : Color.blue;

			if (Handles.Button(pos, Quaternion.identity, 0.05f * size, 0.05f * size, Handles.DotHandleCap))
			{
				if (isSelected) triangle.Remove(i);
				else triangle.Add(i);

				if (triangle.Count == 3)
				{
					var triList = triangles.ToList();

					triList.Add(triangle[0]);
					triList.Add(triangle[1]);
					triList.Add(triangle[2]);

					triangles = triList.ToArray();

					triangle.Clear();

					ret = true;
				}

				return true; // vertex consumed click
			}
		}

		// -----------------------------
		// TRIANGLE RAYCAST DELETE
		// -----------------------------
		if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

			float bestDist = float.MaxValue;
			int bestTri = -1;

			for (int t = 0; t < triangles.Length; t += 3)
			{
				Vector3 a = transform.TransformPoint(positions[triangles[t]]);
				Vector3 b = transform.TransformPoint(positions[triangles[t + 1]]);
				Vector3 c = transform.TransformPoint(positions[triangles[t + 2]]);

				if (IntersectRayTriangle(ray, a, b, c, out float dist))
				{
					if (dist < bestDist)
					{
						bestDist = dist;
						bestTri = t;
					}
				}
			}

			if (bestTri != -1)
			{
				var triList = triangles.ToList();

				triList.RemoveRange(bestTri, 3);

				triangles = triList.ToArray();

				ret = true;

				e.Use();
			}
		}

		return ret;
	}

	private bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
	{
		t = 0;

		Vector3 e1 = v1 - v0;
		Vector3 e2 = v2 - v0;

		Vector3 p = Vector3.Cross(ray.direction, e2);
		float det = Vector3.Dot(e1, p);

		if (Mathf.Abs(det) < 0.000001f)
			return false;

		float invDet = 1f / det;

		Vector3 s = ray.origin - v0;
		float u = Vector3.Dot(s, p) * invDet;
		if (u < 0 || u > 1) return false;

		Vector3 q = Vector3.Cross(s, e1);
		float v = Vector3.Dot(ray.direction, q) * invDet;
		if (v < 0 || u + v > 1) return false;

		t = Vector3.Dot(e2, q) * invDet;

		return t > 0;
	}

	private Vector2 dragStart;
	private bool isDragging = false;

	private bool SelectControl(Transform transform)
	{
		var ret = false;
		triangle = new List<int>();

		Event e = Event.current;

		// -----------------------------
		// START DRAG
		// -----------------------------
		if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
		{
			isDragging = true;
			dragStart = e.mousePosition;
		}

		// -----------------------------
		// DRAG END
		// -----------------------------
		if (e.type == EventType.MouseUp && e.button == 0 && isDragging)
		{
			isDragging = false;

			Rect selectionRect = GetScreenRect(dragStart, e.mousePosition);

			for (int i = 0; i < positions.Length; i++)
			{
				var worldPos = transform.TransformPoint(positions[i]);
				var screenPos = HandleUtility.WorldToGUIPoint(worldPos);

				if (selectionRect.Contains(screenPos))
				{
					if (!selected[i])
					{
						selected[i] = true;
						selects.Add(i);
						ret = true;
					}
				}
			}

			Repaint();
		}

		// -----------------------------
		// DRAW POINTS
		// -----------------------------
		for (int i = 0; i < positions.Length; i++)
		{
			var pos = transform.TransformPoint(positions[i]);
			float size = HandleUtility.GetHandleSize(pos);

			Handles.color = colors[i];
			Handles.DotHandleCap(0, pos, Quaternion.identity, 0.075f * size, EventType.Repaint);

			Handles.color = selected[i] ? Color.yellow : Color.blue;

			if (Handles.Button(pos, Quaternion.identity, 0.05f * size, 0.05f * size, Handles.DotHandleCap))
			{
				selected[i] = !selected[i];

				if (selected[i]) selects.Add(i);
				else selects.Remove(i);

				Repaint();
				ret = true;
			}
		}

		// -----------------------------
		// DRAW RECT (visual feedback)
		// -----------------------------
		if (isDragging)
		{
			Handles.BeginGUI();

			Rect r = GetScreenRect(dragStart, e.mousePosition);
			GUI.color = new Color(0.2f, 0.6f, 1f, 0.15f);
			GUI.DrawTexture(r, Texture2D.whiteTexture);

			GUI.color = Color.cyan;
			GUI.Box(r, GUIContent.none);

			Handles.EndGUI();

			SceneView.RepaintAll();
		}

		return ret;
	}

	private Rect GetScreenRect(Vector2 a, Vector2 b)
	{
		float x = Mathf.Min(a.x, b.x);
		float y = Mathf.Min(a.y, b.y);
		float w = Mathf.Abs(a.x - b.x);
		float h = Mathf.Abs(a.y - b.y);

		return new Rect(x, y, w, h);
	}

	Quaternion oldRot;
	Quaternion rotation = Quaternion.identity;
	Vector3 oldScale = Vector3.one;

	private bool MoveControl(Transform transform)
	{
		triangle = new List<int>();
		var ret = false;
		var center = Vector3.zero;

		for (int i = 0; i < positions.Length; i++)
		{
			var pos = transform.TransformPoint(positions[i]);
			float size = HandleUtility.GetHandleSize(pos);

			//draw color as outline

			Handles.color = colors[i];
			Handles.DotHandleCap(0, pos, Quaternion.identity, 0.075f * size, EventType.Repaint);

			if (selected[i])
			{
				center += pos;
				Handles.color = Color.yellow;
				if (Handles.Button(pos, Quaternion.identity, 0.05f * size, 0.05f * size, Handles.DotHandleCap))
				{
					selected = new bool[positions.Length];
					selected[i] = true;
					selects = new List<int> { i };
					//repaint inspector
					Repaint();
				}
				continue;
			}


			Handles.color = Color.blue;
			if (Handles.Button(pos, Quaternion.identity, 0.05f * size, 0.05f * size, Handles.DotHandleCap))
			{
				selected = new bool[positions.Length];
				selected[i] = true;
				selects = new List<int> { i };
				//repaint inspector
				Repaint();
			}
		}

		if (selects.Count == 0) return false;

		center /= selects.Count;

		if (Tools.current == Tool.Move)
		{
			EditorGUI.BeginChangeCheck();
			var newCenter = Handles.PositionHandle(center, Quaternion.identity);
			ret = EditorGUI.EndChangeCheck();

			if (ret)
			{
				foreach (var i in selects)
				{
					positions[i] = transform.InverseTransformPoint(transform.TransformPoint(positions[i]) + newCenter - center);
				}
			}
		}
		else if (Tools.current == Tool.Rotate)
		{
			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				oldRot = Quaternion.identity;
				rotation = Quaternion.identity;
			}
			EditorGUI.BeginChangeCheck();
			rotation = Handles.RotationHandle(rotation, center);
			ret = EditorGUI.EndChangeCheck();

			if (ret)
			{
				foreach (var i in selects)
				{
					positions[i] = transform.TransformPoint(positions[i]) - center;
					positions[i] = Quaternion.Inverse(oldRot) * positions[i];
					positions[i] = rotation * positions[i];
					positions[i] = transform.InverseTransformPoint(center + positions[i]);
				}
				oldRot = rotation;
			}

		}
		else if (Tools.current == Tool.Scale)
		{
			if (Event.current.type == EventType.MouseUp && Event.current.button == 0) oldScale = Vector3.one;
			EditorGUI.BeginChangeCheck();
			var scale = Handles.ScaleHandle(oldScale, center, Quaternion.identity, HandleUtility.GetHandleSize(center));
			ret = EditorGUI.EndChangeCheck();
			if (ret)
			{
				oldScale = new Vector3(1f / oldScale.x, 1f / oldScale.y, 1f / oldScale.z);
				foreach (var i in selects)
				{
					var p = transform.TransformPoint(positions[i]) - center;
					p = Vector3.Scale(p, oldScale);
					p = Vector3.Scale(p, scale);
					positions[i] = transform.InverseTransformPoint(center + p);
				}

			}
			oldScale = scale;
		}

		return ret;
	}
}


public static class Drag2D
{
	public static Vector2 Handle(Rect rect, Vector2 value, float radius = 4f)
	{
		int id = GUIUtility.GetControlID(FocusType.Passive);

		Event e = Event.current;

		// normalized -> rect space
		Vector2 pos = new Vector2(
			Mathf.Lerp(rect.x, rect.xMax, value.x),
			Mathf.Lerp(rect.yMax, rect.y, value.y));

		Rect handleRect = new Rect(
			pos.x - radius,
			pos.y - radius,
			radius * 2,
			radius * 2);

		switch (e.GetTypeForControl(id))
		{
			case EventType.MouseDown:
				{
					if (e.button == 0 && handleRect.Contains(e.mousePosition))
					{
						GUIUtility.hotControl = id;
						GUIUtility.keyboardControl = id;

						GUI.changed = true; // ключевой момент

						e.Use();
					}

					break;
				}

			case EventType.MouseDrag:
				{
					if (GUIUtility.hotControl == id)
					{
						value.x = Mathf.InverseLerp(rect.x, rect.xMax, e.mousePosition.x);
						value.y = Mathf.InverseLerp(rect.yMax, rect.y, e.mousePosition.y);

						value.x = Mathf.Clamp01(value.x);
						value.y = Mathf.Clamp01(value.y);

						GUI.changed = true; // говорит Unity: "контрол изменился"

						e.Use();
					}

					break;
				}

			case EventType.MouseUp:
				{
					if (GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;
						e.Use();
					}

					break;
				}

			case EventType.Repaint:
				{
					EditorGUI.DrawRect(handleRect, Handles.color);
					break;
				}
		}

		return value;
	}
}
#endif