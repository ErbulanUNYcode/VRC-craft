using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class PcHotBar : UdonSharpBehaviour
	{
		[SerializeField] private InvCell[] hotBar;
		[SerializeField] private RectTransform selectRect;
		[SerializeField] private CanvasRenderer hand;
		[SerializeField] private RectTransform handRoot;

		[SerializeField] private AnimationCurve walkCurveX;
		[SerializeField] private AnimationCurve walkCurveY;
		[SerializeField] private AnimationCurve walkCurveRotZ;

		int selected = 0;
		private VRCPlayerApi localPlayer;
		private Vector2 defaultPos = new Vector3(-50, -5f);
		private void Start()
		{
			localPlayer = Networking.LocalPlayer;
			Select(0);
		}

		private float fallVelocity = 0;

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
		private void FixedUpdate()
		{
			InputLook();

			var velocity = localPlayer.GetVelocity();
			fallVelocity = localPlayer.IsPlayerGrounded() ? fallVelocity * 0.9f : -velocity.y * 3;
			var value = localPlayer.IsPlayerGrounded() ? velocity.y + fallVelocity : velocity.y;
			handRoot.anchoredPosition += new Vector2(0, Mathf.Clamp(-value, -5, 5) / 2);
			var euler = handRoot.localEulerAngles;
			euler.x += Mathf.Clamp(value, -15, 15) / 10;
			handRoot.localEulerAngles = euler;

			velocity.y = 0;
			var walkSpeed = localPlayer.IsPlayerGrounded() ? Vector3.Magnitude(velocity) : 0;
			isWalked = Mathf.Lerp(isWalked, walkSpeed, Time.fixedDeltaTime * 20f);
			walkTime = (walkTime + Time.fixedDeltaTime * walkSpeed / 5) % 1f;
			var walkOffset = new Vector2(walkCurveX.Evaluate(walkTime), walkCurveY.Evaluate(walkTime)) * isWalked * 10;

			handRoot.localRotation = Quaternion.Lerp(handRoot.localRotation, Quaternion.identity, Time.fixedDeltaTime * 10f);
			handRoot.anchoredPosition = Vector2.Lerp(handRoot.anchoredPosition, defaultPos + walkOffset, Time.fixedDeltaTime * 5f);
		}

		private float isWalked;
		private float walkTime;
		private void Select(int index)
		{
			hotBar[selected].SetHand(null);
			selected = index;
			selectRect.localPosition = hotBar[index].hotBarPosition;
			hotBar[index].SetHand(hand);
		}

		private Vector2 inputLook;

		private void InputLook()
		{
			handRoot.anchoredPosition += new Vector2(Mathf.Clamp(-inputLook.x, -15, 15) / 2, Mathf.Clamp(-inputLook.y, -15, 15) / 2);
			var euler = handRoot.localEulerAngles;
			euler.y += Mathf.Clamp(-inputLook.x, -15, 15) / 5;
			euler.x += Mathf.Clamp(inputLook.y, -15, 15) / 5;
			handRoot.localEulerAngles = euler;
		}

		public override void InputLookHorizontal(float value, UdonInputEventArgs args)
		{
			inputLook.x = value * 5;
		}

		public override void InputLookVertical(float value, UdonInputEventArgs args)
		{
			inputLook.y = value * 5;
		}
	}
}