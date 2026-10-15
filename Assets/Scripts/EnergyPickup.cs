using UnityEngine;

// El chocolate caliente: al tocarlo, Enedina recupera energía y desaparece.
// Necesita un Collider 2D con "Is Trigger" activado.
public class EnergyPickup : MonoBehaviour
{
    public int energy = 1;
    public int points = 5;
    public string message = "Hot chocolate! +1 Energy";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GameSession.I.AddEnergy(energy);
        GameSession.I.AddScore(points);
        GameUI.I.Toast(message);
        GameUI.I.PlayPickup();
        Destroy(gameObject);
    }
}
