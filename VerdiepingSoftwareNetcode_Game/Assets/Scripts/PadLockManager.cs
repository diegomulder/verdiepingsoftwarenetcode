using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PadLockManager : NetworkBehaviour
{
	[SerializeField] private GameObject padLockPanel;

	[SerializeField] private string codeWord = "code";

	public TMP_InputField CodeInputField;

	public void PadLockToggle(bool toggle)
	{
		padLockPanel.SetActive(toggle);

		Cursor.lockState = toggle
			? CursorLockMode.None
			: CursorLockMode.Locked;

		Cursor.visible = toggle;

		PlayerController.CanMove = !toggle;
	}

	public void CodeWordBehaviour()
	{
		if (
			CodeInputField.text.ToLower().Trim()
			== codeWord.ToLower().Trim()
		)
		{
			SubmitCodeServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void SubmitCodeServerRpc()
	{
		NetworkManager.SceneManager.LoadScene(
			"MainMenu",
			LoadSceneMode.Single
		);
	}
}