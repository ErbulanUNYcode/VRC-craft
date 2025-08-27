using System;
using UdonSharp;
using UnityEngine;

public class Structure : UdonSharpBehaviour
{
	[SerializeField] public Vector3Int _size;
	[SerializeField] public Vector2Int[] _anchors;
	[SerializeField] public int _root;
	[SerializeField] public int[] _biomes;
	[SerializeField] public int _type = 0;
	private Vector2 noiceOffset;
	[SerializeField] private int _noiceScale = 64;
	[SerializeField] public int _randomHeight = 0;
	[SerializeField] private float randomScale = 0.7f;
	[SerializeField] private float randContrast = 0.4f;
	[SerializeField] private Structure generalParameters;
	[SerializeField] private Structure[] analogStructures;
	[SerializeField] public Vector2Int[] starts;
	[SerializeField] public Vector2Int[] ends;
	[SerializeField] private Color32[] fastMap;
	[SerializeField] int zStep;
	[SerializeField] int yStep;
#if UNITY_EDITOR
	[SerializeField] private int _tileSize;
	[SerializeField] private Texture2D _map;
	public string Name => _map != null ? _map.name : "Structure";
	[SerializeField] public bool isAnalog = false;

	public void UpdateStartsEnds()
	{
		_tileSize = _map.width / _size.x;
		zStep = _size.x;
		yStep = _size.x * _size.z;
		fastMap = new Color32[_size.x * _size.y * _size.z];
		for (int x = 0; x < _size.x; x++)
		{
			for (int y = 0; y < _size.y; y++)
			{
				for (int z = 0; z < _size.z; z++)
				{
					fastMap[x + z * zStep + y * yStep] = GetBlockDataEditor(x, y, z);
				}
			}
		}
		starts = new Vector2Int[_size.y];
		ends = new Vector2Int[_size.y];
		for (int i = 0; i < _size.y; i++)
		{
			starts[i] = new Vector2Int(_size.x, _size.z);
			ends[i] = new Vector2Int(-1, -1);
			for (int j = 0; j < _size.x; j++)
			{
				for (int k = 0; k < _size.z; k++)
				{
					var blockData = GetBlockDataEditor(j, i, k);
					if (blockData.r == 0 && blockData.g == 0) continue;
					if (starts[i].x > j) starts[i].x = j;
					if (starts[i].y > k) starts[i].y = k;
					if (ends[i].x <= j) ends[i].x = j + 1;
					if (ends[i].y <= k) ends[i].y = k + 1;
				}
			}
			if (starts[i].x == _size.x && starts[i].y == _size.z)
			{
				starts[i] = Vector2Int.zero;
				ends[i] = Vector2Int.zero;
			}
		}
	}

	public Color32 GetBlockDataEditor(int x, int y, int z)
	{
		return (Color32)_map.GetPixel(x + (y % _tileSize) * _size.x, z + (y / _tileSize) * _size.z);
	}
#endif

	public int length => analogStructures.Length + 1;

	public Structure this[int index]
	{
		get
		{
			return index < analogStructures.Length ? analogStructures[index] : this;
		}
	}

	public int noiceScale => _noiceScale;
	public void SetNoiceOffset(Vector2 offset)
	{
		if (generalParameters != null)
		{
			_noiceScale = generalParameters.noiceScale;
			noiceOffset = generalParameters.noiceOffset;

			foreach (var analog in analogStructures)
			{
				analog._noiceScale = _noiceScale;
				analog.noiceOffset = noiceOffset;
			}

			return;
		}
		noiceOffset = offset;
		foreach (var analog in analogStructures)
		{
			analog._noiceScale = _noiceScale;
			analog.noiceOffset = noiceOffset;
		}
	}

	public bool GetRandom(int x, int y, int z)
	{
		return Hash01(x + (int)noiceOffset.x, y, z + (int)noiceOffset.y);
	}

	public Color32 GetBlock(int x, int y, int z)
	{
		return fastMap[x + z * zStep + y * yStep];
	}

	public Color32[] GetBlocksData(int y, int z)
	{
		var pos = z * zStep + y * yStep;
		var lineOfMap = new Color32[_size.x];
		Array.Copy(fastMap, pos, lineOfMap, 0, _size.x);
		return lineOfMap;
	}

	private bool Hash01(int x, int y, int z)
	{
		int h = x * (202020253 >> (y & 2)) ^ y * (202020271 >> (z & 2)) ^ z * (202020311 >> (x & 2));
		h ^= (h >> 13);
		return ((h >> 1) & 1) == (h & 1);
	}

	public float GetStructureNoice(float x, float z)
	{
		return Mathf.Max((Mathf.PerlinNoise(
				   x / noiceScale + noiceOffset.x,
				   z / noiceScale + noiceOffset.y) + randomScale) * randContrast, 0);
	}
}