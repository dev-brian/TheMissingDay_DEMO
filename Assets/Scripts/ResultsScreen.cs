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

        var title = UIKit.Text(panel, "DEMO COMPLETED", 36, UIKit.Gold, TextAlignmentOptions.Top);
        title.rectTransform.offsetMax = new Vector2(0, -16);
        title.fontStyle = FontStyles.Bold;

        string stars = "(" + (accuracy >= 90 ? 3 : accuracy >= 70 ? 2 : 1) + "/3 stars)";
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
