using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Basit UI slot component. playerSlotPrefab bu component'i içermeli.
/// İçerik: TMP_Text nameText, TMP_Text readyText veya ikon, Image hostIcon vb.
/// </summary>
public class PlayerSlot : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text readyText;
    public GameObject hostIcon;

    public void SetPlayerInfo(string name, bool isReady, bool isHost)
    {
        if (nameText != null) nameText.text = name;
        if (readyText != null) readyText.text = isReady ? "Hazır" : "Hazır Değil";
        if (hostIcon != null) hostIcon.SetActive(isHost);
    }
}
