using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Chunk : UdonSharpBehaviour
{
	public void SetDebugConsole(DebugConsole console)
	{
		debugConsole = console;
	}

	private DebugConsole debugConsole;
	private Structure[] structures = new Structure[128];
	private Vector3Int[] positions = new Vector3Int[128];
	private int[] randomHeights = new int[128];
	private int count = 0;
	private byte[] blocksData;
	private Vector2Int _position;
	private bool _lastLoad = false;
	private string owner;

	//fix
	private int fixCount = 0;
	private Vector3Int[] fixPositions = new Vector3Int[1];
	private int[] fixToBlocks = new int[1];
	private int[] fixLocalBlocks = new int[1];

	/*
	0-9 is count of blocks
	10-255 is block type (block = value-10)
	*/
	public bool SetBlockLocal(Vector3Int pos, int block, NetworkManager networkManager, WorldData worldData)
	{
		if (!state) return false;

		if (owner == null)
		{
			networkManager.SetOwner(Networking.LocalPlayer.displayName, _position);
			if (Networking.LocalPlayer.isMaster)
			{
				worldData.SetOwner(Networking.LocalPlayer.displayName, _position);
				owner = Networking.LocalPlayer.displayName;
			}
		}

		networkManager.SetBlock(pos, block, SetBlock(pos, block, -1), false);

		return true;
	}

	public bool SetBlockNet(Vector3Int pos, int to, int from, NetworkManager networkManager)
	{
		if (owner == null)
		{
			var fix1 = SetBlock(pos, to, from);

			if (fix1 != from)
			{
				StackFix(pos, to, fix1);
				return false;
			}
			return true;
		}

		if (owner != Networking.LocalPlayer.displayName)
		{
			SetBlock(pos, to, -1);
			return true;
		}

		var fix2 = SetBlock(pos, to, from);

		if (fix2 != from)
		{
			networkManager.SetBlock(pos, fix2, to, true);
			return false;
		}

		return true;
	}

	public int SetBlock(Vector3Int pos, int block, int old)
	{
		var x = pos.x;
		var y = pos.y;
		var z = pos.z;

		x = x & 15;
		z = z & 15;
		var index = (x << 7) + (z << 11) + y;
		block = block + 10;
		//check if block already exists
		if (blocksData == null)
		{
			blocksData = new byte[]
				{
					8,6,7,2,3,10//32768 empty(nature generated) blocks
				};
		}
		//find block type for in this block
		var posID = 0;
		var levelOfNumber = 1;
		var startBlockPos = 0;
		var blocksCount = 0;
		var tempStartBlockPos = 0;
		var tempBlocksCount = 0;

		for (int i = 0; i < blocksData.Length; i++)
		{
			var b = blocksData[i];
			if (b > 9)
			{
				if (blocksCount == 0) blocksCount = 1; //at least one block
				if (posID + blocksCount > index)
				{
					if (old != -1 && old != b - 10)
					{
						return b - 10;
					}

					var newFirstCount = index - posID;
					var centerCount = 1;
					var newLastCount = blocksCount - newFirstCount - centerCount;

					var newDataLength = startBlockPos;
					if (newFirstCount != 0)
					{
						newDataLength += 1 + (newFirstCount == 1 ? 0 : Mathf.FloorToInt(Mathf.Log10(newFirstCount)) + 1);
					}
					else if (startBlockPos != 0 && blocksData[startBlockPos - 1] == block)
					{
						startBlockPos = tempStartBlockPos;
						newDataLength = startBlockPos;
						centerCount += tempBlocksCount;
					}

					if (newLastCount != 0)
					{
						newDataLength += 1 + (newLastCount == 1 ? 0 : Mathf.FloorToInt(Mathf.Log10(newLastCount)) + 1);
					}
					else if (i + 1 < blocksData.Length)
					{
						var j = i + 1;
						var tempLastCount = 0;
						levelOfNumber = 1;
						while (blocksData[j] < 10)
						{
							tempLastCount += blocksData[j] * levelOfNumber;
							levelOfNumber *= 10;
							j++;
						}
						if (tempLastCount == 0) tempLastCount = 1;
						if (blocksData[j] == block)
						{
							centerCount += tempLastCount;
							i = j; //skip this block
						}
					}

					newDataLength += 1 + (centerCount == 1 ? 0 : Mathf.FloorToInt(Mathf.Log10(centerCount)) + 1);

					newDataLength += blocksData.Length - i - 1;

					var newBlocksData = new byte[newDataLength];

					//first part
					Array.Copy(blocksData, 0, newBlocksData, 0, startBlockPos);

					//center first part
					if (newFirstCount > 0)
					{
						if (newFirstCount != 1)
							while (newFirstCount > 0)
							{
								newBlocksData[startBlockPos++] = (byte)(newFirstCount % 10);
								newFirstCount /= 10;
							}
						newBlocksData[startBlockPos++] = b; //block type
					}

					//center part
					{
						if (centerCount != 1)
							while (centerCount > 0)
							{
								newBlocksData[startBlockPos++] = (byte)(centerCount % 10);
								centerCount /= 10;
							}
						newBlocksData[startBlockPos++] = (byte)block; //block type
					}

					//center last part
					if (newLastCount > 0)
					{
						if (newLastCount != 1)
							while (newLastCount > 0)
							{
								newBlocksData[startBlockPos++] = (byte)(newLastCount % 10);
								newLastCount /= 10;
							}
						newBlocksData[startBlockPos++] = b; //block type
					}

					//last part
					Array.Copy(blocksData, i + 1, newBlocksData, startBlockPos, blocksData.Length - i - 1);

					blocksData = newBlocksData;


#if UNITY_EDITOR
					posID = 0;
					levelOfNumber = 1;
					blocksCount = 0;
					var debug = "";
					var debug1 = "[";
					for (int j = 0; j < blocksData.Length; j++)
					{
						debug1 += blocksData[j] + (j < blocksData.Length - 1 ? "," : "");
						var B = blocksData[j];
						if (B > 9)
						{
							debug += blocksCount + " pieces of {" + (B - 10) + "} blocks at position " + pos + '\n';
							posID += blocksCount;
							blocksCount = 0;
							levelOfNumber = 1;
						}
						else
						{
							var count = B * levelOfNumber;
							blocksCount += count;
							levelOfNumber *= 10;
						}
					}
					debug1 += "]";
					debug = debug1 + "\n" + debug + Convert.ToBase64String(blocksData);
					Debug.Log(debug);
#endif
					return b - 10;
				}
				tempStartBlockPos = startBlockPos;
				tempBlocksCount = blocksCount;

				posID += blocksCount;
				blocksCount = 0;
				levelOfNumber = 1;
				startBlockPos = i + 1;
			}
			else
			{
				var count = b * levelOfNumber;
				blocksCount += count;
				levelOfNumber *= 10;
			}
		}
		return -1;
	}

	internal void AddStructure(Structure structure, Vector3Int pos, int randomHeight)
	{
		structures[count] = structure;
		positions[count] = pos;
		randomHeights[count] = randomHeight;
		count++;
	}

	public void ClearStructures(Vector2Int pos)
	{
		_position = pos;
		count = 0;
		structures = new Structure[128];
		positions = new Vector3Int[128];
		randomHeights = new int[128];
		_chunkProgressor = 0;
		_structProgressor = 0;

		if (blocksData == null) return;

		owner = null;
		blocksData = null;
	}

	public byte[] GetData()
	{
		return blocksData;
	}

	public Structure StructureAt(int index) { return structures[index]; }

	public Vector3Int PositionAt(int index) { return positions[index]; }

	public int RandomHeightAt(int index) { return randomHeights[index]; }

	public int Count => count;

	public bool state { get { return _chunkProgressor == 5; } }
	public int chunkProgressor { get { return _chunkProgressor; } }
	public int structProgressor { get { return _structProgressor; } }
	public void NextProgressor(int next = 1) { _chunkProgressor += next; }
	internal void NextStruct(int next = 1) { _structProgressor += next; }

	public bool UpdateOwner(string owner, Vector2Int pos)
	{
		if (pos != _position) return false;
		this.owner = owner;
		if (owner == Networking.LocalPlayer.displayName) _lastLoad = true;
		return true;
	}
	public bool RemoveOwner(string _owner, Vector2Int pos)
	{
		if (pos != _position) return false;
		if (owner != _owner) return false;
		owner = null;
		return true;
	}

	public void StackFix(Vector3Int pos, int to, int from)
	{
		if (fixCount >= fixPositions.Length)
		{
			var newPositions = new Vector3Int[fixPositions.Length * 2];
			var newToBlocks = new int[fixToBlocks.Length * 2];
			var newFromBlocks = new int[fixLocalBlocks.Length * 2];
			Array.Copy(fixPositions, newPositions, fixPositions.Length);
			Array.Copy(fixToBlocks, newToBlocks, fixToBlocks.Length);
			Array.Copy(fixLocalBlocks, newFromBlocks, fixLocalBlocks.Length);
			fixPositions = newPositions;
			fixToBlocks = newToBlocks;
			fixLocalBlocks = newFromBlocks;
		}
		fixPositions[fixCount] = pos;
		fixToBlocks[fixCount] = to;
		fixLocalBlocks[fixCount] = from;
		fixCount++;
	}

	public void FixBack(NetworkManager networkManager)
	{
		if (fixCount == 0) return;
		for (int i = 0; i < fixCount; i++)
		{
			networkManager.SetBlock(fixPositions[i], fixToBlocks[i], fixLocalBlocks[i], true);
		}
		fixCount = 0;
	}

	public bool SetBlockFix(Vector3Int pos, int fix, int oldTo)
	{
		return SetBlock(pos, fix, oldTo) == oldTo;
	}

	public void LoadData(string[] data, Vector2Int pos)
	{
		_position = pos;
		_lastLoad = false;
		if (data == null)
		{
			blocksData = null;
			owner = null;
		}
		else
		{
			if (data[0] != null)
			{
				Debug.Log(data[0]);
				blocksData = Convert.FromBase64String(data[0]);
			}
			else blocksData = null;
			owner = data[1];
		}
	}

	public bool LoadData(string data, Vector2Int pos)
	{
		if (_lastLoad) return false;
		_lastLoad = true;
		if (_position != pos) return false;
		if (data == null)
			blocksData = null;
		else
		{
			Debug.Log(data);
			blocksData = Convert.FromBase64String(data);
		}
		_chunkProgressor = Mathf.Min(_chunkProgressor, 3);
		return true;
	}

	internal bool IsLocalOwner(Vector2Int pos)
	{
		if (pos != _position) return false;
		if (blocksData == null) return false;
		if (owner == null) return false;
		return owner == Networking.LocalPlayer.displayName;
	}

	public bool TryRemoveOwner(string playerName)
	{
		if (owner != null && owner == playerName)
		{
			owner = null;
			return true;
		}
		return false;
	}

	public int _chunkProgressor = 0;
	public int _structProgressor = 0;

	public Vector2Int position { get { return _position; } }
}