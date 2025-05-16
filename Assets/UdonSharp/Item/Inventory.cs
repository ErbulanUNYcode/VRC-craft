
using UdonSharp;
using UnityEngine;

public class Inventory : UdonSharpBehaviour
{
	[SerializeField] private CellController[] cells;
	[SerializeField] private CellController[] arms;
	[SerializeField] private CellController[] craftingCells;
	[SerializeField] private CellController crafted;

}
