using Unity.Netcode;
using UnityEngine;

public class NetworkCustomization : NetworkBehaviour
{
	public NetworkVariable<int> FaceIndex = new();
	public NetworkVariable<int> BodyIndex = new();
	public NetworkVariable<int> HairIndex = new();

	public SkinnedMeshRenderer bodyMeshFilter;
	public SkinnedMeshRenderer hairMeshFilter;
	public Renderer faceRenderer;

	public Mesh[] bodyMeshes;
	public Mesh[] hairMeshes;
	public Material[] faceMaterials;

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			SubmitCustomizationServerRpc(
				LocalCustomization.FaceIndex,
				LocalCustomization.BodyIndex,
				LocalCustomization.HairIndex
			);
		}

		Apply();

		FaceIndex.OnValueChanged += (_, __) => Apply();
		BodyIndex.OnValueChanged += (_, __) => Apply();
		HairIndex.OnValueChanged += (_, __) => Apply();
	}

	[ServerRpc]
	void SubmitCustomizationServerRpc(int face, int body, int hair)
	{
		FaceIndex.Value = face;
		BodyIndex.Value = body;
		HairIndex.Value = hair;
	}

	void Apply()
	{
		bodyMeshFilter.sharedMesh = bodyMeshes[BodyIndex.Value];
		hairMeshFilter.sharedMesh = hairMeshes[HairIndex.Value];
		faceRenderer.material = faceMaterials[FaceIndex.Value];
	}
}