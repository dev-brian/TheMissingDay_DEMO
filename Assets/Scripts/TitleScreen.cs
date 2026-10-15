using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Va en un objeto vacío de la escena de título.
// Hace parpadear "Press any key to start" y con cualquier tecla pasa al juego. Esc cierra el juego.
public class TitleScreen : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena del juego.")]
    public string gameScene = "Cuarto";
    public TMP_Text pressKeyText;
    public ScreenFader fader;

    bool starting;

    void Update()
    {
        if (pressKeyText != null)
            pressKeyText.alpha = 0.25f + 0.75f * Mathf.PingPong(Time.time * 1.2f, 1f);

        if (starting) return;
        if (QuitPressed())
        {
            Application.Quit();   // en el editor no hace nada; en el .exe cierra el juego
            return;
        }
        if (AnyKeyPressed())
        {
            starting = true;
            if (fader != null) fader.FadeToScene(gameScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene);
        }
    }

    static bool QuitPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    static bool AnyKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return key || click;
#else
        return Input.anyKeyDown;
#endif
    }
}
