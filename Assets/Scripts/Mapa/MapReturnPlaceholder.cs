using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Control del botón de retorno que está guardado en la escena.</summary>
public sealed class MapReturnPlaceholder : MonoBehaviour
{
    private static readonly string[] PopupNames = { "Tunda", "Mohan", "Bachue", "MadreMonte", "Silbon" };

    [SerializeField] private Button returnButton;
    [SerializeField] private TMP_Text returnLabel;

    private void Awake()
    {
        if (returnButton == null)
            returnButton = GetComponent<Button>();
        if (returnLabel == null)
            returnLabel = GetComponentInChildren<TMP_Text>(true);

        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToMap);
            returnButton.onClick.AddListener(ReturnToMap);
        }
    }

    private void Update()
    {
        bool pointOpen = HasOpenPoint();
        if (returnButton != null)
            returnButton.interactable = pointOpen;
        if (returnLabel != null)
            returnLabel.text = pointOpen ? "VOLVER AL MAPA" : "ABRE UN PUNTO PARA VOLVER";
    }

    private static bool HasOpenPoint()
    {
        foreach (string popupName in PopupNames)
        {
            GameObject popup = GameObject.Find(popupName);
            if (popup != null && popup.activeInHierarchy)
                return true;
        }

        return false;
    }

    private static void ReturnToMap()
    {
        GameObject map = GameObject.Find("mapa");
        if (map != null)
            map.SetActive(true);

        foreach (string popupName in PopupNames)
        {
            GameObject popup = GameObject.Find(popupName);
            if (popup != null)
                popup.SetActive(false);
        }
    }
}
