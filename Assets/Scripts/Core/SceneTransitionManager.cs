using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;

    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<SceneTransitionManager>();

            if (_instance == null)
            {
                var go = new GameObject("SceneTransitionManager");
                _instance = go.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Transition Settings")]
    [SerializeField] private float _fadeInDuration = 0.5f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private Color _fadeColor = Color.black;

    private Image _fadeImage;
    private bool _isTransitioning = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        if (_fadeImage != null) return;

        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var fadeGO = new GameObject("FadeImage");
        fadeGO.transform.SetParent(canvasGO.transform, false);

        _fadeImage = fadeGO.AddComponent<Image>();
        _fadeImage.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, 0f);

        var fadeRT = fadeGO.GetComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero;
        fadeRT.offsetMax = Vector2.zero;
    }

    public void TransitionToScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(FadeTransitionCoroutine(sceneName));
    }

    private IEnumerator FadeTransitionCoroutine(string sceneName)
    {
        _isTransitioning = true;
        yield return StartCoroutine(FadeIn());
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeOut());
        _isTransitioning = false;
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / _fadeInDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(1f);
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / _fadeOutDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(0f);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (_fadeImage == null) return;
        var color = _fadeImage.color;
        color.a = alpha;
        _fadeImage.color = color;
    }

    public void SetFadeDuration(float fadeInDuration, float fadeOutDuration)
    {
        _fadeInDuration = fadeInDuration;
        _fadeOutDuration = fadeOutDuration;
    }

    public void SetFadeColor(Color color)
    {
        _fadeColor = color;
        if (_fadeImage != null)
        {
            var currentColor = _fadeImage.color;
            currentColor.r = color.r;
            currentColor.g = color.g;
            currentColor.b = color.b;
            _fadeImage.color = currentColor;
        }
    }

    public bool IsTransitioning => _isTransitioning;
}
