
using UdonSharp;
using UnityEngine;

public class Inventory : UdonSharpBehaviour
{
	[SerializeField] private CellController[] cells;
	[SerializeField] private CellController[] arms;
	[SerializeField] private CellController[] craftingCells;
	[SerializeField] private CellController crafted;

	private int

	private CellController selected;

	public int TryGive(int id, int count)
	{
		foreach (CellController cell in cells)
		{
			count = cell.TryGive(id, count);
			if (count == 0) return 0;
		}
		return count;
	}

	private void Start()
	{
		TryGive(0, 10);
		TryGive(1, 15);
		TryGive(2, 3);
	}

	public bool Click(CellController cell)
	{
		if (selected == null)
		{
			selected = cell;
			return true;
		}

		selected.Shuffle(cell);
		selected = null;
		return false;
	}

	public void TryShuffle(Vector3 pos, CellController inputCell)
	{
		foreach (var cell in cells)
		{
			var offset = cell.transform.localPosition - pos;
			if (Mathf.Abs(offset.x) < 8 && Mathf.Abs(offset.y) < 8)
			{
				cell.Shuffle(inputCell);
				selected = null;
				return;
			}
		}
	}
	public bool TryOne(Vector3 pos, int id)
	{
		foreach (var cell in cells)
		{
			var offset = cell.transform.localPosition - pos;
			if (Mathf.Abs(offset.x) < 8 && Mathf.Abs(offset.y) < 8)
			{
				return cell.TryGive(id, 1) == 0;
			}
		}

		return false;
	}
}