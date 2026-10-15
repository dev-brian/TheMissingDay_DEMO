using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Va en cada objeto con el que Enedina interactúa: pistas (cámara, collar, fotografía, hojas del guion)
// y objetos que solo tienen diálogo (el escritorio).
// Necesita un Collider 2D con "Is Trigger" activado, un poco más grande que el dibujo.
//
// Al presionar E dentro del trigger:
//   1) Enedina se agacha (animación Interact) y dice "Antes" (opcional)
//   2) Sale la pregunta de inglés (si "Tiene Pregunta" está marcado) y no sigue hasta acertar
//   3) Enedina dice "Despues" (el diálogo de la pista)
//   4) Si "Cuenta Como Pista": +10 puntos, se suma al contador y se guarda en el diario de pistas
public class Clue : MonoBehaviour
{
    [Tooltip("Elige la pista y al dar Play se llenan solos todos los textos (están en ClueLibrary.cs). " +
             "Deja 'Personalizada' para escribirlos tú abajo.")]
    public ClueId preset = ClueId.Personalizada;

    [Header("Pista")]
    public string clueName = "The Camera";
    [Tooltip("Descripción corta en inglés para el diario de pistas.")]
    [TextArea] public string description = "Enedina's camera. She uses it to make videos.";
    public bool countsAsClue = true;
    public int points = 10;

    [Header("Diálogos (personaje: Enedina)")]
    public string speaker = "ENEDINA";
    [Tooltip("Lo que dice al encontrarla, antes de la pregunta. Puede ir vacío.")]
    [TextArea] public string[] before;
    [Tooltip("Lo que dice después de acertar.")]
    [TextArea] public string[] after = { "This is my camera.", "I use it to make videos." };

    [Header("Pregunta de inglés")]
    public bool hasQuestion = true;
    public GameUI.Question question = new GameUI.Question();

    [Header("Al terminar")]
    [Tooltip("Nuevo texto de misión (vacío = no cambia).")]
    public string nextObjective = "";
    [Tooltip("Ocultar el objeto al recogerlo (para las hojas del guion).")]
    public bool hideWhenDone = false;
    [Tooltip("Enedina se pone de espaldas mientras habla (objetos de la pared: póster, foto).")]
    public bool lookBack = false;
    public string promptText = "Press E to interact";
    public UnityEvent onDone;

    public bool Done { get; private set; }

    bool near;
    bool running;

    void Awake()
    {
        if (preset != ClueId.Personalizada) ClueLibrary.Apply(this);
    }

    void Update()
    {
        if (!near || Done || running || GameUI.I.Busy) return;
        if (GameUI.InteractPressed()) StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        running = true;
        var ui = GameUI.I;
        ui.HidePrompt();
        var player = FindFirstObjectByType<PlayerPlatformer>();
        if (player != null) player.PlayInteract();   // se agacha (antes de congelarla)
        ui.Lock();
        ui.PlayPickup();
        yield return new WaitForSeconds(0.5f);
        if (lookBack && player != null) player.HoldPose("Back");   // de espaldas viendo la pared

        yield return ui.ShowLines(speaker, before);
        if (hasQuestion) yield return ui.Ask(question);
        yield return ui.ShowLines(speaker, after);

        Done = true;
        if (countsAsClue)
        {
            GameSession.I.AddClue(clueName, description);
            GameSession.I.AddScore(points);
            ui.Toast($"+{points}  {clueName}");
        }
        if (!string.IsNullOrEmpty(nextObjective)) ui.SetObjective(nextObjective);
        if (hideWhenDone) foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;

        if (lookBack && player != null) player.ReleasePose();
        ui.Unlock();
        running = false;
        if (countsAsClue && LevelManager.Current != null) LevelManager.Current.ClueFound();
        onDone?.Invoke();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        near = true;
        if (!Done) GameUI.I.ShowPrompt(promptText);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        near = false;
        GameUI.I.HidePrompt();
    }
}
