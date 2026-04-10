using System;
using Mirror;
using UnityEngine;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHostChanged))]
    public bool isHost = false;

    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool isReady = false;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";

    public delegate void PlayerUpdated(LobbyPlayer player);
    public event PlayerUpdated OnPlayerUpdated;

    public override void OnStartServer()
    {
        base.OnStartServer();
        /*if (string.IsNullOrEmpty(playerName) || playerName == "Player")
            playerName = "Player" + UnityEngine.Random.Range(1, 1000);*/

        try
        {
            var data = GetComponent<PlayerDataDemo>();
            if (data != null)
                playerName = data.Name();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        if (LobbyManager.Instance != null)
            LobbyManager.Instance.AddPlayer(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.RemovePlayer(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnPlayerUpdated?.Invoke(this);
        LobbyUIController.Instance?.UpdateActionButton(this);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        OnPlayerUpdated?.Invoke(this);
        LobbyUIController.Instance?.UpdateActionButton(this);
    }

    private void OnHostChanged(bool oldVal, bool newVal)
    {
        OnPlayerUpdated?.Invoke(this);
        if (!isServer)
            LobbyUIController.Instance?.UpdateActionButton(this);
    }

    private void OnReadyChanged(bool oldVal, bool newVal)
    {
        OnPlayerUpdated?.Invoke(this);
        if (!isServer)
            LobbyUIController.Instance?.UpdateActionButton(this);
    }

    private void OnNameChanged(string oldVal, string newVal)
    {
        OnPlayerUpdated?.Invoke(this);
    }

    [Command]
    public void CmdSetReadyStatus(bool ready)
    {
        isReady = ready;
        LobbyManager.Instance?.SendPlayerListToClients();
    }

    [Command]
    public void CmdRequestStartGame()
    {
        // artık LobbyManager.StartGame()'ı çağır
        if (!isHost) return;
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.StartGame();
    }

    [TargetRpc]
    public void TargetStartFailed(NetworkConnection target, string message)
    {
        Debug.Log("Start failed: " + message);
        // UI popup gösterilebilir
    }
}