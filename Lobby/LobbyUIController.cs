using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    public static LobbyUIController Instance;

    public Transform playerListParent;
    public GameObject playerSlotPrefab;
    public Button actionButton;
    private Dictionary<uint, GameObject> spawnedSlots = new Dictionary<uint, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (FindLocalLobbyPlayer() != null && !FindLocalLobbyPlayer().isLocalPlayer) return;
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    public void UpdatePlayerListFromServerData(uint[] netIds, string[] names, bool[] isHosts, bool[] isReadys)
    {
        int count = netIds.Length;

        for (int i = 0; i < count; i++)
        {
            uint id = netIds[i];
            if (!spawnedSlots.ContainsKey(id))
            {
                GameObject slot = Instantiate(playerSlotPrefab, playerListParent);
                spawnedSlots[id] = slot;
            }

            var slotComp = spawnedSlots[id].GetComponent<PlayerSlot>();
            slotComp.SetPlayerInfo(names[i], isReadys[i], isHosts[i]);
        }

        List<uint> toRemove = new List<uint>();
        foreach (var kvp in spawnedSlots)
        {
            bool exists = false;
            for (int i = 0; i < count; i++)
            {
                if (kvp.Key == netIds[i]) { exists = true; break; }
            }
            if (!exists) toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
        {
            Destroy(spawnedSlots[id]);
            spawnedSlots.Remove(id);
        }

        LobbyPlayer local = FindLocalLobbyPlayer();
        UpdateActionButton(local);
    }

    public void UpdateActionButton(LobbyPlayer localPlayer)
    {
        if (localPlayer == null || actionButton == null) return;
        if(!localPlayer.isLocalPlayer) return;
        var txt = actionButton.GetComponentInChildren<TMP_Text>();
        actionButton.onClick.RemoveAllListeners();

        if (localPlayer.isHost)
        {
            txt.text = "Oyna";
            actionButton.onClick.AddListener(() =>
            {
                localPlayer.CmdRequestStartGame();
            });
        }
        else
        {
            txt.text = localPlayer.isReady ? "Hazır Değil" : "Hazır";
            actionButton.onClick.AddListener(() =>
            {
                localPlayer.CmdSetReadyStatus(!localPlayer.isReady);
            });
        }
    }

    // private LobbyPlayer FindLocalLobbyPlayer()
    // {
    //     foreach (var ni in NetworkServer.spawned.Values)
    //     {
    //         var lp = ni.GetComponent<LobbyPlayer>();
    //         if (lp != null && lp.isLocalPlayer)
    //         {
    //             Debug.Log(lp.name);
    //             return lp;
    //         }
    //     }
    //     return null;
    // }
    private LobbyPlayer FindLocalLobbyPlayer()
    {
        var lp = NetworkClient.localPlayer?.GetComponent<LobbyPlayer>();
        if (lp != null)
            return lp;
        return null;
    }

    private void OnActionButtonClicked()
    {
        var local = FindLocalLobbyPlayer();
        if (local == null) return;

        if (local.isHost)
            local.CmdRequestStartGame();
        else
            local.CmdSetReadyStatus(!local.isReady);
    }
}
