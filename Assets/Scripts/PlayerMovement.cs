using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Va en Enedina. Necesita Rigidbody2D (Gravity Scale 0) y un Collider2D en los pies.
// Mueve con WASD o flechas y le avisa al Animator hacia dónde camina.
// Funciona con "Active Input Handling" en New, Old o Both.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 4f;

    Rigidbody2D rb;
    Animator anim;      // Opcional: si Enedina aún no tiene Animator, el script sigue funcionando.
    Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        movement = ReadInput().normalized;

        if (anim != null)
        {
            // Solo actualizamos la dirección cuando se mueve, así al soltar
            // la tecla Enedina se queda mirando hacia donde caminaba.
            if (movement != Vector2.zero)
            {
                anim.SetFloat("MoveX", movement.x);
                anim.SetFloat("MoveY", movement.y);
            }
            anim.SetFloat("Speed", movement.magnitude);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    // Cuando el quiz desactiva este script, Enedina se detiene y queda en idle.
    void OnDisable()
    {
        movement = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);
    }

    static Vector2 ReadInput()
    {
        Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return v;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
#else
        v.x = Input.GetAxisRaw("Horizontal");
        v.y = Input.GetAxisRaw("Vertical");
#endif
        return v;
    }
}
