using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC_MINE.Data;



namespace VRC_MINE.World
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class WorldGenerator : UdonSharpBehaviour
	{
		[NonSerialized] public SmartChunk[] smartChunks;

		#region materials
		[SerializeField] private Material voxelWorldMaterial;
		[SerializeField] private Material readbackMaterial;
		[SerializeField] private Material optimizerMaterial;
		[SerializeField] private Material chunkGeneratorMaterial;
		[SerializeField] private Material chunkTerrainGeneratorMaterial;
		#endregion

		#region prefabs
		[SerializeField] private GameObject colliderPrefab;
		#endregion

		#region objects
		[SerializeField] private Transform collidersParent;
		#endregion

		#region shadows
		[SerializeField] private Transform shadowControl;
		[SerializeField] private Camera shadowCam;
		[SerializeField] private Camera shadowCam1;
		[SerializeField] private Camera godRaysLeft;
		[SerializeField] private Camera godRaysRight;
		[SerializeField] private Camera godRaysPhoto;
		[SerializeField] private Material godRaysMaterial;
		[NonSerialized] public Material worldMaterial;
		[SerializeField] private Shader replacementShader;
		#endregion

		#region systems
		[SerializeField] private GamePlay.BlockSelector blockSelector;
		[SerializeField] private ChunkMeshGenerator chunkMeshGenerator;
		#endregion


		#region textures
		private Texture2D convertor;
		private Texture2D initTexture;
		private Texture2D optimizerCtrl;
		#endregion

		#region GPU generators
		[SerializeField] private CustomRenderTexture optimizer;
		[SerializeField] private CustomRenderTexture voxelWorld;
		[SerializeField] private CustomRenderTexture readbackTexture;
		[SerializeField] private CustomRenderTexture chunkGenerator;
		[SerializeField] private CustomRenderTexture chunkTerrainGenerator;
		#endregion

		#region GPU gen
		private int GPUQueue;
		private Color[] updateColors;
		private byte[] ignoreColors;
		private bool genChunk;
		[SerializeField] private CustomRenderTextureUpdateZone[] chunkZone;
		#endregion

		#region CPU gen
		private bool readbackRead;
		private bool readbackRender;
		private int readbackQueue;
		private Vector2Int readbackRenderPos;
		private Vector2Int readbackReadPos;
		#endregion

		#region cache
		private int zonesCount;
		private CustomRenderTextureUpdateZone[] zones = new CustomRenderTextureUpdateZone[64];
		[HideInInspector, SerializeField] private CustomRenderTextureUpdateZone[] zoneLayers;
		private bool optimizerUpdate;
		private bool optimizerApply;
		private VRCPlayerApi localPlayer;
		[HideInInspector, SerializeField] private Vector2Int[] chunksQueueData;



		private float[] blockX = new float[64];
		private float[] blockZ = new float[64];
		private float[] blockId = new float[64];
		//
		private int[] chunksQueueRadiusSq;
		private int lastGeneratedIndex = -1;
		private int renderingQueueIndex = -1;
		private Vector2Int updatedChunk = Vector2Int.left;
		private bool asyncWorking;
		private Vector2Int worldPos;
		private Collider[] colliders = new Collider[36];
		private Vector3Int oldPos = Vector3Int.down;
		//
		#endregion

		private void Start()
		{
			InitGenerator();
			VRCAsyncGPUReadback.Request(readbackTexture, 0, this);
		}

		private void InitGenerator()
		{
			localPlayer = Networking.LocalPlayer;
			godRaysRight.enabled = localPlayer.IsUserInVR();
			oldPos = Vector3Int.FloorToInt(localPlayer.GetPosition() + localPlayer.GetVelocity() * Time.deltaTime + Vector3.up * 0.5f);
			int colliderIndex = 0;
			for (int y = -1; y <= 2; y++)
				for (int z = -1; z <= 1; z++)
					for (int x = -1; x <= 1; x++)
					{
						GameObject instance = Instantiate(colliderPrefab, collidersParent);
						instance.transform.localPosition = oldPos + new Vector3Int(x, y, z);
						Collider collider = instance.GetComponent<Collider>();
						collider.enabled = false;
						colliders[colliderIndex++] = collider;
					}
			VRCCameraSettings.ScreenCamera.FarClipPlane = 600;

			//arrays
			updateColors = new Color[128];
			updateColors[0] = new Color(0.5f, 0, 0, 0);
			for (var i = 1; i < updateColors.Length; i <<= 1) Array.Copy(updateColors, 0, updateColors, i, i);
			ignoreColors = new byte[98_304];
			ignoreColors[0] = 255;
			ignoreColors[1] = 255;
			ignoreColors[2] = 255;
			for (var i = 3; i < ignoreColors.Length; i <<= 1) Array.Copy(ignoreColors, 0, ignoreColors, i, i);

			//textures
			optimizerCtrl = new Texture2D(512, 192, TextureFormat.R8, false, true);
			optimizerCtrl.filterMode = FilterMode.Point;
			optimizerCtrl.LoadRawTextureData(new byte[512 * 192]);
			optimizerCtrl.Apply(false, false);

			initTexture = new Texture2D(64, 64, TextureFormat.RG16, false, true);
			initTexture.filterMode = FilterMode.Point;
			initTexture.LoadRawTextureData(new byte[64 * 64 * 2]);

			//place tex
			optimizerMaterial.SetTexture("_CtrlTex", optimizerCtrl);

			//shadow
			shadowCam.SetReplacementShader(replacementShader, "RenderType");
			shadowCam1.SetReplacementShader(replacementShader, "RenderType");

		}


		private void Update()
		{
			Vector3Int pos = Vector3Int.FloorToInt(localPlayer.GetPosition() + localPlayer.GetVelocity() * Time.deltaTime + Vector3.up * 0.5f);
			UpdateShadowRendering(pos);
			if (oldPos != pos)
			{
				UpdateColliders(oldPos, pos);
			}
			zonesCount = 0;
			genChunk = false;

			if (optimizerUpdate)
			{
				optimizerCtrl.LoadRawTextureData(ignoreColors);
				optimizerUpdate = false;
			}

			GPUGen();

			if (zonesCount > 0)
			{
				voxelWorldMaterial.SetInt("_ZonesCount", zonesCount);
				voxelWorldMaterial.SetInt("_GenChunk", genChunk ? 1 : 0);
				voxelWorldMaterial.SetFloatArray("_BlockX", blockX);
				voxelWorldMaterial.SetFloatArray("_BlockZ", blockZ);
				voxelWorldMaterial.SetFloatArray("_BlockId", blockId);
				voxelWorld.SetUpdateZones(zones);
				voxelWorld.Update();
			}

			if (optimizerUpdate)
			{
				optimizerCtrl.Apply();
				optimizer.Update();
			}

			oldPos = pos;
			return;
			/*
			if (Time.frameCount == polygonClearFrame) return;
			UpdatePolygonControl();
			UpdateShadowRendering();

			if (chunksQueueData == null || chunksQueueData.Length == 0)
				return;

			if (gpuRealtimeShutdownPending)
			{
				if (chunkTerrainGenerator != null)
					chunkTerrainGenerator.updateMode = CustomRenderTextureUpdateMode.OnDemand;
				if (chunkGenerator != null)
					chunkGenerator.updateMode = CustomRenderTextureUpdateMode.OnDemand;
				if (voxelWorld != null)
					voxelWorld.updateMode = CustomRenderTextureUpdateMode.OnDemand;
				for (int i = 0; i < optimizators.Length; i++)
					if (optimizators[i] != null)
						optimizators[i].updateMode = CustomRenderTextureUpdateMode.OnDemand;
				gpuRealtimeShutdownPending = false;
				FinishPolygonOptimization();
				return;
			}

			if (gpuCurrentIndex >= chunksQueueData.Length)
				return;

			Vector2Int queueChunk = chunksQueueData[gpuCurrentIndex];
			Vector2Int worldChunk = queueChunk + worldPos;

			// Terrain and chunk generators receive block-space coordinates.
			if (chunkTerrainGeneratorMaterial != null)
			{
				chunkTerrainGeneratorMaterial.SetInt("_ChunkPosX", worldChunk.x * 16);
				chunkTerrainGeneratorMaterial.SetInt("_ChunkPosY", worldChunk.y * 16);
			}
			if (chunkGeneratorMaterial != null)
			{
				chunkGeneratorMaterial.SetInt("_ChunkPosX", worldChunk.x * 16);
				chunkGeneratorMaterial.SetInt("_ChunkPosY", worldChunk.y * 16);
			}

			// VoxelWorld addresses its 64x64 chunk ring by the low six bits.
			if (voxelWorld != null)
			{
				Material voxelMaterial = voxelWorld.material;
				if (voxelMaterial != null)
				{
					voxelMaterial.SetInt("_OffsetX", worldChunk.x & 31);
					voxelMaterial.SetInt("_OffsetY", worldChunk.y & 31);
				}
			}
			foreach (var optimizatorMaterial in optimizerMaterials)
				if (optimizatorMaterial != null)
				{
					optimizatorMaterial.SetInt("_OffsetX", worldChunk.x & 63);
					optimizatorMaterial.SetInt("_OffsetY", worldChunk.y & 63);
				}

			gpuCurrentIndex++;
			polygonPendingChunk = worldChunk;
			polygonChunkPending = true;
			if (gpuCurrentIndex >= chunksQueueData.Length)
				gpuRealtimeShutdownPending = true;*/

		}

		private void UpdateColliders(Vector3Int previousPos, Vector3Int pos)
		{
			Vector3Int delta = pos - previousPos;
			bool largeMove = Mathf.Abs(delta.x) > 2 || Mathf.Abs(delta.z) > 2 || Mathf.Abs(delta.y) > 3;
			foreach (Collider collider in colliders)
			{
				Vector3Int cPos = Vector3Int.RoundToInt(collider.transform.localPosition);
				if (largeMove)
				{
					cPos += delta;
				}
				else
				{
					if (cPos.x < pos.x - 1) cPos.x += 3;
					else if (cPos.x > pos.x + 1) cPos.x -= 3;
					if (cPos.z < pos.z - 1) cPos.z += 3;
					else if (cPos.z > pos.z + 1) cPos.z -= 3;
					if (cPos.y < pos.y - 1) cPos.y += 4;
					else if (cPos.y > pos.y + 2) cPos.y -= 4;
				}
				collider.transform.localPosition = cPos;
				UpdateColliderState(collider, cPos);
			}
		}

		private void UpdateColliderState(Collider collider, Vector3Int pos)
		{
			int chunkX = (pos.x >> 4) & 31;
			int chunkZ = (pos.z >> 4) & 31;
			if (pos.y < 0 || pos.y >= 128 || initTexture.GetPixel(chunkX, chunkZ).g == 0)
			{
				collider.enabled = false;
				return;
			}

			int blockIndex = (pos.x & 15) + ((pos.z & 15) << 4) + (pos.y << 8);
			collider.enabled = smartChunks[chunkX + (chunkZ << 5)].blockData[blockIndex] != 0;
		}

		private void GPUGen()
		{
			Vector2Int pos = Vector2Int.zero;
			while (GPUQueue < 1024)
			{
				pos = chunksQueueData[GPUQueue] + worldPos;
				if (initTexture.GetPixel(pos.x & 31, pos.y & 31).r != 0)
					GPUQueue++;
				else
					break;
			}
			if (GPUQueue == 1024) return;
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosX", pos.x * 16);
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosY", pos.y * 16);
			chunkTerrainGenerator.Update();
			chunkGeneratorMaterial.SetInt("_ChunkPosX", pos.x * 16);
			chunkGeneratorMaterial.SetInt("_ChunkPosY", pos.y * 16);
			chunkGenerator.Update();
			voxelWorldMaterial.SetInt("_OffsetX", pos.x);
			voxelWorldMaterial.SetInt("_OffsetY", pos.y);
			genChunk = true;
			AddZone(chunkZone, 0);
			UpdateOptimizerChunk(pos);
			int x = pos.x & 31;
			int z = pos.y & 31;
			initTexture.SetPixel(x, z, new Color(1, initTexture.GetPixel(x, z).g, 0, 0));
		}

		public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
		{
			if (readbackRead)
			{
				readbackRead = false;
				int x = readbackReadPos.x & 31;
				int z = readbackReadPos.y & 31;
				SmartChunk chunk = smartChunks[x + (z << 5)];
				byte[] blockData = chunk.blockData;
				if (blockData == null)
				{
					blockData = new byte[33_280];
					chunk.blockData = blockData;
				}

				if (request.TryGetData(blockData))
				{
					chunk.chunkPosition = readbackReadPos;
					chunk.hasData = true;
					initTexture.SetPixel(x, z, new Color(initTexture.GetPixel(x, z).r, 1, 0, 0));
				}
				else
				{
					DebugErr("GPU readback failed for chunk " + readbackReadPos);
				}
			}

			if (readbackRender)
			{
				readbackRead = true;
				readbackReadPos = readbackRenderPos;
				readbackRender = false;
			}

			Vector2Int pos = Vector2Int.zero;
			while (readbackQueue < 1024)
			{
				pos = chunksQueueData[readbackQueue] + worldPos;
				if (
					(readbackRead && pos == readbackReadPos) ||
					(readbackRender && pos == readbackRenderPos) ||
					initTexture.GetPixel(pos.x & 31, pos.y & 31).g != 0)
					readbackQueue++;
				else
					break;
			}
			if (readbackQueue < 1024)
			{
				readbackRenderPos = pos;
				readbackMaterial.SetInt("_ChunkPosX", pos.x);
				readbackMaterial.SetInt("_ChunkPosY", pos.y);
				readbackTexture.Update();
				readbackRender = true;
			}

			if (readbackRead || readbackRender) VRCAsyncGPUReadback.Request(readbackTexture, 0, this);
		}

		private void AddZone(CustomRenderTextureUpdateZone[] zonesArray, int index)
		{
			Array.Copy(zonesArray, index, zones, zonesCount, 1);
			zonesCount++;
		}

		private void UpdateOptimizerChunk(Vector2Int pos)
		{
			pos.x &= 31;
			pos.y &= 31;

			optimizerCtrl.SetPixels(pos.x >> 1 << 5, (pos.y >> 1) * 12 + 8, 32, 4, updateColors);

			if ((pos.x & 1) == 0)
				optimizerCtrl.SetPixels(pos.x >> 1 << 5, (pos.y >> 1) * 12, 17, 4, updateColors);
			else
			{
				optimizerCtrl.SetPixels((pos.x >> 1 << 5) + 16, (pos.y >> 1) * 12, 16, 4, updateColors);
				optimizerCtrl.SetPixels(((pos.x >> 1 << 5) + 32) & 511, (pos.y >> 1) * 12, 1, 4, updateColors);
			}

			if ((pos.y & 1) == 0)
				optimizerCtrl.SetPixels(pos.x >> 1 << 5, (pos.y >> 1) * 12 + 4, 17, 4, updateColors);
			else
			{
				optimizerCtrl.SetPixels((pos.x >> 1 << 5) + 16, (pos.y >> 1) * 12 + 4, 16, 4, updateColors);
				optimizerCtrl.SetPixels(pos.x >> 1 << 5, (((pos.y >> 1) * 12 + 12) % 192) + 4, 1, 4, updateColors);
			}

			optimizerUpdate = true;
		}


		public void OnPlayerTeleport()
		{
			return;
			/*var pos = Vector3Int.FloorToInt(localPlayer.GetPosition()) / 16;
			if (initTextureLegacy.GetPixel(pos.x & 63, pos.z & 63).r == 1)
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
			SendCustomEventDelayedFrames(nameof(OnPlayerTeleport), 2);*/
		}

		public override void OnPlayerRespawn(VRCPlayerApi player)
		{
			if (player == localPlayer) OnPlayerTeleport();
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
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
			chunkTerrainGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
			chunkGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
			chunkGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
			chunkTerrainGenerator.Initialize();
			chunkGenerator.Initialize();
			lastFrameCount = Time.frameCount;
			readbackRender = true;
		}

		public void OnAsyncGpuReadbackCompleteLegacy(VRCAsyncGPUReadbackRequest request)
		{
			/*if (request.hasError)
			{
				Debug.LogError(debugLogo + "GPU readback error!");
				lastFrameCount = Time.frameCount;
				VRCAsyncGPUReadback.Request(chunkGenerator, 0, this);
				return;
			}*/

			/*if (chunkReading)
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
					initTextureLegacy.SetPixel(readingChunk.x & 63, readingChunk.y & 63, Color.red);
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
			}*/
		}

		/*private byte GetBlock(Vector3Int pos)
		{
			if (pos.y > 255 || pos.y < 0) return 0;
			int chunkX = pos.x >> 4;
			int chunkZ = pos.z >> 4;
			int page = GetWorldPage(chunkX, chunkZ);
			return ((Color32)worldTextures[page].GetPixel(GetWorldTextureX(chunkX, pos.x & 15, pos.z & 15), GetWorldTextureY(chunkZ, pos.y))).r;
		}*/

		private void UpdateLegacy()
		{
			bool ctrl0Edited = false;
			bool ctrl1Edited = false;
			bool lod1Update = false;
			bool lod2Update = false;
			bool lod3Update = false;
			bool worldEdited = false;


			/*if (setBlocksCount > 0)
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
			}*/

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
				/*worldMaterial.SetVector("_ShadowLodOffset", shadowCam1Offset);
				worldMaterial.SetMatrix("_ShadowMatrix", shadowCam.projectionMatrix * shadowCam.worldToCameraMatrix);*/
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
						//collider.enabled = GetBlock(cPos) != 0;
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
						//collider.enabled = GetBlock(cPos) != 0;
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
						initTextureLegacy.SetPixels(clearColor);
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
								initTextureLegacy.SetPixels(ind, 0, 1, 64, clearColor);
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
								initTextureLegacy.SetPixels(0, ind, 64, 1, clearColor);
							}
						}

					}

					if (!asyncWorking)
					{
						readbackRead = false;
						readbackRender = false;
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


		// New visual GPU-generation pipeline entry point.
		private bool shadowPositionReady;
		private Vector3Int shadowRenderPos;

		public override void PostLateUpdate()
		{
			Quaternion moonRotation = shadowControl.rotation;
			Quaternion sunRotation = moonRotation * Quaternion.Euler(0f, 180f, 0f);
			Vector3 moonDirection = shadowControl.forward;
			var leftEye = Camera.StereoscopicEye.Left;
			var rightEye = Camera.StereoscopicEye.Right;
			Vector3 headForward = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
			bool headLooksAtMoon = Vector3.Dot(headForward, moonDirection) > 0f;
			Quaternion headLightRotation = headLooksAtMoon ? moonRotation : sunRotation;

			UpdateGodRaysCamera(godRaysLeft, "_LeftLightProjection",
				VRCCameraSettings.GetEyePosition(leftEye), headLightRotation);
			if (godRaysRight.enabled)
				UpdateGodRaysCamera(godRaysRight, "_RightLightProjection",
					VRCCameraSettings.GetEyePosition(rightEye), headLightRotation);

			var photoCamera = VRCCameraSettings.PhotoCamera;
			// ClientSim does not expose a handheld camera.
			bool photoActive = photoCamera != null && photoCamera.Active;
			bool photoLooksAtMoon = false;
			if (photoActive)
			{
				photoLooksAtMoon = Vector3.Dot(photoCamera.Forward, moonDirection) > 0f;
				UpdateGodRaysCamera(godRaysPhoto, "_PhotoLightProjection", photoCamera.Position,
					photoLooksAtMoon ? moonRotation : sunRotation);
			}
			if (godRaysPhoto.enabled != photoActive)
				godRaysPhoto.enabled = photoActive;
			godRaysMaterial.SetFloat("_PhotoMaskReady", photoActive ? 1f : 0f);
			godRaysMaterial.SetVector("_MoonMask",
				new Vector4(headLooksAtMoon ? 1f : 0f, 0f, photoLooksAtMoon ? 1f : 0f, 0f));
		}

		private void UpdateGodRaysCamera(Camera maskCamera, string projectionName,
			Vector3 eyePosition, Quaternion lightRotation)
		{
			maskCamera.transform.SetPositionAndRotation(eyePosition, lightRotation);
			godRaysMaterial.SetMatrix(projectionName, maskCamera.projectionMatrix * maskCamera.worldToCameraMatrix);
		}

		private void UpdateShadowRendering(Vector3Int pos)
		{
			shadowCam1.enabled = false;
			if ((Time.frameCount & 20) == 0)
				shadowCam1.enabled = true;

			if (!shadowPositionReady || shadowRenderPos != pos)
			{
				Vector3 oldCamPos = shadowCam.transform.position;
				Vector3 oldCamPos1 = shadowCam1.transform.position;
				shadowControl.localPosition = pos;
				Vector3 shadowTarget = shadowControl.TransformPoint(new Vector3(0, 0, -327.68f));

				shadowCam.transform.position = oldCamPos;
				shadowCam.transform.position = shadowCam.transform.TransformPoint(
					Vector3Int.RoundToInt(shadowCam.transform.InverseTransformPoint(
						shadowTarget)));
				shadowCam1.transform.position = oldCamPos1;
				shadowCam1.transform.position = shadowCam1.transform.TransformPoint(
					Vector3Int.RoundToInt(shadowCam1.transform.InverseTransformPoint(
						shadowTarget)));
				shadowCam1.enabled = true;
				shadowRenderPos = pos;
				shadowPositionReady = true;
			}

			Vector3 shadowCam1Offset = shadowCam.transform.InverseTransformPoint(shadowCam1.transform.position);
			shadowCam1Offset.x /= 1000f;
			shadowCam1Offset.y /= 1000f;
			shadowCam1Offset.z /= 65536f;
			worldMaterial.SetVector("_ShadowLodOffset", shadowCam1Offset);
			worldMaterial.SetMatrix("_ShadowMatrix", shadowCam.projectionMatrix * shadowCam.worldToCameraMatrix);
		}





		public void FinishPolygonOptimization()
		{
			foreach (var optimizer in optimizators)
				if (optimizer != null)
				{
					optimizer.Update();
				}
		}

		private void OnDestroy()
		{
			Destroy(convertor);
			Destroy(initTexture);
			Destroy(optimizerCtrl);
		}

		private int setBlocksCount;
		private Vector3Int[] setBlockPositions = new Vector3Int[128];
		private byte[] setBlockValues = new byte[128];

		#region Legacy properties
		// Legacy geometry/readback state. Kept declared while the old algorithms are
		// being copied into the compact GPU pipeline.
		private Material[] optimizerMaterials = new Material[2];
		private CustomRenderTexture[] optimizators;
		private Texture2D[] worldTextures = new Texture2D[16];
		private Texture2D[] ctrlTextures = new Texture2D[2];
		private Texture2D initTextureLegacy;
		private Color[] clearColor = new Color[4096];
		private bool[] worldTextureEdited = new bool[16];
		private bool worldTexturePendingApply;
		private byte[] px = new byte[65536];
		private Vector2Int p;
		private bool queueSearchPrepared;
		private int currentIndex;
		private Vector2Int renderingChunk;
		private int lastFrameCount;

		private bool TryNext()
		{
			return false;
		}
		#endregion

		public void SetBlock(Vector3Int selectedBlock, byte block, byte old)
		{
			return;
			/*
			setBlockValues[setBlocksCount] = block;
			setBlockPositions[setBlocksCount] = selectedBlock;
			setBlocksCount++;
			*/
		}
#if UNITY_EDITOR && !COMPILER_UDONSHARP
		[ContextMenu("Generate queue")]
		public void GenerateQueue()
		{
			const int size = 32;
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

			UnityEditor.Undo.RecordObject(this, "Generate queue");
			chunksQueueData = new Vector2Int[size * size];
			for (int i = 0; i < chunksQueueData.Length; i++) chunksQueueData[i] = Vector2Int.RoundToInt((Vector2)v[i]);
			UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
			UnityEditor.EditorUtility.SetDirty(this);
		}
#endif

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

	public enum ChunkMeshType { PP, MP, PM, MM, EP, PE, EM, ME, EE, NN }
}


