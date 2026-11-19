
using UdonSharp;
using UnityEngine;
using VRC_MINE.GamePlay;
using Random = UnityEngine.Random;

namespace VRC_MINE.World
{
	/// <summary>
	/// Controls the initial biome-generation layer pass.
	/// The scene reference is assigned manually on the controller in the scene.
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class GenLayerController : UdonSharpBehaviour
	{
		[SerializeField] private Material chunkGeneratorMaterial;
		[SerializeField] private Material chunkTerrainGeneratorMaterial;
		[SerializeField] private Material[] genLayers;


		[SerializeField] private CustomRenderTexture[] genTextures;
		[SerializeField] private CustomRenderTexture worldOptimizer;
		[SerializeField] private CustomRenderTexture voxelWorld;


		[SerializeField] private WorldGenerator worldGenerator;
		[SerializeField] private BlockSelector blockSelector;

		[SerializeField] private ChunkMeshGenerator chunkMeshGenerator;

		[SerializeField] private GameObject chunkRendererPrefab;

		[SerializeField] private Material worldMaterial;

		[HideInInspector, SerializeField] private ChunkMeshType[] chunkMeshTypes = new ChunkMeshType[256];

		[SerializeField] private int seed;

		private void Start()
		{
			ClearTextures();

			/*int seed;
#if UNITY_EDITOR
			seed = Random.Range(int.MinValue, int.MaxValue);
#else
			seed = Networking.GetUniqueName(gameObject).GetHashCode();
#endif*/
			SetSeed(seed);
		}
		private void InitializeChunkMeshes()
		{
			chunkMeshGenerator.Generate();
			int[] order = new int[256];
			int[] distances = new int[256];
			for (int index = 0; index < 256; index++)
			{
				int x = index & 15, z = index >> 4;
				int dx = x * 2 - 15, dz = z * 2 - 15;
				int distance = dx * dx + dz * dz;
				int insert = index;
				while (insert > 0 && distances[insert - 1] > distance)
				{
					order[insert] = order[insert - 1];
					distances[insert] = distances[insert - 1];
					insert--;
				}
				order[insert] = index;
				distances[insert] = distance;
			}
			for (int i = 0; i < 256; i++)
			{
				int x = order[i] & 15, z = order[i] >> 4;
				Mesh mesh = chunkMeshGenerator.GetMesh(chunkMeshTypes[x + z * 16]);
				if (mesh == null) continue;
				GameObject chunk = Instantiate(chunkRendererPrefab, transform);
				chunk.transform.localPosition = new Vector3(x * 32 - 256, 0, z * 32 - 256);
				chunk.GetComponentInChildren<MeshFilter>().sharedMesh = mesh;
				if (worldMaterial != null) chunk.GetComponentInChildren<Renderer>().sharedMaterial = worldMaterial;
			}
		}

		private void ClearTextures()
		{
			voxelWorld.ClearUpdateZones();
			voxelWorld.initializationSource = CustomRenderTextureInitializationSource.TextureAndColor;
			voxelWorld.initializationTexture = null;
			voxelWorld.initializationColor = Color.clear;
			voxelWorld.Update();
			worldOptimizer.Update();
		}

		public void SetSeed(int seed)
		{
			DebugLog("seed: " + seed);

			Random.InitState(seed);

			chunkTerrainGeneratorMaterial.SetInt("_Seed", Random.Range(int.MinValue, int.MaxValue));

			chunkGeneratorMaterial.SetInt("_Seed", Random.Range(int.MinValue, int.MaxValue));
			chunkGeneratorMaterial.SetVector("_CavesOffset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_Ore1Offset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_Ore2Offset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_Ore3Offset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_DirtOffset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_GravelOffset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_GraniteOffset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_AndesiteOffset", RandomOffset());
			chunkGeneratorMaterial.SetVector("_DioriteOffset", RandomOffset());

			foreach (var genLayer in genLayers)
				genLayer.SetInt("_Seed", Random.Range(0, int.MaxValue));

			foreach (var texture in genTextures)
				texture.Update();

			SendCustomEventDelayedFrames(nameof(FinishBiomeGeneration), 2);
		}

		public void FinishBiomeGeneration()
		{
			// Give the biome CRT pass a couple of frames to finish, then start
			// the next GPU pipeline stage.
			worldGenerator.enabled = true;
			blockSelector.enabled = true;


			InitializeChunkMeshes();


			enabled = false;
		}

		private Vector3 RandomOffset()
		{
			return new Vector3(
				Random.Range(-10000, 10000),
				Random.Range(-10000, 10000),
				Random.Range(-10000, 10000));
		}

		private void DebugLog(string msg)
		{
			Debug.Log("<color=#0000ff><<<</color><color=#229911>MCGE</color><color=#0000ff>>>></color> " + msg);
		}
		private void DebugWar(string msg)
		{
			Debug.Log("<color=#00ff00><<<</color><color=#229911>MCGE</color><color=#00ff00>>>></color> " + msg);
		}
		private void DebugErr(string msg)
		{
			Debug.Log("<color=#ff0000><<<</color><color=#229911>MCGE</color><color=#ff0000>>>></color> " + msg);
		}
	}
}

/*#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(VRC_MINE.World.GenLayerController))]
public class GenLayerControllerEditor : UnityEditor.Editor
{
	private bool showMeshTypes;
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		// Draw the normal fields from the same serialized snapshot as the grid.
		// Calling DrawDefaultInspector before Update() could leave a second
		// SerializedObject state alive and make merely opening the inspector mark
		// the scene dirty.
		DrawPropertiesExcluding(serializedObject, "chunkMeshTypes");
		showMeshTypes = UnityEditor.EditorGUILayout.Foldout(showMeshTypes, "Chunk Mesh Types (16 x 16)");
		if (showMeshTypes)
		{
			UnityEditor.SerializedProperty array = serializedObject.FindProperty("chunkMeshTypes");
			for (int z = 15; z >= 0; z--)
			{
				UnityEditor.EditorGUILayout.BeginHorizontal();
				for (int x = 0; x < 16; x++)
				{
					int index = x + z * 16;
					UnityEditor.SerializedProperty element = array.GetArrayElementAtIndex(index);
					VRC_MINE.World.ChunkMeshType type = (VRC_MINE.World.ChunkMeshType)element.enumValueIndex;
					Color old = GUI.backgroundColor;
					Color[] colors = { Color.white, new Color(.7f, 1, 1), new Color(1, .7f, 1), new Color(.7f, .7f, 1), Color.cyan, Color.magenta, new Color(0, .7f, 1), new Color(.7f, 0, 1), Color.blue, Color.black };
					GUI.backgroundColor = colors[(int)type];
					int selected = (int)(VRC_MINE.World.ChunkMeshType)UnityEditor.EditorGUILayout.EnumPopup(type, GUILayout.Width(55));
					if (selected != element.enumValueIndex)
						element.enumValueIndex = selected;
					GUI.backgroundColor = old;
				}
				UnityEditor.EditorGUILayout.EndHorizontal();
			}
		}
		if (serializedObject.hasModifiedProperties)
			serializedObject.ApplyModifiedProperties();
	}
}
#endif*/
