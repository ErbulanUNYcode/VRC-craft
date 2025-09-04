using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Random = UnityEngine.Random;

public class WorldController : UdonSharpBehaviour
{
	#region refs
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
	[SerializeField] private Material sky;
	[SerializeField] private Material godRays;
	[SerializeField] private GameObject cubeCollider;
	[SerializeField] private Transform world;
	[SerializeField] private StructureDataManager structures;
	[SerializeField] private DebugConsole debugConsole;
	[SerializeField] private WorldData worldData;
	[SerializeField] private RotateLightCamera rotateLightCamera;
	[SerializeField] private RotateLightCamera rotateLightCameraVR;
	private Chunk[] chunksData;

	[SerializeField] private Vector3IntList setBlockPos;

	[SerializeField] private int treeTest;
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
	private bool isBlockChanged;
	#endregion

	private int generatedChunks = 0;
	private int chunksProgressor = 0;
	private int structProgressor = 0;

	private Texture2D worldTexture;
	[SerializeField] private Texture2D biomes;
	private Texture2D meshController;

	private Color32[] clearData;

	private bool isAnalise = true;
	private bool isPixelsChanged = false;
	private Color32[] stopAnalise = new Color32[193 * 84];
	private Color32[] analise = new Color32[129];

	private Vector3Int oldPlayerPos = new Vector3Int(0, 0, 0);
	private Vector3Int[] colliders = new Vector3Int[36];
	private Collider[] collidersObj = new Collider[36];

	private void Start()
	{
		chunksData = transform.GetChild(0).GetComponentsInChildren<Chunk>();
		player = Networking.LocalPlayer;
		if (player.isMaster)
		{
			if (seed == 0)
			{
				seed = Random.Range(1, 654654);
				RequestSerialization();
				debugConsole.Message("SetSeed:" + seed + " from master " + player.displayName);

				UpdateRandom();

				spawnPoint.transform.position = new Vector3(0.5f, Mathf.Max(GetLandscapeHeight(0, 0), GetMauntainHeight(0, 0)), -3.5f);

				player.TeleportTo(spawnPoint.transform.position, Quaternion.identity);
			}
		}

		worldOptimizer.SetFloat("_OffsetX", 0);
		worldOptimizer.SetFloat("_OffsetZ", 0);

		for (int i = 0; i < stopAnalise.Length; i++) stopAnalise[i] = new Color32(255, 0, 0, 0);
		for (int i = 0; i < analise.Length; i++) analise[i] = new Color32(255, 255, 0, 0);

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

		worldTexture.LoadRawTextureData(new byte[4096 * 2048 * 4]);

		Array.Copy(worldTexture.GetPixels32(), clearData = new Color32[4096 * 256], 4096 * 256);

		Debug.Log(clearData.Length);

		meshController.LoadRawTextureData(new byte[193 * 84 * 4]);

		meshController.Apply();

		worldTexture.Apply();

		miniWorld = true;
	}

	private float skyTime = 60f;
	[UdonSynced]
	private float syncSkyTime = 60f;



	public void SetSeed(int val)
	{
		seed = val;

		debugConsole.Message("SetSeed:" + seed + " from master " + Networking.Master.displayName);

		UpdateRandom();

		spawnPoint.transform.position = new Vector3(0.5f, Mathf.Max(GetLandscapeHeight(0, 0), GetMauntainHeight(0, 0)), -3.5f);

		player.TeleportTo(spawnPoint.transform.position, Quaternion.identity);
	}


