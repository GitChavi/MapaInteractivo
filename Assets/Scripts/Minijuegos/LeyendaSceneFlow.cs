using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the screens already placed in a legend minigame scene.
/// UI objects remain regular, editable objects in the Unity hierarchy.
/// </summary>
public sealed class LeyendaSceneFlow : MonoBehaviour
{
    [SerializeField] private GameObject[] screens;
    [SerializeField] private GameObject incorrectPanel;
    [SerializeField] private int[] correctOptionsByScreen;
    [SerializeField] private int rewardScreen = 2;
    [SerializeField] private string mapScene = "MapaInteractivo";
    [SerializeField] private AudioSource ambientAudio;
    [SerializeField] private Text ambientButtonLabel;

    private int currentScreen;
    private bool ambientEnabled = true;

    private void Awake()
    {
        ShowScreen(0);
        if (incorrectPanel != null) incorrectPanel.SetActive(false);

        // Buttons are named by their editable hierarchy objects: Continue, Option0...
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            switch (button.gameObject.name)
            {
                case "Continue": button.onClick.AddListener(Continue); break;
                case "Option0": button.onClick.AddListener(() => Choose(0)); break;
                case "Option1": button.onClick.AddListener(() => Choose(1)); break;
                case "Option2": button.onClick.AddListener(() => Choose(2)); break;
                case "Option3": button.onClick.AddListener(() => Choose(3)); break;
                case "Retry": button.onClick.AddListener(Retry); break;
                case "ReturnToMap": button.onClick.AddListener(ReturnToMap); break;
                case "AmbientToggle": button.onClick.AddListener(ToggleAmbient); break;
            }
        }
    }

    public void Continue()
    {
        if (currentScreen + 1 < screens.Length) ShowScreen(currentScreen + 1);
    }

    public void Choose(int option)
    {
        bool correct = correctOptionsByScreen != null && currentScreen < correctOptionsByScreen.Length &&
                       option == correctOptionsByScreen[currentScreen];
        if (correct)
        {
            ShowScreen(currentScreen + 1 < screens.Length ? currentScreen + 1 : rewardScreen);
            return;
        }

        if (incorrectPanel != null) incorrectPanel.SetActive(true);
    }

    public void Retry()
    {
        if (incorrectPanel != null) incorrectPanel.SetActive(false);
    }

    public void ReturnToMap()
    {
        if (Application.CanStreamedLevelBeLoaded(mapScene))
            SceneManager.LoadScene(mapScene);
        else
            Debug.LogError($"No se encontró la escena '{mapScene}' en Build Settings.");
    }

    public void ToggleAmbient()
    {
        ambientEnabled = !ambientEnabled;
        if (ambientAudio != null) ambientAudio.mute = !ambientEnabled;
        if (ambientButtonLabel != null)
            ambientButtonLabel.text = ambientEnabled ? "SONIDO: ACTIVADO" : "SONIDO: SILENCIADO";
    }

    private void ShowScreen(int index)
    {
        currentScreen = Mathf.Clamp(index, 0, screens.Length - 1);
        for (int i = 0; i < screens.Length; i++)
            if (screens[i] != null) screens[i].SetActive(i == currentScreen);
    }
}
