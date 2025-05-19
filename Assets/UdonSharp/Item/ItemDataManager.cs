
using UdonSharp;
using UnityEngine;

public class ItemDataManager : UdonSharpBehaviour
{
	[SerializeField] private Item[] items;
	public Item Item(int id)
	{
		return items[id];
	}
}
