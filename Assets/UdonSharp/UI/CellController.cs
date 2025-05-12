using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class CellController : UdonSharpBehaviour
{
	[SerializeField] private ItemData item;
	[SerializeField] private int count;
	[SerializeField] private Image simpleItem;
	[SerializeField] private GameObject blockItem;
	[SerializeField] private Image[] blockItemSides;
	[SerializeField] private TextMeshProUGUI countText;
	private GameObject prefab;


	private void OnEnable()
	{
		if (item == null)
		{
			simpleItem.gameObject.SetActive(false);
			blockItem.gameObject.SetActive(false);
			countText.gameObject.SetActive(false);
			return;
		}

		if (item.itemID < 160)
		{
			simpleItem.gameObject.SetActive(false);
			blockItem.gameObject.SetActive(true);
			countText.gameObject.SetActive(true);
			countText.text = count.ToString();
			foreach (var side in blockItemSides)
			{
				side.material.SetFloat("_BlockID", item.itemID);
			}
		}
	}
}