	private void Update()
	{
		if (seed == 0) return;

		if (!spawnPlatform.activeSelf)
		{
			spawnPoint.transform.position = player.GetPosition();
			spawnPoint.transform.rotation = player.GetRotation();
		}

		WorldAnchor();

		if (Networking.IsOwner(gameObject)) syncSkyTime = skyTime;
		if (Mathf.Abs(syncSkyTime - skyTime) > 2) skyTime = syncSkyTime;
		skyTime += Time.deltaTime;
		var time = skyTime / 24f / 60f;
		worldMaterialX.SetFloat("_InputTime", time - 0.03f);
		worldMaterialY.SetFloat("_InputTime", time - 0.03f);
		worldMaterialZ.SetFloat("_InputTime", time - 0.03f);
		sky.SetFloat("_TimePower", time);
		godRays.SetFloat("_TimePower", time);
		if (rotateLightCamera != null)
			rotateLightCamera.SetTime(time);
		if (rotateLightCameraVR != null)
			rotateLightCameraVR.SetTime(time);


		if (isAnalise)
		{
			meshController.SetPixels32(0, 0, 193, 84, stopAnalise);
			isAnalise = false;
			isPixelsChanged = true;
		}

		if (generatedChunks < 144)
		{
			var pos = chankQueue[generatedChunks] + new Vector2Int((((int)world.position.x) >> 4), (int)(world.position.z / 16));
			var fpos = new Vector2Int((pos.x % 12 + 12) % 12, (pos.y % 12 + 12) % 12);
			while (chunksProgressor == 0 && chunksData[fpos.x + fpos.y * 12].state)
			{
				generatedChunks++;
				if (generatedChunks == 144) break;
				pos = chankQueue[generatedChunks] + new Vector2Int((int)(world.position.x / 16), (((int)world.position.z) >> 4));
				fpos = new Vector2Int((pos.x % 12 + 12) % 12, (pos.y % 12 + 12) % 12);
			}
			if (generatedChunks < 144)
			{
				if (GenerateChunk(pos))
				{
					generatedChunks++;
					chunksData[fpos.x + fpos.y * 12].state = true;
				}
				if (generatedChunks == 4)
				{
					//spawnPlatform.SetActive(false);

					for (int i = 0; i < 36; i++) collidersObj[i].enabled = InBlock(colliders[i]);
				}
			}
		}

		if (setBlockPos.Count > 0)
		{
			for (int i = 0; i < setBlockPos.Count; i++)
			{
				var pos = setBlockPos[i];
				pos.x += 96;
				pos.z += 96;

				var fx = pos.y;
				float px192 = (pos.x % 192 + 192) % 192;
				float pz192 = (pos.z % 192 + 192) % 192;
				var fy = Mathf.FloorToInt(px192 / 32) + (Mathf.FloorToInt(pz192 / 32)) * 6;

				meshController.SetPixel(fx, fy, new Color(1, 1, 0, 0));
				meshController.SetPixel(fx + 1, fy, new Color32(255, 255, 0, 0));

				fx = (pos.x % 193 + 193) % 193;
				fy = Mathf.FloorToInt(pz192 / 32) + Mathf.FloorToInt(((float)pos.y) / 32) * 6 + 36;
				meshController.SetPixel(fx, fy, new Color32(255, 255, 0, 0));
				meshController.SetPixel((fx + 1) % 193, fy, new Color32(255, 255, 0, 0));

				fx = (pos.z % 193 + 193) % 193;
				fy = Mathf.FloorToInt(px192 / 32) + Mathf.FloorToInt(((float)pos.y) / 32) * 6 + 60;
				meshController.SetPixel(fx, fy, new Color(255, 255, 0, 0));
				meshController.SetPixel((fx + 1) % 193, fy, new Color32(255, 255, 0, 0));
			}
			setBlockPos.Clear();
			isPixelsChanged = true;
			isAnalise = true;
		}

		var playerPos = Vector3Int.FloorToInt(player.GetPosition() + Vector3.up * 0.5f);

		if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyUp(KeyCode.Tab) || oldPlayerPos != playerPos)
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

