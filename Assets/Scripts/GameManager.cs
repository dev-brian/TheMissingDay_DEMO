using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El "cerebro" de la partida (Singleton, como el ClueScoreManager de tu práctica 4).
// Lleva el puntaje, la energía (la barra de vida) y las cartas de lotería, y actualiza el HUD.
// Va en un objeto vacío llamado GameManager, uno por escena.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Reglas")]
    public int maxEnergy = 5;
    public int startEnergy = 3;
    public int totalCards = 5;
    public int pointsCorrect = 20;
    public int pointsWrong = -5;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text cardsText;
    [Tooltip("Image con Image Type = Filled, Fill Method = Horizontal.")]
    public Image energyFill;
    [Tooltip("Texto grande en el centro para avisos cortos (+10 THE SUN).")]
    public TMP_Text messageText;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip pickupClip;
    public AudioClip energyClip;

    public int Score { get; private set; }
    public int Energy { get; private set; }
    public int Cards { get; private set; }

    Coroutine messageRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Energy = Mathf.Clamp(startEnergy, 0, maxEnergy);
        if (messageText != null) messageText.text = "";
        RefreshHud();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddScore(int points)
    {
        Score = Mathf.Max(0, Score + points);
        RefreshHud();
    }

    // Devuelve true si la energía llegó a 0. Entonces Enedina descansa y la barra se recupera,
    // sin reiniciar el nivel (las cartas y los puntos se conservan).
    public bool ChangeEnergy(int amount)
    {
        Energy = Mathf.Clamp(Energy + amount, 0, maxEnergy);
        RefreshHud();
        Debug.Log($"[METRIC LOG] Energía: {Energy}/{maxEnergy}");
        if (Energy > 0) return false;
        Debug.Log("[METRIC LOG] Energía en 0: se repite la pregunta");
        ShowMessage("Enedina is tired... Let's try again!", 2.5f);
        StartCoroutine(Recover());
        return true;
    }

    public void CollectCard(string word, int points)
    {
        Cards++;
        AddScore(points);
        Play(pickupClip);
        ShowMessage($"+{points}  {word}");
        Debug.Log($"[METRIC LOG] Carta: {word} | Cartas: {Cards}/{totalCards} | Puntaje: {Score}");
        if (Cards == totalCards)
        {
            AddScore(50);
            ShowMessage("All the cards! +50", 2.5f);
        }
    }

    public void CollectEnergy(int amount)
    {
        ChangeEnergy(amount);
        Play(energyClip);
        ShowMessage($"+{amount} ENERGY");
    }

    // La llama el QuizManager en cada respuesta. Devuelve true si Enedina se cansó.
    public bool OnAnswer(bool correct)
    {
        AddScore(correct ? pointsCorrect : pointsWrong);
        return ChangeEnergy(correct ? 1 : -1);
    }

    public void ShowMessage(string text, float seconds = 1.5f)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(Message(text, seconds));
    }

    IEnumerator Message(string text, float seconds)
    {
        messageText.text = text;
        yield return new WaitForSecondsRealtime(seconds);
        messageText.text = "";
    }

    IEnumerator Recover()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        Energy = Mathf.Clamp(startEnergy, 1, maxEnergy);
        RefreshHud();
    }

    void RefreshHud()
    {
        if (scoreText != null) scoreText.text = Score.ToString("0000");
        if (cardsText != null) cardsText.text = $"{Cards}/{totalCards}";
        if (energyFill != null) energyFill.fillAmount = maxEnergy > 0 ? (float)Energy / maxEnergy : 0f;
    }

    void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
