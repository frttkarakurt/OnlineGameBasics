using Mirror;
using UnityEngine;
using System;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar]
    public string playerName = "Player";

    // Stats component
    public GamePlayerStats Stats { get; private set; }

    // --- Health (server-authoritative) ---
    [Header("Health")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth = 100;

    public int maxHealth = 100;

    // Event for client UI to subscribe
    public event Action<int> OnHealthUpdated;

    private void Awake()
    {
        Stats = GetComponent<GamePlayerStats>();
        if (Stats == null)
            Stats = gameObject.AddComponent<GamePlayerStats>();

        // ensure currentHealth initialized
        currentHealth = maxHealth;
    }

    void Start()
    {
        try
        {
            var data = GetComponent<PlayerDataDemo>();
            if (data != null)
                playerName = data.Name();
        }
        catch (Exception ex) { Debug.Log(ex); }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"GamePlayer started on client: name={playerName}, netId={netId}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // initialize server-side health
        currentHealth = maxHealth;
    }

    // Hook called on clients when currentHealth değişti
    private void OnHealthChanged(int oldVal, int newVal)
    {
        // Yayınla (client'lar PlayerHUD buna abone olacak)
        OnHealthUpdated?.Invoke(newVal);
    }

    // --- Server-side damage API ---
    // attacker/assister parametreleri GamePlayer tipinde (veya null)
    [Server]
    public void TakeDamage(int amount, GamePlayer attacker = null, GamePlayer assister = null)
    {
        if (currentHealth <= 0) return; // already dead

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die(attacker, assister);
        }
    }

    [Server]
    private void Die(GamePlayer attacker, GamePlayer assister)
    {
        // Victim stats
        Stats.AddDeath();

        // Killer stats
        if (attacker != null && attacker != this)
        {
            attacker.Stats.AddKill();
        }

        // Assist
        if (assister != null && assister != attacker && assister != this)
        {
            assister.Stats.AddAssist();
        }

        // Broadcast death to clients for VFX / sound / feed etc.
        RpcOnDeath(netId, attacker != null ? attacker.netId : 0);

        // Respawn logic (örnek): reset health after small delay and respawn position
        // Burada basitçe health resetleyip bırakıyorum; sen spawn/respawn mekanizmanı ekleyebilirsin.
        currentHealth = maxHealth;
    }

    [ClientRpc]
    private void RpcOnDeath(uint victimNetId, uint killerNetId)
    {
        // Client-side efekt / ses tetiklenebilir
        // Örnek debug:
        var victim = NetworkClient.spawned[victimNetId];
        var killer = killerNetId != 0 && NetworkClient.spawned.ContainsKey(killerNetId) ? NetworkClient.spawned[killerNetId] : null;

        string vName = victim != null ? victim.GetComponent<GamePlayer>()?.playerName ?? "Unknown" : "Unknown";
        string kName = killer != null ? killer.GetComponent<GamePlayer>()?.playerName ?? "Unknown" : "Environment";

        Debug.Log($"Death: {vName} killed by {kName}");
        // Burada kill feed'e de ekleyebilirsin.
    }

    // Helper server-side methods (istendiğinde kullanılabilir)
    [Server]
    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }
}