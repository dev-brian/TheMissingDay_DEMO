using UnityEngine;

// La puerta que lleva al siguiente nivel. Cambia de escena con OnTrigger (lo que pide la rúbrica).
// Necesita un Collider 2D con "Is Trigger" activado. Mientras el nivel no esté completo, solo avisa.
// La escena destino debe estar en File > Build Profiles > Scene List.
public class LevelDoor : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena siguiente, por ejemplo N2_SalaAbuela.")]
    public string nextScene = "N2_SalaAbuela";
    [Tooltip("Si está marcado, la puerta solo abre cuando el nivel está completo.")]
    public bool needsLevelComplete = true;
    public string lockedMessage = "I need to find all the clues first.";

    bool used;

    bool CanOpen()
    {
        var level = LevelManager.Current;
        return !needsLevelComplete || level == null || level.Completed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;
        if (!CanOpen()) GameUI.I.Toast(lockedMessage, false);
    }

    // Stay: si Enedina ya estaba en la puerta cuando completó el nivel, también la deja pasar.
    void OnTriggerStay2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player") || !CanOpen() || GameUI.I.Busy) return;
        used = true;
        MetricsLogger.Log("Nivel_" + gameObject.scene.name, 1, true, Time.timeSinceLevelLoad);
        GameUI.I.LoadScene(nextScene);
    }
}
