using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Uno por nivel, en un objeto vacío llamado LevelManager.
// Al empezar: banner del nivel (LEVEL 1 / MY DREAM / Mission...), diálogo inicial y objetivo en el HUD.
// Cuenta las pistas; al juntarlas todas: diálogo de cierre, MISSION COMPLETED, logro y nuevo objetivo
// (por ejemplo "Go to the door"). La puerta (LevelDoor) solo deja pasar cuando el nivel está completo.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Current { get; private set; }

    [Tooltip("Elige el nivel y al dar Play se llenan solos todos los textos (están en ClueLibrary.cs).")]
    public LevelId preset = LevelId.Personalizado;

    [Header("Nivel")]
    public string levelNumber = "LEVEL 1";
    public string levelTitle = "MY DREAM";
    public string mission = "Explore your room.";

    [Header("Diálogo inicial (Enedina)")]
    [TextArea] public string[] introLines = { "I love movies.", "One day, I want to make my own movies." };

    [Header("Pistas")]
    public string counterLabel = "Clues";
    public int totalClues = 3;

    [Header("Al completar")]
    [TextArea] public string[] completeLines = { "I want to make my own movies.", "This is my dream." };
    public string achievementName = "Dreamer";
    public string achievementText = "You found your three movie treasures!";
    [Tooltip("Objetivo después de completar. Vacío si el nivel termina con la cinemática final.")]
    public string afterObjective = "Go to the door.";
    [Tooltip("Qué pasa al completar (por ejemplo, la cinemática final del Nivel 3).")]
    public UnityEvent onCompleted;

    public int Found { get; private set; }
    public bool Completed { get; private set; }

    void Awake()
    {
        Current = this;
        if (preset != LevelId.Personalizado) ClueLibrary.Apply(this);
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    IEnumerator Start()
    {
        var ui = GameUI.I;
        ui.SetObjective("");
        ui.SetCounter(counterLabel, 0, totalClues);
        ui.Lock();
        yield return new WaitForSeconds(0.4f);
        yield return ui.Banner(levelNumber, levelTitle + "\n<size=80%>Mission: " + mission + "</size>", 2.5f);
        yield return ui.ShowLines("ENEDINA", introLines);
        ui.SetObjective(mission);
        ui.Toast("Mission started: " + mission);
        ui.Unlock();
    }

    // La llama cada Clue al registrarse.
    public void ClueFound()
    {
        if (Completed) return;
        Found++;
        GameUI.I.SetCounter(counterLabel, Found, totalClues);
        if (Found >= totalClues) StartCoroutine(Complete());
    }

    IEnumerator Complete()
    {
        Completed = true;
        var ui = GameUI.I;
        ui.Lock();
        yield return new WaitForSeconds(0.3f);
        yield return ui.ShowLines("ENEDINA", completeLines);
        yield return ui.Banner("MISSION COMPLETED", levelTitle);
        if (GameSession.I.UnlockAchievement(achievementName))
            yield return ui.Banner("ACHIEVEMENT UNLOCKED!", achievementName + "\n<size=80%>" + achievementText + "</size>");
        ui.SetObjective(afterObjective);
        ui.Unlock();
        onCompleted?.Invoke();
    }
}
