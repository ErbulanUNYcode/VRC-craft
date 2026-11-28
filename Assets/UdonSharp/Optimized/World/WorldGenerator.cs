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

		#region materials
		[NonSerialized] public Material worldMaterial;
		#endregion

		#region systems
		[SerializeField] private GamePlay.BlockSelector blockSelector;
		[SerializeField] private ChunkMeshGenerator chunkMeshGenerator;
		#endregion


		#region textures
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
		private VRCPlayerApi localPlayer;
		[HideInInspector, SerializeField] private Vector2Int[] chunksQueueData;



		private Color[] clearColors = new Color[512 * 192];
		private float[] blockX = new float[64];
		private float[] blockZ = new float[64];
		private float[] blockId = new float[64];
		private Vector4[] clearChunks = new Vector4[4];
		private Vector4[] zoneOffsets = new Vector4[64];
		private int clearChunksCount;
		//
		private Vector2Int p;
		private bool asyncWorking;
		private Vector2Int worldPos;
		private Collider[] colliders = new Collider[36];
		private Vector3Int oldPos = Vector3Int.down;
		private bool teleportPending;
		private float teleportGravity;
		private float teleportJump;
		//
		#endregion

		private void Start()
		{
			InitGenerator();
			asyncWorking = true;
			VRCAsyncGPUReadback.Request(readbackTexture, 0, this);
		}

		private void InitGenerator()
		{
			localPlayer = Networking.LocalPlayer;
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

			initTexture = new Texture2D(32, 32, TextureFormat.RG16, false, true);
			initTexture.filterMode = FilterMode.Point;
			initTexture.SetPixels(0, 0, 32, 32, clearColors);

			//place tex
			optimizerMaterial.SetTexture("_CtrlTex", optimizerCtrl);

		}


		private void Update()
		{
			zonesCount = 0;
			clearChunksCount = 0;
			genChunk = false;
			if (optimizerUpdate)
			{
				optimizerCtrl.LoadRawTextureData(ignoreColors);
				optimizerUpdate = false;
			}

			Vector3Int pos = Vector3Int.FloorToInt(localPlayer.GetPosition() + localPlayer.GetVelocity() * Time.deltaTime + Vector3.up * 0.5f);
			if (oldPos != pos)
			{
				UpdateCenter(pos);
				UpdateColliders(oldPos, pos);
			}

			GPUGen();

			UpdateChangedBlocks();

			if (zonesCount > 0)
			{
				UpdateVoxelWorld();
			}

			if (optimizerUpdate)
			{
				optimizerCtrl.Apply();
				optimizer.Update();
			}

			oldPos = pos;
		}

		private void UpdateVoxelWorld()
		{
			voxelWorldMaterial.SetInt("_ZonesCount", zonesCount);
			voxelWorldMaterial.SetInt("_GenChunk", genChunk ? 1 : 0);
			voxelWorldMaterial.SetFloatArray("_BlockX", blockX);
			voxelWorldMaterial.SetFloatArray("_BlockZ", blockZ);
			voxelWorldMaterial.SetFloatArray("_BlockId", blockId);
			voxelWorldMaterial.SetVectorArray("_ZoneOffsets", zoneOffsets);
			voxelWorldMaterial.SetVectorArray("_ClearChunks", clearChunks);
			voxelWorldMaterial.SetInt("_ClearChunksCount", clearChunksCount);
			voxelWorld.SetUpdateZones(zones);
			voxelWorld.Update();
		}

		public void SetBlock(Vector3Int selectedBlock, byte block, byte old)
		{
			if (selectedBlock.y < 0 || selectedBlock.y > 127) return;
			int chunkX = selectedBlock.x >> 4;
			int chunkZ = selectedBlock.z >> 4;
			if (!IsChunkInCurrentWorld(new Vector2Int(chunkX, chunkZ))) return;
			smartChunks[(chunkX & 31) + ((chunkZ & 31) << 5)].SetBlock(selectedBlock, block);
			for (int i = 0; i < colliders.Length; i++)
			{
				Collider collider = colliders[i];
				if (Vector3Int.RoundToInt(collider.transform.localPosition) == selectedBlock)
				{
					collider.enabled = block != 0;
					break;
				}
			}
			setBlockValues[setBlocksCount] = block;
			setBlockPositions[setBlocksCount] = selectedBlock;
			setBlocksCount++;
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

		private void UpdateCenter(Vector3Int pos)
		{
			var oldP = p;
			if (pos.x > p.x + 32)
			{
				p.x = pos.x >> 5 << 5;
				if (pos.z > p.y + 16)
					p.y = (pos.z >> 5 << 5) + 32;
				else if (pos.z < p.y - 16)
					p.y = pos.z >> 5 << 5;
			}
			else if (pos.x < p.x - 32)
			{
				p.x = (pos.x >> 5 << 5) + 32;
				if (pos.z > p.y + 16)
					p.y = (pos.z >> 5 << 5) + 32;
				else if (pos.z < p.y - 16)
					p.y = pos.z >> 5 << 5;
			}

			if (pos.z > p.y + 32)
			{
				p.y = pos.z >> 5 << 5;
				if (pos.x > p.x + 16)
					p.x = (pos.x >> 5 << 5) + 32;
				else if (pos.x < p.x - 16)
					p.x = pos.x >> 5 << 5;
			}
			else if (pos.z < p.y - 32)
			{
				p.y = pos.z >> 5 << 5;
				if (pos.x > p.x + 16)
					p.x = (pos.x >> 5 << 5) + 32;
				else if (pos.x < p.x - 16)
					p.x = pos.x >> 5 << 5;
			}

			if (p != oldP)
			{
				Vector2Int newWorldPos = new Vector2Int(p.x >> 4, p.y >> 4);
				ClearChunks(worldPos, newWorldPos);
				optimizerUpdate = true;
				worldPos = newWorldPos;
				transform.position = new Vector3(p.x, transform.position.y, p.y);
			}
		}

		private void ClearChunks(Vector2Int oldWorldPos, Vector2Int newWorldPos)
		{
			Vector2Int minWorldPos = Vector2Int.Min(oldWorldPos, newWorldPos);
			Vector2Int delta = Vector2Int.Max(oldWorldPos, newWorldPos) - minWorldPos;
			minWorldPos = new Vector2Int((minWorldPos.x + 16) & 31, (minWorldPos.y + 16) & 31);
			if (delta.x >= 32 || delta.y >= 32)
			{
				AddClearZone(new Vector4(0, 0, 32, 32));

				GPUQueue = 0;
				readbackQueue = 0;
				if (!asyncWorking)
				{
					asyncWorking = true;
					VRCAsyncGPUReadback.Request(readbackTexture, 0, this);
				}
				return;
			}
			else if (minWorldPos.x + delta.x > 32)
			{
				AddClearZone(new Vector4(minWorldPos.x, 0, 32 - minWorldPos.x, 32));
				int minX = minWorldPos.x + delta.x - 32;
				AddClearZone(new Vector4(0, 0, minX, 32));
				if (delta.y > 0)
				{
					int width = minWorldPos.x - minX;
					if (minWorldPos.y + delta.y > 32)
					{
						AddClearZone(new Vector4(minX, minWorldPos.y, width, 32 - minWorldPos.y));
						AddClearZone(new Vector4(minX, 0, width, minWorldPos.y + delta.y - 32));
					}
					else
					{
						AddClearZone(new Vector4(minX, minWorldPos.y, width, delta.y));
					}
				}
			}
			else if (minWorldPos.y + delta.y > 32)
			{
				AddClearZone(new Vector4(0, minWorldPos.y, 32, 32 - minWorldPos.y));
				int minY = minWorldPos.y + delta.y - 32;
				AddClearZone(new Vector4(0, 0, 32, minY));
				if (delta.x > 0)
				{
					AddClearZone(new Vector4(minWorldPos.x, minY, delta.x, minWorldPos.y - minY));
				}
			}
			else if (delta.x > 0)
			{
				AddClearZone(new Vector4(minWorldPos.x, 0, delta.x, 32));
				if (delta.y > 0)
				{
					if (minWorldPos.x > 0)
						AddClearZone(new Vector4(0, minWorldPos.y, minWorldPos.x, delta.y));

					int endX = minWorldPos.x + delta.x;
					if (endX < 32)
						AddClearZone(new Vector4(endX, minWorldPos.y, 32 - endX, delta.y));
				}
			}
			else if (delta.y > 0)
			{
				AddClearZone(new Vector4(0, minWorldPos.y, 32, delta.y));
			}

			if (!asyncWorking)
			{
				asyncWorking = true;
				VRCAsyncGPUReadback.Request(readbackTexture, 0, this);
			}

			float moveDistance = delta.magnitude;
			GPUQueue = FindShiftedQueueStart(GPUQueue, moveDistance);
			readbackQueue = FindShiftedQueueStart(readbackQueue, moveDistance);
		}

		private int FindShiftedQueueStart(int queueIndex, float moveDistance)
		{
			int cappedIndex = Mathf.Min(queueIndex, 739);
			float R = ((Vector2)chunksQueueData[cappedIndex] + new Vector2(0.5f, 0.5f)).magnitude - moveDistance;
			float radiusSquared = Mathf.Max(0f, R);
			radiusSquared *= radiusSquared;
			int left = 0;
			int right = cappedIndex + 1;
			while (left < right)
			{
				int middle = (left + right) >> 1;
				Vector2 center = (Vector2)chunksQueueData[middle] + new Vector2(0.5f, 0.5f);
				if (center.sqrMagnitude < radiusSquared)
					left = middle + 1;
				else
					right = middle;
			}
			return Mathf.Max(0, left - 1);
		}

		private void AddClearZone(Vector4 region)
		{
			AddZone(chunkZone, 0);
			clearChunks[clearChunksCount] = region;
			clearChunksCount++;
			initTexture.SetPixels((int)region.x, (int)region.y, (int)region.z, (int)region.w, clearColors);

			int optimizerX = ((int)region.x >> 1) * 32;
			int optimizerY = ((int)region.y >> 1) * 12;
			int optimizerWidth = ((int)region.z >> 1) * 32;
			int optimizerHeight = ((int)region.w >> 1) * 12;
			optimizerCtrl.SetPixels(optimizerX, optimizerY, optimizerWidth, optimizerHeight, clearColors);
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
				if (initTexture.GetPixel(pos.x & 31, pos.y & 31).r != 0f)
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
			initTexture.SetPixel(x, z, Color.red);
		}

		public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
		{
			readbackRead = readbackRead && IsChunkInCurrentWorld(readbackReadPos);
			readbackRender = readbackRender && IsChunkInCurrentWorld(readbackRenderPos);

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
					initTexture.SetPixel(x, z, Color.white);
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
					initTexture.GetPixel(pos.x & 31, pos.y & 31).g != 0f)
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
			else asyncWorking = false;
		}

		private bool IsChunkInCurrentWorld(Vector2Int chunkPos)
		{
			Vector2Int offset = chunkPos - worldPos;
			return offset.x >= -16 && offset.x < 16 &&
				offset.y >= -16 && offset.y < 16;
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

		private void UpdateChangedBlocks()
		{
			if (setBlocksCount == 0) return;
			for (int i = 0; i < setBlocksCount; i++)
			{
				Vector3Int pos = setBlockPositions[i];
				blockX[zonesCount] = pos.x;
				blockZ[zonesCount] = pos.z;
				blockId[zonesCount] = setBlockValues[i];
				zoneOffsets[zonesCount] = new Vector4((pos.x + 256) & 511, (pos.z + 256) & 511, 0, 0);
				AddZone(zoneLayers, pos.y);
				var ch = new Vector2Int((pos.x >> 5 & 15) << 5, (pos.z >> 5 & 15) * 12);
				optimizerCtrl.SetPixel(pos.x & 511, ch.y + (pos.y >> 5), updateColors[0]);
				optimizerCtrl.SetPixel((pos.x + 1) & 511, ch.y + (pos.y >> 5), updateColors[0]);
				optimizerCtrl.SetPixel(ch.x + (pos.z & 31), ch.y + (pos.y >> 5) + 4, updateColors[0]);
				optimizerCtrl.SetPixel(ch.x + ((pos.z + 1) & 31), (((pos.z + 1) >> 5) & 15) * 12 + (pos.y >> 5) + 4, updateColors[0]);
				optimizerCtrl.SetPixel(ch.x + (pos.y & 31), ch.y + (pos.y >> 5) + 8, updateColors[0]);
				pos.y--;
				if (pos.y > -1)
					optimizerCtrl.SetPixel(ch.x + (pos.y & 31), ch.y + (pos.y >> 5) + 8, updateColors[0]);
			}
			optimizerUpdate = true;
			setBlocksCount = 0;
		}


		public void OnPlayerTeleport()
		{

		}

		public override void OnPlayerRespawn(VRCPlayerApi player)
		{
			if (player == localPlayer) OnPlayerTeleport();
		}

		private void OnDestroy()
		{
			Destroy(initTexture);
			Destroy(optimizerCtrl);
		}

		private int setBlocksCount;
		private Vector3Int[] setBlockPositions = new Vector3Int[128];
		private byte[] setBlockValues = new byte[128];

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
			Debug.LogWarning("<color=#00ff00><<<</color><color=#229911>MCGE</color><color=#00ff00>>>></color> " + msg);
		}
		private void DebugErr(string msg)
		{
			Debug.LogError("<color=#ff0000><<<</color><color=#229911>MCGE</color><color=#ff0000>>>></color> " + msg);
		}
	}

	public enum ChunkMeshType { PP, MP, PM, MM, EP, PE, EM, ME, EE, NN }
}
