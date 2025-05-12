using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Random = UnityEngine.Random;

public class WorldController : UdonSharpBehaviour
{
	[UdonSynced] public string changeBlock = "0/0/0/0";
	public string _changeBlock = "0/0/0/0";

	[SerializeField] bool miniWorld;
	[SerializeField] private GameObject spawnPoint;
	[SerializeField] private GameObject spawnPlatform;
	[SerializeField] private int menuLocationSeed = 654654;
	[SerializeField] private GameObject loadingLine;
	[SerializeField] private GameObject loadingWindow;
	[SerializeField] private GameObject BaseUI;
	[SerializeField] private Material worldMaterialX;
	[SerializeField] private Material worldMaterialY;
	[SerializeField] private Material worldMaterialZ;
	[SerializeField] private Material worldOptimizer;
	[SerializeField] private GameObject cubeCollider;

	[SerializeField] private Vector3IntList setBlockPos;

	#region blocksData
	private BlockVisualType[] blocksVisualTypes;
	private GameObject[] blocks;
	#endregion

	private VRCPlayerApi player;

	#region menu world data
	private bool onMenuLocation = true;
	#endregion

	#region world data
	private int seed;
	private Vector2 landscapeNoiseOffset1;
	private int landscapeNoiseScale1 = 64;
	private Vector2 landscapeNoiseOffset2;
	private int landscapeNoiseScale2 = 256;
	private Vector2 mountainsNoiseOffset1;
	private int mountainsNoiseScale1 = 32;
	private Vector2 mountainsNoiseOffset2;
	private int mountainsNoiseScale2 = 256;
	byte[] byteData = new byte[4096 * 2048 * 4];
	private bool isBlockChanged;
	#endregion

	private int generatedChunks = 0;

	private Texture2D worldTexture;
	private Texture2D meshController;

	private bool isAnalise = true;
	private bool isPixelsChanged = false;
	private Color[] stopAnalise = new Color[193 * 84];
	private Color[] analise = new Color[129];

	private Vector3Int oldPlayerPos = new Vector3Int(0, 0, 0);
	private Vector3Int[] colliders = new Vector3Int[36];
	private Collider[] collidersObj = new Collider[36];

