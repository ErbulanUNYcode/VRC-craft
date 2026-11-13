using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRC_MINE.World
{
	public class SectionMeshGen : MonoBehaviour
	{
		[SerializeField] private Mesh generatedMesh;
		[SerializeField] private GameObject sectionPrefab;

		// Static Y/Z template. Only X is patched for each section.
		private readonly Vector3[] tempV =
		{
			new Vector3(1, 0, 0), new Vector3(1, 2, 0), new Vector3(1, 2, 2), new Vector3(1, 0, 2),
			new Vector3(0, 1, 0), new Vector3(2, 1, 0), new Vector3(2, 1, 2), new Vector3(0, 1, 2),
			new Vector3(0, 0, 1), new Vector3(2, 0, 1), new Vector3(2, 2, 1), new Vector3(0, 2, 1)
		};

		public Mesh Generate()
		{
			Vector3[] vertices = new Vector3[64 * 3 * 4];
			int[] triangles = new int[64 * 3 * 6];

			// Binary-fill the complete vertex array from the 12-vertex template.
			System.Array.Copy(tempV, 0, vertices, 0, tempV.Length);
			for (int i = tempV.Length; i < vertices.Length; i <<= 1)
				System.Array.Copy(vertices, 0, vertices, i, Mathf.Min(i, vertices.Length - i));

			// Each quad owns four vertices. Three consecutive quads belong to one section.
			int count = 0;
			for (int x = 0; x < 64; x += 16)
			{
				for (int y = 0; y < 64; y += 16)
				{
					for (int z = 0; z < 64; z += 16)
					{
						var pos = new Vector3(x, y, z);
						for (int i = 0; i < 12; i++)
						{
							vertices[count++] += pos;
						}
					}
				}
			}

			// One-dimensional fill: every six indices address the next four vertices.
			count = 0;
			for (int i = 0; i < triangles.Length; i += 6)
			{
				triangles[i + 0] = count;
				triangles[i + 5] = count++;
				triangles[i + 1] = count++;
				triangles[i + 2] = count;
				triangles[i + 3] = count++;
				triangles[i + 4] = count++;
			}

			Mesh mesh = new Mesh();
			mesh.name = "SectionMesh_64x64x64";
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.bounds = new Bounds(new Vector3(32, 32, 32), new Vector3(64, 64, 64));
			return mesh;
		}

#if UNITY_EDITOR
		[ContextMenu("Generate Mesh Asset")]
		internal void GenerateMeshAsset()
		{
			Mesh mesh = Generate();
			string folder = "Assets/Meshes";
			string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/SectionMesh_64x64x64.asset");
			AssetDatabase.CreateAsset(mesh, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			generatedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
			MeshFilter filter = GetComponent<MeshFilter>();
			if (filter != null) filter.sharedMesh = generatedMesh;
			EditorUtility.SetDirty(this);
			EditorUtility.SetDirty(mesh);
		}

		[ContextMenu("Place Section Prefabs")]
		internal void PlaceSectionPrefabs()
		{
			GameObject prefab = sectionPrefab != null
				? sectionPrefab
				: AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/section.prefab");
			if (prefab == null)
			{
				Debug.LogError("Assign Section Prefab in the inspector.", this);
				return;
			}

			for (int x = -512; x < 512; x += 64)
			for (int z = -512; z < 512; z += 64)
			for (int y = 0; y < 256; y += 64)
			{
				GameObject section = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
				section.name = $"{prefab.name} ({x}, {y}, {z})";
				section.transform.localPosition = new Vector3(x, y, z);
			}
		}
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(SectionMeshGen))]
	public class SectionMeshGenEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			SectionMeshGen generator = (SectionMeshGen)target;
			if (GUILayout.Button("Generate Mesh Asset")) generator.GenerateMeshAsset();
			if (GUILayout.Button("Place Section Prefabs (16 x 16 x 4)")) generator.PlaceSectionPrefabs();
		}
	}
#endif
}
