using System;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class WorldData : UdonSharpBehaviour
{
	private string[] owners = new string[1];
	private string[] world = new string[1];
	private string[] startData = new string[1];
	private Vector2Int[] positions = new Vector2Int[1];
	private int count = 0;

	public void SetData(string block, Vector2Int position)
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
				startData[mid] = owners[mid] == null ? block : null;
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
			Array.Copy(startData, left, startData, left + 1, count - left);
		}

		world[left] = block;
		startData[left] = block;
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
				startData[mid] = owner == null ? world[mid] : null;
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
			Array.Copy(startData, left, startData, left + 1, count - left);
		}

		world[left] = null;
		startData[left] = null;
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
		var newStartData = new string[newLength];

		Array.Copy(world, newWorld, world.Length);
		Array.Copy(positions, newPositions, positions.Length);
		Array.Copy(owners, newOwners, owners.Length);
		Array.Copy(startData, newStartData, startData.Length);

		world = newWorld;
		positions = newPositions;
		owners = newOwners;
		startData = newStartData;
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
		return startData;
	}

	public string[] GetOwners()
	{
		return owners;
	}

	public Vector2Int[] GetPositions()
	{
		return positions;
	}

	public bool HasOwner(Vector2Int vector2Int)
	{
		var data = GetData(vector2Int);
		return data != null && data[1] != null;
	}

	public void RemoveOwner(string playerName)
	{
		for (int i = 0; i < count; i++)
		{
			if (owners[i] == playerName)
				owners[i] = null;
		}
	}

	public void LoadSnapshot(string[] _data, string[] _owners, Vector2Int[] _positions)
	{
		owners = _owners;
		world = _data;
		startData = _data;
		positions = _positions;
		count = _positions.Length;
	}

	public void RemovePlayerOwning(string playerName)
	{
		for (int i = 0; i < count; i++)
		{
			if (owners[i] == playerName)
			{
				owners[i] = null;
				startData[i] = world[i];
			}
		}
	}
}
