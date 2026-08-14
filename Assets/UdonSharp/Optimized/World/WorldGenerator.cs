using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC_MINE.World;

namespace VRC_MINE.World
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class WorldGenerator : UdonSharpBehaviour
	{
		#region biomes	
		[SerializeField] private GameObject dataGameObject;
		private MineBiome[] biomes;
		private MineStructure[] structures;
		[SerializeField] private Material[] genLayers;
		[SerializeField] private Material continentsLayer;
		[SerializeField] private CustomRenderTexture[] genTextures;
		[SerializeField] private CustomRenderTexture miniGenTexture;
		#endregion

		#region prefabs
		[SerializeField] private GameObject chunkRendererPrefab;
		[SerializeField] private GameObject colliderPrefab;
		#endregion

		#region objects
		[SerializeField] private Transform collidersParent;
		#endregion

		[HideInInspector]
		[SerializeField] private Vector2Int[] chunksQueueData;

		#region materials
		[SerializeField] private Material worldMaterial;
		[SerializeField] private Material worldShadowMaterial;
		[SerializeField] private Material optimizatorMaterial;
		[SerializeField] private Material chunkGeneratorMaterial;
		[SerializeField] private Material chunkTerrainGeneratorMaterial;
		#endregion

		#region shadows
		[SerializeField] private Transform shadowControl;
		[SerializeField] private Camera shadowCam;
		[SerializeField] private Camera shadowCam1;
		[SerializeField] private Shader replacementShader;
		#endregion

		[HideInInspector]
		[SerializeField]
		private ChunkMeshType[] chunkMeshTypes = new ChunkMeshType[256];

		private VRCPlayerApi localPlayer;
		private MeshFilter[] meshFilters = new MeshFilter[256];

		#region textures
		private Texture2D worldTexture;
		private Texture2D lightTexture;
		private Texture2D clearTexture;
		private Texture2D miniTex;
		#endregion

		private Collider[] colliders = new Collider[36];
		private Vector3Int oldPos = Vector3Int.down;

		#region generators
		[SerializeField] private CustomRenderTexture optimizator;
		[SerializeField] private CustomRenderTexture chunkGenerator;
		[SerializeField] private CustomRenderTexture chunkTerrainGenerator;
		[SerializeField] private ChunkMeshGenerator chunkMeshGenerator;
		#endregion


		private int prevChunk;
		private int currentChunk;
		private Vector2Int[] initPositions = new Vector2Int[1_024];
		private Vector2Int worldPos;
		private Color[] clearChunkColor = new Color[32_768];
		private Color[] clearOptimizatorColor = new Color[16];
		private const string debugLogo = "<color=#00ff00><<<</color><color=#0000ff>MCGE</color><color=#00ff00>>>></color>  ";
		private const string debugWarningLogo = "<color=#00ff00><<<</color><color=#ff0000>MCGE</color><color=#00ff00>>>></color>  ";
		[UdonSynced] int seed = int.MinValue;

		private void Start()
		{
			biomes = dataGameObject.GetComponents<MineBiome>();
			Debug.LogWarning(debugLogo + biomes.Length + " biomes found");
			structures = dataGameObject.GetComponentsInChildren<MineStructure>();
			Debug.LogWarning(debugLogo + structures.Length + " structures found");
			enabled = false;
			localPlayer = Networking.LocalPlayer;
			InitWorld();
			if (!Networking.IsOwner(gameObject)) return;


			if (seed == int.MinValue)
			{
				seed = Random.Range(int.MinValue + 1, int.MaxValue);
				RequestSerialization();
				StartBiomeGenerator();
			}
		}

		public override void OnDeserialization()
		{
			StartBiomeGenerator();
		}

		private void StartBiomeGenerator()
		{
			Debug.Log(debugLogo + "seed is " + seed);
			Random.InitState(seed);
			continentsLayer.SetInt("_Seed", seed);
			chunkTerrainGeneratorMaterial.SetInt("_Seed", seed);
			foreach (var genLayer in genLayers)
			{
				genLayer.SetInt("_Seed", Random.Range(0, int.MaxValue));
			}

			foreach (var texture in genTextures)
			{
				texture.initializationMode = CustomRenderTextureUpdateMode.Realtime;
			}
			SendCustomEventDelayedFrames(nameof(FinishBiomeGenerator), 2);
		}

		public void FinishBiomeGenerator()
		{
			foreach (var texture in genTextures)
			{
				texture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			}

			enabled = true;

			ReStartGeneration();
		}

		void StartSystem()
		{

			Debug.Log(debugLogo + "World generator with " + 32 + "x" + 32 + " chunks started!");
		}

		private void InitWorld()
		{
			#region init
			for (int i = 0; i < 16; i++) clearOptimizatorColor[i] = Color.red;

			shadowCam.SetReplacementShader(replacementShader, "RenderType");
			shadowCam1.SetReplacementShader(replacementShader, "RenderType");
			var cam = VRCCameraSettings.ScreenCamera;
			/*var masks = cam.CullingMask;
			masks.value ^= 1 << 27;
			cam.CullingMask = masks.value;*/
			cam.FarClipPlane = 400;

			for (int i = 0; i < 1024; i++) initPositions[i] = Vector2Int.one * 10_000_000;

			optimizator.initializationSource = CustomRenderTextureInitializationSource.TextureAndColor;
			optimizator.initializationColor = Color.clear;
			optimizator.Initialize();
			chunkMeshGenerator.CustomStart();
			localPlayer.SetGravityStrength(3);

			miniTex = new Texture2D(256, 128, TextureFormat.R8, false, true);
			miniTex.filterMode = FilterMode.Point;
			for (int i = 0; i < 36; i++)
			{
				colliders[i] = Instantiate(colliderPrefab, collidersParent).GetComponent<Collider>();
				colliders[i].transform.localPosition = new Vector3(i % 3 - 1, i / 9 - 1, (i / 3) % 3 - 1);
			}

			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					var mesh = chunkMeshGenerator.GetMesh(chunkMeshTypes[i + j * 16]);
					if (mesh == null) continue;
					var chunk = Instantiate(chunkRendererPrefab, transform).transform;
					chunk.localPosition = new Vector3(-256 + i * 32, 0, -256 + j * 32);
					meshFilters[i * 16 + j] = chunk.GetComponent<MeshFilter>();
					meshFilters[i * 16 + j].mesh = mesh;
				}
			}

			if (worldTexture != null) Destroy(worldTexture);

			lightTexture = new Texture2D(8192, 4096, TextureFormat.RG16, false, true);
			lightTexture.LoadRawTextureData(new byte[lightTexture.width * lightTexture.height * 2]);
			lightTexture.Apply();
			worldMaterial.SetTexture("_LightTex", lightTexture);

			worldTexture = new Texture2D(8192, 4096, TextureFormat.R8, false, true);
			worldTexture.LoadRawTextureData(new byte[worldTexture.width * worldTexture.height]);
			worldTexture.Apply();
			worldMaterial.SetTexture("_MainTex", worldTexture);
			worldMaterial.SetTexture("_MainTex2", worldTexture);
			worldShadowMaterial.SetTexture("_MainTex", worldTexture);
			optimizatorMaterial.SetTexture("_WorldTex", worldTexture);
			clearTexture = new Texture2D(16, 16, TextureFormat.R8, false, true);
			clearTexture.filterMode = FilterMode.Point;
			clearTexture.LoadRawTextureData(new byte[clearTexture.width * clearTexture.height]);
			clearTexture.Apply();
			optimizatorMaterial.SetTexture("_ClearTex", clearTexture);
			#endregion
		}
		int lastFrameCount = 0;
		private void GenerateChunk()
		{
			var pos = chunksQueueData[currentChunk] * 16 + worldPos * 16;
			lastFrameCount = Time.frameCount;
			VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
			chunkGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
			chunkGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
			chunkTerrainGeneratorMaterial.SetInt("_ReadOffsetX", chunksQueueData[currentChunk].x << 4);
			chunkTerrainGeneratorMaterial.SetInt("_ReadOffsetY", chunksQueueData[currentChunk].y << 4);
			chunkTerrainGenerator.Initialize();
			SendCustomEventDelayedFrames(nameof(GenerateChunkB), 1);
		}
		public void GenerateChunkB()
		{
			chunkGenerator.Initialize();
		}

		private byte[] px = new byte[65536];
		public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
		{
			ManualOnAsyncGpuReadbackComplete(request);
		}
		private VRCAsyncGPUReadbackRequest lastRequest;
		public void ManualOnAsyncGpuReadbackCompleteDelay()
		{
			Debug.Log(debugLogo + "GPU readback complete delay");
			ManualOnAsyncGpuReadbackComplete(lastRequest);
		}
		private void ManualOnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
		{
			if (Time.frameCount - lastFrameCount < 2)
			{
				lastRequest = request;
				SendCustomEventDelayedFrames(nameof(ManualOnAsyncGpuReadbackCompleteDelay), 1);
				return;
			}

			if (request.hasError)
			{
				Debug.LogError(debugLogo + "GPU readback error!");
				lastFrameCount = Time.frameCount;
				VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
				return;
			}

			Vector2Int pos;
			if (prevChunk == -1)
			{
				miniGenTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
				optimizator.initializationSource = CustomRenderTextureInitializationSource.Material;
				optimizator.initializationMode = CustomRenderTextureUpdateMode.Realtime;

				prevChunk = currentChunk;

				do
				{
					currentChunk++;
					if (currentChunk == chunksQueueData.Length) break;
					pos = chunksQueueData[currentChunk] + worldPos;
				} while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos && currentChunk < chunksQueueData.Length);

				GenerateChunk();
				return;
			}


			if (currentChunk == -1)
			{
				ReStartGeneration();
				return;
			}

			if (!request.TryGetData(px)) return;

			miniTex.LoadRawTextureData(px);
			//miniTex.Apply();
			pos = chunksQueueData[prevChunk] + worldPos;
			var data = miniTex.GetPixels();

			if (pos == new Vector2Int(-6, -1))
				data[14669] = new Color32(15, 100, 200, 0);
			var mpx = pos.x >> 1 & 15;
			var mpy = pos.y >> 1 & 15;
			if (clearTexture.GetPixel(mpx, mpy).r == 1)
			{
				clearTexture.SetPixel(pos.x >> 1 & 15, pos.y >> 1 & 15, Color.clear);
				clearTexture.Apply();
				if ((pos.x & 31) != mpx << 1 || (pos.y & 31) != mpy << 1)
					worldTexture.SetPixels(mpx << 9, mpy << 8, 256, 128, clearChunkColor);

				if ((pos.x & 31) != (mpx << 1) + 1 || (pos.y & 31) != mpy << 1)
					worldTexture.SetPixels((mpx << 9) + 256, mpy << 8, 256, 128, clearChunkColor);

				if ((pos.x & 31) != mpx << 1 || (pos.y & 31) != (mpy << 1) + 1)
					worldTexture.SetPixels(mpx << 9, (mpy << 8) + 128, 256, 128, clearChunkColor);

				if ((pos.x & 31) != (mpx << 1) + 1 || (pos.y & 31) != (mpy << 1) + 1)
					worldTexture.SetPixels((mpx << 9) + 256, (mpy << 8) + 128, 256, 128, clearChunkColor);
			}
			worldTexture.SetPixels(pos.x << 8 & 8191, pos.y << 7 & 4095, 256, 128, data);
			worldTexture.Apply();
			optimizatorMaterial.SetInt("_ChunkX", pos.x & 31);
			optimizatorMaterial.SetInt("_ChunkY", pos.y & 31);
			initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] = pos;


			prevChunk = currentChunk;

			if (prevChunk == chunksQueueData.Length)
			{
				Debug.Log(debugLogo + "World generation complete!");
				/*chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
				chunkTerrainGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;*/
				optimizator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
				return;
			}

			if (currentChunk + 1 == chunksQueueData.Length)
			{
				currentChunk++;
				VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
				return;
			}
			do
			{
				currentChunk++;
				pos = chunksQueueData[currentChunk] + new Vector2Int((int)transform.position.x >> 4, (int)transform.position.z >> 4);
			} while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos && currentChunk < chunksQueueData.Length - 1);
			GenerateChunk();
		}

		private byte GetBlock(Vector3Int pos)
		{
			if (pos.y > 127 || pos.y < 0) return 0;
			var ch = new Vector2Int(((pos.x >> 4) & 31) << 8, ((pos.z >> 4) & 31) << 7);
			return ((Color32)worldTexture.GetPixel(ch.x + (pos.x & 15) + ((pos.z & 15) << 4), ch.y + pos.y)).r;
		}

		private void Update()
		{
			shadowCam1.enabled = false;
			var frame = Time.frameCount;
			if (prevChunk < chunksQueueData.Length / 4 && (frame & 31) == 0) shadowCam1.enabled = true;
			var pos = Vector3Int.FloorToInt(localPlayer.GetPosition() + localPlayer.GetVelocity() * Time.deltaTime + Vector3.up * 0.5f);
			if (oldPos == pos) return;

			var oldCamPos = shadowCam.transform.position;
			var oldCamPos1 = shadowCam1.transform.position;

			shadowControl.transform.localPosition = pos;

			shadowCam.transform.position = oldCamPos;
			shadowCam.transform.position = shadowCam.transform.TransformPoint(Vector3Int.RoundToInt(shadowCam.transform.InverseTransformPoint(shadowControl.transform.TransformPoint(new Vector3(0, 0, -327.68f)))));
			shadowCam1.transform.position = oldCamPos1;
			shadowCam1.transform.position = shadowCam1.transform.TransformPoint(Vector3Int.RoundToInt(shadowCam1.transform.InverseTransformPoint(shadowControl.transform.TransformPoint(new Vector3(0, 0, -327.68f)))));
			var shadowCam1Offset = shadowCam.transform.InverseTransformPoint(shadowCam1.transform.position);
			shadowCam1Offset.x /= 1000;
			shadowCam1Offset.y /= 1000;
			shadowCam1Offset.z /= 65536;
			worldMaterial.SetVector("_ShadowLodOffset", shadowCam1Offset);
			worldMaterial.SetMatrix("_ShadowMatrix", shadowCam.projectionMatrix * shadowCam.worldToCameraMatrix);
			shadowCam1.enabled = true;
			oldPos = pos - oldPos;


			#region Colliders
			if (Mathf.Abs(oldPos.x) > 2 || Mathf.Abs(oldPos.z) > 2 || Mathf.Abs(oldPos.y) > 3)
			{
				foreach (var collider in colliders)
				{
					var cPos = Vector3Int.RoundToInt(collider.transform.localPosition);
					cPos += oldPos;
					collider.transform.localPosition = cPos;
					collider.enabled = GetBlock(cPos) != 0;
				}
			}
			else
			{
				foreach (var collider in colliders)
				{
					var cPos = Vector3Int.RoundToInt(collider.transform.localPosition);
					if (cPos.x < pos.x - 1) cPos.x += 3;
					else if (cPos.x > pos.x + 1) cPos.x -= 3;
					if (cPos.z < pos.z - 1) cPos.z += 3;
					else if (cPos.z > pos.z + 1) cPos.z -= 3;
					if (cPos.y < pos.y - 1) cPos.y += 4;
					else if (cPos.y > pos.y + 2) cPos.y -= 4;
					collider.transform.localPosition = cPos;
					collider.enabled = GetBlock(cPos) != 0;
				}
			}
			oldPos = pos;
			#endregion

			#region chunk step
			var p = Vector3Int.RoundToInt(transform.position);
			var oldP = p;
			if (pos.x > p.x + 32)
			{

				p.x = pos.x >> 5 << 5;
				if (pos.z > p.z + 16)
					p.z = (pos.z >> 5 << 5) + 32;
				else if (pos.z < p.z - 16)
					p.z = pos.z >> 5 << 5;
			}
			else if (pos.x < p.x - 32)
			{
				p.x = (pos.x >> 5 << 5) + 32;
				if (pos.z > p.z + 16)
					p.z = (pos.z >> 5 << 5) + 32;
				else if (pos.z < p.z - 16)
					p.z = pos.z >> 5 << 5;
			}

			if (pos.z > p.z + 32)
			{
				p.z = pos.z >> 5 << 5;
				if (pos.x > p.x + 16)
					p.x = (pos.x >> 5 << 5) + 32;
				else if (pos.x < p.x - 16)
					p.x = pos.x >> 5 << 5;
			}
			else if (pos.z < p.z - 32)
			{
				p.z = pos.z >> 5 << 5;
				if (pos.x > p.x + 16)
					p.x = (pos.x >> 5 << 5) + 32;
				else if (pos.x < p.x - 16)
					p.x = pos.x >> 5 << 5;
			}

			if (p != oldP)
			{
				transform.position = p;
				worldPos = new Vector2Int(Mathf.FloorToInt(transform.position.x) >> 4, Mathf.FloorToInt(transform.position.z) >> 4);
				if (prevChunk == chunksQueueData.Length)
				{
					ReStartGeneration();
				}
				else
					currentChunk = -1;

				var changed = false;
				int id1, id2;
				if (p.x != oldP.x)
				{
					changed = true;
					p.x = (p.x >> 5) - 8;
					oldP.x = (oldP.x >> 5) - 8;
					for (int i = 0; i < 16; i++)
					{
						id1 = i;
						if (i < (p.x & 15)) id1 += 16;
						id1 += p.x >> 4 << 4;

						id2 = i;
						if (i < (oldP.x & 15)) id2 += 16;
						id2 += oldP.x >> 4 << 4;

						if (id1 != id2)
						{
							clearTexture.SetPixels(i, 0, 1, 16, clearOptimizatorColor);
							for (int j = 0; j < 32; j++)
							{
								initPositions[i * 2 + j * 32].x++;
								initPositions[i * 2 + j * 32 + 1].x++;
							}
						}
					}
				}

				if (p.z != oldP.z)
				{
					changed = true;
					p.z = (p.z >> 5) - 8;
					oldP.z = (oldP.z >> 5) - 8;
					for (int i = 0; i < 16; i++)
					{
						id1 = i;
						if (i < (p.z & 15)) id1 += 16;
						id1 += p.z >> 4 << 4;

						id2 = i;
						if (i < (oldP.z & 15)) id2 += 16;
						id2 += oldP.z >> 4 << 4;

						if (id1 != id2)
						{
							clearTexture.SetPixels(0, i, 16, 1, clearOptimizatorColor);
							for (int j = 0; j < 32; j++)
							{
								initPositions[j + i * 64].y++;
								initPositions[j + i * 64 + 32].y++;
							}
						}
					}
				}

				if (changed)
				{
					clearTexture.Apply();
					optimizator.initializationMode = CustomRenderTextureUpdateMode.Realtime;
				}
			}

			#endregion
		}

		private void ReStartGeneration()
		{
			genLayers[8].SetInt("_OffsetX", 5053 + worldPos.x * 4);
			genLayers[8].SetInt("_OffsetY", 5053 + worldPos.y * 4);
			miniGenTexture.initializationMode = CustomRenderTextureUpdateMode.Realtime;
			currentChunk = 0;
			var pos = chunksQueueData[currentChunk] + worldPos;
			while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos)
			{
				currentChunk++;
				pos = chunksQueueData[currentChunk] + worldPos;
			}
			/*chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
			chunkTerrainGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;*/
			prevChunk = -1;
			GenerateChunk();
		}

		private void OnDestroy()
		{
			//destroy textures
			Destroy(worldTexture);
			Destroy(lightTexture);
			Destroy(miniTex);
		}

		/*
		[ContextMenu("Generate queue")]
		public void GenerateQueue()
		{
			var v = new Vector3[1024];
			for (int i = 0; i < 32; i++)
			{
				for (int j = 0; j < 32; j++)
				{
					v[i * 32 + j] = new Vector3(i - 16, j - 16, new Vector2(i - 15.5f, j - 15.5f).sqrMagnitude);
				}
			}

			System.Array.Sort(v, (a, b) => a.z.CompareTo(b.z));

			chunksQueueData = new Vector2Int[1024];
			for (int i = 0; i < 1024; i++) chunksQueueData[i] = Vector2Int.RoundToInt((Vector2)v[i]);
		}*/
	}

	public enum ChunkMeshType { PP, MP, PM, MM, EP, PE, EM, ME, EE, NN }
}



