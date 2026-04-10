using TMPro;
using UnityEngine;

/// <summary>
/// Tek bir satır: isim + kills + deaths + assists.
/// ScoreboardUI tarafından SetData ile güncellenir.
/// </summary>
public class ScoreboardRow : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;
    public TMP_Text assistsText;

    // Public read-only erişim sıralama için
    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    public int Assists { get; private set; }

    public void SetData(string playerName, int kills, int deaths, int assists)
    {
        if (nameText != null) nameText.text = playerName ?? "Player";
        if (killsText != null) killsText.text = kills.ToString();
        if (deathsText != null) deathsText.text = deaths.ToString();
        if (assistsText != null) assistsText.text = assists.ToString();

        Kills = kills;
        Deaths = deaths;
        Assists = assists;
    }
}
