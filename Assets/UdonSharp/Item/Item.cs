
using System;
using UdonSharp;
using UnityEngine;

public class Item : UdonSharpBehaviour
{
	[SerializeField] private string _nameEN;
	[SerializeField] private string _nameRU;
	[SerializeField] private int _count = 64;
	[SerializeField] private bool _isBlockItem;
	[SerializeField] private int _iconId;
	[SerializeField] private Vector2Int _craftSize;
	[SerializeField] private Vector3Int[] _craft;
	[SerializeField] private int _craftCount = 1;
	[SerializeField] private Vector2Int[] _alternateItems;
	[SerializeField] private SetType _setType = SetType.symple;
	[SerializeField] private int[] _sets = new int[1];
	[SerializeField] private ItemDataManager _itemDataManager;

	public string _name { get { return _itemDataManager == null || _itemDataManager.Language == "ru" ? _nameRU : _nameEN; } }
	public int maxCount { get { return _count; } }
	public bool isBlockItem { get { return _isBlockItem; } }
	public int iconId { get { return _iconId; } }
	public bool isCraftable { get { return _craftSize != Vector2Int.zero; } }
	public Vector2Int craftSize { get { return _craftSize; } }
	public Vector3Int[] craft { get { return _craft; } }
	public Vector2Int[] alternateItems { get { return _alternateItems; } }
	public SetType setType { get { return _setType; } }
	public int[] sets { get { return _sets; } }
}

[Serializable]
public enum SetType
{
	symple,
	side,
	front,
	none
}