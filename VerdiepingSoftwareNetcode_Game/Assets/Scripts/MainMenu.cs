using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;

public class MainMenu : MonoBehaviour
{
	public Button StartGameButton;
	public Button CustomizeButton;
	public string gameSceneName = "GameScene";
	public TextMeshProUGUI ResponseText;

	private bool customizationToggle = false;
	private bool menuToggle = true;
	private bool settingsToggle = false;

	public GameObject InGamePanel;
	public GameObject CustomizationPanel;
	public GameObject MainPanel;
	public GameObject SettingsPanel;

	public GameObject MainMenuCam;

	[SerializeField] private Button _hostButton;
	[SerializeField] private Button _clientButton;
	[SerializeField] private Button _disconnectButton;

	private Coroutine _connectionTimeoutRoutine;
	private bool _isTryingToConnect = false;

	void Start()
	{
		_hostButton.onClick.AddListener(HostButtonOnClick);
		_clientButton.onClick.AddListener(ClientButtonOnClick);
		_disconnectButton.onClick.AddListener(DisconnectOnClick);
		
		if(NetworkManager.Singleton.IsConnectedClient)
		{
			ConnectionButtonsToggle(true);
			MenuToggle();
		}
		else ConnectionButtonsToggle(false);
	}

	private void Update()
	{
		if (NetworkManager.Singleton == null) return;

		if (!NetworkManager.Singleton.IsListening)
		{
			StartGameButton.interactable = false;
			return;
		}

		StartGameButton.interactable = NetworkManager.Singleton.IsHost;
		ResponseText.gameObject.SetActive(!NetworkManager.Singleton.IsHost);

		if (Input.GetKeyDown(KeyCode.Escape)) MenuToggle();
	}

	public void StartGame()
	{
		if (!NetworkManager.Singleton.IsHost) return;

		NetworkManager.Singleton.SceneManager.LoadScene(
			gameSceneName,
			LoadSceneMode.Single
		);
	}

	public void CustomizationToggle()
	{
		customizationToggle = !customizationToggle;

		CustomizationPanel.SetActive(customizationToggle);
		MainPanel.SetActive(!customizationToggle);
	}

	public void SettingsToggle()
	{
		settingsToggle = !settingsToggle;

		SettingsPanel.SetActive(settingsToggle);
		MainPanel.SetActive(!settingsToggle);
	}

	private void MenuToggle()
	{
		menuToggle = !menuToggle;

		if (menuToggle)
		{
			PlayerController.CanMove = false;
			InGamePanel.SetActive(false);
			MainPanel.SetActive(true);
			SwitchLockstate(!menuToggle);
		}
		else
		{
			PlayerController.CanMove = true;
			InGamePanel.SetActive(true);
			MainPanel.SetActive(false);
			SwitchLockstate(!menuToggle);
		}
	}

	private void SwitchLockstate(bool value)
	{
		if (value)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			Cursor.lockState= CursorLockMode.None;
			Cursor.visible = true;
		}
	}

	public void OnQuit() 
	{
		if (NetworkManager.Singleton != null)
			NetworkManager.Singleton.Shutdown();

		Application.Quit();
	}

	private void ConnectionButtonsToggle(bool connected)
	{
		_hostButton.gameObject.SetActive(!connected);
		_clientButton.gameObject.SetActive(!connected);
		MainMenuCam.SetActive(!connected);
		CustomizeButton.interactable = !connected;

		_disconnectButton.gameObject.SetActive(connected);
	}

	private void HostButtonOnClick()
	{
		NetworkManager.Singleton.StartHost();
		ConnectionButtonsToggle(true);
		MenuToggle();
	}

	private void ClientButtonOnClick()
	{
		if (NetworkManager.Singleton.IsListening || _isTryingToConnect)
			return;

		_isTryingToConnect = true;

		NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;

		NetworkManager.Singleton.StartClient();

		_connectionTimeoutRoutine = StartCoroutine(ConnectionTimeout());
	}

	private IEnumerator ConnectionTimeout()
	{
		float timeout = 5f;
		float timer = 0f;

		while (timer < timeout)
		{
			if (NetworkManager.Singleton.IsConnectedClient)
				yield break;

			timer += Time.deltaTime;
			yield return null;
		}

		Debug.Log("Connection timed out.");

		if (NetworkManager.Singleton.IsClient)
			NetworkManager.Singleton.Shutdown();

		_isTryingToConnect = false;
	}

	private void OnConnected(ulong clientId)
	{
		Debug.Log("Connected to server! " + clientId);

		if (_connectionTimeoutRoutine != null)
			StopCoroutine(_connectionTimeoutRoutine);

		NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;

		_isTryingToConnect = false;

		ConnectionButtonsToggle(true);
		MenuToggle();
	}

	private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
	{
		Debug.Log("Host Disconnected! " + clientId);

		Debug.Log("Leaving lobby...");
		DisconnectOnClick();
		if (!menuToggle)
			MenuToggle();

		NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
	}

	private void DisconnectOnClick()
	{
		if (NetworkManager.Singleton != null) 
			NetworkManager.Singleton.Shutdown();
		
		ConnectionButtonsToggle(false);
	}
}