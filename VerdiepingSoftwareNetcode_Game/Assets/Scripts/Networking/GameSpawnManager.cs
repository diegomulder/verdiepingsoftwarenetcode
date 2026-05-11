using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameSpawnManager : NetworkBehaviour
{
	public static GameSpawnManager Instance;

	[Header("Assign spawnpoints from the active scene")]
	public Transform[] Spawns;

	private void Awake()
	{
		// Maak persistent
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;

		NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
	}

	private void OnDestroy()
	{
		if (NetworkManager.Singleton == null) return;

		NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
		NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
	}

	private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode)
	{
		if (!IsServer) return;

		StartCoroutine(DelayedSpawn());
	}

	private void OnClientConnected(ulong clientId)
	{
		if (!IsServer) return;

		StartCoroutine(DelayedSpawn());
	}

	private IEnumerator DelayedSpawn()
	{
		yield return null;

		if (Spawns == null || Spawns.Length == 0)
			yield break;

		var clients = new List<NetworkClient>(
			NetworkManager.Singleton.ConnectedClientsList
		);

		// Sorteer zodat Host (ClientId 0) altijd eerste spawn krijgt
		clients.Sort((a, b) => a.ClientId.CompareTo(b.ClientId));

		foreach (var client in clients)
		{
			if (client.PlayerObject == null || !client.PlayerObject.IsSpawned)
				continue;

			int spawnSlot = (int)(client.ClientId % (ulong)Spawns.Length);

			client.PlayerObject.transform.SetPositionAndRotation(
				Spawns[spawnSlot].position,
				Spawns[spawnSlot].rotation
			);
		}


		PlayerController.CanMove = true;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}