using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "BlockTexture", menuName = "MineTools/BlockTexture")]
public class BlockTexture : ScriptableObject
{
	[HideInInspector]
	[SerializeField]
	private SideData[] sides = new SideData[6];

	public void SetSide(BlockSide side, SideData value)
	{
		sides[(int)side] = value;
	}

	public SideData GetSide(BlockSide side)
	{
		return sides[(int)side];
	}

	internal Color32[] GetSideData(BlockSide side)
	{
		var d = sides[(int)side].texture.GetPixels32();

		if (side == BlockSide.Front || side == BlockSide.Left)
		{
			var copy = d;
			d = new Color32[d.Length];
			for (int x = 0; x < 16; x++)
			{
				for (int y = 0; y < 16; y++)
				{
					d[x + y * 16] = copy[15 - x + y * 16];
				}
			}
		}
		if (side == BlockSide.Down)
		{
			var copy = d;
			d = new Color32[d.Length];
			for (int x = 0; x < 16; x++)
			{
				for (int y = 0; y < 16; y++)
				{
					d[x + y * 16] = copy[x + (15 - y) * 16];
				}
			}
		}
		return d;
	}

	public enum BlockSide
	{
		Left, Right,
		Down, Up,
		Back, Front
	}

	[Serializable]
	public struct SideData
	{
		public Texture2D texture;
		public bool flipDiagonal;
		public bool flipHorizontal;
		public bool flipVertical;
		public bool randomFlipDiagonal;
		public bool randomFlipHorizontal;
		public bool randomFlipVertical;
	}
}

[InitializeOnLoad]
public static class BlockTextureIconDrawer
{
	static BlockTextureIconDrawer()
	{
		EditorApplication.projectWindowItemOnGUI += OnProjectGUI;
	}

	static void OnProjectGUI(string guid, Rect rect)
	{
		string path = AssetDatabase.GUIDToAssetPath(guid);
		var obj = AssetDatabase.LoadAssetAtPath<Object>(path);

		if (obj is not BlockTexture block)
			return;
		var S = block.GetSide(BlockTexture.BlockSide.Back);
		Texture2D icon = S.texture;
		if (icon == null)
			return;

		// Unity-scale-aware size
		float size = rect.height; // ключевой момент
		Rect iconRect;
		if (size == 16) iconRect = new Rect(rect.x + 3, rect.y, size, size);
		else iconRect = new Rect(rect.x + 3, rect.y, size - 15, size - 15);
		GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
	}
}

public class BlockTextureEditorWindow : EditorWindow
{
	private BlockTexture asset;
	private Vector3[] p = new Vector3[8];
	private Vector2 rot = new Vector2(0.5f, 0.25f);
	private bool dragging;
	private Vector2 lastMouse;
	private Vector2 firstMouse;
	private Material mat;

	[MenuItem("Tools/Block Texture Window")]
	public static void Open()
	{
		GetWindow<BlockTextureEditorWindow>("BlockTexture Editor");
	}

	public void SetAsset(BlockTexture bt)
	{
		asset = bt;
		mat = new Material(Shader.Find("Unlit/Transparent"));
		mat.color = new Color(2f, 2f, 2f, 1f);
		Repaint();
	}

