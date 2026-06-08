using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class WorldGenerator : UdonSharpBehaviour
{
#if UNITY_EDITOR
	[StepRange(4, 16, 2)]
#endif
	[SerializeField] private int visibleDistance = 4;
	#region Prefabs
	[SerializeField] private GameObject chunkRendererPrefab;
	#endregion
	#region Data
	[HideInInspector]
	[SerializeField] private Vector2Int[] chunksQueueData;
	#endregion
	#region Links
	[SerializeField] private Material worldMaterial;
	[SerializeField] private Material optimizatorMaterial;
	#endregion
	#region Cache
	private MeshFilter[] meshFilters;
	private Vector2Int[] chunksQueue;
	[SerializeField]
	private Texture2D worldTexture;
	[SerializeField]
	private Texture2D miniTex;
	[SerializeField]
	private Texture2D chunkInitPos;
	[SerializeField]
	private CustomRenderTexture optimizator;
	#endregion
	#region Systems
	[SerializeField]
	private ChunkMeshGenerator chunkMeshGenerator;
	#endregion
	void Start()
	{
		localPlayer = Networking.LocalPlayer;
		localPlayer.SetGravityStrength(3);

		if (miniTex != null) Destroy(worldTexture);
		miniTex = new Texture2D(256, 128, TextureFormat.R8, false);

		colliders = new Collider[36];
		for (int i = 0; i < 36; i++)
		{
			colliders[i] = Instantiate(colliderPrefab, collidersParent).GetComponent<Collider>();
			colliders[i].transform.localPosition = new Vector3(i % 3 - 1, i / 9 - 1, (i / 3) % 3 - 1);
		}

		InitChunkDistance();

		Debug.Log("World generator with " + visibleDistance * 2 + "x" + visibleDistance * 2 + " chunks started!");
		preQueueIndex = -1;
		chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.Realtime;
		optimizator.initializationMode = CustomRenderTextureUpdateMode.Realtime;
		GenerateChunk();
	}

	private void InitChunkDistance()
	{
		#region create/recreate + position   meshFilters
		if (meshFilters != null) foreach (var filter in meshFilters) Destroy(filter.gameObject);
		meshFilters = new MeshFilter[visibleDistance * visibleDistance];
		for (int i = 0; i < visibleDistance; i++)
		{
			for (int j = 0; j < visibleDistance; j++)
			{
				var chunk = Instantiate(chunkRendererPrefab, transform).transform;
				chunk.localPosition = new Vector3(-visibleDistance * 16 + i * 32, 0, -visibleDistance * 16 + j * 32);
				meshFilters[i * visibleDistance + j] = chunk.GetComponent<MeshFilter>();
				meshFilters[i * visibleDistance + j].mesh = chunkMeshGenerator.GetMesh(15);
			}
		}
		#endregion
		#region create queue
		chunksQueue = new Vector2Int[visibleDistance * visibleDistance * 4];
		var id = 0;
		for (int i = 0; i < chunksQueueData.Length; i++)
		{
			if
			(
				chunksQueueData[i].x < -visibleDistance ||
				chunksQueueData[i].x >= visibleDistance ||
				chunksQueueData[i].y < -visibleDistance ||
				chunksQueueData[i].y >= visibleDistance
			) continue;
			chunksQueue[id++] = chunksQueueData[i];
		}
		#endregion
		#region textures
		if (worldTexture != null) Destroy(worldTexture);
		worldTexture = new Texture2D(visibleDistance * 2 * 256, visibleDistance * 2 * 128, TextureFormat.R8, false);
		worldTexture.LoadRawTextureData(new byte[worldTexture.width * worldTexture.height]);
		worldTexture.Apply();
		worldMaterial.SetTexture("_MainTex", worldTexture);
		optimizatorMaterial.SetTexture("_WorldTex", worldTexture);

		if (chunkInitPos != null) Destroy(chunkInitPos);
		chunkInitPos = new Texture2D(visibleDistance * 2, visibleDistance * 2, TextureFormat.RGBA32, false);
		var chunkInitPosData = new Color[chunkInitPos.width * chunkInitPos.height];
		for (int i = 0; i < chunkInitPosData.Length; i++) chunkInitPosData[i] = Color.white;
		chunkInitPos.SetPixels(chunkInitPosData);
		chunkInitPos.Apply();
		#endregion
	}

	[SerializeField] private Material chunkGeneratorMaterial;
	[SerializeField] private CustomRenderTexture chunkGenerator;
	private int chunkQueueIndex = 0;
	private int preQueueIndex = 0;
	private void GenerateChunk()
	{
		var pos = chunksQueue[chunkQueueIndex] + new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16);
		chunkGeneratorMaterial.SetInt("_ChunkPosX", pos.x);
		chunkGeneratorMaterial.SetInt("_ChunkPosY", pos.y);
		VRCAsyncGPUReadback.Request(chunkGenerator, 0, (IUdonEventReceiver)this);
	}
	public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
	{
		if (preQueueIndex != -1)
		{
			if (request.hasError)
			{
				Debug.LogError("GPU readback error!");
				return;
			}
			else
			{
				var px = new byte[chunkGenerator.width * chunkGenerator.height];
				if (!request.TryGetData(px)) return;
				miniTex.LoadRawTextureData(px);
				miniTex.Apply();
				var pos = chunksQueue[preQueueIndex] + new Vector2Int((int)transform.position.x >> 4, (int)transform.position.z >> 4);
				worldTexture.SetPixels((pos.x * 256) & (worldTexture.width - 1), (pos.y * 128) & (worldTexture.height - 1), 256, 128, miniTex.GetPixels());
				worldTexture.Apply();
				optimizatorMaterial.SetInt("_ChunkX", pos.x);
				optimizatorMaterial.SetInt("_ChunkY", pos.y);
				pos += Vector2Int.one * 32768;
				chunkInitPos.SetPixel(pos.x % chunkInitPos.width, pos.y % chunkInitPos.height, new Color32((byte)(pos.x & 255), (byte)(pos.x >> 8), (byte)(pos.y & 255), (byte)(pos.y >> 8)));
				chunkInitPos.Apply();
			}
		}
		preQueueIndex = chunkQueueIndex;
		if (preQueueIndex == chunksQueue.Length)
		{
			Debug.Log("World generation complete!");
			return;
		}
		chunkQueueIndex++;
		if (chunkQueueIndex == chunksQueue.Length)
		{
			VRCAsyncGPUReadback.Request(chunkGenerator, 0, (IUdonEventReceiver)this);
			chunkGenerator.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			optimizator.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			return;
		}
		GenerateChunk();
	}
	[SerializeField] private GameObject colliderPrefab;
	[SerializeField] private Transform collidersParent;
	private Collider[] colliders;
	private VRCPlayerApi localPlayer;
	private byte GetBlock(Vector3Int pos)
	{
		if (pos.y > 127 || pos.y < 0) return 0;
		if (pos.y == 0) return 1;//tipa bedrock
		var ch = new Vector2Int(((pos.x >> 4) & 31) << 8, ((pos.z >> 4) & 31) << 7);
		return ((Color32)worldTexture.GetPixel(ch.x + (pos.x & 15) + ((pos.z & 15) << 4), ch.y + pos.y)).r;
	}
	private void Update()
	{
		var newPos = Vector3Int.FloorToInt(localPlayer.GetPosition());
		if (Vector3Int.RoundToInt(collidersParent.position) != newPos)
		{
			collidersParent.position = newPos;
			foreach (var collider in colliders)
			{
				collider.enabled = GetBlock(Vector3Int.RoundToInt(collider.transform.position)) != 0;
			}
		}
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
public class StepRangeAttribute : PropertyAttribute
{
	public float min;
	public float max;
	public float step;

	public StepRangeAttribute(float min, float max, float step)
	{
		this.min = min;
		this.max = max;
		this.step = Mathf.Max(step, 0.0001f);
	}
}

[CustomPropertyDrawer(typeof(StepRangeAttribute))]
public class StepRangeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		StepRangeAttribute range = (StepRangeAttribute)attribute;

		if (property.propertyType == SerializedPropertyType.Float)
		{
			float value = property.floatValue;

			value = EditorGUI.Slider(position, label, value, range.min, range.max);

			value = Snap(value, range.step);

			property.floatValue = value;
		}
		else if (property.propertyType == SerializedPropertyType.Integer)
		{
			int value = property.intValue;

			float f = EditorGUI.Slider(position, label, value, range.min, range.max);

			int snapped = Mathf.RoundToInt(Snap(f, range.step));

			property.intValue = snapped;
		}
		else
		{
			EditorGUI.LabelField(position, label.text, "StepRange only supports float/int");
		}
	}

	private float Snap(float value, float step)
	{
		if (step <= 0f) return value;
		return Mathf.Round(value / step) * step;
	}
}
#endif