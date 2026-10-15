using System.Collections.Generic;
using UnityEngine;

// Datos de toda la partida: puntos, energía, pistas, logros y métricas.
// No se pone en ninguna escena: se crea solo la primera vez que se usa y pasa de un nivel a otro
// (DontDestroyOnLoad). Es un Singleton, como el ClueScoreManager de tu práctica 4.
public class GameSession : MonoBehaviour
{
    static GameSession instance;

    public static GameSession I
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("GameSession");
                instance = go.AddComponent<GameSession>();
            }
            return instance;
        }
    }

    public const int MaxEnergy = 5;
    public const int StartEnergy = 3;

    public int Score { get; private set; }
    public int Energy { get; private set; } = StartEnergy;

    // Pistas encontradas (nombre y descripción en inglés) para el diario de pistas.
    public readonly List<string> ClueNames = new List<string>();
    public readonly List<string> ClueDescriptions = new List<string>();
    public readonly List<string> Achievements = new List<string>();

    // Métricas pedagógicas para la pantalla de resultados.
    public int CorrectAnswers;
    public int WrongAnswers;
    public float StartTime;

    // Avisa al HUD cuando algo cambia.
    public event System.Action Changed;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        StartTime = Time.time;
    }

    public void AddScore(int points)
    {
        Score = Mathf.Max(0, Score + points);
        Changed?.Invoke();
    }

    public void AddEnergy(int amount)
    {
        Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
        Changed?.Invoke();
    }

    public void AddClue(string clueName, string description)
    {
        if (!ClueNames.Contains(clueName))
        {
            ClueNames.Add(clueName);
            ClueDescriptions.Add(description);
        }
        Changed?.Invoke();
    }

    // Devuelve true solo la primera vez (el logro se desbloquea una sola vez).
    public bool UnlockAchievement(string achievement)
    {
        if (string.IsNullOrEmpty(achievement) || Achievements.Contains(achievement)) return false;
        Achievements.Add(achievement);
        Changed?.Invoke();
        return true;
    }

    // Para "Play" desde el menú: empieza todo de cero.
    public void ResetAll()
    {
        Score = 0;
        Energy = StartEnergy;
        ClueNames.Clear();
        ClueDescriptions.Clear();
        Achievements.Clear();
        CorrectAnswers = 0;
        WrongAnswers = 0;
        StartTime = Time.time;
        Changed?.Invoke();
    }
}
