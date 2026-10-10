using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Va en el objeto clave (calendario, caja...). Necesita un Collider2D con "Is Trigger" activado.
// Enedina debe tener el Tag "Player" y un Rigidbody2D + Collider2D.
public class InteractableObject : MonoBehaviour
{
    [Tooltip("Objeto hijo con el letrero [E]. Se muestra solo cuando Enedina está cerca.")]
    public GameObject prompt;
    public QuizManager quiz;
    [Tooltip("Si está activo, el objeto ya no se puede usar después de resolver el desafío.")]
    public bool singleUse = true;

    bool playerNear;
    bool solved;

    void Start()
    {
        if (prompt != null) prompt.SetActive(false);
    }

    void Update()
    {
        if (!playerNear || solved || quiz == null || quiz.IsOpen) return;
        if (InteractPressed()) quiz.Open(OnQuizFinished);
    }

    void OnQuizFinished()
    {
        if (singleUse)
        {
            solved = true;
            if (prompt != null) prompt.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = true;
        if (prompt != null && !solved) prompt.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = false;
        if (prompt != null) prompt.SetActive(false);
    }

    static bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}
