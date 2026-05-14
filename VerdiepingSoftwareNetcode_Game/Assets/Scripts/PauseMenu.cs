using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
	public GameObject pausePanel;
	public GameObject settingsPanel;

	private bool isPaused;

	private void OnEnable()
	{
		if (NetworkManager.Singleton != null)
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
	}

	private void OnDisable()
	{
		if (NetworkManager.Singleton != null)
			NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
	}

	private void OnClientDisconnected(ulong clientId)
	{
		if (clientId == NetworkManager.ServerClientId)
		{
			Debug.Log("Host disconnected. Returning to MainMenu.");

			if (NetworkManager.Singleton != null &&
				NetworkManager.Singleton.IsListening)
			{
				NetworkManager.Singleton.Shutdown();
			}

			SceneManager.LoadScene("MainMenu");
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			TogglePause();
		}
	}

	public void TogglePause()
	{
		isPaused = !isPaused;

		pausePanel.SetActive(isPaused);

		Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
		Cursor.visible = isPaused;

		PlayerController.CanMove = !isPaused;
	}

	public void Resume()
	{
		TogglePause();
	}

	public void OpenSettings()
	{
		settingsPanel.SetActive(true);
	}

	public void CloseSettings()
	{
		settingsPanel.SetActive(false);
	}

	public void QuitToMenu()
	{
		if (NetworkManager.Singleton.IsHost)
		{
			NetworkManager.Singleton.SceneManager.LoadScene(
				"MainMenu",
				LoadSceneMode.Single
			);
		}
		else if (NetworkManager.Singleton.IsClient)
		{
			NetworkManager.Singleton.Shutdown();
			SceneManager.LoadScene("MainMenu");
		}
	}
}