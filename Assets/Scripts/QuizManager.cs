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
        [TextArea] public string clue = "You find an old note about June 14th...";
        [TextArea] public string question = "Who _______ about that day?";
        public string[] options = { "know", "knows", "is knowing" };
        public int correctIndex = 1;
        [TextArea] public string hint = "Use third-person singular (-s): he / she / it / who + verb-s.";
        [TextArea] public string success = "Correct! \"Who knows about that day?\" You got a new clue.";
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
        }
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
