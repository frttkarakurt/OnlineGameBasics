using Mirror;
using UnityEngine;

/// <summary>
/// Basit sağlık sistemi. Hasar server tarafında uygulanır.
/// attacker parametresi varsa, server öldürmeyi/assisti bu script'ten tetikler.
/// </summary>
public class Health : NetworkBehaviour
{
    [SyncVar]
    public int maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth = 100;

    // Ölüm sonra yeniden doğma için delay vs. ekleyebilirsin
    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        // İstersen health bar UI güncellemesi vs. burada yapılır (target rpc ile)
    }

    /// <summary>
    /// Server-side çağır: hasar uygula. attacker GamePlayer olabilir (veya null).
    /// </summary>
    [Server]
    public void TakeDamage(int amount, GamePlayer attacker = null, GamePlayer assister = null)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die(attacker, assister);
        }
    }

    [Server]
    private void Die(GamePlayer attacker, GamePlayer assister)
    {
        // Bu objenin GamePlayer component'i (victim)
        var victimGP = GetComponent<GamePlayer>();
        if (victimGP == null)
        {
            Debug.LogWarning("Health.Die: victim GamePlayer bulunamadı.");
            return;
        }

        // Server-side stat güncellemeleri
        // Victim death
        victimGP.Stats.AddDeath();

        // Killer kill
        if (attacker != null)
        {
            // attacker başka bir GamePlayer ise
            attacker.Stats.AddKill();
        }

        // Assist
        if (assister != null && assister != attacker && assister != victimGP)
        {
            assister.Stats.AddAssist();
        }

        // Öldüktan sonra yapılacaklar:
        // - Respawn (isteğe bağlı)
        // - Spectator moda geç
        // - UI / match update
        RpcOnDeath(victimGP.netId, attacker != null ? attacker.netId : 0);
    }

    // İsteğe bağlı: tüm client'lara ölüm animasyonu/efekt bildir
    [ClientRpc]
    private void RpcOnDeath(uint victimNetId, uint killerNetId)
    {
        // Client-side görsel efekt, ses, killer highlight vb. yapılabilir.
        // Örnek: Debug.Log($"Player {victimNetId} öldü. Killer: {killerNetId}");
    }
}
