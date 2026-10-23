
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace VRC_MINE.UI
{

	public class VrHotUI : UdonSharpBehaviour
	{
		[SerializeField] private RectTransform coursorRoot;
		[SerializeField] private Image coursor;
		[SerializeField] private Image select;
		[SerializeField] private Sprite[] coursorSprites;
		[SerializeField] private Sprite[] selectSprites;
		[SerializeField] private RectTransform[] windows;
		[SerializeField] private InvSystem invSystem;
		private int selectedIndex = 0;
		private VRCPlayerApi localPlayer;
		private bool isActive = false;
		private VRCTweenHandle scaleTweenHandle = new VRCTweenHandle();
		private VRCTweenHandle[] scaleWinTweenHandles = new VRCTweenHandle[4];
		private void Start()
		{
			transform.localScale = Vector3.zero;
			localPlayer = Networking.LocalPlayer;
			foreach (var win in windows) if (win != null) win.transform.localScale = Vector3.zero;
		}

		private void Update()
		{
			if (transform.localScale == Vector3.zero) return;
			var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
			var leftHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
			var delay = Vector3.Normalize((leftHand.position + Quaternion.LookRotation(head.rotation * Vector3.forward) * Vector3.right * 0.06f) - head.position);
			delay.y = -0.25f;
			transform.rotation = Quaternion.LookRotation(delay);
			transform.position = (leftHand.position + transform.rotation * Vector3.right * 0.06f) - head.position; ;

			if (isActive)
			{
				var dir = leftHand.rotation * Vector3.up;
				dir = transform.InverseTransformPoint(dir);
				var angle = Mathf.Clamp(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, -80, 80) - 90;
				selectedIndex = Mathf.FloorToInt(-angle / 45);
				coursor.sprite = coursorSprites[selectedIndex];
				select.sprite = selectSprites[selectedIndex];
				coursorRoot.localRotation = Quaternion.Euler(0, 0, angle);
			}
		}

		DateTime useStartTime = DateTime.MinValue;
		DateTime grabStartTime = DateTime.MinValue;

		public override void InputUse(bool value, UdonInputEventArgs args)
		{
			if (args.handType == HandType.RIGHT) return;
			if (value)
			{
				if (grabStartTime == DateTime.MinValue)
				{
					useStartTime = DateTime.Now;
				}
				else if (grabStartTime + TimeSpan.FromSeconds(0.1) >= DateTime.Now)
				{
					if (scaleTweenHandle.IsPlaying) scaleTweenHandle.Kill();
					scaleTweenHandle = transform.TweenScale(Vector3.one * 0.8f, 0.25f, VRCTweenEase.OutBack);
					var win = windows[selectedIndex];
					if (win != null)
					{
						var tween = scaleWinTweenHandles[selectedIndex];
						if (tween.IsPlaying) tween.Kill();
						scaleWinTweenHandles[selectedIndex] = windows[selectedIndex].transform.TweenScale(Vector3.zero, 0.25f, VRCTweenEase.InOutCubic);
						if (selectedIndex == 2) invSystem.Close();
					}
					isActive = true;
				}
			}
			else
			{
				useStartTime = DateTime.MinValue;
				if (isActive)
				{
					if (scaleTweenHandle.IsPlaying) scaleTweenHandle.Kill();
					scaleTweenHandle = transform.TweenScale(Vector3.zero, 0.25f, VRCTweenEase.InBack);
					var win = windows[selectedIndex];
					if (win != null)
					{
						var tween = scaleWinTweenHandles[selectedIndex];
						if (tween.IsPlaying) tween.Kill();
						scaleWinTweenHandles[selectedIndex] = windows[selectedIndex].transform.TweenScale(Vector3.one, 0.25f, VRCTweenEase.InOutCubic);
					}
					isActive = false;
				}
			}
		}

		public override void InputGrab(bool value, UdonInputEventArgs args)
		{
			if (args.handType == HandType.RIGHT) return;
			if (value)
			{
				if (useStartTime == DateTime.MinValue)
				{
					grabStartTime = DateTime.Now;
				}
				else if (useStartTime + TimeSpan.FromSeconds(0.1) >= DateTime.Now)
				{
					if (scaleTweenHandle.IsPlaying) scaleTweenHandle.Kill();
					scaleTweenHandle = transform.TweenScale(Vector3.one * 0.8f, 0.25f, VRCTweenEase.OutBack);
					var win = windows[selectedIndex];
					if (win != null)
					{
						var tween = scaleWinTweenHandles[selectedIndex];
						if (tween.IsPlaying) tween.Kill();
						scaleWinTweenHandles[selectedIndex] = windows[selectedIndex].transform.TweenScale(Vector3.zero, 0.25f, VRCTweenEase.InOutCubic);
						if (selectedIndex == 2) invSystem.Close();
					}
					isActive = true;
				}
			}
			else
			{
				grabStartTime = DateTime.MinValue;
				if (isActive)
				{
					if (scaleTweenHandle.IsPlaying) scaleTweenHandle.Kill();
					scaleTweenHandle = transform.TweenScale(Vector3.zero, 0.25f, VRCTweenEase.InBack);
					var win = windows[selectedIndex];
					if (win != null)
					{
						var tween = scaleWinTweenHandles[selectedIndex];
						if (tween.IsPlaying) tween.Kill();
						scaleWinTweenHandles[selectedIndex] = windows[selectedIndex].transform.TweenScale(Vector3.one, 0.25f, VRCTweenEase.InOutCubic);
					}
					isActive = false;
				}
			}
		}
	}
}
