
using UdonSharp;
using UnityEngine;

public class Item : UdonSharpBehaviour
{
	[SerializeField] private int _id;
	[SerializeField] private string _name;
	[SerializeField] private int _count = 64;
	[SerializeField] private bool _isBlockItem;
	[SerializeField] private int _blockItemId;
	[SerializeField] private Sprite _icon;

	public int id => _id;
	public string nameInfo => _name;
	public int maxCount => _count;
	public bool isBlockItem => _isBlockItem;

	public int blockItemId => _blockItemId;

	public Sprite icon => _icon;
}