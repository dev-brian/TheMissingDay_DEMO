using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú principal, armado por código: fondo de la sala de Enedina, título, Enedina, y botones con iconos
// Play, Options y Exit. Va en un objeto vacío "Menu" de la escena Menu.
// Las imágenes salen de Assets/Resources (Fondos/menu, Iconos/...). Si falta alguna, el menú funciona igual.
public class MainMenu : MonoBehaviour
{
    public string firstScene = "C01_Intro";

    RectTransform canvas;
    GameObject mainPanel, optionsPanel;

    void Start()
    {
        Time.timeScale = 1f;
        canvas = UIKit.Canvas(transform, "Menu", 10);

        // Fondo
        var bg = UIKit.Stretch(UIKit.Box(canvas, "Fondo", new Color32(26, 18, 32, 255)));
        var bgSprite = Resources.Load<Sprite>("Fondos/menu");
        if (bgSprite != null)
        {
            var img = bg.GetComponent<Image>();
            img.sprite = bgSprite;
            img.color = new Color(0.55f, 0.5f, 0.6f);   // un poco oscuro para que se lea el menú
        }

        mainPanel = new GameObject("Principal", typeof(RectTransform));
        mainPanel.transform.SetParent(canvas, false);
        UIKit.Stretch((RectTransform)mainPanel.transform);
        var root = mainPanel.transform;

        // Título
        var titleBox = UIKit.Place(new GameObject("Titulo", typeof(RectTransform)).GetComponent<RectTransform>(),
                                   new Vector2(0.5f, 1f), new Vector2(-130, -110), new Vector2(560, 130));
        titleBox.SetParent(root, false);
        var small = UIKit.Text(titleBox, "YAVIGA PRESENTS", 18, UIKit.Teal, TextAlignmentOptions.Top);
        var title = UIKit.Text(titleBox, "THE MISSING DAY", 58, UIKit.Gold, TextAlignmentOptions.Bottom);
        title.fontStyle = FontStyles.Bold;

        // Enedina
        var ene = Resources.Load<Sprite>("Iconos/enedina");
        if (ene != null)
        {
            var e = UIKit.Place(UIKit.Box(root, "Enedina", Color.white), new Vector2(0.5f, 0.5f), new Vector2(260, -40), new Vector2(160, 240));
            var img = e.GetComponent<Image>();
            img.sprite = ene;
            img.preserveAspect = true;
        }

        // Botones
        var first = UIKit.Button(root, "Play", UIKit.LoadIcon("jugar"), new Vector2(-130, 20), new Vector2(260, 52), Play);
        UIKit.Button(root, "Options", UIKit.LoadIcon("opciones"), new Vector2(-130, -46), new Vector2(260, 52), () => ShowOptions(true));
        UIKit.Button(root, "Exit", UIKit.LoadIcon("salir"), new Vector2(-130, -112), new Vector2(260, 52), Exit);
        first.Select();

        // Controles
        var help = UIKit.Place(new GameObject("Controles", typeof(RectTransform)).GetComponent<RectTransform>(),
                               new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(900, 30));
        help.SetParent(root, false);
        UIKit.Text(help, "A / D  move     SHIFT  run     SPACE  jump     E  interact     ESC  pause", 15, UIKit.Cream);
    }

    void Play()
    {
        GameSession.I.ResetAll();
        SceneManager.LoadScene(firstScene);
    }

    void ShowOptions(bool on)
    {
        if (optionsPanel == null) optionsPanel = OptionsPanel.Build(canvas, () => ShowOptions(false));
        optionsPanel.SetActive(on);
        mainPanel.SetActive(!on);
    }

    void Exit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
