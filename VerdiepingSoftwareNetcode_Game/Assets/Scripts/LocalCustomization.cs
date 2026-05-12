using UnityEngine;

public class LocalCustomization : MonoBehaviour
{
	public static int FaceIndex;
	public static int BodyIndex;
	public static int HairIndex;

	public SkinnedMeshRenderer bodyMeshFilter;
	public SkinnedMeshRenderer hairMeshFilter;
	public Renderer faceRenderer;

	public Mesh[] bodyMeshes;
	public Mesh[] hairMeshes;
	public Material[] faceMaterials;

	void Start()
	{
		Load();
		Apply();
	}

	public void NextFace()
	{
		FaceIndex = (FaceIndex + 1) % faceMaterials.Length;
		Apply();
	}

	public void PrevFace()
	{
		FaceIndex = (FaceIndex - 1 + faceMaterials.Length) % faceMaterials.Length;
		Apply();
	}

	public void NextBody()
	{
		BodyIndex = (BodyIndex + 1) % bodyMeshes.Length;
		Apply();
	}

	public void PrevBody()
	{
		BodyIndex = (BodyIndex - 1 + bodyMeshes.Length) % bodyMeshes.Length;
		Apply();
	}

	public void NextHair()
	{
		HairIndex = (HairIndex + 1) % hairMeshes.Length;
		Apply();
	}

	public void PrevHair()
	{
		HairIndex = (HairIndex - 1 + hairMeshes.Length) % hairMeshes.Length;
		Apply();
	}

	void Apply()
	{
		bodyMeshFilter.sharedMesh = bodyMeshes[BodyIndex];
		hairMeshFilter.sharedMesh = hairMeshes[HairIndex];
		faceRenderer.material = faceMaterials[FaceIndex];
	}

	public void Save()
	{
		PlayerPrefs.SetInt("Face", FaceIndex);
		PlayerPrefs.SetInt("Body", BodyIndex);
		PlayerPrefs.SetInt("Hair", HairIndex);
	}

	void Load()
	{
		FaceIndex = PlayerPrefs.GetInt("Face", 0);
		BodyIndex = PlayerPrefs.GetInt("Body", 0);
		HairIndex = PlayerPrefs.GetInt("Hair", 0);
	}
}