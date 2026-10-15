using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Pausa con Esc: Resume, Options y Main Menu. GameUI la agrega sola en cada nivel.
public class PauseMenu : MonoBehaviour
{
    public string menuScene = "Menu";

    RectTransform canvas;
    GameObject pausePanel, optionsPanel;
    public static bool Paused { get; private set; }

    void Update()
    {
        if (ClueJournal.Open || ClueJournal.ClosedFrame == Time.frameCount) return;   // Esc cierra primero el diario
        if (EscPressed())
        {
            if (optionsPanel != null && optionsPanel.activeSelf) ShowOptions(false);
            else SetPaused(!Paused);
        }
    }

    void SetPaused(bool on)
    {
        if (on && pausePanel == null) Build();
        Paused = on;
        Time.timeScale = on ? 0f : 1f;
        pausePanel.SetActive(on);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        var player = FindFirstObjectByType<PlayerPlatformer>();
        if (player != null && !GameUI.I.Busy) player.enabled = !on;
    }

    void ShowOptions(bool on)
    {
        if (optionsPanel == null) optionsPanel = OptionsPanel.Build(canvas, () => ShowOptions(false));
        optionsPanel.SetActive(on);
        pausePanel.SetActive(!on);
    }

    void Build()
    {
        canvas = UIKit.Canvas(transform, "Pausa", 70);
        var dim = UIKit.Stretch(UIKit.Box(canvas, "Oscuro", new Color(0, 0, 0, 0.6f)));
        pausePanel = dim.gameObject;
        var panel = UIKit.Framed(dim, "Panel", Vector2.zero, new Vector2(360, 300));
        var title = UIKit.Text(panel, "PAUSED", 34, UIKit.Gold, TextAlignmentOptions.Top);
        title.rectTransform.offsetMax = new Vector2(0, -18);
        title.fontStyle = FontStyles.Bold;
        UIKit.Button(panel, "Resume", UIKit.LoadIcon("jugar"), new Vector2(0, 40), new Vector2(240, 46), () => SetPaused(false));
        UIKit.Button(panel, "Options", UIKit.LoadIcon("opciones"), new Vector2(0, -16), new Vector2(240, 46), () => ShowOptions(true));
        UIKit.Button(panel, "Main Menu", UIKit.LoadIcon("salir"), new Vector2(0, -72), new Vector2(240, 46), GoToMenu);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        Paused = false;
        if (Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
    }

    void OnDestroy()
    {
        if (Paused) { Time.timeScale = 1f; Paused = false; }
    }

    static bool EscPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
