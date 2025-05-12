
using System;
using UdonSharp;
using UnityEngine;

public class Vector3IntList : UdonSharpBehaviour
{
	private Vector3Int[] _data = new Vector3Int[1];
	private int _count;
	public void Create(Vector3Int[] data)
	{
		_count = data.Length;
		int size = 1;
		while (size < _data.Length)
		{
			size *= 2;
		}
		_data = new Vector3Int[size];
		Array.Copy(data, _data, _count);
	}

	public void Create(int count)
	{
		_count = count;
		int size = 1;
		while (size < _count)
		{
			size *= 2;
		}
		_data = new Vector3Int[size];
	}

	public void Add(Vector3Int value)
	{
		if (_count >= _data.Length)
		{
			Vector3Int[] newData = new Vector3Int[_data.Length * 2];
			for (int i = 0; i < _data.Length; i++)
			{
				newData[i] = _data[i];
			}
			_data = newData;
		}
		_data[_count] = value;
		_count++;
	}

	public Vector3Int this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
			{
				Debug.LogError("Index out of range");
				return Vector3Int.zero;
			}
			return _data[index];
		}

		set
		{
			if (index < 0 || index >= _count)
			{
				Debug.LogError("Index out of range");
				return;
			}
			_data[index] = value;
		}
	}

	public void Clear()
	{
		_data = new Vector3Int[1];
		_count = 0;
	}

	public Vector3Int[] GetArray()
	{
		Vector3Int[] result = new Vector3Int[_count];
		for (int i = 0; i < _count; i++)
		{
			Array.Copy(_data, result, _count);
		}
		return result;
	}

	public int Count { get { return _count; } }

	//find index of value
	public int IndexOf(Vector3Int value)
	{
		for (int i = 0; i < _count; i++)
		{
			if (_data[i] == value)
			{
				return i;
			}
		}
		return -1;
	}
}
