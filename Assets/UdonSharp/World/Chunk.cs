using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class Chunk : UdonSharpBehaviour
{
	private Structure[] structures = new Structure[128];
	private Vector3Int[] positions = new Vector3Int[128];
	private int[] randomHeights = new int[128];
	private int count = 0;
	private byte[] blocksData;
	private Vector2Int _position;
	private string owner;

	//stack comands
	private int stackCount = 0;
	private Vector3Int[] stackPositions = new Vector3Int[1];
	private int[] stackTo = new int[1];
	private int[] stackFrom = new int[1];
	private string[] stackOwners = new string[1];

	//fix
	private int fixCount = 0;
	private Vector3Int[] fixPositions = new Vector3Int[1];
	private int[] fixToBlocks = new int[1];
	private int[] fixLocalBlocks = new int[1];
	private string[] fixOwners = new string[1];

	/*
	0-9 is count of blocks
	10-255 is block type (block = value-10)
	*/
	public bool SetBlockLocal(Vector3Int pos, int block, NetworkManager networkManager)
	{
		if (!state) return false;

		if (hasOwner)
		{
			networkManager.SetBlock(pos, block, SetBlock(pos, block, -1));
			return true;
		}

		if (Networking.LocalPlayer.isMaster)
		{
			networkManager.PublicOwner(Networking.LocalPlayer.displayName, pos, block, SetBlock(pos, block, -1));
			return true;
		}

		networkManager.WantOwner(pos, block, SetBlock(pos, block, -1));

		return true;
	}
	public bool SetBlockNet(Vector3Int pos, int to, int from, string playerName, NetworkManager networkManager)
	{
		if (!state)
		{
			StackComand(pos, to, from, playerName);
			return false;
		}

		if (!hasOwner)
		{
			var fix1 = SetBlock(pos, to, from);

			if (fix1 != from)
			{
				StackFix(pos, to, fix1, playerName);
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
			networkManager.FixBack(pos, to, fix2, playerName);
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
		var index = x + (z << 4) + (y << 8);
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

	public void ClearStructures(WorldData dataSystem)
	{
		count = 0;
		structures = new Structure[128];
		positions = new Vector3Int[128];
		randomHeights = new int[128];
		_chunkProgressor = 0;

		if (blocksData == null) return;

		dataSystem.AddData(Convert.ToBase64String(blocksData), _position);

		blocksData = null; //clear blocks data
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
	public void nextProgressor(int next = 1) { _chunkProgressor += next; }

	public bool UpdateOwner(string owner, Vector2Int pos, DebugConsole debugConsole)
	{
		debugConsole.Message(_position + "->" + pos);
		if (pos != _position) return false;
		this.owner = owner;
		return true;
	}

	public void StackComand(Vector3Int pos, int block, int from, string playerName)
	{
		if (stackCount >= stackPositions.Length)
		{
			var newPositions = new Vector3Int[stackPositions.Length * 2];
			var newBlocks = new int[stackTo.Length * 2];
			var newOwners = new string[stackOwners.Length * 2];
			Array.Copy(stackPositions, newPositions, stackPositions.Length);
			Array.Copy(stackTo, newBlocks, stackTo.Length);
			Array.Copy(stackOwners, newOwners, stackOwners.Length);
			stackPositions = newPositions;
			stackTo = newBlocks;
			stackOwners = newOwners;
		}
		stackPositions[stackCount] = pos;
		stackTo[stackCount] = block;
		stackOwners[stackCount] = playerName;
		stackCount++;
	}

	public void StackFix(Vector3Int pos, int to, int from, string playerName)
	{
		if (fixCount >= fixPositions.Length)
		{
			var newPositions = new Vector3Int[fixPositions.Length * 2];
			var newToBlocks = new int[fixToBlocks.Length * 2];
			var newFromBlocks = new int[fixLocalBlocks.Length * 2];
			var newOwners = new string[fixOwners.Length * 2];
			Array.Copy(fixPositions, newPositions, fixPositions.Length);
			Array.Copy(fixToBlocks, newToBlocks, fixToBlocks.Length);
			Array.Copy(fixLocalBlocks, newFromBlocks, fixLocalBlocks.Length);
			Array.Copy(fixOwners, newOwners, fixOwners.Length);
			fixPositions = newPositions;
			fixToBlocks = newToBlocks;
			fixLocalBlocks = newFromBlocks;
			fixOwners = newOwners;
		}
		fixPositions[fixCount] = pos;
		fixToBlocks[fixCount] = to;
		fixLocalBlocks[fixCount] = from;
		fixOwners[fixCount] = playerName;
		fixCount++;
	}

	public void FixBack(NetworkManager networkManager)
	{
		if (fixCount == 0) return;
		networkManager.FixBack(fixPositions, fixToBlocks, fixLocalBlocks, fixOwners);
	}

	public bool SetBlockFix(Vector3Int pos, int fix, int oldTo)
	{
		/*if (!state)
		{
			StackComand(pos, fix, oldTo, null);
			return;
		}*/

		return SetBlock(pos, fix, oldTo) == oldTo;
	}

	public void LoadData(string[] data, Vector2Int pos)
	{
		_position = pos;

		if (data == null)
		{
			blocksData = null;
			owner = null;
		}
		else
		{
			if (data[0] != null)
				blocksData = Convert.FromBase64String(data[0]);
			else blocksData = null;
			owner = data[1];
		}
	}

	public void LoadData(string data, Vector2Int pos, DebugConsole debugConsole)
	{
		if (_position != pos) return;
		blocksData = Convert.FromBase64String(data);
		debugConsole.Message($"Loaded chunk at {pos} with {blocksData.Length} bytes of data");
		_chunkProgressor = Mathf.Min(_chunkProgressor, 3);
	}

	internal bool isLocalOwner(Vector2Int pos)
	{
		if (blocksData == null) return false;
		if (pos != _position) return false;
		if (owner == null) return false;
		return owner == Networking.LocalPlayer.displayName;
	}

	public int _chunkProgressor = 0;

	public bool hasOwner { get { return owner != null; } }

	public Vector2Int position { get { return _position; } }
}