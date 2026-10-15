using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Va en una Image negra que cubre toda la pantalla, con un componente Canvas Group.
// Al empezar la escena aclara de negro a transparente; FadeToScene oscurece y cambia de escena.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public float fadeTime = 0.8f;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;   // no tapa los clics de los botones
    }

    void Start()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    public void FadeToScene(string sceneName)
    {
        StopAllCoroutines();
        StartCoroutine(FadeAndLoad(sceneName));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        yield return Fade(group.alpha, 1f);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        group.alpha = to;
    }
}
