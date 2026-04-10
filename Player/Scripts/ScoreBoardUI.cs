using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem; // new input system

/// <summary>
/// Scoreboard: New Input System ile Tab basılı tuttuğunda gösterir, bırakınca gizler.
/// Açılırken client-side olarak sahnedeki GamePlayer'ları tarayıp günceller.
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    private InputActions controls;
    public static ScoreboardUI Instance;

    [Header("UI refs")]
    public GameObject scoreboardPanel;       // Ana panel (başta kapalı)
    public Transform playerListParent;       // Satırların parent'i (Vertical Layout)
    public GameObject playerRowPrefab;       // ScoreboardRow prefab

    // Internal
    private Dictionary<uint, GameObject> rows = new Dictionary<uint, GameObject>();
    private bool isOpen = false;

    private void Awake()
    {
        Instance = this;
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        controls = new InputActions();

    }

    private void Update()
    {
        bool tabPressed = IsTabPressed();

        if (tabPressed && !isOpen)
        {
            OpenScoreboard();
        }
        else if (!tabPressed && isOpen)
        {
            CloseScoreboard();
        }
    }

    // Yeni Input System veya fallback kontrolü
    private bool IsTabPressed()
    {
        // New Input System varsa bunu kullan
        if (Keyboard.current != null)
        {
            // .isPressed returns true while held down
            return controls.Player.TabKey.IsPressed();
        }

        // Fallback: eski Input sistemi
        return Input.GetKey(KeyCode.Tab);
    }

    private void OpenScoreboard()
    {
        isOpen = true;
        if (scoreboardPanel != null) scoreboardPanel.SetActive(true);
        RefreshFromNetwork();
    }

    private void CloseScoreboard()
    {
        isOpen = false;
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
    }

    /// <summary>
    /// Client-side: sahnedeki tüm GamePlayer nesnelerini tarar ve scoreboard'u günceller.
    /// Sadece panel açıldığında çağrılır.
    /// </summary>
    public void RefreshFromNetwork()
    {
        if (playerRowPrefab == null || playerListParent == null) return;

        var foundNetIds = new HashSet<uint>();
        var players = new List<GamePlayer>();

        // NetworkIdentity.spawned üzerinden client'taki tüm spawn'ları tara
        foreach (var ni in NetworkClient.spawned.Values)
        {
            if (ni == null) continue;
            var gp = ni.GetComponent<GamePlayer>();
            if (gp != null)
            {
                players.Add(gp);
                foundNetIds.Add(gp.netId);
            }
        }

        // Oluştur veya güncelle
        foreach (var gp in players)
        {
            if (rows.TryGetValue(gp.netId, out var existingRow))
            {
                var comp = existingRow.GetComponent<ScoreboardRow>();
                if (comp != null)
                    comp.SetData(gp.playerName, gp.Stats.kills, gp.Stats.deaths, gp.Stats.assists);
            }
            else
            {
                var row = Instantiate(playerRowPrefab, playerListParent);
                var comp = row.GetComponent<ScoreboardRow>();
                if (comp != null)
                    comp.SetData(gp.playerName, gp.Stats.kills, gp.Stats.deaths, gp.Stats.assists);
                rows[gp.netId] = row;
            }
        }

        // Silinen oyuncuları kaldır
        var toRemove = new List<uint>();
        foreach (var kvp in rows)
        {
            if (!foundNetIds.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var id in toRemove) rows.Remove(id);

        // Opsiyonel: kills'a göre sırala
        SortRowsByKillsDesc();
    }

    private void SortRowsByKillsDesc()
    {
        var list = new List<(uint netId, GameObject row, int kills)>();
        foreach (var kvp in rows)
        {
            var comp = kvp.Value.GetComponent<ScoreboardRow>();
            int k = comp != null ? comp.Kills : 0;
            list.Add((kvp.Key, kvp.Value, k));
        }

        list.Sort((a, b) => b.kills.CompareTo(a.kills));
        for (int i = 0; i < list.Count; i++)
        {
            list[i].row.transform.SetSiblingIndex(i);
        }
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

}