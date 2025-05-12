
using UdonSharp;
using UnityEngine;

public class AttachToTransform : UdonSharpBehaviour
{
	[SerializeField] private bool attachToPosition = true;
	[SerializeField] private bool attachToRotation = true;
	[SerializeField] private GameObject objectToAttach;
	private Transform transformToAttachTo;
	void Start()
	{
		transformToAttachTo = objectToAttach.transform;
	}

	void Update()
	{
		if (attachToPosition)
		{
			transform.position = transformToAttachTo.position;
		}
		if (attachToRotation)
		{
			transform.rotation = transformToAttachTo.rotation;
		}
	}
}
