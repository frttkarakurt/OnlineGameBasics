using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Oyuncu istatistikleri: kills / deaths / assists
/// SyncVar hook'ları ile client'lara iletilir ve UI event'leri tetiklenir.
/// </summary>
public class GamePlayerStats : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnKillsChanged))]
    public int kills = 0;

    [SyncVar(hook = nameof(OnDeathsChanged))]
    public int deaths = 0;

    [SyncVar(hook = nameof(OnAssistsChanged))]
    public int assists = 0;

    // UI için local event'ler (client'lar abone olur)
    public event Action<int> OnKillsUpdated;
    public event Action<int> OnDeathsUpdated;
    public event Action<int> OnAssistsUpdated;

    // --- Server-side API ---
    [Server]
    public void AddKill(int amount = 1)
    {
        kills += amount;
    }

    [Server]
    public void AddDeath(int amount = 1)
    {
        deaths += amount;
    }

    [Server]
    public void AddAssist(int amount = 1)
    {
        assists += amount;
    }

    [Server]
    public void ResetStats()
    {
        kills = 0;
        deaths = 0;
        assists = 0;
    }

    // --- Hooks (client tarafında tetiklenir) ---
    private void OnKillsChanged(int oldVal, int newVal) => OnKillsUpdated?.Invoke(newVal);
    private void OnDeathsChanged(int oldVal, int newVal) => OnDeathsUpdated?.Invoke(newVal);
    private void OnAssistsChanged(int oldVal, int newVal) => OnAssistsUpdated?.Invoke(newVal);
}
