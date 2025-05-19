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

	private int count = 0;
	private int id = 0;

	public void Shuffle(CellController cell)
	{
		var tempId = id;
		var tempCount = count;

		id = cell.id;
		count = cell.count;

		cell.id = tempId;
		cell.count = tempCount;

		cell.UpdateVisuals();
		UpdateVisuals();

		selected.SetActive(false);
	}

	private void Sync(int id, int count)
	{
		this.id = id;
		this.count = count;
		UpdateVisuals();
	}

	public int TryGive(int id, int count)
	{
		var count2 = _TryGive(id, count);

		UpdateVisuals();

		return count2;
	}
	public int _TryGive(int id, int count)
	{
		if (this.count == 0)
		{
			this.id = id;
			this.count = Mathf.Min(count, itemData.Item(id).maxCount);
			return Mathf.Max(0, count - this.count);
		}

		if (this.id != id) return count;

		if (this.count + count > itemData.Item(id).maxCount)
		{
			count = this.count + count - itemData.Item(id).maxCount;
			this.count = itemData.Item(id).maxCount;
		}
		else
		{
			this.count += count;
			count = 0;
		}

		return 0;
	}

	private void OnEnable()
	{
		selected.SetActive(false);
		UpdateVisuals();
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

		if (itemData.Item(id).isBlockItem)
		{
			blockItem.SetActive(true);
			for (int i = 0; i < blockItemSides.Length; i++)
			{
				blockItemSides[i].color = new Color(1f, 1f, 1f, ((float)itemData.Item(id).blockItemId) / 255f);
			}
			countText.gameObject.SetActive(true);
			countText.text = count.ToString();
			if (count == 1) countText.gameObject.SetActive(false);
			simpleItem.gameObject.SetActive(false);
		}
		else
		{
			blockItem.SetActive(false);
			countText.gameObject.SetActive(true);
			countText.text = count.ToString();
			if (count == 1) countText.gameObject.SetActive(false);
			simpleItem.gameObject.SetActive(true);
			simpleItem.sprite = itemData.Item(id).icon;
		}
	}

	public void _Click()
	{
		var pos = Vector3Int.RoundToInt(scrollRect.content.localPosition) - new Vector3(-18, 18, 0);
		if (pos == Vector3.zero) selected.SetActive(inventory.Click(this));
	}

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (scrollRect == null) return;
		if (value) return;
		if (!Input.GetKey(KeyCode.Tab)) return;
		var pos = Vector3Int.RoundToInt(scrollRect.content.localPosition) - new Vector3(-18, 18, 0);
		if (pos != Vector3.zero)
		{
			pos += transform.localPosition;
			selected.SetActive(false);

			inventory.TryShuffle(pos, this);

			scrollRect.horizontalNormalizedPosition = 0.5f;
			scrollRect.verticalNormalizedPosition = 0.5f;
		}
	}

	public override void InputDrop(bool value, UdonInputEventArgs args)
	{
		if (count == 0) return;
		if (scrollRect == null) return;
		if (!value) return;
		if (!Input.GetKey(KeyCode.Tab)) return;
		var pos = Vector3Int.RoundToInt(scrollRect.content.localPosition) - new Vector3(-18, 18, 0);
		if (pos != Vector3.zero)
		{
			pos += transform.localPosition;
			selected.SetActive(false);

			if (inventory.TryOne(pos, id))
			{
				count--;
				UpdateVisuals();
			}
		}
	}
}
