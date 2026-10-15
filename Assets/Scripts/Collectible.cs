using UnityEngine;

// Coleccionable con OnTrigger (tu práctica 5): carta de lotería (suma puntos) o chocolate (suma energía).
// Necesita un Collider2D con "Is Trigger" activado. Enedina debe tener el Tag "Player".
public class Collectible : MonoBehaviour
{
    public enum Kind { LoteriaCard, Energy }

    public Kind kind = Kind.LoteriaCard;
    [Tooltip("Palabra en inglés que aprende el jugador, por ejemplo THE SUN.")]
    public string word = "THE SUN";
    public int points = 10;
    public int energy = 1;

    [Header("Flotar (solo visual)")]
    public float bobHeight = 0.12f;
    public float bobSpeed = 2.5f;

    Vector3 start;

    void Start()
    {
        start = transform.localPosition;
    }

    void Update()
    {
        // Sube y baja un poquito para llamar la atención, en pasos de 1 píxel (1/16).
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = start + new Vector3(0f, Mathf.Round(y * 16f) / 16f, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (kind == Kind.LoteriaCard) gm.CollectCard(word, points);
            else gm.CollectEnergy(energy);
        }
        Destroy(gameObject);
    }
}
