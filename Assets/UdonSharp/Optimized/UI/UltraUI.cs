using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VRC_MINE.UI
{
	public class UltraUI : UdonSharpBehaviour
	{
		[SerializeField] private GameObject inventory;
		[SerializeField] private GameObject aim;
		[SerializeField] private MeshCollider meshCollider;
		private Mesh mesh;
		private Vector3[] vertices;

		private void Start()
		{
			if (Networking.LocalPlayer.IsUserInVR()) Destroy(gameObject);

			mesh = new Mesh();
			vertices = new Vector3[4];
			mesh.vertices = vertices;
			mesh.triangles = new int[]
			{
			0,1,2,
			2,3,0
			};
		}

		public void ThroughEvent()
		{
			var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
			var center = (-transform.position + head.position + head.rotation * Vector3.forward * 0.35f) / transform.localScale.x;
			var up = head.rotation * Vector3.up * 0.3f / transform.localScale.x;
			var right = head.rotation * Vector3.right * 0.5f / transform.localScale.x;
			vertices[0] = center - up - right;
			vertices[1] = center + up - right;
			vertices[2] = center + up + right;
			vertices[3] = center - up + right;
			mesh.vertices = vertices;
			meshCollider.sharedMesh = mesh;
		}
		private void LateUpdate()
		{
			var boxCollider = inventory.GetComponent<BoxCollider>();
			if (boxCollider != null) Destroy(boxCollider);

			if (!Input.GetKey(KeyCode.Tab))
			{
				inventory.SetActive(false);
				aim.SetActive(true);
				return;
			}
			inventory.gameObject.SetActive(true);
			aim.SetActive(false);
			//SendCustomEventDelayedFrames(nameof(ThroughEvent), 0);
			ThroughEvent();
		}
	}
}