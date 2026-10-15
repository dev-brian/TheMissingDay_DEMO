using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ventana de diálogo con el desafío de inglés. Va en un objeto vacío de la escena.
// Muestra la pista y la pregunta, valida la respuesta, da retroalimentación y registra métricas.
public class QuizManager : MonoBehaviour
{
    [Serializable]
    public class Challenge
    {
        public string id = "L1_PresentSimple";
        [TextArea] public string clue = "An old golden necklace... it is not Enedina's.";
        [TextArea] public string question = "The necklace _______ very old.";
        public string[] options = { "look", "looks", "is look" };
        public int correctIndex = 1;
        [TextArea] public string hint = "The necklace = it. In Present Simple, he / she / it + verb-s: looks.";
        [TextArea] public string success = "Correct! \"The necklace looks very old.\" But... whose necklace is this?";
    }

    [Header("Desafíos (se juegan en orden)")]
    public Challenge[] challenges = { new Challenge() };

    [Header("UI")]
    public GameObject panel;
    public TMP_Text clueText;
    public TMP_Text questionText;
    public Button[] optionButtons = new Button[3];
    public TMP_Text feedbackText;
    public Button continueButton;

    [Header("Enedina (para congelarla mientras responde)")]
    [Tooltip("Arrastra aquí a Enedina desde la Hierarchy.")]
    public PlayerMovement playerMovement;
    public Rigidbody2D playerBody;
    public Animator playerAnimator;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    public Color correctColor = new Color(0.3f, 0.85f, 0.4f);
    public Color wrongColor = new Color(0.95f, 0.4f, 0.35f);

    public bool IsOpen => panel != null && panel.activeSelf;

    int current;
    int attempts;
    float startTime;
    Action onFinished;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => Answer(index));
        }
        if (continueButton != null) continueButton.onClick.AddListener(Next);
    }

    public void Open(Action finished = null)
    {
        onFinished = finished;
        current = 0;
        SetPlayerFrozen(true);
        panel.SetActive(true);
        ShowChallenge();
    }

    void ShowChallenge()
    {
        Challenge c = challenges[current];
        attempts = 0;
        startTime = Time.unscaledTime;
        clueText.text = c.clue;
        questionText.text = c.question;
        feedbackText.text = "";
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool used = i < c.options.Length;
            optionButtons[i].gameObject.SetActive(used);
            optionButtons[i].interactable = true;
            if (used) optionButtons[i].GetComponentInChildren<TMP_Text>().text = $"{(char)('A' + i)}) {c.options[i]}";
        }
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        optionButtons[0].Select();
    }

    void Answer(int index)
    {
        Challenge c = challenges[current];
        attempts++;
        bool correct = index == c.correctIndex;
        MetricsLogger.LogAnswer(c.id, attempts, c.options[index], correct);
        // Avisa al GameManager (puntos y energía). Devuelve true si Enedina se quedó sin energía.
        bool tired = GameManager.Instance != null && GameManager.Instance.OnAnswer(correct);

        if (correct)
        {
            feedbackText.color = correctColor;
            feedbackText.text = c.success;
            Play(correctClip);
            foreach (var b in optionButtons) b.interactable = false;
            MetricsLogger.Log(c.id, attempts, true, Time.unscaledTime - startTime);
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.Select();
            }
            else
            {
                StartCoroutine(NextAfter(2.5f));
            }
        }
        else
        {
            feedbackText.color = wrongColor;
            feedbackText.text = $"Not quite. {c.hint} Try again!";
            Play(wrongClip);
            optionButtons[index].interactable = false;
            if (tired) RetryChallenge();
        }
    }

    // Sin energía: se repite solo esta pregunta (no el nivel). Los intentos y el tiempo siguen contando para las métricas.
    void RetryChallenge()
    {
        Challenge c = challenges[current];
        feedbackText.text = $"Enedina is tired... Let's read the clue again. {c.hint}";
        for (int i = 0; i < optionButtons.Length && i < c.options.Length; i++) optionButtons[i].interactable = true;
        optionButtons[0].Select();
    }

    IEnumerator NextAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        Next();
    }

    void Next()
    {
        current++;
        if (current < challenges.Length)
        {
            ShowChallenge();
            return;
        }
        panel.SetActive(false);
        SetPlayerFrozen(false);
        onFinished?.Invoke();
    }

    void SetPlayerFrozen(bool frozen)
    {
        if (playerMovement != null) playerMovement.enabled = !frozen;
        if (playerBody != null)
        {
#if UNITY_6000_0_OR_NEWER
            playerBody.linearVelocity = Vector2.zero;
#else
            playerBody.velocity = Vector2.zero;
#endif
        }
        if (frozen && playerAnimator != null)
        {
            // Deja a Enedina en idle. Cambia "Speed" si tu Animator usa otro nombre.
            foreach (var p in playerAnimator.parameters)
                if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) playerAnimator.SetFloat(p.nameHash, 0f);
        }
    }

    void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
