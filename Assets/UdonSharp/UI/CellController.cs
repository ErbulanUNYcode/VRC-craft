using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon.Common;

public class CellController : UdonSharpBehaviour
{
	[SerializeField] private ScrollRect scrollRect;
	[SerializeField] private Image simpleItem;
	[SerializeField] private GameObject blockItem;
	[SerializeField] private Image[] blockItemSides;
	[SerializeField] private TextMeshProUGUI countText;
	[SerializeField] private GameObject selected;

	[SerializeField] private Inventory inventory;
	[SerializeField] private ItemDataManager itemData;
	[SerializeField] private CellController syncCell;
	[SerializeField] private CellController draggedCell;

	private int count = 0;
	private int id = 0;
	private bool isVR = false;
	private bool isDragging = false;

	private void Start()
	{
		UpdateVisuals();
	}

	private void Update()
	{

		if (scrollRect == null) return;

		if (!Input.GetKey(KeyCode.Tab))
		{
			if (isDragging)
			{
				TryGive(draggedCell.id, draggedCell.count);
				draggedCell.count = 0;
				draggedCell.UpdateVisuals();
				isDragging = false;
				selected.SetActive(false);

				scrollRect.horizontalNormalizedPosition = 0.5f;
				scrollRect.verticalNormalizedPosition = 0.5f;
			}
			return;
		}

		var pos = Vector3Int.RoundToInt(scrollRect.content.localPosition) - new Vector3(-18, 18, 0);
		if (!isDragging && pos == Vector3.zero) return;
		if (!isDragging && count != 0)
		{
			inventory.Deselect();

			isDragging = true;
			if (selected != null) selected.SetActive(false);
			draggedCell.Shuffle(this);
		}
		pos += transform.localPosition;
		draggedCell.transform.localPosition = pos;
	}

	public void TryOne(CellController cell)
	{
		if (cell.count == 0) return;

		if (TryGive(cell.id, 1) == 1) return;

		cell.count--;
		cell.UpdateVisuals();
	}

	public void Shuffle(CellController cell)
	{
		if (selected != null) selected.SetActive(false);

		if (cell == this) return;

		if (cell.id == id)
		{
			count = cell.TryGive(id, count);
			UpdateVisuals();
			return;
		}

		var tempId = id;
		var tempCount = count;

		id = cell.id;
		count = cell.count;

		cell.id = tempId;
		cell.count = tempCount;

		cell.UpdateVisuals();
		UpdateVisuals();
	}

	public void Sync(int id, int count)
	{
		this.id = id;
		this.count = count;
		UpdateVisuals();
	}

	public void Sync(CellController cell)
	{
		Sync(cell.id, cell.count);
	}

	public int TryGive(int id, int count)
	{
		var count2 = _TryGive(id, count);

		UpdateVisuals();

		return count2;
	}


	public void TryGive(CellController cell)
	{
		cell.count = _TryGive(cell.id, cell.count);
		cell.UpdateVisuals();
		UpdateVisuals();
	}

	public int _TryGive(int id, int count)
	{
		if (this.count == 0)
		{
			this.id = id;
			this.count = Mathf.Min(count, itemData[id].maxCount);
			return Mathf.Max(0, count - this.count);
		}

		if (this.id != id) return count;

		if (this.count + count > itemData[id].maxCount)
		{
			count = this.count + count - itemData[id].maxCount;
			this.count = itemData[id].maxCount;
		}
		else
		{
			this.count += count;
			count = 0;
		}

		return count;
	}

	private void OnEnable()
	{
		if (selected == null) return;
		selected.SetActive(false);
	}

	private void UpdateVisuals()
	{
		if (syncCell != null) syncCell.Sync(this.id, this.count);

		if (count == 0)
		{
			blockItem.SetActive(false);
			countText.gameObject.SetActive(false);
			simpleItem.gameObject.SetActive(false);
			return;
		}

		if (itemData[id].isBlockItem)
		{
			blockItem.SetActive(true);
			for (int i = 0; i < blockItemSides.Length; i++)
			{
				blockItemSides[i].color = new Color(1f, 1f, 1f, ((float)itemData[id].iconId) / 255f);
			}
			countText.gameObject.SetActive(true);
			countText.text = count.ToString();
			countText.gameObject.SetActive(count != 1);
			simpleItem.gameObject.SetActive(false);
		}
		else
		{
			blockItem.SetActive(false);
			countText.gameObject.SetActive(true);
			countText.text = count.ToString();
			countText.gameObject.SetActive(count != 1);
			simpleItem.gameObject.SetActive(true);
			simpleItem.sprite = itemData.Icon(id);
		}
	}

