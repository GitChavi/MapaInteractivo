using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Conecta los botones Pausar/Reanudar y Repetir al hijo NarrationAudioSource
/// del punto abierto. Se muestran junto al popup activo y se ocultan al volver
/// al mapa; se desactivan si el punto no tiene clip de narración.
/// </summary>
public sealed class NarrationControlsPlaceholder : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button repeatButton;
    [SerializeField] private TMP_Text pauseLabel;
    [SerializeField] private TMP_Text repeatLabel;

    private AudioSource currentSource;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += FindSceneController;
    }

    private static void FindSceneController(Scene scene, LoadSceneMode mode)
    {
        GameObject controls = GameObject.Find("NarrationControls");
        if (controls != null && controls.GetComponent<NarrationControlsPlaceholder>() == null)
            controls.AddComponent<NarrationControlsPlaceholder>();
    }

    private void Awake()
    {
        if (pauseButton == null)
            pauseButton = transform.Find("PauseNarration")?.GetComponent<Button>();
        if (repeatButton == null)
            repeatButton = transform.Find("RepeatNarration")?.GetComponent<Button>();
        if (pauseLabel == null && pauseButton != null)
            pauseLabel = pauseButton.GetComponentInChildren<TMP_Text>(true);
        if (repeatLabel == null && repeatButton != null)
            repeatLabel = repeatButton.GetComponentInChildren<TMP_Text>(true);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
        if (repeatButton != null)
            repeatButton.onClick.AddListener(RepeatNarration);
    }

    private void Update()
    {
        bool pointOpen = IsPointOpen();
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(pointOpen);
        if (repeatButton != null)
            repeatButton.gameObject.SetActive(pointOpen);

        AudioSource source = FindNarrationSource();
        if (source != currentSource)
        {
            currentSource = source;
            paused = false;
            if (currentSource != null && currentSource.clip != null)
            {
                currentSource.playOnAwake = false;
                currentSource.loop = false;
                currentSource.spatialBlend = 0f;
                currentSource.Play();
            }
        }

        bool available = currentSource != null && currentSource.clip != null;
        if (pauseButton != null)
            pauseButton.interactable = available;
        if (repeatButton != null)
            repeatButton.interactable = available;
        if (pauseLabel != null)
            pauseLabel.text = available ? paused ? "Reanudar audio" : "Pausar audio" : "Sin narración";
        if (repeatLabel != null)
            repeatLabel.text = "Repetir audio";
    }

    public void TogglePause()
    {
        if (currentSource == null || currentSource.clip == null)
            return;

        paused = !paused;
        if (paused)
            currentSource.Pause();
        else
            currentSource.UnPause();
    }

    public void RepeatNarration()
    {
        if (currentSource == null || currentSource.clip == null)
            return;

        currentSource.Stop();
        currentSource.time = 0f;
        currentSource.Play();
        paused = false;
    }

    private static AudioSource FindNarrationSource()
    {
        string[] pointNames = { "Tunda", "Mohan", "Bachue", "MadreMonte", "Silbon" };
        foreach (string pointName in pointNames)
        {
            GameObject point = GameObject.Find(pointName);
            if (point == null || !point.activeInHierarchy)
                continue;

            foreach (AudioSource source in point.GetComponentsInChildren<AudioSource>(true))
            {
                if (source.gameObject.name == "NarrationAudioSource" && source.clip != null)
                    return source;
            }
        }

        return null;
    }

    private static bool IsPointOpen()
    {
        string[] pointNames = { "Tunda", "Mohan", "Bachue", "MadreMonte", "Silbon" };
        foreach (string pointName in pointNames)
        {
            GameObject point = GameObject.Find(pointName);
            if (point != null && point.activeInHierarchy)
                return true;
        }

        return false;
    }
}
