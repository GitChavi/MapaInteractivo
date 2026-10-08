using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Pause and repeat controls for the narration attached to an open point.</summary>
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
        AudioSource source = FindNarrationSource();
        if (source != currentSource)
        {
            currentSource = source;
            paused = false;
        }

        bool available = currentSource != null && currentSource.clip != null;
        if (pauseButton != null)
            pauseButton.interactable = available;
        if (repeatButton != null)
            repeatButton.interactable = available;
        if (pauseLabel != null)
            pauseLabel.text = available ? paused ? "Reanudar" : "Pausar" : "Sin narración";
        if (repeatLabel != null)
            repeatLabel.text = "Repetir";
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
        string[] pointNames = { "Tunda", "Mohan", "MadreMonte", "Silbon" };
        foreach (string pointName in pointNames)
        {
            GameObject point = GameObject.Find(pointName);
            if (point == null || !point.activeInHierarchy)
                continue;

            foreach (AudioSource source in point.GetComponentsInChildren<AudioSource>(true))
            {
                if (source.clip != null)
                    return source;
            }
        }

        return null;
    }
}