//show chunkMeshTypes grid 16x16
#if UNITY_EDITOR
[CustomEditor(typeof(WorldGenerator))]
public class ChunkMeshGeneratorEditor : Editor
{
	Color GetColor(ChunkMeshType type)
	{
		switch (type)
		{
			case ChunkMeshType.PP: return new Color(1.0f, 1.0f, 1.0f);
			case ChunkMeshType.MP: return new Color(0.7f, 1.0f, 1.0f);
			case ChunkMeshType.PM: return new Color(1.0f, 0.7f, 1.0f);
			case ChunkMeshType.MM: return new Color(0.7f, 0.7f, 1.0f);

			case ChunkMeshType.EP: return new Color(0.0f, 1.0f, 1.0f);
			case ChunkMeshType.PE: return new Color(1.0f, 0.0f, 1.0f);
			case ChunkMeshType.EM: return new Color(0.0f, 0.7f, 1.0f);
			case ChunkMeshType.ME: return new Color(0.7f, 0.0f, 1.0f);

			case ChunkMeshType.EE: return new Color(0.0f, 0.0f, 1.0f);
		}

		return Color.black;
	}

	private List<FolderGrup> folderGrups = new List<FolderGrup>();

	private void OnEnable()
	{
		var group = new FolderGrup() { name = "BiomeGenerator", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("dataGameObject"));
		group.properties.Add(serializedObject.FindProperty("genLayers"));
		group.properties.Add(serializedObject.FindProperty("continentsLayer"));
		group.properties.Add(serializedObject.FindProperty("genTextures"));
		group.properties.Add(serializedObject.FindProperty("miniGenTexture"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Prefabs", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("chunkRendererPrefab"));
		group.properties.Add(serializedObject.FindProperty("colliderPrefab"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "SceneObjects", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("collidersParent"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Materials", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("worldMaterial"));
		group.properties.Add(serializedObject.FindProperty("worldShadowMaterial"));
		group.properties.Add(serializedObject.FindProperty("optimizatorMaterial"));
		group.properties.Add(serializedObject.FindProperty("chunkGeneratorMaterial"));
		group.properties.Add(serializedObject.FindProperty("chunkTerrainGeneratorMaterial"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Shadows", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("shadowControl"));
		group.properties.Add(serializedObject.FindProperty("shadowCam"));
		group.properties.Add(serializedObject.FindProperty("shadowCam1"));
		group.properties.Add(serializedObject.FindProperty("replacementShader"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Generators", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("optimizator"));
		group.properties.Add(serializedObject.FindProperty("chunkGenerator"));
		group.properties.Add(serializedObject.FindProperty("chunkTerrainGenerator"));
		group.properties.Add(serializedObject.FindProperty("chunkMeshGenerator"));
		folderGrups.Add(group);
	}
	bool chunkMeshTypesFold = false;
	public override void OnInspectorGUI()
	{
		//base.OnInspectorGUI();

		serializedObject.Update();
		for (int i = 0; i < folderGrups.Count; i++)
		{
			var group = folderGrups[i];
			var key = "MINE/WorldGenerator/" + group.name;
			var fold = EditorGUILayout.Foldout(EditorPrefs.GetBool(key), group.name);
			EditorPrefs.SetBool(key, fold);
			if (fold)
			{
				EditorGUI.indentLevel++;
				foreach (var property in group.properties)
				{
					EditorGUILayout.PropertyField(property);
				}
				EditorGUI.indentLevel--;
			}
		}

		chunkMeshTypesFold = EditorGUILayout.Foldout(chunkMeshTypesFold, "Chunk Mesh Types");

		if (chunkMeshTypesFold)
		{

			SerializedProperty array = serializedObject.FindProperty("chunkMeshTypes");

			if (array != null)
			{
				const int size = 16;

				if (array.arraySize != size * size)
					array.arraySize = size * size;

				for (int y = size - 1; y > -1; y--)
				{
					EditorGUILayout.BeginHorizontal();

					for (int x = 0; x < size; x++)
					{
						int index = x + y * size;
						SerializedProperty element = array.GetArrayElementAtIndex(index);

						ChunkMeshType type = (ChunkMeshType)element.enumValueIndex;

						Color oldColor = GUI.backgroundColor;
						GUI.backgroundColor = GetColor(type);

						element.enumValueIndex = (int)(ChunkMeshType)EditorGUILayout.EnumPopup(
							type,
							GUILayout.Width(55)
						);

						GUI.backgroundColor = oldColor;
					}

					EditorGUILayout.EndHorizontal();
				}
			}
		}
		serializedObject.ApplyModifiedProperties();
	}

	private struct FolderGrup
	{
		public string name;
		public List<SerializedProperty> properties;
	}
}
#endif