	void OnGUI()
	{
		asset = EditorGUILayout.ObjectField(asset, typeof(BlockTexture), false) as BlockTexture;
		EditorGUILayout.LabelField("rotation: " + rot);
		if (asset == null)
		{
			return;
		}

		Event e = Event.current;

		if (e.type == EventType.MouseDown && e.button == 0)
		{
			dragging = true;
			lastMouse = e.mousePosition;
			firstMouse = e.mousePosition;
		}

		if (e.type == EventType.MouseUp && e.button == 0)
		{
			dragging = false;
		}

		if (dragging && e.type == EventType.MouseDrag)
		{
			Vector2 delta = e.mousePosition - lastMouse;

			rot.x -= 0.006f * delta.x;
			rot.y += 0.009f * delta.y;
			rot.x = (rot.x + (Mathf.PI * 2)) % (Mathf.PI * 2);
			rot.y = Mathf.Clamp(rot.y, -0.6f, 0.6f);

			lastMouse = e.mousePosition;

			Repaint();
		}

		for (int i = 0; i < 4; i++)
		{
			p[i].x = Mathf.Sin(rot.x + 1.5707964f * i - 2.3561945f) * 0.70710677f;
			p[i + 4].x = p[i].x;
			p[i].z = Mathf.Cos(rot.x + 1.5707964f * i - 2.3561945f) * 0.70710677f;
			p[i + 4].z = p[i].z;
			p[i].y = 0.5f;
			p[i + 4].y = -0.5f;
		}

		var size = Mathf.Min(position.width, position.height);
		Vector2 center = position.size / 2;

		Handles.color = Color.red;
		for (int i = 0; i < 8; i++)
		{
			var a = Mathf.Atan2(p[i].y, p[i].z);
			var d = new Vector2(p[i].y, p[i].z).magnitude;
			p[i].y = Mathf.Sin(a + rot.y) * d;

			p[i].x *= size / 2;
			p[i].y *= -size / 2;
			p[i].x += center.x;
			p[i].y += center.y;
			p[i].z = 0;
		}

		Handles.color = Color.black;
		DrawSide(e, BlockTexture.BlockSide.Left, 5, 1, 0, 4);
		DrawSide(e, BlockTexture.BlockSide.Right, 7, 3, 2, 6);
		DrawSide(e, BlockTexture.BlockSide.Down, 5, 4, 7, 6);
		DrawSide(e, BlockTexture.BlockSide.Up, 0, 1, 2, 3);
		DrawSide(e, BlockTexture.BlockSide.Back, 4, 0, 3, 7);
		DrawSide(e, BlockTexture.BlockSide.Front, 6, 2, 1, 5);

		Handles.color = Color.black;
		Handles.DrawAAPolyLine(size / 40, center, (Vector3)center + (p[3] - p[0]) / 4);
		Handles.DrawAAPolyLine(size / 40, center, (Vector3)center + (p[0] - p[4]) / 4);
		Handles.DrawAAPolyLine(size / 40, center, (Vector3)center + (p[1] - p[0]) / 4);


		Handles.color = Color.red;
		Handles.DrawAAPolyLine(size / 50, center, (Vector3)center + (p[3] - p[0]) / 4);
		Handles.color = Color.green;
		Handles.DrawAAPolyLine(size / 50, center, (Vector3)center + (p[0] - p[4]) / 4);
		Handles.color = Color.blue;
		Handles.DrawAAPolyLine(size / 50, center, (Vector3)center + (p[1] - p[0]) / 4);


		Handles.EndGUI();
	}

	private void DrawSide(Event e, BlockTexture.BlockSide side, int a, int b, int c, int d)
	{
		if ((p[b].x - p[a].x) * (p[c].y - p[a].y) - (p[b].y - p[a].y) * (p[c].x - p[a].x) < 0f) return;

		var s = asset.GetSide(side);
		mat.mainTexture = s.texture;

		GL.PushMatrix();
		mat.SetPass(0);
		GL.LoadPixelMatrix();

		GL.Begin(GL.TRIANGLES);
		var uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };

		if (s.flipHorizontal)
		{
			var t = uv[0];
			uv[0] = uv[3];
			uv[3] = t;
			t = uv[1];
			uv[1] = uv[2];
			uv[2] = t;
		}

		if (s.flipVertical)
		{
			var t = uv[0];
			uv[0] = uv[1];
			uv[1] = t;
			t = uv[3];
			uv[3] = uv[2];
			uv[2] = t;
		}

		if (s.flipDiagonal)
		{
			var t = uv[1];
			uv[1] = uv[3];
			uv[3] = t;
		}

		// два треугольника (0-1-2, 0-2-3)
		DrawTri1(p[a], p[b], p[c], uv[0], uv[1], uv[2]);
		DrawTri2(p[c], p[d], p[a], uv[2], uv[3], uv[0]);

