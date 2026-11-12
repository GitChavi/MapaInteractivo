using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón del mapa para silenciar o activar el ambiente de las leyendas.
/// El sonido lo maneja <see cref="AmbienteLeyenda"/>; este botón solo cambia el silencio global.
/// </summary>
public sealed class AmbientAudioTogglePlaceholder : MonoBehaviour
{
    [SerializeField] private Button ambientButton;
    [SerializeField] private TMP_Text ambientLabel;

    private void Awake()
    {
        if (ambientButton == null)
            ambientButton = GetComponent<Button>();
        if (ambientLabel == null)
            ambientLabel = GetComponentInChildren<TMP_Text>(true);

        if (ambientButton != null)
        {
            ambientButton.onClick.RemoveListener(AmbienteLeyenda.AlternarSilencio);
            ambientButton.onClick.AddListener(AmbienteLeyenda.AlternarSilencio);
        }
    }

    private void Update()
    {
        bool disponible = AmbienteLeyenda.AlgunoAbierto;
        if (ambientButton != null)
            ambientButton.interactable = disponible;
        if (ambientLabel != null)
        {
            ambientLabel.text = !disponible ? "ABRE UN PUNTO PARA OIR AMBIENTE"
                : AmbienteLeyenda.Silenciado ? "ACTIVAR AMBIENTE" : "SILENCIAR AMBIENTE";
        }
    }
}
