using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

public class CellController : UdonSharpBehaviour
{
	[SerializeField] private ScrollRect scrollRect;
	[SerializeField] private int count;
	[SerializeField] private Image simpleItem;
	[SerializeField] private GameObject blockItem;
	[SerializeField] private Image[] blockItemSides;
	[SerializeField] private TextMeshProUGUI countText;
	private GameObject prefab;
	VRCPlayerApi player;

	private void Start()
	{
		player = Networking.LocalPlayer;
	}

	private void OnEnable()
	{
		if (scrollRect != null)
		{
			scrollRect.horizontalNormalizedPosition = 0.5f;
			scrollRect.verticalNormalizedPosition = 0.5f;
		}

		/*{
			simpleItem.gameObject.SetActive(false);
			blockItem.gameObject.SetActive(false);
			countText.gameObject.SetActive(false);
			return;
		}

		{
			simpleItem.gameObject.SetActive(false);
			blockItem.gameObject.SetActive(true);
			countText.gameObject.SetActive(true);
			countText.text = count.ToString();
			/*foreach (var side in blockItemSides)
			{
				side.material.SetFloat("_BlockID", item.itemID);
			}*/
	}

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (value) return;

		scrollRect.horizontalNormalizedPosition = 0.5f;
		scrollRect.verticalNormalizedPosition = 0.5f;
	}
}
