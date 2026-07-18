using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

[CreateAssetMenu(fileName = "TextureColorBaker", menuName = "Tools/Texture Color Baker")]
public class TextureColorizer : ScriptableObject
{
	public Texture2D sourceTexture;
	public Color tint = Color.white;

#if UNITY_EDITOR
	public void Bake()
	{
		if (sourceTexture == null)
		{
			Debug.LogWarning("Source texture is null");
			return;
		}

		Texture2D tex = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);

		Color[] pixels = sourceTexture.GetPixels();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] *= tint;
			pixels[i] += Color.white * 0.2f;
		}

		tex.SetPixels(pixels);
		tex.filterMode = FilterMode.Point;
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.Apply();

		byte[] png = tex.EncodeToPNG();
		string outputPath = AssetDatabase.GetAssetPath(sourceTexture);
		outputPath = outputPath.Substring(0, outputPath.Length - 4) + "_colored.png";
		System.IO.File.WriteAllBytes(outputPath, png);

		AssetDatabase.Refresh();
	}
#endif
}

//Bake button in the inspector
#if UNITY_EDITOR
[CustomEditor(typeof(TextureColorizer))]
public class TextureColorizerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		if (GUILayout.Button("Bake"))
		{
			((TextureColorizer)target).Bake();
		}
	}
}
#endif