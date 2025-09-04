using System;
using UdonSharp;
using UnityEngine;

public class Chunk : UdonSharpBehaviour
{
	private Structure[] structures = new Structure[128];
	private Vector3Int[] positions = new Vector3Int[128];
	private int[] randomHeights = new int[128];
	private int count = 0;
	private byte[] blocksData;
	private Vector2Int position;

	/*
	0-9 is count of blocks
	10-255 is block type (block = value-10)
	*/
	public void SetBlock(int x, int y, int z, byte block)
	{
		var index = x + (z << 4) + (y << 8);

		if (block < 10 || index < 0 || index >= 32768)
		{
			Debug.LogError("Invalid block data: " + block + " at " + x + "," + y + "," + z + '\n' + index);
			return;
		}

		//check if block already exists
		if (blocksData == null)
		{
			blocksData = new byte[]
				{
					8,6,7,2,3,10//8192 empty(nature generated) blocks
				};
		}
		//find block type for in this block
		var pos = 0;
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
				if (pos + blocksCount > index)
				{
					var newFirstCount = index - pos;
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
						newBlocksData[startBlockPos++] = block; //block type
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

					break;
				}
				tempStartBlockPos = startBlockPos;
				tempBlocksCount = blocksCount;

				pos += blocksCount;
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
#if UNITY_EDITOR
		pos = 0;
		levelOfNumber = 1;
		blocksCount = 0;
		var debug = "";
		var debug1 = "[";
		for (int i = 0; i < blocksData.Length; i++)
		{
			debug1 += blocksData[i] + (i < blocksData.Length - 1 ? "," : "");
			var b = blocksData[i];
			if (b > 9)
			{
				debug += blocksCount + " pieces of {" + (b - 10) + "} blocks at position " + pos + '\n';
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
		debug1 += "]";
		debug = debug1 + "\n" + debug + Convert.ToBase64String(blocksData);
		Debug.Log(debug);
#endif
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
		state = false;

		if (blocksData == null) return;

		if (blocksData != null)
			dataSystem.AddData(Convert.ToBase64String(blocksData), position);

		blocksData = null; //clear blocks data
	}

	public byte[] AddData(string data, Vector2Int pos)
	{
		position = pos;

		if (data == null) blocksData = null;
		else
			blocksData = Convert.FromBase64String(data);

		return blocksData;
	}

	public Structure StructureAt(int index) { return structures[index]; }

	public Vector3Int PositionAt(int index) { return positions[index]; }

	public int RandomHeightAt(int index) { return randomHeights[index]; }

	public int Count => count;

	public bool state = false;
}