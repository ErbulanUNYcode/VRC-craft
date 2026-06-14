using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class WorldGenerator : UdonSharpBehaviour
{
	[SerializeField] private GameObject chunkRendererPrefab;
	[SerializeField] private GameObject colliderPrefab;
	[SerializeField] private Transform collidersParent;

	[HideInInspector]
	[SerializeField] private Vector2Int[] chunksQueueData;

	[SerializeField] private Material worldMaterial;
	[SerializeField] private Material optimizatorMaterial;
	[SerializeField] private Material chunkGeneratorMaterial;

	private VRCPlayerApi localPlayer;
	private MeshFilter[] meshFilters = new MeshFilter[256];
	private Texture2D worldTexture;
	private Texture2D clearChunksTexture;
	private Texture2D miniTex;
	private Collider[] colliders = new Collider[36];
	private Vector3Int oldPos = Vector3Int.down;

	[SerializeField] private CustomRenderTexture optimizator;
	[SerializeField] private CustomRenderTexture chunkGenerator;

	[SerializeField] private ChunkMeshGenerator chunkMeshGenerator;

	[NonSerialized]
	public int preIndex = 0;
	[NonSerialized]
	public int chunkIndex = 0;
	private Vector2Int[] initPositions = new Vector2Int[1024];
	private Vector2Int worldPos;

	void Start()
	{
		for (int i = 0; i < 1024; i++) initPositions[i] = Vector2Int.one * 10_000_000;
		chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.Realtime;
		optimizator.initializationSource = CustomRenderTextureInitializationSource.TextureAndColor;
		optimizator.initializationColor = Color.clear;
		optimizator.Initialize();
		chunkMeshGenerator.CustomStart();
		localPlayer = Networking.LocalPlayer;
		localPlayer.SetGravityStrength(3);

		if (miniTex != null) Destroy(worldTexture);
		miniTex = new Texture2D(256, 128, TextureFormat.R8, false);

		for (int i = 0; i < 36; i++)
		{
			colliders[i] = Instantiate(colliderPrefab, collidersParent).GetComponent<Collider>();
			colliders[i].transform.localPosition = new Vector3(i % 3 - 1, i / 9 - 1, (i / 3) % 3 - 1);
		}

		InitWorld();

		Debug.Log("<<<SUPER CRAFT>>> World generator with " + 32 + "x" + 32 + " chunks started!");
		ReStartGeneration();
	}

	private void InitWorld()
	{
		#region create/recreate + position   meshFilters
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (new Vector2(-7.5f + i, -7.5f + j).sqrMagnitude > 64) continue;
				var chunk = Instantiate(chunkRendererPrefab, transform).transform;
				chunk.localPosition = new Vector3(-256 + i * 32, 0, -256 + j * 32);
				meshFilters[i * 16 + j] = chunk.GetComponent<MeshFilter>();
				meshFilters[i * 16 + j].mesh = chunkMeshGenerator.GetMesh(0);
			}
		}
		#endregion
		#region textures
		if (worldTexture != null) Destroy(worldTexture);
		worldTexture = new Texture2D(8192, 4096, TextureFormat.R8, false);
		worldTexture.LoadRawTextureData(new byte[worldTexture.width * worldTexture.height]);
		worldTexture.Apply();
		worldMaterial.SetTexture("_MainTex", worldTexture);
		optimizatorMaterial.SetTexture("_WorldTex", worldTexture);
		clearChunksTexture = new Texture2D(16, 16, TextureFormat.R8, false);
		clearChunksTexture.LoadRawTextureData(new byte[clearChunksTexture.width * clearChunksTexture.height]);
		clearChunksTexture.Apply();
		optimizatorMaterial.SetTexture("_ClearChunksTex", clearChunksTexture);
		#endregion
	}

	private void GenerateChunk()
	{
		var pos = chunksQueueData[chunkIndex] + new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16);
		chunkGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
		chunkGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
		VRCAsyncGPUReadback.Request(chunkGenerator, 0, (IUdonEventReceiver)this);
	}
	public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
	{
		if (request.hasError)
		{
			Debug.LogError("GPU readback error!");
			VRCAsyncGPUReadback.Request(chunkGenerator, 0, (IUdonEventReceiver)this);
			return;
		}

		Vector2Int pos;
		if (preIndex == -1)
		{
			optimizator.initializationSource = CustomRenderTextureInitializationSource.Material;
			optimizator.initializationMode = CustomRenderTextureUpdateMode.Realtime;

			preIndex = chunkIndex;

			do
			{
				chunkIndex++;
				if (chunkIndex == chunksQueueData.Length) break;
				pos = chunksQueueData[chunkIndex] + worldPos;
			} while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos && chunkIndex < chunksQueueData.Length);

			GenerateChunk();
			return;
		}


		if (chunkIndex == -1)
		{
			ReStartGeneration();
			return;
		}

		var px = new byte[chunkGenerator.width * chunkGenerator.height];
		if (!request.TryGetData(px)) return;

		miniTex.LoadRawTextureData(px);
		miniTex.Apply();
		pos = chunksQueueData[preIndex] + worldPos;
		worldTexture.SetPixels((pos.x * 256) & (worldTexture.width - 1), (pos.y * 128) & (worldTexture.height - 1), 256, 128, miniTex.GetPixels());
		worldTexture.Apply();
		optimizatorMaterial.SetInt("_ChunkX", pos.x);
		optimizatorMaterial.SetInt("_ChunkY", pos.y);
		initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] = pos;


		preIndex = chunkIndex;

		if (preIndex == chunksQueueData.Length)
		{
			Debug.Log("<<<SUPER CRAFT>>> World generation complete!");
			chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			optimizator.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			return;
		}

		if (chunkIndex + 1 == chunksQueueData.Length)
		{
			chunkIndex++;
			VRCAsyncGPUReadback.Request(chunkGenerator, 0, (IUdonEventReceiver)this);
			return;
		}
		do
		{
			chunkIndex++;
			pos = chunksQueueData[chunkIndex] + new Vector2Int((int)transform.position.x >> 4, (int)transform.position.z >> 4);
		} while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos && chunkIndex < chunksQueueData.Length - 1);
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
		var pos = Vector3Int.FloorToInt(localPlayer.GetPosition() + Vector3.up * 0.5f);
		if (oldPos == pos) return;
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
		if (p != Vector3Int.RoundToInt(transform.position))
		{
			transform.position = p;
			worldPos = new Vector2Int((int)transform.position.x >> 4, (int)transform.position.z >> 4);
			if (preIndex == chunksQueueData.Length)
			{
				ReStartGeneration();
			}
			else
				chunkIndex = -1;
		}

		#endregion
	}

	private void ReStartGeneration()
	{
		chunkIndex = 0;
		var pos = chunksQueueData[chunkIndex] + worldPos;
		while (initPositions[(pos.x & 31) + ((pos.y & 31) << 5)] == pos)
		{
			chunkIndex++;
			pos = chunksQueueData[chunkIndex] + worldPos;
		}
		chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.Realtime;
		preIndex = -1;
		GenerateChunk();
	}

	private void OnDestroy()
	{
		//destroy textures
		Destroy(worldTexture);
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

#if UNITY_EDITOR
//show info in inspector
[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var target = (WorldGenerator)serializedObject.targetObject;
		DrawDefaultInspector();
		GUILayout.Space(5);
		GUILayout.Label("chunkIndex/" + target.chunkIndex.ToString());
		GUILayout.Label("preIndex" + target.preIndex.ToString());
	}
}
#endif