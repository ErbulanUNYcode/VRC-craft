
using UdonSharp;
using UnityEngine;
namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class PcHotBar : UdonSharpBehaviour
	{
		[SerializeField] private InvCell[] hotBar;
		[SerializeField] private RectTransform[] hotBarRects;
		[SerializeField] private RectTransform selectRect;
		int selected = 0;

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
			if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
			if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);
			if (Input.GetKeyDown(KeyCode.Alpha4)) Select(3);
			if (Input.GetKeyDown(KeyCode.Alpha5)) Select(4);
			if (Input.GetKeyDown(KeyCode.Alpha6)) Select(5);
			if (Input.GetKeyDown(KeyCode.Alpha7)) Select(6);
			if (Input.GetKeyDown(KeyCode.Alpha8)) Select(7);
			if (Input.GetKeyDown(KeyCode.Alpha9)) Select(8);
			//mouse while axis
			var mouse = Input.GetAxis("Mouse ScrollWheel");
			if (mouse != 0) Select((selected - (mouse > 0 ? 1 : -1) + hotBar.Length) % hotBar.Length);
		}

		private void Select(int index)
		{
			selected = index;
			selectRect.localPosition = hotBarRects[index].localPosition;
		}
	}
}