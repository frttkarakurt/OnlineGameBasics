using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Health UI")]
    public Image healthFillImage; // Type = Filled, Fill Method = Horizontal
    public TMP_Text healthText;
    public float lerpSpeed = 8f;
    private GamePlayer localPlayer;
    private float targetFill;
    private float currentFill;

    private void OnEnable()
    {
        StartCoroutine(AttachWhenReady());
    }

    private IEnumerator AttachWhenReady()
    {
        while (NetworkClient.localPlayer == null)
            yield return null;

        localPlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();
        if (localPlayer == null)
            yield break;

        localPlayer.OnHealthUpdated += OnHealthChanged;
        OnHealthChanged(localPlayer.currentHealth);
    }

    private void OnDisable()
    {
        if (localPlayer != null)
            localPlayer.OnHealthUpdated -= OnHealthChanged;
    }

    private void Update()
    {
        if (healthFillImage == null) return;

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * lerpSpeed);
        healthFillImage.fillAmount = currentFill;
    }

    private void OnHealthChanged(int newHealth)
    {
        if (healthText != null)
            healthText.text = $"HP: {newHealth}";

        if (localPlayer != null)
            targetFill = Mathf.Clamp01((float)newHealth / localPlayer.maxHealth);
    }
}
