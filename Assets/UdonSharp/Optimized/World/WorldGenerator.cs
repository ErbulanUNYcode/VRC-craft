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
		private MineStructure[] biomesData;
		private Vector2Int[] biomesIndex;
		[SerializeField] private Material[] genLayers;
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

		#region systems
		[SerializeField] private GamePlay.BlockSelector blockSelector;
		#endregion

		[HideInInspector]
		[SerializeField]
		private ChunkMeshType[] chunkMeshTypes = new ChunkMeshType[256];

		private VRCPlayerApi localPlayer;
		private MeshFilter[] meshFilters = new MeshFilter[256];

		#region textures
		private Texture2D worldTexture;
		private Texture2D lightTexture;
		private Texture2D ctrlTexture;
		private Texture2D convertor;
		#endregion

		private Vector3Int oldPos = Vector3Int.down;

		#region generators
		[SerializeField] private CustomRenderTexture optimizator;
		[SerializeField] private CustomRenderTexture chunkGenerator;
		[SerializeField] private CustomRenderTexture chunkTerrainGenerator;
		[SerializeField] private ChunkMeshGenerator chunkMeshGenerator;
		#endregion

		private int currentIndex;
		int lastFrameCount = 0;
		private Vector2Int readingChunk;
		private Vector2Int renderingChunk;
		private Vector2Int updateChunk = Vector2Int.left;
		private bool chunkReading;
		private bool chunkRendering;
		private Vector2Int worldPos;

		private const string debugLogo = "<color=#00ff00><<<</color><color=#0000ff>MCGE</color><color=#00ff00>>>></color>  ";
		private const string debugWarningLogo = "<color=#00ff00><<<</color><color=#ff0000>MCGE</color><color=#00ff00>>>></color>  ";

		[UdonSynced] int seed = int.MinValue;

		#region cache
		private Collider[] colliders = new Collider[36];
		private Vector2Int[] initPositions = new Vector2Int[1_024];
		private Color[] clearColor = new Color[32_768];//0
		private Color[] updateColor = new Color[384];//0.5
		private Color[] ignoreColor = new Color[384];//1
		private byte[] px = new byte[65536];
		#endregion

		private void Start()
		{
			InitWorld();
			enabled = false;
			if (Networking.IsOwner(gameObject) && seed == int.MinValue)
			{
				seed = Random.Range(0, int.MaxValue);
				RequestSerialization();
				StartBiomeGenerator();
			}
		}

		private void InitWorld()
		{
			#region base
			localPlayer = Networking.LocalPlayer;
			localPlayer.SetGravityStrength(3);
			for (int i = 0; i < 1024; i++) initPositions[i] = Vector2Int.one * 10_000_000;
			for (int i = 0; i < 384; i++)
			{
				updateColor[i].r = 0.5f;
				ignoreColor[i].r = 1;
			}
			#endregion

			#region biomes/structures
			var biomes = dataGameObject.GetComponents<MineBiome>();
			Debug.LogWarning(debugLogo + biomes.Length + " biomes found");
			var oldCount = 0;
			biomesData = new MineStructure[0];
			biomesIndex = new Vector2Int[biomes.Length];
			for (int i = 0; i < biomes.Length; i++)
			{
				var biome = biomes[i];
				biomesData = biome.SplitThis(biomesData);
				biomesIndex[i] = new Vector2Int(oldCount, biomesData.Length - oldCount);
				oldCount = biomesData.Length;
			}
			#endregion

			#region cameras
			shadowCam.SetReplacementShader(replacementShader, "RenderType");
			shadowCam1.SetReplacementShader(replacementShader, "RenderType");
			var cam = VRCCameraSettings.ScreenCamera;
			/*var masks = cam.CullingMask;
			masks.value ^= 1 << 27;
			cam.CullingMask = masks.value;*/
			cam.FarClipPlane = 400;
			cam.AllowMSAA = false;
			#endregion

			#region colliders
			for (int i = 0; i < 36; i++)
			{
				colliders[i] = Instantiate(colliderPrefab, collidersParent).GetComponent<Collider>();
				colliders[i].transform.localPosition = new Vector3(i % 3 - 1, i / 9 - 1, (i / 3) % 3 - 1);
			}
			#endregion

			#region meshes
			chunkMeshGenerator.Generate();
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
			#endregion

			#region textures
			convertor = new Texture2D(256, 128, TextureFormat.R8, false, true);
			convertor.filterMode = FilterMode.Point;

			lightTexture = new Texture2D(8192, 4096, TextureFormat.R8, false, true);
			lightTexture.LoadRawTextureData(new byte[lightTexture.width * lightTexture.height]);
			lightTexture.Apply();

			worldTexture = new Texture2D(8192, 4096, TextureFormat.R8, false, true);
			worldTexture.LoadRawTextureData(new byte[worldTexture.width * worldTexture.height]);
			worldTexture.Apply();

			ctrlTexture = new Texture2D(512, 192, TextureFormat.R8, false, true);
			ctrlTexture.filterMode = FilterMode.Point;
			ctrlTexture.LoadRawTextureData(new byte[ctrlTexture.width * ctrlTexture.height]);
			ctrlTexture.Apply();
			#endregion

			#region materials
			worldShadowMaterial.SetTexture("_MainTex", worldTexture);
			optimizatorMaterial.SetTexture("_WorldTex", worldTexture);
			optimizatorMaterial.SetTexture("_CtrlTex", ctrlTexture);
			worldMaterial.SetTexture("_LightTex", lightTexture);
			worldMaterial.SetTexture("_MainTex", worldTexture);
			#endregion

			blockSelector.SetWorldTexture(worldTexture);
		}

		public override void OnDeserialization() { SendCustomEventDelayedFrames(nameof(StartBiomeGenerator), 2); }
		private void StartBiomeGenerator()
		{
			Debug.Log(debugLogo + "seed is " + seed);
			Random.InitState(seed);
			chunkTerrainGeneratorMaterial.SetInt("_Seed", seed);
			chunkGeneratorMaterial.SetInt("_Seed", seed);
			chunkGeneratorMaterial.SetVector("_CavesOffset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_Ore1Offset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_Ore2Offset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_Ore3Offset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_DirtOffset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_GravelOffset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_GraniteOffset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_AndesiteOffset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			chunkGeneratorMaterial.SetVector("_DioriteOffset", new Vector3(Random.Range(-10000, 10000), Random.Range(-10000, 10000), Random.Range(-10000, 10000)));
			foreach (var genLayer in genLayers) genLayer.SetInt("_Seed", Random.Range(0, int.MaxValue));
			foreach (var texture in genTextures) texture.Initialize();
			SendCustomEventDelayedFrames(nameof(FinishBiomeGenerator), 2);
		}
		public void FinishBiomeGenerator()
		{
			enabled = true;

			SetMatData();
			VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
		}

		private void StartGeneration()
		{
			/*var pos = chunksQueueData[currentIndex] + worldPos;
			while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos || renderingChunk == pos || readingChunk == pos)
			{
				currentIndex++;
				pos = chunksQueueData[currentIndex] + worldPos;
			}
			chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
			chunkTerrainGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
			GenerateChunk();*/
		}

		private bool TryNext()
		{
			var pos = chunksQueueData[currentIndex] + worldPos;

			while (currentIndex < chunksQueueData.Length)
			{
				pos = chunksQueueData[currentIndex] + worldPos;
				if (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos || renderingChunk == pos || readingChunk == pos)
				{
					currentIndex++;
				}
				else
				{
					return true;
				}
			}
			return false;
		}

		private void SetMatData()
		{
			var queuePos = chunksQueueData[currentIndex];
			var pos = queuePos + worldPos;
			var miniGenMaterial = miniGenTexture.material;
			renderingChunk = pos;
			pos *= 16;
			if (miniGenMaterial.GetInt("_OffsetX") != 5053 + worldPos.x * 4 || miniGenMaterial.GetInt("_OffsetY") != 5053 + worldPos.y * 4)
			{
				miniGenTexture.material.SetInt("_OffsetX", 5053 + worldPos.x * 4);
				miniGenTexture.material.SetInt("_OffsetY", 5053 + worldPos.y * 4);
				miniGenTexture.Initialize();
			}
			chunkGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
			chunkGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
			chunkTerrainGeneratorMaterial.SetInt("_ReadOffsetX", queuePos.x << 4);
			chunkTerrainGeneratorMaterial.SetInt("_ReadOffsetY", queuePos.y << 4);
			chunkTerrainGenerator.Initialize();
			chunkGenerator.Initialize();
			lastFrameCount = Time.frameCount;
			chunkRendering = true;
		}

		public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
		{
			/*if (request.hasError)
			{
				Debug.LogError(debugLogo + "GPU readback error!");
				lastFrameCount = Time.frameCount;
				VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
				return;
			}*/


			if (chunkReading)
			{
				if (!request.TryGetData(px)) return;
				convertor.LoadRawTextureData(px);
				var data = convertor.GetPixels();
				worldTexture.SetPixels(readingChunk.x << 8 & 8191, readingChunk.y << 7 & 4095, 256, 128, data);
				worldTexture.Apply();
				updateChunk = new Vector2Int(readingChunk.x & 31, readingChunk.y & 31);
			}

			if (chunkRendering)
			{
				chunkRendering = false;
				readingChunk = renderingChunk;
				chunkReading = true;
				if (TryNext()) SetMatData();
				VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
			}






			/*if (readingChunk == -1)
			{
				miniGenTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
				optimizator.initializationSource = CustomRenderTextureInitializationSource.Material;
				optimizator.initializationMode = CustomRenderTextureUpdateMode.Realtime;

				readingChunk = renderingChunk;

				do
				{
					renderingChunk++;
					if (renderingChunk == chunksQueueData.Length) break;
					pos = chunksQueueData[renderingChunk] + worldPos;
				} while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos && renderingChunk < chunksQueueData.Length);

				SetMatData();
				return;
			}*/


			/*if (renderingChunk == -1)
			{
				StartGeneration();
				return;
			}*/




			//miniTex.Apply();
			/*var mpx = pos.x >> 1 & 15;
			var mpy = pos.y >> 1 & 15;*/
			/*if (clearTexture.GetPixel(mpx, mpy).r == 1)
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
			}*/

			/*optimizatorMaterial.SetInt("_ChunkX", pos.x & 31);
			optimizatorMaterial.SetInt("_ChunkY", pos.y & 31);
			initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] = pos;*/

			//if (readingChunk == chunksQueueData.Length)
			{
				Debug.Log(debugLogo + "World generation complete!");
				/*chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
				chunkTerrainGenerator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;*/
				//optimizator.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
				return;
			}

			/*if (renderingChunk + 1 == chunksQueueData.Length)
			{
				renderingChunk++;
				VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
				return;
			}
			do
			{
				renderingChunk++;
				pos = chunksQueueData[renderingChunk] + new Vector2Int((int)transform.position.x >> 4, (int)transform.position.z >> 4);
			} while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos && renderingChunk < chunksQueueData.Length - 1);
			SetMatData();*/
		}

		private byte GetBlock(Vector3Int pos)
		{
			if (pos.y > 127 || pos.y < 0) return 0;
			var ch = new Vector2Int(((pos.x >> 4) & 31) << 8, ((pos.z >> 4) & 31) << 7);
			return ((Color32)worldTexture.GetPixel(ch.x + (pos.x & 15) + ((pos.z & 15) << 4), ch.y + pos.y)).r;
		}



		private void Update()
		{
			/*if (updateChunk != Vector2Int.left)
			{
				ctrlTexture.SetPixels(updateChunk.x *
			}*/

			shadowCam1.enabled = false;
			if ((Time.frameCount & 20) == 0) shadowCam1.enabled = true;
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
				/*if (readingChunk == chunksQueueData.Length)
				{
					StartGeneration();
				}
				else
					renderingChunk = -1;*/

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
							//clearTexture.SetPixels(i, 0, 1, 16, clearOptimizatorColor);
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
							//clearTexture.SetPixels(0, i, 16, 1, clearOptimizatorColor);
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
					//clearTexture.Apply();
					optimizator.initializationMode = CustomRenderTextureUpdateMode.Realtime;
				}
			}

			#endregion
		}

		private void OnDestroy()
		{
			//destroy textures
			Destroy(worldTexture);
			Destroy(lightTexture);
			Destroy(convertor);
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
		group.properties.Add(serializedObject.FindProperty("seed"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Systems", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("blockSelector"));
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