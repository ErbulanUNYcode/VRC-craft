using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class VrHotBar : UdonSharpBehaviour
	{
		[SerializeField] private RectTransform[] hotBarView;
		[SerializeField] private CanvasRenderer[] hotBarRenderers;
		[SerializeField] private TextMeshProUGUI[] hotBarTexts;
		[SerializeField] private InvCell[] hotBarCells;

		[SerializeField] private Image background;
		[SerializeField] private Image selectedRect;
		[SerializeField] private TextMeshProUGUI countText;
		[SerializeField] private CanvasRenderer[] animationRenderers;
		[SerializeField] private RectTransform handRoot;
		private int currentItemCount;
		private Color transparentColor = new Color(1, 1, 1, 0);
		private VRCPlayerApi localPlayer;
		private int selectedIndex = 0;

		private void Start()
		{
			localPlayer = Networking.LocalPlayer;
			background.color = transparentColor;
			selectedRect.color = transparentColor;
			selectedRect.transform.localPosition = hotBarView[selectedIndex].localPosition;
			hotBarCells[selectedIndex].SetHand(animationRenderers[0]);
			hotBarCells[(selectedIndex + 8) % 9].SetHand(animationRenderers[1]);
			hotBarCells[(selectedIndex + 1) % 9].SetHand(animationRenderers[2]);
			for (int i = 0; i < 9; i++)
			{
				hotBarRenderers[i].SetColor(transparentColor);
				hotBarTexts[i].color = transparentColor;
			}
		}

		int axisStarted;
		bool hotSelectStarted;
		private DateTime startTime;
		private Vector3 originOffset;

		private void Update()
		{
			var axis = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical");
			var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
			var rightHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
			var delay = rightHand.position - head.position;

			var dir = delay;
			dir.y = 0;
			handRoot.position = delay;
			animationRenderers[0].transform.parent.rotation = rightHand.rotation * Quaternion.Euler(90, 0, 90);


			if (hotSelectStarted)
			{
				if (Mathf.Abs(axis) < 0.2f)
				{
					hotSelectStarted = false;
					axisStarted = 0;
					if (backgroundTweenHandle.IsPlaying)
					{
						backgroundTweenHandle.Kill();
						selectedRectTweenHandle.Kill();
						hotBarTweenHandle.Kill();
						foreach (var handle in countTextTweenHandles) handle.Kill();
					}
					backgroundTweenHandle = background.TweenColor(transparentColor, 0.2f, VRCTweenEase.Linear);
					selectedRectTweenHandle = selectedRect.TweenColor(transparentColor, 0.2f, VRCTweenEase.Linear);
					hotBarTweenHandle = VRCTween.TweenColor(hotBarColor, transparentColor, 0.2f, this, nameof(hotBarColor), nameof(UpdateHotBarColor), VRCTweenEase.Linear);
					for (int i = 0; i < 9; i++) countTextTweenHandles[i] = hotBarTexts[i].TweenColor(transparentColor, 0.2f, VRCTweenEase.Linear);
				}
				else
				{
					transform.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position - head.position + originOffset;
					var newSelectedIndex = Mathf.Clamp(Mathf.RoundToInt(transform.InverseTransformPoint(delay).x / 18) + 4, 0, 8);
					if (newSelectedIndex != selectedIndex)
					{
						hotBarCells[selectedIndex].SetHand(null);
						selectedIndex = newSelectedIndex;
						hotBarCells[selectedIndex].SetHand(animationRenderers[3]);
					}
					selectedRect.transform.localPosition = hotBarView[selectedIndex].localPosition;
					return;
				}
			}


			if (axisStarted == 0)
			{
				if (Mathf.Abs(axis) > 0.8f)
				{
					axisStarted = axis > 0 ? 1 : -1;
					startTime = DateTime.Now + TimeSpan.FromSeconds(0.3f);
				}
				return;
			}


			if (startTime > DateTime.Now)
			{
				if (Mathf.Abs(axis) < 0.2f)
				{

					hotBarCells[selectedIndex].SetHand(null);
					selectedIndex = (selectedIndex + axisStarted + 9) % 9;
					hotBarCells[selectedIndex].SetHand(animationRenderers[3]);
					selectedRect.transform.localPosition = hotBarView[selectedIndex].localPosition;
					axisStarted = 0;
				}
				return;
			}


			if (axis > 0)
			{
				hotSelectStarted = true;
				if (backgroundTweenHandle.IsPlaying)
				{
					backgroundTweenHandle.Kill();
					selectedRectTweenHandle.Kill();
					hotBarTweenHandle.Kill();
					foreach (var handle in countTextTweenHandles) handle.Kill();
				}
				backgroundTweenHandle = background.TweenColor(Color.white, 0.2f, VRCTweenEase.Linear);
				selectedRectTweenHandle = selectedRect.TweenColor(Color.white, 0.2f, VRCTweenEase.Linear);
				hotBarTweenHandle = VRCTween.TweenColor(hotBarColor, Color.white, 0.2f, this, nameof(hotBarColor), nameof(UpdateHotBarColor), VRCTweenEase.Linear);
				for (int i = 0; i < 9; i++) countTextTweenHandles[i] = hotBarTexts[i].TweenColor(Color.white, 0.2f, VRCTweenEase.Linear);
				dir = Vector3.Normalize(rightHand.rotation * new Vector3(1, 0, 1));
				dir.y = -0.25f;
				transform.rotation = Quaternion.LookRotation(dir);
				transform.position = delay + Vector3.up * 0.06f + (transform.position - selectedRect.transform.position);
				originOffset = head.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position + transform.position;
				return;
			}
		}
		[NonSerialized] public Color hotBarColor = new Color(1, 1, 1, 0);
		public void UpdateHotBarColor()
		{
			for (int i = 0; i < 9; i++) hotBarRenderers[i].SetColor(hotBarColor);
		}

		private VRCTweenHandle backgroundTweenHandle;
		private VRCTweenHandle selectedRectTweenHandle;
		private VRCTweenHandle hotBarTweenHandle;
		private VRCTweenHandle[] countTextTweenHandles = new VRCTweenHandle[9];
	}
}