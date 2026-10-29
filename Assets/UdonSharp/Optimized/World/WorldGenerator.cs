using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC_MINE.World;
using Random = UnityEngine.Random;



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
		[SerializeField] private Material[] optimizatorMaterials;
		[SerializeField] private Renderer[] worldTextureDebugRenderers;
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

		private VRCPlayerApi localPlayer;

		#region textures
		private Texture2D[] worldTextures = new Texture2D[16];
		// Temporary API probe: intentionally unused, only to test UdonSharp support.
		[SerializeField] private Texture2DArray worldTextureArrayProbe;
		//private Texture2D lightTexture;
		private Texture2D[] ctrlTextures = new Texture2D[2];
		private Texture2D convertor;
		private Texture2D initTexture;
		#endregion

		// Temporary API probe. Do not call from the generation pipeline yet.
		private void ProbeTexture2DArraySupport()
		{
			//worldTextureArrayProbe = new Texture2DArray(256, 256, 16, TextureFormat.R8, false, true);
			//worldTextureArrayProbe.Apply(false, false);
		}

		private Vector3Int oldPos = Vector3Int.down;

		#region generators
		[SerializeField] private CustomRenderTexture[] optimizators;
		[SerializeField] private CustomRenderTexture chunkGenerator;
		[SerializeField] private CustomRenderTexture chunkTerrainGenerator;
		[SerializeField] private ChunkMeshGenerator chunkMeshGenerator;
		#endregion

		private int currentIndex;
		private int[] chunksQueueRadiusSq;
		private int lastGeneratedIndex = -1;
		private int renderingQueueIndex = -1;
		private bool queueSearchPrepared;
		int lastFrameCount = 0;
		private Vector2Int readingChunk;
		private Vector2Int renderingChunk;
		private Vector2Int updateChunk = Vector2Int.left;
		private bool chunkReading;
		private bool chunkRendering;
		private bool asyncWorking;
		private Vector2Int worldPos;

		private const string debugLogo = "<color=#00ff00><<<</color><color=#0000ff>MCGE</color><color=#00ff00>>>></color>  ";
		private const string debugWarningLogo = "<color=#00ff00><<<</color><color=#ff0000>MCGE</color><color=#00ff00>>>></color>  ";

		int seed;// int.MinValue;

		#region cache
		private Collider[] colliders = new Collider[36];
		private Color[] clearColor = new Color[4_096];//0
		private Color[] updateColors = new Color[16];//0.5
		private bool[] worldTextureEdited = new bool[16];
		private bool worldTexturePendingApply;
		private byte[] ignoreColors = new byte[65_536];//1
		private byte[] px = new byte[65_536];
		private Vector2Int p;
		#endregion

		private void Start()
		{
			InitWorld();
			seed = Networking.GetUniqueName(gameObject).GetHashCode();
			StartBiomeGenerator();
			// Keep the world-generation Update loop disabled while biome layers are running.
			enabled = false;
		}

		public void OnPlayerTeleport()
		{
			var pos = Vector3Int.FloorToInt(localPlayer.GetPosition()) / 16;
			if (initTexture.GetPixel(pos.x & 63, pos.z & 63).r == 1)
			{
				localPlayer.SetGravityStrength(3);
				localPlayer.Immobilize(false);
				foreach (var collider in colliders)
				{
					collider.enabled = GetBlock(Vector3Int.RoundToInt(collider.transform.localPosition)) != 0;
				}
				return;
			}
			localPlayer.SetGravityStrength(0);
			localPlayer.Immobilize(true);
			localPlayer.SetVelocity(Vector3.zero);
			SendCustomEventDelayedFrames(nameof(OnPlayerTeleport), 2);
		}

		public override void OnPlayerRespawn(VRCPlayerApi player)
		{
			if (player == localPlayer) OnPlayerTeleport();
		}

		private void InitWorld()
		{
			BuildQueueRadiusData();
			#region base
			localPlayer = Networking.LocalPlayer;
			localPlayer.SetGravityStrength(3);
			updateColors[0].r = 0.5f;
			for (int i = 1; i < updateColors.Length; i <<= 1) Array.Copy(updateColors, 0, updateColors, i, Mathf.Min(i, updateColors.Length - i));
			// The optimizers use the raw R8 value: 1 means "ignore/discard".
			ignoreColors[0] = 255;
			for (int i = 1; i < ignoreColors.Length; i <<= 1) Array.Copy(ignoreColors, 0, ignoreColors, i, Mathf.Min(i, ignoreColors.Length - i));
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
			cam.FarClipPlane = 400;
			/*var masks = cam.CullingMask;
			masks.value ^= 1 << 27;
			cam.CullingMask = masks.value;*/
			#endregion

			#region colliders
			for (int i = 0; i < 36; i++)
			{
				colliders[i] = Instantiate(colliderPrefab, collidersParent).GetComponent<Collider>();
				colliders[i].transform.localPosition = new Vector3(i % 3 - 1, i / 9 - 1, (i / 3) % 3 - 1);
			}
			#endregion

			#region textures
			convertor = new Texture2D(256, 256, TextureFormat.R8, false, true);

			/*lightTexture = new Texture2D(8192, 4096, TextureFormat.R8, false, true);
			lightTexture.LoadRawTextureData(new byte[lightTexture.width * lightTexture.height]);
			lightTexture.Apply();*/

			for (int i = 0; i < worldTextures.Length; i++)
			{
				worldTextures[i] = new Texture2D(4_096, 4_096, TextureFormat.R8, false, true);
				worldTextures[i].filterMode = FilterMode.Point;
				worldTextures[i].wrapMode = TextureWrapMode.Clamp;
				worldTextures[i].LoadRawTextureData(new byte[worldTextures[i].width * worldTextures[i].height]);
				worldTextures[i].Apply();
			}
			if (worldTextureDebugRenderers != null)
				for (int i = 0; i < worldTextureDebugRenderers.Length && i < worldTextures.Length; i++)
					if (worldTextureDebugRenderers[i] != null)
						worldTextureDebugRenderers[i].material.SetTexture("_MainTex", worldTextures[i]);
			/*ctrlTexture = new Texture2D(512, 192, TextureFormat.R8, false, true);
			ctrlTexture.filterMode = FilterMode.Point;
			ctrlTexture.LoadRawTextureData(new byte[ctrlTexture.width * ctrlTexture.height]);
			ctrlTexture.Apply();*/

			ctrlTextures[0] = new Texture2D(256, 256, TextureFormat.R8, false, true);
			ctrlTextures[0].filterMode = FilterMode.Point;
			ctrlTextures[0].LoadRawTextureData(new byte[65_536]);
			ctrlTextures[0].Apply();

			ctrlTextures[1] = new Texture2D(64, 64, TextureFormat.R8, false, true);
			ctrlTextures[1].filterMode = FilterMode.Point;
			ctrlTextures[1].LoadRawTextureData(ignoreColors);
			ctrlTextures[1].Apply();

			initTexture = new Texture2D(64, 64, TextureFormat.R8, false, true);
			initTexture.filterMode = FilterMode.Point;
			initTexture.LoadRawTextureData(new byte[4096]);
			initTexture.Apply();
			#endregion

			#region materials
			for (int i = 0; i < optimizatorMaterials.Length; i++)
			{
				for (int page = 0; page < worldTextures.Length; page++)
					optimizatorMaterials[i].SetTexture("_WorldTex" + page, worldTextures[page]);
				if (i < ctrlTextures.Length) optimizatorMaterials[i].SetTexture("_CtrlTex", ctrlTextures[i]);
				optimizators[i].initializationMode = CustomRenderTextureUpdateMode.Realtime;
			}
			optimizatorMaterials[1].SetTexture("_PrevLodTex", optimizators[0]);
			optimizatorMaterials[2].SetTexture("_PrevLodTex", optimizators[1]);
			for (int page = 0; page < worldTextures.Length; page++)
				worldMaterial.SetTexture("_WorldTex" + page, worldTextures[page]);
			#endregion

			blockSelector.SetData(worldTextures);
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
			miniGenTexture.material.SetInt("_OffsetX", 5053 + worldPos.x * 4);
			miniGenTexture.material.SetInt("_OffsetY", 5053 + worldPos.y * 4);
			miniGenTexture.Initialize();
			SetMaterialPositions();
			asyncWorking = true;
			VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
			enabled = true;
		}

		private bool TryNext()
		{
			while (currentIndex < chunksQueueData.Length)
			{
				var pos = chunksQueueData[currentIndex] + worldPos;
				if (initTexture.GetPixel(pos.x & 63, pos.y & 63).r != 0 || renderingChunk == pos || readingChunk == pos)
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

		private void BuildQueueRadiusData()
		{
			if (chunksQueueData == null) return;

			chunksQueueRadiusSq = new int[chunksQueueData.Length];
			for (int i = 0; i < chunksQueueData.Length; i++)
			{
				Vector2 center = (Vector2)chunksQueueData[i] + Vector2.one * 0.5f;
				chunksQueueRadiusSq[i] = Mathf.CeilToInt(center.sqrMagnitude);
			}
		}

		private int FindRadiusStart(int radiusSq)
		{
			int left = 0;
			int right = chunksQueueRadiusSq.Length;

			while (left < right)
			{
				int middle = (left + right) >> 1;
				if (chunksQueueRadiusSq[middle] < radiusSq) left = middle + 1;
				else right = middle;
			}

			if (left == chunksQueueRadiusSq.Length) return left;

			int layerRadius = chunksQueueRadiusSq[left];
			while (left > 0 && chunksQueueRadiusSq[left - 1] == layerRadius) left--;

			if (left > 0)
			{
				int previousLayerRadius = chunksQueueRadiusSq[left - 1];
				while (left > 0 && chunksQueueRadiusSq[left - 1] == previousLayerRadius) left--;
			}

			return left;
		}

		private void PrepareQueueAfterShift(Vector2Int oldCenter, Vector2Int newCenter)
		{
			if (chunksQueueRadiusSq == null || lastGeneratedIndex < 0)
			{
				currentIndex = 0;
				return;
			}

			float oldRadius = Mathf.Sqrt(chunksQueueRadiusSq[lastGeneratedIndex]);
			float moveDistance = (newCenter - oldCenter).magnitude;
			float newRadius = Mathf.Max(0f, oldRadius - moveDistance);
			int newRadiusSq = Mathf.CeilToInt(newRadius * newRadius);
			currentIndex = FindRadiusStart(newRadiusSq);
		}

		private void SetMaterialPositions()
		{
			var queuePos = chunksQueueData[currentIndex];
			renderingQueueIndex = currentIndex;
			var pos = queuePos + worldPos;
			renderingChunk = pos;
			pos *= 16;
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

		private int GetWorldPage(int chunkX, int chunkZ)
		{
			return ((chunkX >> 4) & 3) | (((chunkZ >> 4) & 3) << 2);
		}

		private int GetWorldTextureX(int chunkX, int localX, int localZ)
		{
			return ((chunkX & 15) << 8) + localX + (localZ << 4);
		}

		private int GetWorldTextureY(int chunkZ, int worldY)
		{
			return ((chunkZ & 15) << 8) + worldY;
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

			var miniGenMaterial = miniGenTexture.material;
			if (miniGenMaterial.GetInt("_OffsetX") != 5053 + worldPos.x * 4 || miniGenMaterial.GetInt("_OffsetY") != 5053 + worldPos.y * 4)
			{
				miniGenMaterial.SetInt("_OffsetX", 5053 + worldPos.x * 4);
				miniGenMaterial.SetInt("_OffsetY", 5053 + worldPos.y * 4);
				miniGenTexture.Initialize();
				if (!queueSearchPrepared) currentIndex = 0;
				queueSearchPrepared = false;
			}

			if (chunkReading)
			{
				var offset = readingChunk - worldPos;
				if (offset.x > -33 && offset.y > -33 && offset.x < 32 && offset.y < 32)
				{
					if (!request.TryGetData(px)) return;
					convertor.LoadRawTextureData(px);
					var data = convertor.GetPixels();
					int page = GetWorldPage(readingChunk.x, readingChunk.y);
					worldTextures[page].SetPixels((readingChunk.x & 15) << 8, (readingChunk.y & 15) << 8, 256, 256, data);
					// Temporary timing test: match the old pipeline and apply immediately in the async callback.
					worldTextures[page].Apply(false, false);
					worldTextureEdited[page] = false;
					worldTexturePendingApply = true;
					lastGeneratedIndex = renderingQueueIndex;
					initTexture.SetPixel(readingChunk.x & 63, readingChunk.y & 63, Color.red);
					updateChunk = new Vector2Int(readingChunk.x & 63, readingChunk.y & 63);
				}
				else
				{
					Debug.LogWarning(debugWarningLogo + "skipped chunk " + offset);
				}
			}

			if (chunkRendering)
			{
				chunkRendering = false;
				readingChunk = renderingChunk;
				chunkReading = true;
				if (TryNext()) SetMaterialPositions();
				VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
			}
			else
			{
				asyncWorking = false;
				Debug.Log(debugLogo + "World generation complete!");
			}
		}

		private byte GetBlock(Vector3Int pos)
		{
			if (pos.y > 255 || pos.y < 0) return 0;
			int chunkX = pos.x >> 4;
			int chunkZ = pos.z >> 4;
			int page = GetWorldPage(chunkX, chunkZ);
			return ((Color32)worldTextures[page].GetPixel(GetWorldTextureX(chunkX, pos.x & 15, pos.z & 15), GetWorldTextureY(chunkZ, pos.y))).r;
		}

		private bool ctrl0Update;
		private bool ctrl1Update;

		private void Update()
		{
			bool ctrl0Edited = false;
			bool ctrl1Edited = false;
			bool lod1Update = false;
			bool lod2Update = false;
			bool lod3Update = false;
			bool worldEdited = false;

			if (ctrl0Update)
			{
				ctrl0Update = false;
				ctrl0Edited = true;
				ctrlTextures[0].LoadRawTextureData(ignoreColors);
				optimizators[0].initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			}

			if (ctrl1Update)
			{
				ctrl1Update = false;
				ctrl1Edited = true;
				ctrlTextures[1].LoadRawTextureData(ignoreColors);
				optimizators[1].initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			}

			if (updateChunk != Vector2Int.left)
			{
				ctrlTextures[0].SetPixels(updateChunk.x << 2, updateChunk.y << 2, 4, 4, updateColors);
				ctrlTextures[1].SetPixel(updateChunk.x, updateChunk.y, Color.gray);
				updateChunk = Vector2Int.left;
				ctrl0Edited = true;
				ctrl1Edited = true;
				ctrl0Update = true;
				ctrl1Update = true;
				lod1Update = true;
				lod2Update = true;
				lod3Update = true;
				worldEdited = true;
			}


			if (setBlocksCount > 0)
			{
				for (int i = 0; i < setBlocksCount; i++)
				{
					var setPos = setBlockPositions[i];
					int chunkX = setPos.x >> 4;
					int chunkZ = setPos.z >> 4;
					int page = GetWorldPage(chunkX, chunkZ);
					worldTextures[page].SetPixel(GetWorldTextureX(chunkX, setPos.x & 15, setPos.z & 15), GetWorldTextureY(chunkZ, setPos.y), (Color)new Color32(setBlockValues[i], 0, 0, 0));
					worldTextureEdited[page] = true;
					ctrlTextures[0].SetPixel((setPos.x >> 2) & 255, (setPos.z >> 2) & 255, Color.gray);
					ctrlTextures[1].SetPixel((setPos.x >> 4) & 63, (setPos.z >> 4) & 63, Color.gray);
				}
				worldEdited = true;
				ctrl0Edited = true;
				ctrl1Edited = true;
				ctrl0Update = true;
				ctrl1Update = true;
				lod1Update = true;
				lod2Update = true;
				lod3Update = true;
				foreach (var collider in colliders)
				{
					collider.enabled = GetBlock(Vector3Int.RoundToInt(collider.transform.localPosition)) != 0;
				}
				setBlocksCount = 0;
			}

			shadowCam1.enabled = false;
			if ((Time.frameCount & 20) == 0) shadowCam1.enabled = true;
			var pos = Vector3Int.FloorToInt(localPlayer.GetPosition() + localPlayer.GetVelocity() * Time.deltaTime + Vector3.up * 0.5f);
			if (oldPos != pos)
			{

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
				var oldP = p;
				if (pos.x > p.x + 16)
				{
					p.x = pos.x >> 4 << 4;
					if (pos.z > p.y + 8)
						p.y = (pos.z >> 4 << 4) + 16;
					else if (pos.z < p.y - 8)
						p.y = pos.z >> 4 << 4;
				}
				else if (pos.x < p.x - 16)
				{
					p.x = (pos.x >> 4 << 4) + 16;
					if (pos.z > p.y + 8)
						p.y = (pos.z >> 4 << 4) + 16;
					else if (pos.z < p.y - 8)
						p.y = pos.z >> 4 << 4;
				}

				if (pos.z > p.y + 16)
				{
					p.y = pos.z >> 4 << 4;
					if (pos.x > p.x + 8)
						p.x = (pos.x >> 4 << 4) + 16;
					else if (pos.x < p.x - 8)
						p.x = pos.x >> 4 << 4;
				}
				else if (pos.z < p.y - 16)
				{
					p.y = pos.z >> 4 << 4;
					if (pos.x > p.x + 8)
						p.x = (pos.x >> 4 << 4) + 16;
					else if (pos.x < p.x - 8)
						p.x = pos.x >> 4 << 4;
				}

				if (p != oldP)
				{
					var oldWorldPos = worldPos;
					worldPos = new Vector2Int(p.x >> 4, p.y >> 4);
					var offset = worldPos - oldWorldPos;
					PrepareQueueAfterShift(oldWorldPos, worldPos);
					queueSearchPrepared = true;
					ctrl1Edited = true;
					lod2Update = true;
					lod3Update = true;
					if (Mathf.Abs(offset.x) > 63 || Mathf.Abs(offset.y) > 63)
					{
						ctrlTextures[1].SetPixels(clearColor);
						initTexture.SetPixels(clearColor);
					}
					else
					{
						if (offset.x != 0)
						{
							var SE = worldPos.x > oldWorldPos.x
								? new Vector2Int(oldWorldPos.x, worldPos.x)
								: new Vector2Int(worldPos.x, oldWorldPos.x);
							for (int i = SE.x; i < SE.y; i++)
							{
								var ind = (i + 32) & 63;
								ctrlTextures[1].SetPixels(ind, 0, 1, 64, clearColor);
								initTexture.SetPixels(ind, 0, 1, 64, clearColor);
							}
						}

						if (offset.y != 0)
						{
							var SE = worldPos.y > oldWorldPos.y
								? new Vector2Int(oldWorldPos.y, worldPos.y)
								: new Vector2Int(worldPos.y, oldWorldPos.y);
							for (int i = SE.x; i < SE.y; i++)
							{
								var ind = (i + 32) & 63;
								ctrlTextures[1].SetPixels(0, ind, 64, 1, clearColor);
								initTexture.SetPixels(0, ind, 64, 1, clearColor);
							}
						}

					}

					if (!asyncWorking)
					{
						chunkReading = false;
						chunkRendering = false;
						if (TryNext())
						{
							SetMaterialPositions();
							asyncWorking = true;
							VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
						}
					}
				}
			}
			#endregion
			if (worldEdited || worldTexturePendingApply)
			{
				for (int i = 0; i < worldTextures.Length; i++)
				{
					if (!worldTextureEdited[i]) continue;
					worldTextures[i].Apply(false, false);
					worldTextureEdited[i] = false;
				}
				worldTexturePendingApply = false;
			}
			if (ctrl0Edited) ctrlTextures[0].Apply();
			if (ctrl1Edited) ctrlTextures[1].Apply();
			if (lod1Update) optimizators[0].initializationMode = CustomRenderTextureUpdateMode.Realtime;
			if (lod2Update) optimizators[1].initializationMode = CustomRenderTextureUpdateMode.Realtime;
			if (lod3Update) optimizators[2].initializationMode = CustomRenderTextureUpdateMode.Realtime;
		}

		private void OnDestroy()
		{
			//destroy textures
			foreach (var texture in worldTextures) Destroy(texture);
			//Destroy(lightTexture);
			Destroy(convertor);
			Destroy(initTexture);
			foreach (var tex in ctrlTextures) Destroy(tex);
		}

		private int setBlocksCount;
		private Vector3Int[] setBlockPositions = new Vector3Int[128];
		private byte[] setBlockValues = new byte[128];

		public void SetBlock(Vector3Int selectedBlock, byte block, byte old)
		{
			setBlockValues[setBlocksCount] = block;
			setBlockPositions[setBlocksCount] = selectedBlock;
			setBlocksCount++;
		}
		/*#if UNITY_EDITOR
				[ContextMenu("Generate queue")]
				public void GenerateQueue()
				{
					const int size = 64;
					const int halfSize = size >> 1;
					var v = new Vector3[size * size];
					for (int i = 0; i < size; i++)
					{
						for (int j = 0; j < size; j++)
						{
							v[i * size + j] = new Vector3(i - halfSize, j - halfSize, new Vector2(i - (halfSize - 0.5f), j - (halfSize - 0.5f)).sqrMagnitude);
						}
					}

					System.Array.Sort(v, (a, b) => a.z.CompareTo(b.z));

					chunksQueueData = new Vector2Int[size * size];
					for (int i = 0; i < chunksQueueData.Length; i++) chunksQueueData[i] = Vector2Int.RoundToInt((Vector2)v[i]);
				}
		#endif*/
	}

	public enum ChunkMeshType { PP, MP, PM, MM, EP, PE, EM, ME, EE, NN }
}


#region editor
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
		folderGrups.Clear();
		var group = new FolderGrup() { name = "BiomeGenerator", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("dataGameObject"));
		group.properties.Add(serializedObject.FindProperty("genLayers"));
		group.properties.Add(serializedObject.FindProperty("genTextures"));
		group.properties.Add(serializedObject.FindProperty("miniGenTexture"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Prefabs", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("colliderPrefab"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "SceneObjects", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("collidersParent"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Materials", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("worldMaterial"));
		group.properties.Add(serializedObject.FindProperty("worldShadowMaterial"));
		group.properties.Add(serializedObject.FindProperty("optimizatorMaterials"));
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
		group.properties.Add(serializedObject.FindProperty("optimizators"));
		group.properties.Add(serializedObject.FindProperty("chunkGenerator"));
		group.properties.Add(serializedObject.FindProperty("chunkTerrainGenerator"));
		group.properties.Add(serializedObject.FindProperty("chunkMeshGenerator"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Systems", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("blockSelector"));
		group.properties.Add(serializedObject.FindProperty("worldTextureArrayProbe"));
		folderGrups.Add(group);
	}
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

		serializedObject.ApplyModifiedProperties();
	}

	private struct FolderGrup
	{
		public string name;
		public List<SerializedProperty> properties;
	}
}
#endif
#endregion
