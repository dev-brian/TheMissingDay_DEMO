using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Movimiento LATERAL de Enedina (vista de lado, con gravedad), como pide el documento técnico v2.
// Reemplaza a PlayerMovement.cs (ese era para la vista desde arriba).
//
// Va en el objeto Enedina, que necesita:
//   - Rigidbody 2D: Body Type Dynamic, Gravity Scale 3, Freeze Rotation Z, Interpolate.
//   - Capsule Collider 2D con el material "SinFriccion" (para no quedarse pegada a las paredes).
//   - Tag "Player".
// El SpriteRenderer y el Animator pueden ir en el mismo objeto o en un hijo.
// Controles: A/D o flechas = caminar, Shift = correr, Espacio = saltar.
//
// Las animaciones se cambian desde aquí con su NOMBRE, así no tienes que dibujar flechas en el Animator.
// En el Animator deben existir estados con estos nombres exactos: Idle, Walk, Run, Jump, Fall, Land, Interact.
// Opcionales (vistas de frente y espalda de la hoja): IdleFront (si se queda quieta unos segundos voltea a la
// cámara) y Back (de espaldas mientras revisa algo de la pared, como el póster o la foto).
// Si falta alguno, el script lo salta sin error.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPlatformer : MonoBehaviour
{
    [Header("Caminar y correr")]
    public float walkSpeed = 4f;
    public float runSpeed = 6.5f;

    [Header("Salto")]
    public float jumpSpeed = 12f;
    [Tooltip("Si sueltas Espacio antes de tiempo, el salto se corta a esta fracción (salto corto).")]
    [Range(0f, 1f)] public float jumpCut = 0.5f;
    [Tooltip("Segundos en los que todavía puedes saltar después de salir de una orilla.")]
    public float coyoteTime = 0.1f;
    [Tooltip("Segundos que se recuerda un Espacio presionado justo antes de tocar el piso.")]
    public float jumpBuffer = 0.12f;

    [Header("Piso")]
    [Tooltip("Capas que cuentan como piso: Suelo (y Plataformas si la creas).")]
    public LayerMask groundLayers;
    public Vector2 groundCheckSize = new Vector2(0.7f, 0.12f);

    [Header("Vistas de frente y espalda")]
    [Tooltip("Segundos quieta antes de voltear a la cámara (estado IdleFront).")]
    public float idleFrontDelay = 3f;

    [Header("Referencias (se llenan solas si las dejas vacías)")]
    public SpriteRenderer sprite;
    public Animator anim;
    public AudioSource audioSource;
    public AudioClip jumpClip;

    public bool IsGrounded { get; private set; }
    public bool FacingRight { get; private set; } = true;

    Rigidbody2D rb;
    Collider2D body;
    float moveX;
    bool running;
    float lastGroundedTime = -10f;
    float lastJumpPressTime = -10f;
    float interactUntil;
    float landUntil;
    bool wasGrounded = true;
    string currentState;
    string heldPose;
    float stillSince;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    // Crea el HUD y la pausa (Esc) aunque la escena todavía no tenga LevelManager ni pistas.
    void Start()
    {
        GameUI.I.RefreshHud();
    }

    void Update()
    {
        moveX = ReadMoveX();
        running = ReadRun();

        if (JumpPressed()) lastJumpPressTime = Time.time;

        // Salto variable: si sueltas Espacio mientras sube, sube menos.
        if (JumpReleased() && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCut);

        // Voltear: el dibujo mira a la derecha; para la izquierda se espeja con flipX.
        if (moveX > 0.01f) FacingRight = true;
        else if (moveX < -0.01f) FacingRight = false;
        if (sprite != null) sprite.flipX = !FacingRight;

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        IsGrounded = CheckGround();
        if (IsGrounded) lastGroundedTime = Time.time;
        if (IsGrounded && !wasGrounded) landUntil = Time.time + 0.12f;   // amortigua al caer
        wasGrounded = IsGrounded;

        Vector2 v = rb.linearVelocity;
        v.x = moveX * (running ? runSpeed : walkSpeed);

        bool wantsJump = Time.time - lastJumpPressTime <= jumpBuffer;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (wantsJump && canJump)
        {
            v.y = jumpSpeed;
            lastJumpPressTime = -10f;
            lastGroundedTime = -10f;
            IsGrounded = false;
            if (audioSource != null && jumpClip != null) audioSource.PlayOneShot(jumpClip);
        }

        rb.linearVelocity = v;
    }

    bool CheckGround()
    {
        if (body == null) return false;
        if (rb.linearVelocity.y > 0.05f) return false;   // va subiendo: no está en el piso
        Bounds b = body.bounds;
        Vector2 feet = new Vector2(b.center.x, b.min.y);
        return Physics2D.OverlapBox(feet, groundCheckSize, 0f, groundLayers) != null;
    }

    void UpdateAnimation()
    {
        string next;
        if (Time.time < interactUntil) next = "Interact";
        else if (!IsGrounded) next = rb.linearVelocity.y > 0.1f ? "Jump" : "Fall";
        else if (Time.time < landUntil) next = "Land";
        else if (Mathf.Abs(moveX) > 0.01f) next = running ? "Run" : "Walk";
        else next = Time.time - stillSince >= idleFrontDelay && HasState("IdleFront") ? "IdleFront" : "Idle";
        if (next != "Idle" && next != "IdleFront") stillSince = Time.time;
        PlayState(next);
    }

    void PlayState(string state)
    {
        if (anim == null || state == currentState) return;
        if (!HasState(state))   // si falta el estado, no truena
        {
            if (state == "Land") PlayState(Mathf.Abs(moveX) > 0.01f ? (running ? "Run" : "Walk") : "Idle");
            return;
        }
        anim.Play(state);
        currentState = state;
    }

    bool HasState(string state) => anim != null && anim.HasState(0, Animator.StringToHash(state));

    // Clue la usa para que Enedina se ponga de espaldas ("Back") mientras ve algo de la pared.
    public void HoldPose(string state)
    {
        if (!HasState(state) || !anim.isActiveAndEnabled) return;
        heldPose = state;
        anim.Play(state);
        currentState = state;
    }

    public void ReleasePose()
    {
        heldPose = null;
        currentState = null;
        stillSince = Time.time;
    }

    // Otros scripts la llaman al recoger un objeto: Enedina se agacha un momento.
    public void PlayInteract(float seconds = 0.75f)
    {
        interactUntil = Time.time + seconds;
        PlayState("Interact");
    }

    // Cuando un diálogo o pregunta desactiva este script, Enedina se detiene (la gravedad sigue).
    void OnDisable()
    {
        moveX = 0f;
        if (rb != null) rb.linearVelocity = new Vector2(0f, Mathf.Min(rb.linearVelocity.y, 0f));
        currentState = null;
        stillSince = Time.time;
        // Al cerrar la escena o apagar a Enedina no se toca el Animator (evita el aviso "animator is inactive").
        if (anim == null || !anim.isActiveAndEnabled || !gameObject.activeInHierarchy) return;
        // Si se está agachando a recoger algo o está de espaldas, deja esa animación.
        if (Time.time >= interactUntil && heldPose == null && HasState("Idle")) anim.Play("Idle");
    }

    // Dibuja en la Scene el rectángulo que revisa el piso (amarillo).
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<Collider2D>();
        if (c == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(c.bounds.center.x, c.bounds.min.y, 0f), groundCheckSize);
    }

    static float ReadMoveX()
    {
        float x = 0f;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return 0f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
#else
        x = Input.GetAxisRaw("Horizontal");
#endif
        return x;
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

    static bool JumpReleased()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;
#else
        return Input.GetKeyUp(KeyCode.Space);
#endif
    }
}