	public void _Click()
	{
		if (scrollRect != null)
		{
			var pos = Vector3Int.RoundToInt(scrollRect.content.localPosition) - new Vector3(-18, 18, 0);
			if (pos != Vector3.zero) return;
		}
		if (count == 0) inventory.TryClick(this);
		else selected.SetActive(inventory.Click(this));
	}

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		Vector3 pos = Vector3.zero;

		if (scrollRect != null)
		{
			pos = Vector3Int.RoundToInt(scrollRect.content.localPosition) - new Vector3(-18, 18, 0);

			scrollRect.horizontalNormalizedPosition = 0.5f;
			scrollRect.verticalNormalizedPosition = 0.5f;
		}
		else return;
		if (value) return;
		if (!Input.GetKey(KeyCode.Tab)) return;
		if (!isDragging) return;
		isDragging = false;

		pos += transform.localPosition;
		if (count == 0)
			inventory.TryShuffle(pos, draggedCell);
		else
		{
			inventory.TryGive(pos, draggedCell);
			if (draggedCell.count != 0) TryGive(draggedCell);

			if (draggedCell.count != 0) inventory.TryGive(draggedCell.id, draggedCell.count);
			draggedCell.count = 0;
			draggedCell.UpdateVisuals();
		}

		TryGive(draggedCell.id, draggedCell.count);

		draggedCell.count = 0;
		draggedCell.UpdateVisuals();
	}

	public override void InputDrop(bool value, UdonInputEventArgs args)
	{
		if (scrollRect == null) return;
		if (!value) return;
		if (!Input.GetKey(KeyCode.Tab)) return;
		if (!isDragging) return;
		if (draggedCell.count == 0) return;
		var pos = Vector3Int.RoundToInt(scrollRect.content.localPosition) - new Vector3(-18, 18, 0) + transform.localPosition;

		if (inventory.TryOne(pos, draggedCell.id))
		{
			draggedCell.count--;
			draggedCell.UpdateVisuals();
		}
	}

	public int[] TryClickBlock(Vector3Int selected, Vector3Int air, bool set)
	{

		if (!set)
		{
			return new int[] { (int)SetPosition.selected, 1 };
		}
		else
		{
			if (count == 0) return null;

			var setType = itemData[id].setType;

			if (setType == SetType.none) return null;


			int[] returned = null;

			if (setType == SetType.symple)
				return new int[] { (int)SetPosition.air, itemData[id].sets[0] + 1 };
			else if (setType == SetType.side)
			{
				if (selected.x > air.x)
					returned = new int[] { (int)SetPosition.air, itemData[id].sets[0] + 1 };
				else if (selected.x < air.x)
					returned = new int[] { (int)SetPosition.air, itemData[id].sets[3] + 1 };
				else if (selected.y > air.y)
					returned = new int[] { (int)SetPosition.air, itemData[id].sets[1] + 1 };
				else if (selected.y < air.y)
					returned = new int[] { (int)SetPosition.air, itemData[id].sets[4] + 1 };
				else if (selected.z > air.z)
					returned = new int[] { (int)SetPosition.air, itemData[id].sets[2] + 1 };
				else if (selected.z < air.z)
					returned = new int[] { (int)SetPosition.air, itemData[id].sets[5] + 1 };
			}
			else if (setType == SetType.front)
			{
				var offset = inventory.OrderOffset;

				var fronts = new float[]
				{
					Mathf.Max(0, offset.x),
					Mathf.Max(0, offset.y),
					Mathf.Max(0, offset.z),
					Mathf.Max(0, -offset.x),
					Mathf.Max(0, -offset.y),
					Mathf.Max(0, -offset.z)
				};

				var best = -1;
				var bestValue = 0f;
				for (int i = 0; i < fronts.Length; i++)
				{
					if (itemData[id].sets[i] == 0) continue;
					if (fronts[i] > bestValue)
					{
						bestValue = fronts[i];
						best = i;
					}
				}

				returned = new int[] { (int)SetPosition.air, itemData[id].sets[best] + 1 };
			}
			return returned;
		}
	}

	public void Use()
	{
		count--;
		//infinite items
		if (count == 0) count = itemData[id].maxCount;
		UpdateVisuals();
	}
}

public enum SetPosition
{
	selected,
	air
}
