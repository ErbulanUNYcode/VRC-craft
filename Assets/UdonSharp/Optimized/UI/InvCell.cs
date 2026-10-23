using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class InvCell : UdonSharpBehaviour
	{
		[SerializeField] private CellType type;
		[SerializeField] private Image image;
		[SerializeField] private InvSystem invSystem;
		[SerializeField] private CanvasRenderer icon;
		[SerializeField] private ItemData itemData;
		[SerializeField] private TextMeshProUGUI countText;
		[SerializeField] private Slider selectSlider;
		[SerializeField] private InvCell hotBar;
		private int count = 0;
		private byte itemType = 0;

		public void OnEnter()
		{
			image.color = new Color(1, 1, 1, 0.2f);
			invSystem.EnterCell(this, count > 0 ? itemData.GetName(itemType) : null);
		}

		public void OnExit()
		{
			image.color = new Color(1, 1, 1, 0);
			invSystem.ExitCell(this);
		}

		public void GiveSavedItems(byte _itemType, int _count)
		{
			icon.materialCount = 1;
			if (_count == 0) return;

			itemType = _itemType;
			icon.SetMesh(itemData.GetMesh(_itemType));
			icon.SetMaterial(itemData.GetMaterial(_itemType), 0);

			count = _count;

			countText.text = count < 2 ? "" : count.ToString();
			if (hotBar != null)
			{
				hotBar.CloneThis(this);
				if (hand != null)
				{
					hand.SetMaterial(itemData.GetMaterial(_itemType), 0);
					hand.SetMesh(itemData.GetMesh(_itemType));
				}
			}
		}

		private void CloneThis(InvCell invCell)
		{
			countText.text = invCell.countText.text;
			icon.materialCount = 1;
			icon.SetMaterial(invCell.icon.GetMaterial(0), 0);
			icon.SetMesh(invCell.icon.GetMesh());
			if (hand != null)
			{
				hand.SetMaterial(invCell.icon.GetMaterial(0), 0);
				hand.SetMesh(invCell.icon.GetMesh());
			}
		}

		private int selectCount = 0;
		public string TrySelect()
		{
			if (count == 0) return null;
			selectCount = count;
			countText.text = "0";
			if (hotBar != null) hotBar.countText.text = "0";
			count = 0;
			dropStarted = DateTime.Now;
			return selectCount.ToString();
		}
		public string TrySelectHalf()
		{
			if (count == 0) return null;
			selectCount = count - count / 2;
			count = count - selectCount;
			countText.text = count < 2 ? "" : count.ToString();
			if (hotBar != null) hotBar.countText.text = countText.text;
			dropStarted = DateTime.Now;
			return selectCount.ToString();
		}

		private bool dropType = false;
		private DateTime dropStarted;

		private InvCell[] dropCells = new InvCell[40];
		private int dropCellsCount;
		public void SetDropType(bool v)
		{
			dropType = v;
			dropCellsCount = 0;
		}
		public string TryDropTo(InvCell enteredCell, string text)
		{
			if (dropCellsCount == selectCount) return text;//full used
			if (enteredCell.type == CellType.craft) return text;//dont drop to craft
			for (int i = 0; i < dropCellsCount; i++)
			{
				if (dropCells[i] == enteredCell) return text;//is in drop cells
			}
			if (enteredCell.count > 0 && enteredCell.itemType != itemType) return text;//reserved type
			var maxCount = itemData.MaxCount(itemType);
			if (enteredCell.count == maxCount) return text;//cell is full
			dropCells[dropCellsCount++] = enteredCell;
			if (enteredCell.count == 0 && enteredCell != this)
			{
				enteredCell.itemType = itemType;
				enteredCell.icon.materialCount = 1;
				enteredCell.icon.SetMaterial(icon.GetMaterial(0), 0);
				enteredCell.icon.SetMesh(icon.GetMesh());
				if (enteredCell.hotBar != null)
				{
					enteredCell.hotBar.icon.materialCount = 1;
					enteredCell.hotBar.icon.SetMaterial(icon.GetMaterial(0), 0);
					enteredCell.hotBar.icon.SetMesh(icon.GetMesh());
					var hbh = enteredCell.hand;
					if (hbh != null)
					{
						hbh.SetMaterial(icon.GetMaterial(0), 0);
						hbh.SetMesh(icon.GetMesh());
					}
				}
			}
			var dropCounts = dropType ? selectCount / dropCellsCount : 1;
			var dropRemain = selectCount - dropCounts * dropCellsCount;
			for (int i = 0; i < dropCellsCount; i++)
			{
				var dropCell = dropCells[i];
				var sum = dropCell.count + dropCounts;
				if (sum > maxCount)
				{
					dropRemain += sum - maxCount;
					sum = maxCount;
				}
				dropCell.countText.text = sum < 2 ? "" : sum.ToString();
				if (dropCell.hotBar != null) dropCell.hotBar.countText.text = dropCell.countText.text;
			}

			return dropRemain.ToString();
		}

		public string TryDrop(string text, InvCell enteredCell)
		{
			if (dropCellsCount == 0)
			{
				if (enteredCell == null || enteredCell.type == CellType.craft || count > 0) return text;
				count = selectCount;
				dropCellsCount = 0;
				//shuffle
				var tempType = itemType;
				var tempCount = count;
				itemType = enteredCell.itemType;
				count = enteredCell.count;
				icon.SetMaterial(enteredCell.icon.GetMaterial(0), 0);
				icon.SetMesh(enteredCell.icon.GetMesh());
				countText.text = count < 2 ? "" : count.ToString();
				if (hotBar != null)
				{
					hotBar.CloneThis(this);
					if (hand != null)
					{
						hand.SetMaterial(enteredCell.icon.GetMaterial(0), 0);
						hand.SetMesh(enteredCell.icon.GetMesh());
					}
				}
				enteredCell.itemType = tempType;
				enteredCell.count = tempCount;
				enteredCell.icon.SetMaterial(itemData.GetMaterial(tempType), 0);
				enteredCell.icon.SetMesh(itemData.GetMesh(tempType));
				enteredCell.countText.text = tempCount < 2 ? "" : tempCount.ToString();
				if (enteredCell.hotBar != null)
				{
					enteredCell.hotBar.CloneThis(enteredCell);
					if (enteredCell.hand != null)
					{
						enteredCell.hand.SetMaterial(itemData.GetMaterial(tempType), 0);
						enteredCell.hand.SetMesh(itemData.GetMesh(tempType));
					}
				}
				return "";
			}

			var maxCount = itemData.MaxCount(itemType);

			if (dropCellsCount == 1 && dropCells[0] == this && DateTime.Now - dropStarted < TimeSpan.FromSeconds(0.3f) && (type == CellType.cell || type == CellType.desk))
			{
				count += selectCount;
				selectCount = 0;
				foreach (var cell in desk)
				{
					if (count == maxCount) break;
					if (cell == this) continue;
					if (cell.count == 0) continue;
					if (cell.itemType != itemType) continue;
					var remain = Mathf.Max(0, count + cell.count - maxCount);
					count += cell.count - remain;
					cell.count = remain;
					if (remain == 0)
					{
						cell.countText.text = "";
						cell.icon.SetMesh(null);
						if (cell.hotBar != null)
						{
							cell.hotBar.icon.SetMesh(null);
							if (cell.hand != null) cell.hand.SetMesh(null);
						}
					}
					else cell.countText.text = remain < 2 ? "" : remain.ToString();
					if (cell.hotBar != null) cell.hotBar.countText.text = cell.countText.text;
				}
				foreach (var cell in inv)
				{
					if (count == maxCount) break;
					if (cell == this) continue;
					if (cell.count == 0) continue;
					if (cell.itemType != itemType) continue;
					var remain = Mathf.Max(0, count + cell.count - maxCount);
					count += cell.count - remain;
					cell.count = remain;
					if (remain == 0)
					{
						cell.countText.text = "";
						cell.icon.SetMesh(null);
						if (cell.hotBar != null)
						{
							cell.hotBar.icon.SetMesh(null);
							if (cell.hand != null) cell.hand.SetMesh(null);
						}
					}
					else cell.countText.text = remain.ToString();
					if (cell.hotBar != null) cell.hotBar.countText.text = cell.countText.text;
				}

				countText.text = count < 2 ? "" : count.ToString();
				if (hotBar != null) hotBar.countText.text = countText.text;
				return "";
			}
			var dropCounts = dropType ? selectCount / dropCellsCount : 1;
			var dropRemain = selectCount - dropCounts * dropCellsCount;
			for (int i = 0; i < dropCellsCount; i++)
			{
				var dropCell = dropCells[i];

				var sum = dropCell.count + dropCounts;
				if (sum > maxCount)
				{
					dropRemain += sum - maxCount;
					sum = maxCount;
				}
				dropCell.count = sum;
			}

			selectCount = dropRemain;

			if (count == 0 && selectCount == 0)
			{
				countText.text = "";
				icon.SetMesh(null);
				if (hotBar != null)
				{
					hotBar.countText.text = "";
					hotBar.icon.SetMesh(null);
					if (hand != null) hand.SetMesh(null);
				}
			}

			return selectCount == 0 ? "" : selectCount.ToString();
		}

		private InvCell[] desk;
		private InvCell[] arm;
		private InvCell[] inv;

		public void SetDesk(InvCell[] desk) { this.desk = desk; }
		public void SetArm(InvCell[] arm) { this.arm = arm; }
		public void SetInv(InvCell[] inv) { this.inv = inv; }
		private CanvasRenderer hand;
		public Vector3 hotBarPosition => hotBar.transform.localPosition;

		public void SetHand(CanvasRenderer hand)
		{
			this.hand = hand;
			if (hand == null) return;
			hand.materialCount = 1;
			hand.SetMaterial(icon.GetMaterial(0), 0);
			hand.SetMesh(icon.GetMesh());
		}
	}

	public enum CellType
	{
		cell,
		arm,
		desk,
		craft
	}
}
