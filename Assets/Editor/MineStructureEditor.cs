using UnityEditor;
using UnityEngine;

public class MineStructureEditor : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
}

public static class MineStructureCreator
{
	[MenuItem("Assets/Create/MineTools/MineStructure")]
	public static void Create()
	{
		string folder = "Assets";

		if (Selection.activeObject != null)
		{
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (AssetDatabase.IsValidFolder(path))
				folder = path;
			else
				folder = System.IO.Path.GetDirectoryName(path);
		}

		string prefabPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/New MineStructure.prefab");

		PrefabUtility.SaveAsPrefabAsset(new GameObject(), prefabPath);

		GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

		prefab.name = "New MineStructure";
		prefab.AddComponent<MineStructure>();

		PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
		PrefabUtility.UnloadPrefabContents(prefab);

		Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
	}
}