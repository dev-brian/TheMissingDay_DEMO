using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Va en un objeto vacío de la escena del cuarto.
// Show() muestra el panel de fin de demo. Con el panel visible, R vuelve al título.
// Esc cierra el juego en cualquier momento.
public class DemoEnd : MonoBehaviour
{
    public GameObject endPanel;
    [Tooltip("Objetos que se ocultan al terminar, por ejemplo el texto del objetivo.")]
    public GameObject[] hideOnEnd;
    public string titleScene = "Titulo";
    public ScreenFader fader;

    void Start()
    {
        if (endPanel != null) endPanel.SetActive(false);
    }

    public void Show()
    {
        foreach (var go in hideOnEnd)
            if (go != null) go.SetActive(false);
        if (endPanel != null) endPanel.SetActive(true);
        Debug.Log("[METRIC LOG] Demo completada en " + Time.timeSinceLevelLoad.ToString("0") + "s");
    }

    void Update()
    {
        if (KeyDown(true))
        {
            Application.Quit();   // en el editor no hace nada; en el .exe cierra el juego
            return;
        }
        if (endPanel != null && endPanel.activeSelf && KeyDown(false))
        {
            if (fader != null) fader.FadeToScene(titleScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(titleScene);
        }
    }

    static bool KeyDown(bool escape)
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return false;
        return escape ? k.escapeKey.wasPressedThisFrame : k.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(escape ? KeyCode.Escape : KeyCode.R);
#endif
    }
}