		GL.End();
		GL.PopMatrix();

		//lines
		Handles.DrawLine(p[a], p[b]);
		Handles.DrawLine(p[b], p[c]);
		Handles.DrawLine(p[c], p[d]);
		Handles.DrawLine(p[d], p[a]);

		var center = (p[a] + p[c]) / 2;
		var changed = false;

		Rect rect = new Rect(center.x - 18, center.y - 28, 16, 16);
		EditorGUI.DrawRect(rect, s.flipDiagonal ? Color.green : Color.gray);
		if (GUI.Button(rect, " D", GUIStyle.none))
		{
			Undo.RecordObject(asset, "Flip Diagonal");
			s.flipDiagonal = !s.flipDiagonal;
			asset.SetSide(side, s);
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		rect = new Rect(center.x - 18, center.y - 8, 16, 16);
		EditorGUI.DrawRect(rect, s.flipHorizontal ? Color.green : Color.gray);
		if (GUI.Button(rect, " H", GUIStyle.none))
		{
			Undo.RecordObject(asset, "Flip Horizontal");
			s.flipHorizontal = !s.flipHorizontal;
			asset.SetSide(side, s);
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		rect = new Rect(center.x - 18, center.y + 12, 16, 16);
		EditorGUI.DrawRect(rect, s.flipVertical ? Color.green : Color.gray);
		if (GUI.Button(rect, " V", GUIStyle.none))
		{
			Undo.RecordObject(asset, "Flip Vertical");
			s.flipVertical = !s.flipVertical;
			asset.SetSide(side, s);
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		rect = new Rect(center.x + 2, center.y - 28, 16, 16);
		EditorGUI.DrawRect(rect, s.randomFlipDiagonal ? Color.green : Color.gray);
		if (GUI.Button(rect, "RD", GUIStyle.none))
		{
			Undo.RecordObject(asset, "Flip Diagonal");
			s.randomFlipDiagonal = !s.randomFlipDiagonal;
			asset.SetSide(side, s);
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		rect = new Rect(center.x + 2, center.y - 8, 16, 16);
		EditorGUI.DrawRect(rect, s.randomFlipHorizontal ? Color.green : Color.gray);
		if (GUI.Button(rect, "RH", GUIStyle.none))
		{
			Undo.RecordObject(asset, "Flip Horizontal");
			s.randomFlipHorizontal = !s.randomFlipHorizontal;
			asset.SetSide(side, s);
			EditorUtility.SetDirty(asset);
			changed = true;
		}

		rect = new Rect(center.x + 2, center.y + 12, 16, 16);
		EditorGUI.DrawRect(rect, s.randomFlipVertical ? Color.green : Color.gray);
		if (GUI.Button(rect, "RV", GUIStyle.none))
		{
			Undo.RecordObject(asset, "Flip Vertical");
			s.randomFlipVertical = !s.randomFlipVertical;
			asset.SetSide(side, s);
			EditorUtility.SetDirty(asset);
			changed = true;
		}


		if (!changed && IsInside(p[a], p[b], p[c], p[d], Event.current.mousePosition) && e.type == EventType.MouseUp && e.button == 0 && firstMouse == e.mousePosition)
		{
			TexturePickerWindow.Open(tex =>
			{
				Undo.RecordObject(asset, "Change Front");
				var s = asset.GetSide(side);
				s.texture = tex;
				asset.SetSide(side, s);
				EditorUtility.SetDirty(asset);
				Repaint();
			});
		}
	}

	bool IsInside(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 p)
	{
		float Cross(Vector2 v1, Vector2 v2, Vector2 v3)
		{
			return (v2.x - v1.x) * (v3.y - v1.y) - (v2.y - v1.y) * (v3.x - v1.x);
		}

		float c1 = Cross(a, b, p);
		float c2 = Cross(b, c, p);
		float c3 = Cross(c, d, p);
		float c4 = Cross(d, a, p);

		bool hasNeg = (c1 < 0) || (c2 < 0) || (c3 < 0) || (c4 < 0);
		bool hasPos = (c1 > 0) || (c2 > 0) || (c3 > 0) || (c4 > 0);

		return !(hasNeg && hasPos);
	}

	void DrawTri1(Vector2 a, Vector2 b, Vector2 c, Vector2 uv1, Vector2 uv2, Vector2 uv3)
	{
		GL.TexCoord2(uv1.x, uv1.y); GL.Vertex3(a.x, a.y, 0);
		GL.TexCoord2(uv2.x, uv2.y); GL.Vertex3(b.x, b.y, 0);
		GL.TexCoord2(uv3.x, uv3.y); GL.Vertex3(c.x, c.y, 0);
	}
	void DrawTri2(Vector2 a, Vector2 b, Vector2 c, Vector2 uv1, Vector2 uv2, Vector2 uv3)
	{
		GL.TexCoord2(uv1.x, uv1.y); GL.Vertex3(a.x, a.y, 0);
		GL.TexCoord2(uv2.x, uv2.y); GL.Vertex3(b.x, b.y, 0);
		GL.TexCoord2(uv3.x, uv3.y); GL.Vertex3(c.x, c.y, 0);
	}
}

public class TexturePickerWindow : EditorWindow
{
	private static System.Action<Texture2D> onSelect;
	private Texture2D[] textures;

	private static readonly string targetPath = "Assets/Textures/Atlas/TextureSides";

	private string hoveredName = "";
	private Texture2D hoveredTexture = null;

	public static void Open(System.Action<Texture2D> callback)
	{
		onSelect = callback;

		var window = GetWindow<TexturePickerWindow>("Texture Picker");
		window.minSize = new Vector2(300, 400);

		window.LoadTextures();
		window.ShowUtility();
	}

	private void LoadTextures()
	{
		string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { targetPath });

		textures = new Texture2D[guids.Length];

		for (int i = 0; i < guids.Length; i++)
		{
			string path = AssetDatabase.GUIDToAssetPath(guids[i]);
			textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
	}

	private void OnGUI()
	{
		if (textures == null || textures.Length == 0)
		{
			GUILayout.Label("No textures found.");
			return;
		}

		int size = 24;
		int columns = Mathf.Max(1, (int)(position.width - 3) / (size + 3));

		Event e = Event.current;
		hoveredTexture = null;
		hoveredName = "";

		for (int i = 0; i < textures.Length; i++)
		{
			if (i % columns == 0)
				EditorGUILayout.BeginHorizontal();

			var tex = textures[i];

			if (tex != null)
			{
				Rect r = GUILayoutUtility.GetRect(size, size);

				if (GUI.Button(r, tex))
				{
					onSelect?.Invoke(tex);
					Close();
				}

				if (r.Contains(e.mousePosition))
				{
					hoveredTexture = tex;
					hoveredName = tex.name;
				}
			}
			else
			{
				GUILayout.Space(size);
			}

			bool isLastInRow = (i % columns == columns - 1);
			bool isLastItem = (i == textures.Length - 1);

			if (isLastInRow || isLastItem)
				EditorGUILayout.EndHorizontal();
		}

		DrawHoverTooltip();
		Repaint(); // чтобы tooltip двигался за мышью
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

		GUI.Box(rect, hoveredName);
	}
}

[InitializeOnLoad]
public static class BlockTextureDoubleClickOpener
{
	static BlockTextureDoubleClickOpener()
	{
		EditorApplication.projectWindowItemOnGUI += OnProjectGUI;
	}

	static void OnProjectGUI(string guid, Rect rect)
	{
		Event e = Event.current;
		if (e == null) return;

		if (e.type == EventType.MouseDown && e.clickCount == 2 && rect.Contains(e.mousePosition))
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);

			if (obj is BlockTexture bt)
			{
				var window = EditorWindow.GetWindow<BlockTextureEditorWindow>("BlockTexture Editor");
				window.SetAsset(bt);

				e.Use();
			}
		}
	}
}