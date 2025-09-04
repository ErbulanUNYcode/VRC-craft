using System;
using UdonSharp;
using UnityEngine;

public class WorldData : UdonSharpBehaviour
{
	private string[] owners = new string[1];
	private string[] world = new string[1];
	private Vector2Int[] positions = new Vector2Int[1];
	private int count = 0;

	public void AddData(string block, Vector2Int position)
	{
		if (block == null) return;

		int left = 0;
		int right = count - 1;

		while (left <= right)
		{
			int mid = (left + right) >> 1;

			int cmp = positions[mid].x - position.x;
			if (cmp == 0)
				cmp = positions[mid].y - position.y;

			if (cmp == 0)
			{
				world[mid] = block;
				return;
			}
			else if (cmp < 0)
				left = mid + 1;
			else
				right = mid - 1;
		}

		if (count >= world.Length)
			ExpandArrays();

		if (left < count)
		{
			Array.Copy(world, left, world, left + 1, count - left);
			Array.Copy(positions, left, positions, left + 1, count - left);
			Array.Copy(owners, left, owners, left + 1, count - left);
		}

		world[left] = block;
		positions[left] = position;
		owners[left] = null;
		count++;
	}


	public void SetOwner(string owner, Vector2Int position)
	{
		int left = 0;
		int right = count - 1;

		while (left <= right)
		{
			int mid = (left + right) >> 1;

			int cmp = positions[mid].x - position.x;
			if (cmp == 0)
				cmp = positions[mid].y - position.y;

			if (cmp == 0)
			{
				owners[mid] = owner;
				return;
			}
			else if (cmp < 0)
				left = mid + 1;
			else
				right = mid - 1;
		}

		if (count >= world.Length)
			ExpandArrays();

		if (left < count)
		{
			Array.Copy(world, left, world, left + 1, count - left);
			Array.Copy(positions, left, positions, left + 1, count - left);
			Array.Copy(owners, left, owners, left + 1, count - left);
		}

		world[left] = null;
		positions[left] = position;
		owners[left] = owner;
		count++;
	}


	private void ExpandArrays()
	{
		int newLength = world.Length * 2;

		var newWorld = new string[newLength];
		var newPositions = new Vector2Int[newLength];
		var newOwners = new string[newLength];

		Array.Copy(world, newWorld, world.Length);
		Array.Copy(positions, newPositions, positions.Length);
		Array.Copy(owners, newOwners, owners.Length);

		world = newWorld;
		positions = newPositions;
		owners = newOwners;
	}

	public string[] GetData(Vector2Int position)
	{
		int left = 0;
		int right = count - 1;

		while (left <= right)
		{
			int mid = (left + right) >> 1;
			int cmp = positions[mid].x - position.x;
			if (cmp == 0)
				cmp = positions[mid].y - position.y;

			if (cmp == 0)
				return new string[] { world[mid], owners[mid] };
			else if (cmp < 0)
				left = mid + 1;
			else
				right = mid - 1;
		}

		return null;
	}

	public string[] GetWorldData()
	{
		var data = new string[count * 2];
		Array.Copy(world, 0, data, 0, count);
		Array.Copy(owners, 0, data, count, count);
		return data;
	}

	public int[] GetPositions()
	{
		var data = new int[count * 2];
		int idx = 0;
		for (int i = 0; i < count; i++)
		{
			data[idx++] = positions[i].x;
			data[idx++] = positions[i].y;
		}
		return data;
	}

	public void SetWorldData(string[] Wdata, int[] Pdata)
	{
		count = Wdata.Length >> 1;

		world = new string[count];
		owners = new string[count];
		positions = new Vector2Int[count];

		Array.Copy(Wdata, 0, world, 0, count);
		Array.Copy(Wdata, count, owners, 0, count);

		int idx = 0;
		for (int i = 0; i < count; i++)
		{
			positions[i] = new Vector2Int(Pdata[idx++], Pdata[idx++]);
		}
	}
}
