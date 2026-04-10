using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;
    public List<LobbyPlayer> lobbyPlayers = new List<LobbyPlayer>();

    [Header("Game scene & prefab (assign in inspector)")]
    public string gameSceneName = "Game";
    public GameObject gamePlayerPrefab; // assign in inspector

    // Server-side: connectionId -> playerName
    private Dictionary<int, string> pendingPlayerNames = new Dictionary<int, string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }


    [Server]
    public void AddPlayer(LobbyPlayer player)
    {
        if (!lobbyPlayers.Contains(player))
        {
            lobbyPlayers.Add(player);

            if (!lobbyPlayers.Exists(p => p.isHost))
                SetHost(player);
            Instance = this;
            SendPlayerListToClients();
            if (isServer)
                DontDestroyOnLoad(this);
        }
    }

    [Server]
    public void RemovePlayer(LobbyPlayer player)
    {
        if (lobbyPlayers.Contains(player))
        {
            bool wasHost = player.isHost;
            lobbyPlayers.Remove(player);

            if (wasHost && lobbyPlayers.Count > 0)
                SetHost(lobbyPlayers[0]);

            SendPlayerListToClients();
        }
    }

    [Server]
    private void SetHost(LobbyPlayer newHost)
    {
        foreach (var p in lobbyPlayers)
        {
            if (p != null)
                p.isHost = false;
        }

        if (newHost != null)
        {
            newHost.isHost = true;
            newHost.isReady = true;
        }
    }

    [Server]
    public void SendPlayerListToClients()
    {
        int count = lobbyPlayers.Count;
        uint[] netIds = new uint[count];
        string[] names = new string[count];
        bool[] isHosts = new bool[count];
        bool[] isReadys = new bool[count];

        for (int i = 0; i < count; i++)
        {
            var p = lobbyPlayers[i];
            netIds[i] = p.netId;
            names[i] = p.playerName;
            isHosts[i] = p.isHost;
            isReadys[i] = p.isReady;
        }

        RpcUpdateClients(netIds, names, isHosts, isReadys);
    }

    [ClientRpc]
    private void RpcUpdateClients(uint[] netIds, string[] names, bool[] isHosts, bool[] isReadys)
    {
        LobbyUIController.Instance?.UpdatePlayerListFromServerData(netIds, names, isHosts, isReadys);
    }

    [Server]
    public bool AreAllPlayersReady()
    {
        foreach (var p in lobbyPlayers)
            if (!p.isReady) return false;
        return true;
    }

    // -------------------
    // Oyun başlatma akışı
    // -------------------
    [Server]
    public void StartGame()
    {
        if (!AreAllPlayersReady())
        {
            var host = lobbyPlayers.Find(p => p.isHost);
            if (host != null)
                host.TargetStartFailed(host.connectionToClient, "Hazır olmayan oyuncular var!");
            return;
        }

        if (string.IsNullOrEmpty(gameSceneName) || gamePlayerPrefab == null)
        {
            Debug.LogError("LobbyManager: gameSceneName veya gamePlayerPrefab atanmadı!");
            return;
        }

        // pendingPlayerNames doldur
        pendingPlayerNames.Clear();
        foreach (var p in lobbyPlayers)
        {
            var conn = p.connectionToClient;
            if (conn != null)
            {
                pendingPlayerNames[conn.connectionId] = p.playerName;
            }
        }

        // Sahne değişikliği başlat
        NetworkManager.singleton.ServerChangeScene(gameSceneName);
    }

    // ReplacePlayersOnScene - düzeltilmiş sürüm
    [Server]
    public void ReplacePlayersOnScene()
    {
        if (pendingPlayerNames.Count == 0)
            return;

        var keys = new List<int>(pendingPlayerNames.Keys);
        foreach (var connId in keys)
        {
            // NetworkServer.connections map'i NetworkConnectionToClient değerleri içerir
            NetworkConnectionToClient conn;
            if (NetworkServer.connections.TryGetValue(connId, out conn))
            {
                // Instantiate gamePlayerPrefab on server
                GameObject newPlayerObj = Instantiate(gamePlayerPrefab);
                var gp = newPlayerObj.GetComponent<GamePlayer>();
                if (gp != null)
                {
                    // set SyncVar before replace so client sees it
                    gp.playerName = pendingPlayerNames[connId];
                }

                // Try to obtain existing player controller id (netId) for this connection
                uint playerControllerId = 0;
                if (conn.identity != null)
                {
                    // conn.identity is the old player object's NetworkIdentity
                    playerControllerId = conn.identity.netId;
                }
                //ReplacePlayerOptions replacePlayerOptions = new ReplacePlayerOptions();
                // Use the overload that expects (NetworkConnectionToClient, GameObject, uint)
                NetworkServer.ReplacePlayerForConnection(conn, newPlayerObj, playerControllerId);
            }
            else
            {
                Debug.LogWarning($"LobbyManager: connection {connId} not found when replacing players.");
            }
        }

        pendingPlayerNames.Clear();

        // Opsiyonel: lobbyPlayers listesini temizle (lobby artık game sahnesinde)
        lobbyPlayers.Clear();
    }
}