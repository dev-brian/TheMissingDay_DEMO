using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Diario de pistas: TAB lo abre y lo cierra en cualquier nivel. GameUI lo agrega solo.
// Muestra las pistas encontradas (con su descripción en inglés) y los logros.
public class ClueJournal : MonoBehaviour
{
    public static bool Open { get; private set; }
    public static int ClosedFrame = -1;   // para que el mismo Esc no abra también la pausa

    RectTransform canvas;
    GameObject root;
    TextMeshProUGUI body;

    void Update()
    {
        if (PauseMenu.Paused) return;
        if (Open && (TabPressed() || EscPressed())) SetOpen(false);
        else if (!Open && TabPressed() && !GameUI.I.Busy) SetOpen(true);
    }

    void SetOpen(bool on)
    {
        if (root == null) Build();
        Open = on;
        if (!on) ClosedFrame = Time.frameCount;
        root.SetActive(on);
        if (on) { Refresh(); GameUI.I.Lock(); }
        else GameUI.I.Unlock();
    }

    void Refresh()
    {
        var s = GameSession.I;
        var sb = new System.Text.StringBuilder();
        if (s.ClueNames.Count == 0) sb.Append("<color=#7CB8AE>No clues yet. Explore and press E near objects.</color>\n");
        for (int i = 0; i < s.ClueNames.Count; i++)
        {
            sb.Append("<color=#C9A24D>").Append(i + 1).Append(". ").Append(s.ClueNames[i]).Append("</color>\n");
            sb.Append("<size=85%>").Append(s.ClueDescriptions[i]).Append("</size>\n\n");
        }
        sb.Append("<color=#EE8A3A>ACHIEVEMENTS</color>\n");
        if (s.Achievements.Count == 0) sb.Append("<size=85%>-</size>");
        else sb.Append("<size=85%>").Append(string.Join("   ", s.Achievements)).Append("</size>");
        body.text = sb.ToString();
    }

    void Build()
    {
        canvas = UIKit.Canvas(transform, "Diario", 65);
        var dim = UIKit.Stretch(UIKit.Box(canvas, "Oscuro", new Color(0, 0, 0, 0.55f)));
        root = dim.gameObject;
        var panel = UIKit.Framed(dim, "Panel", Vector2.zero, new Vector2(620, 440));
        var title = UIKit.Text(panel, "CLUE JOURNAL", 30, UIKit.Gold, TextAlignmentOptions.Top);
        title.rectTransform.offsetMax = new Vector2(0, -16);
        title.fontStyle = FontStyles.Bold;
        body = UIKit.Text(panel, "", 18, UIKit.Cream, TextAlignmentOptions.TopLeft);
        body.rectTransform.offsetMin = new Vector2(34, 44);
        body.rectTransform.offsetMax = new Vector2(-34, -66);
        body.overflowMode = TextOverflowModes.Ellipsis;
        var hint = UIKit.Text(panel, "TAB  close", 14, UIKit.Teal, TextAlignmentOptions.Bottom);
        hint.rectTransform.offsetMin = new Vector2(0, 14);
    }

    void OnDestroy()
    {
        if (Open) Open = false;
    }

    static bool TabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
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
