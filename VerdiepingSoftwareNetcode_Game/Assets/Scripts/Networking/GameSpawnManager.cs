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
		if (Instance == null)
			Instance = this;
		else
			Destroy(gameObject);
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer)
			return;

		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
	}

	private void OnDestroy()
	{
		if (NetworkManager.Singleton == null)
			return;

		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
	}

	private void OnSceneLoaded(
		string sceneName,
		LoadSceneMode mode,
		List<ulong> clientsCompleted,
		List<ulong> clientsTimedOut)
	{
		if (!IsServer)
			return;

		StartCoroutine(SpawnAllPlayers());
	}

	private IEnumerator SpawnAllPlayers()
	{
		yield return null;

		if (Spawns == null || Spawns.Length == 0)
			yield break;

		foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
		{
			while (client.PlayerObject == null || !client.PlayerObject.IsSpawned)
				yield return null;
		}

		yield return null;

		foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
		{
			TeleportToSpawn(client);
		}

		yield return null;

		PlayerController.CanMove = true;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public void RespawnPlayer(ulong clientId)
	{
		if (!IsServer)
			return;

		if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
			return;

		if (client.PlayerObject == null)
			return;

		TeleportToSpawn(client);
	}

	private void TeleportToSpawn(NetworkClient client)
	{
		int spawnSlot =
			(int)(client.ClientId % (ulong)Spawns.Length);

		var obj = client.PlayerObject;

		var controller = obj.GetComponent<CharacterController>();

		if (controller != null)
			controller.enabled = false;

		obj.transform.SetPositionAndRotation(
			Spawns[spawnSlot].position,
			Spawns[spawnSlot].rotation
		);

		if (controller != null)
			controller.enabled = true;
	}
}