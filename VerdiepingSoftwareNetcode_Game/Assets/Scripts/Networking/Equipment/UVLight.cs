using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UVLight : NetworkBehaviour
{
	[SerializeField] private Light UV_Light;

	[SerializeField] private Material[] UVLightMaterials;
	[SerializeField] private Renderer UVRenderer;

	[Header("UV Detection")]
	[SerializeField] private float uvDistance = 3f;
	[SerializeField] private LayerMask uvLayer;
	[SerializeField] private float fadeSpeed = 5f;
	[SerializeField] private float detectRate = 0.05f;

	private TextMeshPro currentText;

	private float detectTimer;

	private NetworkVariable<bool> UVlightState = new NetworkVariable<bool>(
		false,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	private void OnEnable()
	{
		UVlightState.OnValueChanged += OnUVStateChanged;
	}

	private void OnDisable()
	{
		UVlightState.OnValueChanged -= OnUVStateChanged;
	}

	private void Update()
	{
		if (!IsOwner)
			return;

		if (Input.GetMouseButtonDown(1))
			UVlightState.Value = !UVlightState.Value;

		detectTimer -= Time.deltaTime;

		if (detectTimer <= 0f)
		{
			detectTimer = detectRate;
			HandleUVDetection();
		}
	}

	void HandleUVDetection()
	{
		if (!UVlightState.Value)
		{
			if (currentText != null)
				UpdateTextAlphaServerRpc(
					currentText.GetComponentInParent<NetworkObject>().NetworkObjectId,
					0f
				);

			return;
		}

		Ray ray = Camera.main.ViewportPointToRay(
			new Vector3(0.5f, 0.5f, 0f)
		);

		if (Physics.Raycast(ray, out RaycastHit hit, uvDistance, uvLayer))
		{
			TextMeshPro text =
				hit.collider.GetComponentInChildren<TextMeshPro>();

			if (text != null)
			{
				currentText = text;

				NetworkObject netObj =
					text.GetComponentInParent<NetworkObject>();

				if (netObj != null)
				{
					UpdateTextAlphaServerRpc(
						netObj.NetworkObjectId,
						1f
					);
				}

				return;
			}
		}

		if (currentText != null)
		{
			NetworkObject netObj =
				currentText.GetComponentInParent<NetworkObject>();

			if (netObj != null)
			{
				UpdateTextAlphaServerRpc(
					netObj.NetworkObjectId,
					0f
				);
			}
		}
	}

	[ServerRpc]
	void UpdateTextAlphaServerRpc(ulong objectId, float target)
	{
		UpdateTextAlphaClientRpc(objectId, target);
	}

	[ClientRpc]
	void UpdateTextAlphaClientRpc(ulong objectId, float target)
	{
		if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
			objectId,
			out NetworkObject netObj))
			return;

		TextMeshPro text =
			netObj.GetComponentInChildren<TextMeshPro>();

		if (text == null)
			return;

		Color color = text.color;

		color.a = Mathf.MoveTowards(
			color.a,
			target,
			Time.deltaTime * fadeSpeed
		);

		text.color = color;

		if (target == 0f && color.a <= 0f)
		{
			color.a = 0f;
			text.color = color;
		}
	}

	private void OnUVStateChanged(bool oldValue, bool newValue)
	{
		UV_Light.gameObject.SetActive(newValue);

		UVRenderer.material = newValue
			? UVLightMaterials[1]
			: UVLightMaterials[0];
	}

	public override void OnNetworkSpawn()
	{
		OnUVStateChanged(false, UVlightState.Value);
	}
}