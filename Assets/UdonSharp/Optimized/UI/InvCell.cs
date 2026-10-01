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
		[SerializeField] private Mesh mesh;
		[SerializeField] private Material material;
		[SerializeField] private ItemData itemData;

		[SerializeField] private TextMeshProUGUI countText;

		private int count = 0;
		private byte itemType = 0;

		public void OnEnter()
		{
			image.color = new Color(1, 1, 1, 0.2f);
			invSystem.SelectedCell(this);
		}

		public void OnExit()
		{
			image.color = new Color(1, 1, 1, 0);
			invSystem.DeselectedCell(this);
		}

		public int TryGiveItems(byte _itemType, int _count)
		{
			if (itemType != _itemType || icon.materialCount == 0)
			{
				icon.materialCount = 1;
				if (count == 0)
				{
					itemType = _itemType;
					icon.SetMesh(itemData.GetMesh(_itemType));
					icon.SetMaterial(itemData.GetMaterial(_itemType), 0);
				}
				else return count;
			}

			count += _count;

			_count = Mathf.Max(0, count - itemData.MaxCount(_itemType));

			count -= _count;
			countText.text = count.ToString();

			return _count;
		}

		public void Select(RectTransform selectRect)
		{
			selectRect.gameObject.SetActive(true);
			selectRect.position = transform.position;
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