#region editor
//show chunkMeshTypes grid 16x16
#if UNITY_EDITOR
/*[CustomEditor(typeof(WorldGenerator))]
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
		var group = new FolderGrup() { name = "Prefabs", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("colliderPrefab"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "SceneObjects", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("collidersParent"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Materials", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("worldMaterial"));
		group.properties.Add(serializedObject.FindProperty("optimizerMaterials"));
		group.properties.Add(serializedObject.FindProperty("chunkGeneratorMaterial"));
		group.properties.Add(serializedObject.FindProperty("chunkTerrainGeneratorMaterial"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Shadows", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("shadowControl"));
		group.properties.Add(serializedObject.FindProperty("shadowCam"));
		group.properties.Add(serializedObject.FindProperty("shadowCam1"));
		group.properties.Add(serializedObject.FindProperty("godRaysLeft"));
		group.properties.Add(serializedObject.FindProperty("godRaysRight"));
		group.properties.Add(serializedObject.FindProperty("godRaysPhoto"));
		group.properties.Add(serializedObject.FindProperty("godRaysMaterial"));
		group.properties.Add(serializedObject.FindProperty("replacementShader"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Generators", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("optimizators"));
		group.properties.Add(serializedObject.FindProperty("chunkGenerator"));
		group.properties.Add(serializedObject.FindProperty("chunkTerrainGenerator"));
		group.properties.Add(serializedObject.FindProperty("voxelWorld"));
		group.properties.Add(serializedObject.FindProperty("readbackTexture"));
		group.properties.Add(serializedObject.FindProperty("chunkMeshGenerator"));
		folderGrups.Add(group);

		group = new FolderGrup() { name = "Systems", properties = new List<SerializedProperty>() };
		group.properties.Add(serializedObject.FindProperty("blockSelector"));
		folderGrups.Add(group);
	}
	public override void OnInspectorGUI()
	{
		//base.OnInspectorGUI();

		//serializedObject.Update();
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

		// Applying an untouched snapshot is harmless, but avoid even calling the
		// serialization path while merely opening/repainting the inspector.
		if (serializedObject.hasModifiedProperties) Debug.Log("hasModifiedProperties");
		//serializedObject.ApplyModifiedProperties();
	}

	private struct FolderGrup
	{
		public string name;
		public List<SerializedProperty> properties;
	}
}*/
#endif
#endregion