	private void Start()
	{
		for (int i = 0; i < stopAnalise.Length; i++)
		{
			stopAnalise[i] = new Color(1, 0, 0, 0);
		}

		for (int i = 0; i < analise.Length; i++)
		{
			analise[i] = new Color(1, 1, 0, 0);
		}

		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 4; k++)
				{
					colliders[i + j * 3 + k * 9] = new Vector3Int(i, k, j);
					collidersObj[i + j * 3 + k * 9] = Instantiate(cubeCollider).GetComponent<Collider>();
				}
			}
		}

		player = Networking.LocalPlayer;

		//init blocks data
		var blockComponents = GetComponentsInChildren<Block>();

		int n = blockComponents.Length;
		for (int i = 0; i < n - 1; i++)
		{
			for (int j = 0; j < n - i - 1; j++)
			{
				if (blockComponents[j].GetIndex() > blockComponents[j + 1].GetIndex())
				{
					var temp = blockComponents[j];
					blockComponents[j] = blockComponents[j + 1];
					blockComponents[j + 1] = temp;
				}
			}
		}

		blocks = new GameObject[blockComponents.Length + 1];
		blocksVisualTypes = new BlockVisualType[blockComponents.Length + 1];
		for (int i = 0; i < blockComponents.Length; i++)
		{
			blocks[i + 1] = blockComponents[i].GetBlock();
			blocksVisualTypes[i + 1] = blockComponents[i].GetVisualType();
		}

		UpdateRandom();

		spawnPoint.transform.position = new Vector3(0, GetLandscapeHeight(0, 0) + 2, 0);

		worldTexture = new Texture2D(4_096, 2_048, TextureFormat.RGBA32, false, true);
		worldTexture.filterMode = FilterMode.Point;
		worldTexture.wrapMode = TextureWrapMode.Clamp;
		worldTexture.ignoreMipmapLimit = true;


		meshController = new Texture2D(193, 84, TextureFormat.RGBA32, false, true);
		meshController.filterMode = FilterMode.Point;
		meshController.wrapMode = TextureWrapMode.Clamp;
		meshController.ignoreMipmapLimit = true;

		worldMaterialX.SetTexture("_MainTex", worldTexture);
		worldMaterialY.SetTexture("_MainTex", worldTexture);
		worldMaterialZ.SetTexture("_MainTex", worldTexture);
		worldOptimizer.SetTexture("_WorldTex", worldTexture);
		worldOptimizer.SetTexture("_ControllerTex", meshController);

		worldTexture.LoadRawTextureData(byteData);

		meshController.LoadRawTextureData(new byte[193 * 84 * 4]);

		meshController.Apply();

		worldTexture.Apply();
	}

	[UdonSynced]
	private float m_frame = 0f;

	private void Update()
	{
		m_frame += Time.deltaTime / 60 / 12;
		worldMaterialX.SetFloat("_InputTime", m_frame);
		worldMaterialY.SetFloat("_InputTime", m_frame);
		worldMaterialZ.SetFloat("_InputTime", m_frame);

		if (isAnalise)
		{
			meshController.SetPixels(0, 0, 193, 84, stopAnalise);
			isAnalise = false;
			isPixelsChanged = true;
		}

		if (generatedChunks < 144)
		{
			GenerateChunk(generatedChunks % 12 - 6, generatedChunks / 12 - 6);
			generatedChunks++;
			if (generatedChunks == 144)
			{
				spawnPlatform.SetActive(false);
			}
		}

		if (setBlockPos.Count > 0)
		{
			for (int i = 0; i < setBlockPos.Count; i++)
			{
				var pos = setBlockPos[i];

				var fx = pos.y;
				var fy = Mathf.FloorToInt(((float)pos.x) / 32) + 3 + (Mathf.FloorToInt(((float)pos.z) / 32) + 3) * 6;

				meshController.SetPixel(fx, fy, new Color(1, 1, 0, 0));
				meshController.SetPixel(fx + 1, fy, new Color(1, 1, 0, 0));

				fx = pos.x + 96;
				fy = Mathf.FloorToInt(((float)pos.z) / 32) + 3 + Mathf.FloorToInt(((float)pos.y) / 32) * 6 + 36;
				meshController.SetPixel(fx, fy, new Color(1, 1, 0, 0));
				meshController.SetPixel(fx + 1, fy, new Color(1, 1, 0, 0));

				fx = pos.z + 96;
				fy = Mathf.FloorToInt(((float)pos.x) / 32) + 3 + Mathf.FloorToInt(((float)pos.y) / 32) * 6 + 60;
				meshController.SetPixel(fx, fy, new Color(1, 1, 0, 0));
				meshController.SetPixel(fx + 1, fy, new Color(1, 1, 0, 0));
			}
			setBlockPos.Clear();
			isPixelsChanged = true;
			isAnalise = true;
		}

		var playerPos = Vector3Int.FloorToInt(player.GetPosition());

		if (oldPlayerPos != playerPos)
		{
			oldPlayerPos = playerPos;

			for (int i = 0; i < 36; i++)
			{
				while (colliders[i].x < playerPos.x - 1) colliders[i].x += 3;
				while (colliders[i].x > playerPos.x + 1) colliders[i].x -= 3;
				while (colliders[i].z < playerPos.z - 1) colliders[i].z += 3;
				while (colliders[i].z > playerPos.z + 1) colliders[i].z -= 3;
				while (colliders[i].y < playerPos.y - 1) colliders[i].y += 4;
				while (colliders[i].y > playerPos.y + 2) colliders[i].y -= 4;

				collidersObj[i].transform.position = colliders[i] + Vector3.one / 2f;

				collidersObj[i].enabled = InBlock(colliders[i]);
			}
		}

		if (isBlockChanged)
		{
			isBlockChanged = false;

			worldTexture.LoadRawTextureData(byteData);
			worldTexture.Apply();


			for (int i = 0; i < 36; i++)
			{
				collidersObj[i].enabled = InBlock(colliders[i]);
			}
		}

		if (isPixelsChanged)
		{
			isPixelsChanged = false;
			meshController.Apply();
		}
	}

	private void GenerateChunk(int x, int z)
	{
		x *= 16;
		z *= 16;
		var fx = (x + 8192) % 256;
		var fy = (z + 8192) % 256;

		for (var i = 0; i < 16; i++)
		{
			for (var j = 0; j < 16; j++)
			{
				var px = (i + fx) * 4 + (j + fy) * 16_384;

				var height = GetLandscapeHeight(x + i, z + j);
				var mHeight = GetMauntainHeight(x + i, z + j);
				byteData[px + 2] = (byte)mHeight;
				byteData[px + 2 + 1024] = (byte)height;
			}
		}
		worldTexture.LoadRawTextureData(byteData);
		worldTexture.Apply();

		x = Mathf.FloorToInt(((float)x / 32) + 3);
		z = Mathf.FloorToInt(((float)z / 32) + 3);

		meshController.SetPixels(0, x + z * 6, 129, 1, analise);
		for (var i = 0; i < 4; i++) meshController.SetPixels(x * 32, 36 + z + i * 6, 33, 1, analise);
		for (var i = 0; i < 4; i++) meshController.SetPixels(z * 32, 60 + x + i * 6, 33, 1, analise);
		isPixelsChanged = true;
		isAnalise = true;
	}

	public void SetBlock(Vector3Int pos, int block)
	{
		if (pos.y < 0 || pos.y > 127) return;

		setBlockPos.Add(pos);

		var fx = (pos.x + 8192) % 256;
		var fy = (pos.z + 8192) % 256;

		var px = fx * 4 + fy * 16_384;

		byteData[px + (pos.y % 16 * 1024) + (pos.y / 16) * 4_194_304] = (byte)block;

		isBlockChanged = true;
	}

	private int GetLandscapeHeight(float x, float z)
	{
		return
			Mathf.RoundToInt(
				Mathf.PerlinNoise(
					x / landscapeNoiseScale1 + landscapeNoiseOffset1.x,
					z / landscapeNoiseScale1 + landscapeNoiseOffset1.y) *
				Mathf.PerlinNoise(
					x / landscapeNoiseScale2 + landscapeNoiseOffset2.x,
					z / landscapeNoiseScale2 + landscapeNoiseOffset2.y) * 40 + 30);
	}

	private int GetMauntainHeight(float x, float z)
	{
		return
			Mathf.Max(1, Mathf.RoundToInt(
				(Mathf.PerlinNoise(
					x / mountainsNoiseScale1 + mountainsNoiseOffset1.x,
					z / mountainsNoiseScale1 + mountainsNoiseOffset1.y) * 30 + 60) *
				((1 - Mathf.Pow(1 - Mathf.PerlinNoise(
					x / mountainsNoiseScale2 + mountainsNoiseOffset2.x,
					z / mountainsNoiseScale2 + mountainsNoiseOffset2.y), 2)) * 6 - 5)));
	}

	private void UpdateRandom()
	{
		Random.InitState(GetSeed());

		landscapeNoiseOffset1 = GetNoiceOffset(landscapeNoiseScale1);
		landscapeNoiseOffset2 = GetNoiceOffset(landscapeNoiseScale2);

		mountainsNoiseOffset1 = GetNoiceOffset(mountainsNoiseScale1);
		mountainsNoiseOffset2 = GetNoiceOffset(mountainsNoiseScale2);
	}

	private Vector2 GetNoiceOffset(float scale)
	{
		scale = 4096 / scale;
		var x = Random.Range(-10000 + scale * 2, 10000 - scale * 2);
		if (x < 0) x -= scale; else x += scale;
		var z = Random.Range(-10000 + scale * 2, 10000 - scale * 2);
		if (z < 0) z -= scale; else z += scale;
		return new Vector2(x, z);
	}

	private int GetSeed()
	{
		if (onMenuLocation) return menuLocationSeed;
		return seed;
	}

	public bool InBlock(Vector3Int pos)
	{
		if (pos.y < 0 || pos.y > 127) return false;

		var fx = (pos.x + 8192) % 256;
		var fy = (pos.z + 8192) % 256;
		var px = fx * 4 + fy * 16_384;

		var h = byteData[px + 2];

		if (h == 0) return false;

		var block = byteData[px + (pos.y % 16 * 1024) + (pos.y / 16) * 4_194_304];

		if (block == 0)
		{
			if (pos.y < h) return true;
			h = byteData[px + 2 + 1024];
			return pos.y < h;
		}

		return block > 1;
	}
}
