using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Va en Enedina (el objeto padre). Necesita Rigidbody2D (Gravity Scale 0) y un Collider2D en los pies.
// El dibujo y el Animator van en un hijo llamado "Visual": así el salto sube el dibujo
// mientras la sombra y el collider se quedan en el piso.
// Controles: WASD o flechas = mover, Shift = correr, Espacio = saltar.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 4f;
    public float runMultiplier = 1.6f;

    [Header("Salto")]
    public float jumpHeight = 0.9f;      // en unidades (16 px = 1 unidad)
    public float jumpDuration = 0.45f;   // segundos en el aire
    [Tooltip("Capa de los obstáculos bajos que se pueden saltar (cajas).")]
    public string jumpableLayer = "Saltable";

    [Header("Referencias (se llenan solas si las dejas vacías)")]
    public Transform visual;             // hijo con SpriteRenderer + Animator
    public AudioSource audioSource;
    public AudioClip jumpClip;

    public bool IsJumping { get; private set; }

    Rigidbody2D rb;
    Collider2D feet;
    Animator anim;
    SpriteRenderer sprite;
    Vector2 movement;
    Vector2 facing = Vector2.down;
    bool running;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        feet = GetComponent<Collider2D>();
        if (visual == null)
        {
            var found = transform.Find("Visual");
            visual = found != null ? found : transform;
        }
        anim = visual.GetComponent<Animator>();
        sprite = visual.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        movement = ReadMove().normalized;
        running = ReadRun();

        if (movement != Vector2.zero) facing = movement;

        // Voltear: el dibujo de perfil mira a la derecha; para la izquierda se espeja.
        if (sprite != null)
        {
            if (movement.x < -0.01f) sprite.flipX = true;
            else if (movement.x > 0.01f) sprite.flipX = false;
        }

        if (anim != null)
        {
            anim.SetFloat("MoveX", facing.x);
            anim.SetFloat("MoveY", facing.y);
            anim.SetFloat("Speed", movement.magnitude);
            anim.SetBool("Jumping", IsJumping);
            // Al correr, la animación de caminar se reproduce más rápido.
            anim.speed = running && movement != Vector2.zero ? runMultiplier : 1f;
        }

        if (!IsJumping && JumpPressed()) StartCoroutine(Jump());
    }

    void FixedUpdate()
    {
        float speed = moveSpeed * (running ? runMultiplier : 1f);
        rb.linearVelocity = movement * speed;
    }

    IEnumerator Jump()
    {
        IsJumping = true;
        if (audioSource != null && jumpClip != null) audioSource.PlayOneShot(jumpClip);

        int me = gameObject.layer;
        int low = LayerMask.NameToLayer(jumpableLayer);
        if (low >= 0) Physics2D.IgnoreLayerCollision(me, low, true);

        for (float t = 0f; t < jumpDuration; t += Time.deltaTime)
        {
            float k = t / jumpDuration;
            SetHeight(4f * jumpHeight * k * (1f - k));   // parábola: sube y baja
            yield return null;
        }
        SetHeight(0f);

        // Si cae encima de una caja, sigue "en el aire" hasta salir de ella.
        if (low >= 0)
        {
            while (feet != null && Physics2D.OverlapBox(feet.bounds.center, feet.bounds.size, 0f, 1 << low) != null)
                yield return null;
            Physics2D.IgnoreLayerCollision(me, low, false);
        }
        IsJumping = false;
    }

    void SetHeight(float h)
    {
        if (visual != null && visual != transform) visual.localPosition = new Vector3(0f, h, 0f);
    }

    // Cuando el quiz desactiva este script, Enedina se detiene y queda en idle.
    void OnDisable()
    {
        movement = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.speed = 1f;
        }
    }

    static Vector2 ReadMove()
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

    static bool ReadRun()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }

    static bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
