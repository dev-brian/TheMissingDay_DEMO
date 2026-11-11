using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Pantalla de resultados al terminar la demo: puntos, respuestas, precisión, tiempo, pistas y logros.
// Va en un objeto vacío "Resultados" de la escena Resultados. También guarda una fila en metrics.csv.
public class ResultsScreen : MonoBehaviour
{
    public string menuScene = "Menu";
    public string firstScene = "C01_Intro";

    void Start()
    {
        Time.timeScale = 1f;
        var s = GameSession.I;
        int answers = s.CorrectAnswers + s.WrongAnswers;
        int accuracy = answers > 0 ? Mathf.RoundToInt(100f * s.CorrectAnswers / answers) : 0;
        float seconds = Time.time - s.StartTime;
        MetricsLogger.Log("DEMO_COMPLETA", answers, accuracy >= 70, seconds);

        var canvas = UIKit.Canvas(transform, "Resultados", 10);
        UIKit.Stretch(UIKit.Box(canvas, "Fondo", new Color32(26, 18, 32, 255)));
        var panel = UIKit.Framed(canvas, "Panel", new Vector2(0, 20), new Vector2(700, 430));

        var title = UIKit.Text(panel, "YOU WIN, DETECTIVE!", 36, UIKit.Gold, TextAlignmentOptions.Top);
        title.rectTransform.offsetMax = new Vector2(0, -16);
        title.fontStyle = FontStyles.Bold;

        int starCount = accuracy >= 90 ? 3 : accuracy >= 70 ? 2 : 1;
        string tier = starCount == 3 ? "GOLD" : starCount == 2 ? "SILVER" : "BRONZE";
        string stars = "(" + starCount + "/3) " + tier + " BADGE";

        // Insignia de ganador (oro / plata / bronce según la precisión), dibujada en pixel art por código.
        var badge = Badge(starCount);
        UIKit.Icon(panel, badge, new Vector2(64, 168), 72);
        UIKit.Icon(panel, badge, new Vector2(636, 168), 72);
        string message = accuracy >= 90 ? "Excellent, Detective!" : accuracy >= 70 ? "Great job, Detective!" : "Good try! Play again to improve.";

        Row(panel, "Points", s.Score.ToString(), 120);
        Row(panel, "Correct answers", s.CorrectAnswers + " / " + answers, 82);
        Row(panel, "Accuracy", accuracy + "%  " + stars, 44);
        Row(panel, "Time", Mathf.FloorToInt(seconds / 60f) + ":" + Mathf.FloorToInt(seconds % 60f).ToString("00"), 6);
        Row(panel, "Clues found", s.ClueNames.Count.ToString(), -32);
        Row(panel, "Achievements", s.Achievements.Count > 0 ? string.Join(", ", s.Achievements) : "-", -70);

        var msg = UIKit.Place(new GameObject("Mensaje", typeof(RectTransform)).GetComponent<RectTransform>(),
                              new Vector2(0.5f, 0.5f), new Vector2(0, -122), new Vector2(620, 36));
        msg.SetParent(panel, false);
        UIKit.Text(msg, message, 22, UIKit.Orange).fontStyle = FontStyles.Bold;

        var first = UIKit.Button(canvas, "Play Again", UIKit.LoadIcon("jugar"), new Vector2(-140, -230), new Vector2(240, 48), PlayAgain);
        UIKit.Button(canvas, "Main Menu", UIKit.LoadIcon("salir"), new Vector2(140, -230), new Vector2(240, 48), GoToMenu);
        first.Select();
    }

    // Medalla 32x32: círculo con borde oscuro, estrella crema al centro y listón verde azulado.
    static Sprite Badge(int starCount)
    {
        Color32 fill = starCount == 3 ? new Color32(201, 162, 77, 255)
                     : starCount == 2 ? new Color32(190, 196, 204, 255)
                     : new Color32(176, 112, 64, 255);
        Color32 shade = Color32.Lerp(fill, new Color32(26, 18, 32, 255), 0.35f);
        Color32 dark = new Color32(26, 18, 32, 255);
        Color32 star = UIKit.Cream;
        Color32 ribbon = UIKit.Teal;
        Color32 ribbonDark = UIKit.TealDark;
        Color32 clear = new Color32(0, 0, 0, 0);

        const int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[S * S];
        var c = new Vector2(15.5f, 19.5f);
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            var p = new Vector2(x, y);
            float d = Vector2.Distance(p, c);
            Color32 col = clear;
            // listón (abajo, detrás de la medalla)
            if (y < 12 && ((x >= 9 && x <= 13) || (x >= 18 && x <= 22)) && !(y < 3 && (x == 11 || x == 20)))
                col = (x == 9 || x == 13 || x == 18 || x == 22) ? ribbonDark : ribbon;
            if (d <= 12.5f) col = dark;
            if (d <= 11.3f) col = d > 9.3f ? shade : fill;
            // estrella de 5 picos
            Vector2 v = p - c;
            float ang = Mathf.Atan2(v.y, v.x) - Mathf.PI / 2f;
            float k = Mathf.Cos(5f * ang) * 0.5f + 0.5f;
            if (d <= Mathf.Lerp(3.0f, 8.0f, Mathf.Pow(k, 4f))) col = star;
            px[y * S + x] = col;
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 16);
    }

    static void Row(RectTransform panel, string label, string value, float y)
    {
        var row = UIKit.Place(new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>(),
                              new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(600, 34));
        row.SetParent(panel, false);
        var l = UIKit.Text(row, label, 21, UIKit.Teal, TextAlignmentOptions.MidlineLeft);
        l.rectTransform.offsetMax = new Vector2(-330, 0);
        var v = UIKit.Text(row, value, 21, UIKit.Cream, TextAlignmentOptions.MidlineLeft);
        v.rectTransform.offsetMin = new Vector2(280, 0);
        v.fontStyle = FontStyles.Bold;
    }

    void PlayAgain()
    {
        GameSession.I.ResetAll();
        SceneManager.LoadScene(firstScene);
    }

    void GoToMenu()
    {
        if (Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
    }
}
