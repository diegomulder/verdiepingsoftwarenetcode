using Unity.Netcode;
using UnityEngine;

public class InteractionManager : NetworkBehaviour
{
	public LayerMask interactLayerMask;
	public Camera mainCamera;
	public float maxDistance = 2f;
	public KeyCode interactionKey = KeyCode.E;

	private PadLockManager padLock;

	private void Start()
	{

	}

	private void Update()
	{
		if (!IsOwner) return;

		RaycastCheck();
	}

	void RaycastCheck()
	{
		if (Input.GetKeyDown(interactionKey))
		{
			Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(
				ray,
				out RaycastHit hit,
				maxDistance,
				interactLayerMask))
			{
				if (hit.collider.TryGetComponent(
					out PadLockManager padLock))
				{
					padLock.PadLockToggle(true);
				}
			}
		}
	}
}