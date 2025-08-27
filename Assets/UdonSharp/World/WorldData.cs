
using System;
using UdonSharp;
using UnityEngine;

public class WorldData : UdonSharpBehaviour
{
	private string[] blocks = new string[1];
	Vector2Int[] positions = new Vector2Int[1];
	private int count = 0;

	public void AddData(string block, Vector2Int position)
	{
		if (block == null) return;

		for (int i = 0; i < count; i++)
		{
			if (positions[i] == position)
			{
				blocks[i] = block;
				return; // Update existing block
			}
		}

		if (count >= blocks.Length)
		{
			var newBlocks = new string[blocks.Length * 2];
			var newPositions = new Vector2Int[positions.Length * 2];
			Array.Copy(blocks, newBlocks, blocks.Length);
			Array.Copy(positions, newPositions, positions.Length);

			blocks = newBlocks;
			positions = newPositions;
		}
		blocks[count] = block;
		positions[count] = position;
		count++;
	}

	public string GetData(Vector2Int position)
	{
		for (int i = 0; i < count; i++)
		{
			if (positions[i] == position)
			{
				return blocks[i];
			}
		}

		return position.x + ":" + position.y;
	}
}