				collidersObj[i].enabled = InBlock(colliders[i]) && (!Input.GetKey(KeyCode.Tab) || colliders[i] == playerPos - Vector3Int.up);
			}
		}

		if (Input.GetKeyDown(KeyCode.Tab)) player.Immobilize(true);

		if (Input.GetKeyUp(KeyCode.Tab)) player.Immobilize(false);

		if (isBlockChanged)
		{
			isBlockChanged = false;
			worldTexture.Apply();

			for (int i = 0; i < 36; i++) collidersObj[i].enabled = InBlock(colliders[i]);
		}

		if (isPixelsChanged)
		{
			isPixelsChanged = false;
			meshController.Apply();
		}
	}

	private void WorldAnchor()
	{
		var oldPosition = world.position;
		if (player.GetPosition().x < world.position.x - 32) world.position = world.position + Vector3.left * 32;
		if (player.GetPosition().x > world.position.x + 32) world.position = world.position + Vector3.right * 32;
		if (player.GetPosition().z < world.position.z - 32) world.position = world.position + Vector3.back * 32;
		if (player.GetPosition().z > world.position.z + 32) world.position = world.position + Vector3.forward * 32;
		if (oldPosition == world.position) return;

		if (world.position.x > oldPosition.x)
		{
			var pos = ((int)(oldPosition.x - 96) % 256 + 256) % 256;
			for (int i = 0; i < 16; i++) worldTexture.SetPixels32(pos + i * 256, 0, 32, 2048, clearData);
			for (int i = 0; i < 12; i++)
			{
				pos = (((int)world.position.x) >> 4) + 4;
				chunksData[(pos % 12 + 12) % 12 + i * 12].ClearStructures(worldData);
				pos = (((int)world.position.x) >> 4) + 5;
				chunksData[(pos % 12 + 12) % 12 + i * 12].ClearStructures(worldData);
			}
		}
		else if (world.position.x < oldPosition.x)
		{
			var pos = ((int)(world.position.x + 96) % 256 + 256) % 256;
			for (int i = 0; i < 16; i++) worldTexture.SetPixels32(pos + i * 256, 0, 32, 2048, clearData);
			for (int i = 0; i < 12; i++)
			{
				pos = (((int)world.position.x) >> 4) + 6;
				chunksData[(pos % 12 + 12) % 12 + i * 12].ClearStructures(worldData);
				pos = (((int)world.position.x) >> 4) + 7;
				chunksData[(pos % 12 + 12) % 12 + i * 12].ClearStructures(worldData);
			}
		}

		if (world.position.z > oldPosition.z)
		{
			var pos = ((int)(oldPosition.z - 96) % 256 + 256) % 256;
			for (int i = 0; i < 8; i++) worldTexture.SetPixels32(0, pos + i * 256, 4096, 32, clearData);
			for (int i = 0; i < 24; i++)
			{
				pos = (((int)world.position.z) >> 4) * 12 + 48;
				chunksData[(pos % 144 + 144) % 144 + i].ClearStructures(worldData);
			}
		}
		else if (world.position.z < oldPosition.z)
		{
			var pos = ((int)(world.position.z + 96) % 256 + 256) % 256;
			for (int i = 0; i < 8; i++) worldTexture.SetPixels32(0, pos + i * 256, 4096, 32, clearData);
			for (int i = 0; i < 24; i++)
			{
				pos = (((int)world.position.z) >> 4) * 12 + 72;
				chunksData[(pos % 144 + 144) % 144 + i].ClearStructures(worldData);
			}
		}

		worldTexture.Apply();

		generatedChunks = 0;
		chunksProgressor = 0;
		structProgressor = 0;

		worldOptimizer.SetFloat("_OffsetX", world.position.x / 32f);
		worldOptimizer.SetFloat("_OffsetZ", world.position.z / 32f);
	}

	private Color32[] generatorColors = new Color32[256];
	private bool GenerateChunk(Vector2Int position)
	{
		var x = position.x;
		var z = position.y;
		if (chunksProgressor == 0)//landscape+biome
		{
			x *= 16;
			z *= 16;

			var fx = (x & 255 + 256) & 255;
			var fy = (z & 255 + 256) & 255;
			for (var i = 0; i < 16; i++)
			{
				for (var j = 0; j < 16; j++)
				{
					byte height = (byte)GetLandscapeHeight(x + i, z + j);
					byte mHeight = (byte)GetMauntainHeight(x + i, z + j);
					generatorColors[i + j * 16] = new Color32(0, 0, height > mHeight ? height : mHeight, (byte)(height > mHeight ? 0 : 4));
				}
			}
			worldTexture.SetPixels32(fx, fy, 16, 16, generatorColors);
			worldTexture.Apply();
			chunksProgressor++;
		}
		else if (chunksProgressor == 1)//structures + their positions
		{
			var chunk = chunksData[(x % 12 + 12) % 12 + (z % 12 + 12) % 12 * 12];
			x *= 16;
			z *= 16;

			for (int i = 0; i < structures.Length; i++)
			{
				var structure = structures[i];
				var w = structure._size.x * 2 - 2;
				var h = structure._size.z * 2 - 2;
				var scx = Mathf.FloorToInt((float)x / w) * w;
				var scz = Mathf.FloorToInt((float)z / h) * h;
				var nscx = scx + w / 2;
				if (nscx > x) nscx -= w;
				var nscz = scz + h / 2;
				if (nscz > z) nscz -= h;

				switch (structure._type)
				{
					case 0://trees
						for (int ix = 0; ix < 2; ix++)
						{
							for (int iz = 0; iz < 2; iz++)
							{
								int startX = (ix == 0 ? scx : nscx);
								int startZ = (iz == 0 ? scz : nscz);

								for (int j = startX; j < x + 16; j += w)
								{
									for (int k = startZ; k < z + 16; k += h)
									{
										var noice = structure.GetStructureNoice(j, k);
										Random.InitState((int)(noice * 125_647) + i);
										var count = Mathf.RoundToInt(Random.Range(0, noice));
										var aStructure = structure[Random.Range(0, structure.length)];
										for (int l = 0; l < count; l++)
										{
											var pos = new Vector2Int(
												Random.Range(0, aStructure._size.x - 1) + j,
												Random.Range(0, aStructure._size.z - 1) + k
											);

											if (pos.x + aStructure.maxEnd.x <= x || pos.x + aStructure.minStart.x >= x + 16 ||
											   pos.y + aStructure.maxEnd.y <= z || pos.y + aStructure.minStart.y >= z + 16) continue;

											var approved = false;
											var height = 0;
											var randomHeight = 0;
											height = 128;
											foreach (var anchor in aStructure._anchors)
											{
												var a = anchor;

												var pos2 = new Vector2Int(
													pos.x + a.x,
													pos.y + a.y
												);

												var landscapeHeight = GetLandscapeHeight(pos2.x, pos2.y);
												var mHeight = GetMauntainHeight(pos2.x, pos2.y);
												var data = landscapeHeight > mHeight ? 0 : 1;
												foreach (var biome in aStructure._biomes)
												{
													if (data == biome)
													{
														approved = true;
													}
												}
												data = (landscapeHeight > mHeight ? landscapeHeight : mHeight) - aStructure._root;
												if (data < height) height = data;
											}

											randomHeight = Random.Range(0, aStructure._randomHeight);

											if (!approved) continue;

											chunk.AddStructure(
												aStructure,
												new Vector3Int(
													pos.x,
													height,
													pos.y
												)
												,
												randomHeight
											);
										}
									}
								}
							}
						}
						break;
					case 1://ores
						for (int ix = 0; ix < 2; ix++)
						{
							for (int iz = 0; iz < 2; iz++)
							{
								int startX = (ix == 0 ? scx : nscx);
								int startZ = (iz == 0 ? scz : nscz);

								for (int j = startX; j < x + 16; j += w)
								{
									for (int k = startZ; k < z + 16; k += h)
									{
										var noice = structure.GetStructureNoice(j, k);
										Random.InitState((int)(noice * 125_647) + i);
										var approved = Random.Range(0, 1000) < structure._root;
										var aStructure = structure[Random.Range(0, structure.length)];
										if (approved)
										{
											var pos = new Vector2Int(
												Random.Range(0, aStructure._size.x - 1) + j,
												Random.Range(0, aStructure._size.z - 1) + k
											);

											if (pos.x + aStructure._size.x <= x || pos.x >= x + 16 ||
											   pos.y + aStructure._size.z <= z || pos.y >= z + 16) continue;

											var height = 0;
											var randomHeight = 0;

											height = Random.Range(2, aStructure._randomHeight);

											var landscapeHeight = GetLandscapeHeight(pos.x, pos.y);
											var mHeight = GetMauntainHeight(pos.x, pos.y);
											var data = Mathf.Max(landscapeHeight, mHeight);

											if (data + 2 < height) continue;

											chunk.AddStructure(
												aStructure,
												new Vector3Int(
													pos.x,
													height,
													pos.y
												)
												,
												randomHeight
											);
										}
									}
								}
							}
						}
						break;
				}
			}
			chunksProgressor++;
		}
		else if (chunksProgressor == 2)//generate structures
		{
			var chunk = chunksData[(x % 12 + 12) % 12 + (z % 12 + 12) % 12 * 12];
			x *= 16;
			z *= 16;
			for (int i = structProgressor; i < chunk.Count; i++)
			{
				if (i == structProgressor + 10) break;
				var structure = chunk.StructureAt(i);
				var pos = chunk.PositionAt(i);
				var randomHeight = chunk.RandomHeightAt(i);
				switch (structure._type)
				{
					case 0://trees
						   //if (!structure.isAnalog && structure.Name.Contains("big") && !structure.Name.Contains("_2"))
						for (int Y = 0; Y < structure._size.y + randomHeight; Y++)
						{
							var fy = Y < structure._root ? Y : Mathf.Max(Y - randomHeight, structure._root);

							var mx = Mathf.Min(structure.ends[fy].x, x + 16 - pos.x);
							var mz = Mathf.Min(structure.ends[fy].y, z + 16 - pos.z);

							for (int Z = Mathf.Max(structure.starts[fy].y, z - pos.z); Z < mz; Z++)
							{
								var blocks = structure.GetBlocksData(fy, Z);
								for (int X = Mathf.Max(structure.starts[fy].x, x - pos.x); X < mx; X++)
								{
									var block = blocks[X];
									var sx = pos.x + X;
									var sy = pos.y + Y;
									var sz = pos.z + Z;

									var fx = (sx & 255 + 256) & 255;
									var fz = (sz & 255 + 256) & 255;

									fx += (sy & 15) * 256;
									fz += sy / 16 * 256;
									if (block.b == 0 && ((Color32)worldTexture.GetPixel(fx, fz)).r > 1) continue;
									var b = structure.GetRandom(sx, sy, sz) ? block.r : block.g;
									worldTexture.SetPixel(fx, fz, new Color32(b, 0, 0, 0));
								}
							}
						}
						break;
					case 1://ores
						{
							for (int Y = 0; Y < structure._size.y + randomHeight; Y++)
							{
								var fy = Y < structure._root ? Y : Mathf.Max(Y - randomHeight, structure._root);

								var mx = Mathf.Min(structure.ends[fy].x, x + 16 - pos.x);
								var mz = Mathf.Min(structure.ends[fy].y, z + 16 - pos.z);

								for (int Z = Mathf.Max(structure.starts[fy].y, z - pos.z); Z < mz; Z++)
								{
									var blocks = structure.GetBlocksData(fy, Z);
									for (int X = Mathf.Max(structure.starts[fy].x, x - pos.x); X < mx; X++)
									{
										var block = blocks[X];
										var sx = pos.x + X;
										var sy = pos.y + Y;
										var sz = pos.z + Z;

										var fx = (sx & 255 + 256) & 255;
										var fz = (sz & 255 + 256) & 255;

										var data = (Color32)worldTexture.GetPixel(fx, fz);
										data = (Color32)biomes.GetPixel(data.a, Mathf.Clamp(sy - data.b + 5, 0, 5));
										if (data.r != 2) continue;
										var b = structure.GetRandom(sx, sy, sz) ? block.r : block.g;
										fx += (sy & 15) * 256;
										fz += sy / 16 * 256;
										worldTexture.SetPixel(fx, fz, new Color32(b, 0, 0, 0));
									}
								}
							}
						}
						break;
				}
			}
			structProgressor += 10;
			worldTexture.Apply();
			if (structProgressor >= chunk.Count)
			{
				structProgressor = 0;
				chunksProgressor++;
			}
		}
		else if (chunksProgressor == 3)//generate builds
		{
			var chunk = chunksData[(x % 12 + 12) % 12 + (z % 12 + 12) % 12 * 12];

			var data = chunk.AddData(worldData.GetData(new Vector2Int(x, z))[0], new Vector2Int(x, z));

			var X = x * 16;
			var Z = z * 16;
			var fx = (X & 255 + 256) & 255;
			var fz = (Z & 255 + 256) & 255;

			var pos = 0;
			var levelOfNumber = 1;
			var blocksCount = 0;

			if (data != null)
			{
				for (int i = 0; i < data.Length; i++)
				{
					var b = data[i];
					if (b > 9)
					{
						if (blocksCount == 0) blocksCount = 1;
						if (b > 10)
							for (int j = 0; j < blocksCount; j++)
							{
								var p = pos + j;
								var sx = p & 15;
								var sz = (p >> 4) & 15;
								var sy = p >> 8;

								X = fx + sx + (sy & 15) * 256;
								Z = fz + sz + (sy >> 4) * 256;

								worldTexture.SetPixel(X, Z, new Color32((byte)(b - 10), 0, 0, 0));
							}

						pos += blocksCount;
						blocksCount = 0;
						levelOfNumber = 1;
					}
					else
					{
						var count = b * levelOfNumber;
						blocksCount += count;
						levelOfNumber *= 10;
					}
				}

				worldTexture.Apply();
				chunksProgressor++;
			}
			else
			{
				chunksProgressor += 2;
			}
		}
		else
		{ chunksProgressor++; }

		if (chunksProgressor == 5)//update mesh controller
		{
			x += 6;
			z += 6;
			var fx = (x % 12 + 12) % 12;
			var fz = (z % 12 + 12) % 12;
			fx >>= 1;
			fz >>= 1;
			meshController.SetPixels32(0, fx + fz * 6, 129, 1, analise);

			var f2x = ((x * 16) % 193 + 193) % 193;
			var f2z = ((z * 16) % 193 + 193) % 193;
			for (var i = 0; i < 4; i++)
			{
				if (f2x + 17 <= 193)
					meshController.SetPixels32(f2x, 36 + fz + i * 6, 17, 1, analise);
				else
				{
					var size = 193 - f2x;
					meshController.SetPixels32(f2x, 36 + fz + i * 6, size, 1, analise);
					meshController.SetPixels32(0, 36 + fz + i * 6, 17 - size, 1, analise);
				}

				if (f2z + 17 <= 193)
					meshController.SetPixels32(f2z, 60 + fx + i * 6, 17, 1, analise);
				else
				{
					var size = 193 - f2z;
					meshController.SetPixels32(f2z, 60 + fx + i * 6, size, 1, analise);
					meshController.SetPixels32(0, 60 + fx + i * 6, 17 - size, 1, analise);
				}
			}
			isPixelsChanged = true;
			isAnalise = true;
			isPixelsChanged = true;
			isAnalise = true;

			chunksProgressor = 0;
			return true;
		}

		return false;
	}

	public bool SetBlock(Vector3Int pos, int block)
	{

		if (pos.y < 1 || pos.y > 127 ||
			pos.x < world.position.x - 96 ||
			pos.x > world.position.x + 95 ||
			pos.z < world.position.z - 96 ||
			pos.z > world.position.z + 95) return false;

		var chX = pos.x >> 4;
		var chZ = pos.z >> 4;
		chX = (chX % 12 + 12) % 12;
		chZ = (chZ % 12 + 12) % 12;
		chunksData[chX + chZ * 12].SetBlock((pos.x & 15 + 16) & 15, pos.y, (pos.z & 15 + 16) & 15, (byte)(block + 10));

		setBlockPos.Add(pos);

		var fx = (pos.x & 255 + 256) & 255;
		var fy = (pos.z & 255 + 256) & 255;
		worldTexture.SetPixel(fx + (pos.y & 15) * 256, fy + (pos.y >> 4) * 256, new Color32((byte)block, 0, 0, 0));

		var px = fx * 4 + fy * 16_384;

		isBlockChanged = true;

		return true;
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

		for (int i = 0; i < structures.Length; i++)
		{
			var str = structures[i];
			str.SetNoiceOffset(GetNoiceOffset(str.noiceScale));
		}
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

	public int GetSeed()
	{
		return seed;
	}

	public bool InBlock(int x, int y, int z)
	{
		return InBlock(new Vector3Int(x, y, z));
	}
	public bool InBlock(Vector3Int pos)
	{
		if (!miniWorld) return false;
		var fx = (pos.x % 256 + 256) % 256;
		var fy = (pos.z % 256 + 256) % 256;

		var px = ((Color32)worldTexture.GetPixel(fx + (pos.y % 16) * 256, fy + pos.y / 16 * 256)).r;

		if (px != 0f)
		{
			return px > 1;
		}

		px = ((Color32)worldTexture.GetPixel(fx, fy)).b;

		return pos.y < px;
	}

	internal void SetChunksData(string[] intArray, int[] positions)
	{
		worldData.SetWorldData(intArray, positions);
	}

	private Vector2Int[] chankQueue =
	{
		new Vector2Int(0, 0),
		new Vector2Int(1, 0),
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(1, 1),
		new Vector2Int(1, -1),
		new Vector2Int(-1, 1),
		new Vector2Int(-1, -1),
		new Vector2Int(2, 0),
		new Vector2Int(0, 2),
		new Vector2Int(0, -2),
		new Vector2Int(-2, 0),
		new Vector2Int(2, 1),
		new Vector2Int(2, -1),
		new Vector2Int(1, 2),
		new Vector2Int(1, -2),
		new Vector2Int(-1, 2),
		new Vector2Int(-1, -2),
		new Vector2Int(-2, 1),
		new Vector2Int(-2, -1),
		new Vector2Int(2, 2),
		new Vector2Int(2, -2),
		new Vector2Int(-2, 2),
		new Vector2Int(-2, -2),
		new Vector2Int(3, 0),
		new Vector2Int(0, 3),
		new Vector2Int(0, -3),
		new Vector2Int(-3, 0),
		new Vector2Int(3, 1),
		new Vector2Int(3, -1),
		new Vector2Int(1, 3),
		new Vector2Int(1, -3),
		new Vector2Int(-1, 3),
		new Vector2Int(-1, -3),
		new Vector2Int(-3, 1),
		new Vector2Int(-3, -1),
		new Vector2Int(3, 2),
		new Vector2Int(3, -2),
		new Vector2Int(2, 3),
		new Vector2Int(2, -3),
		new Vector2Int(-2, 3),
		new Vector2Int(-2, -3),
		new Vector2Int(-3, 2),
		new Vector2Int(-3, -2),
		new Vector2Int(4, 0),
		new Vector2Int(0, 4),
		new Vector2Int(0, -4),
		new Vector2Int(-4, 0),
		new Vector2Int(4, 1),
		new Vector2Int(4, -1),
		new Vector2Int(1, 4),
		new Vector2Int(1, -4),
		new Vector2Int(-1, 4),
		new Vector2Int(-1, -4),
		new Vector2Int(-4, 1),
		new Vector2Int(-4, -1),
		new Vector2Int(3, 3),
		new Vector2Int(3, -3),
		new Vector2Int(-3, 3),
		new Vector2Int(-3, -3),
		new Vector2Int(4, 2),
		new Vector2Int(4, -2),
		new Vector2Int(2, 4),
		new Vector2Int(2, -4),
		new Vector2Int(-2, 4),
		new Vector2Int(-2, -4),
		new Vector2Int(-4, 2),
		new Vector2Int(-4, -2),
		new Vector2Int(5, 0),
		new Vector2Int(4, 3),
		new Vector2Int(4, -3),
		new Vector2Int(3, 4),
		new Vector2Int(3, -4),
		new Vector2Int(0, 5),
		new Vector2Int(0, -5),
		new Vector2Int(-3, 4),
		new Vector2Int(-3, -4),
		new Vector2Int(-4, 3),
		new Vector2Int(-4, -3),
		new Vector2Int(-5, 0),
		new Vector2Int(5, 1),
		new Vector2Int(5, -1),
		new Vector2Int(1, 5),
		new Vector2Int(1, -5),
		new Vector2Int(-1, 5),
		new Vector2Int(-1, -5),
		new Vector2Int(-5, 1),
		new Vector2Int(-5, -1),
		new Vector2Int(5, 2),
		new Vector2Int(5, -2),
		new Vector2Int(2, 5),
		new Vector2Int(2, -5),
		new Vector2Int(-2, 5),
		new Vector2Int(-2, -5),
		new Vector2Int(-5, 2),
		new Vector2Int(-5, -2),
		new Vector2Int(4, 4),
		new Vector2Int(4, -4),
		new Vector2Int(-4, 4),
		new Vector2Int(-4, -4),
		new Vector2Int(5, 3),
		new Vector2Int(5, -3),
		new Vector2Int(3, 5),
		new Vector2Int(3, -5),
		new Vector2Int(-3, 5),
		new Vector2Int(-3, -5),
		new Vector2Int(-5, 3),
		new Vector2Int(-5, -3),
		new Vector2Int(0, -6),
		new Vector2Int(-6, 0),
		new Vector2Int(1, -6),
		new Vector2Int(-1, -6),
		new Vector2Int(-6, 1),
		new Vector2Int(-6, -1),
		new Vector2Int(2, -6),
		new Vector2Int(-2, -6),
		new Vector2Int(-6, 2),
		new Vector2Int(-6, -2),
		new Vector2Int(5, 4),
		new Vector2Int(5, -4),
		new Vector2Int(4, 5),
		new Vector2Int(4, -5),
		new Vector2Int(-4, 5),
		new Vector2Int(-4, -5),
		new Vector2Int(-5, 4),
		new Vector2Int(-5, -4),
		new Vector2Int(3, -6),
		new Vector2Int(-3, -6),
		new Vector2Int(-6, 3),
		new Vector2Int(-6, -3),
		new Vector2Int(5, 5),
		new Vector2Int(5, -5),
		new Vector2Int(-5, 5),
		new Vector2Int(-5, -5),
		new Vector2Int(4, -6),
		new Vector2Int(-4, -6),
		new Vector2Int(-6, 4),
		new Vector2Int(-6, -4),
		new Vector2Int(5, -6),
		new Vector2Int(-5, -6),
		new Vector2Int(-6, 5),
		new Vector2Int(-6, -5),
		new Vector2Int(-6, -6)
	};
}