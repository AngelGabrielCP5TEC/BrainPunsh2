using TMPro;
using UnityEngine;
using UnityEngine.UI;

// In-RING overlay shown at end of best-of-3 match.
//   • Fullscreen win/loss image
//   • "Press R to retry" caption
//   • R key → fade music & return to MainMenu via SceneTransitionManager
// Builds its own Canvas at runtime — just drop this component on a GameObject in RING
// and assign the two sprites in the Inspector.
public class MatchEndOverlay : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite _winSprite;
    [SerializeField] private Sprite _loseSprite;

    [Header("Caption")]
    [SerializeField] private string _caption = "Press R to retry";
    [SerializeField] private int _captionFontSize = 56;
    [SerializeField] private Color _captionColor = Color.white;

    [Header("Behaviour")]
    [SerializeField] private KeyCode _retryKey = KeyCode.R;
    [SerializeField] private string _mainMenuScene = "MainMenu";
    [SerializeField] private float _backgroundDim = 0.8f;
    [SerializeField] private bool _preserveAspect = true;

    private Canvas _canvas;
    private GameObject _root;
    private Image _bg;
    private Image _imageDisplay;
    private TextMeshProUGUI _captionText;
    private bool _active;

    void Awake()
    {
        BuildOverlay();
        Hide();
    }

    void Update()
    {
        if (!_active) return;
        if (Input.GetKeyDown(_retryKey))
        {
            Time.timeScale = 1f;
            AudioManager.Instance?.StopMusic();
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionToScene(_mainMenuScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuScene);
        }
    }

    public void ShowWin()
    {
        if (_winSprite != null) _imageDisplay.sprite = _winSprite;
        Show();
        AudioManager.Instance?.PlayWinMusic();
    }

    public void ShowLose()
    {
        if (_loseSprite != null) _imageDisplay.sprite = _loseSprite;
        Show();
        AudioManager.Instance?.PlayLoseMusic();
    }

    private void Show() { _root.SetActive(true); _active = true; }
    private void Hide() { _root.SetActive(false); _active = false; }

    private void BuildOverlay()
    {
        // Top-level Canvas (separate from HUD so we sort cleanly above it)
        _root = new GameObject("MatchEndCanvas");
        _root.transform.SetParent(transform, false);
        _canvas = _root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _root.AddComponent<GraphicRaycaster>();

        // Dim background
        var bgGO = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(_root.transform, false);
        StretchFill(bgGO.GetComponent<RectTransform>());
        _bg = bgGO.GetComponent<Image>();
        _bg.color = new Color(0f, 0f, 0f, _backgroundDim);

        // Image display
        var imgGO = new GameObject("Image", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(_root.transform, false);
        var imgRT = imgGO.GetComponent<RectTransform>();
        imgRT.anchorMin = new Vector2(0.5f, 0.5f);
        imgRT.anchorMax = new Vector2(0.5f, 0.5f);
        imgRT.pivot     = new Vector2(0.5f, 0.5f);
        imgRT.anchoredPosition = new Vector2(0f, 60f);
        imgRT.sizeDelta = new Vector2(900f, 600f);
        _imageDisplay = imgGO.GetComponent<Image>();
        _imageDisplay.preserveAspect = _preserveAspect;

        // Caption
        var capGO = new GameObject("Caption", typeof(RectTransform));
        capGO.transform.SetParent(_root.transform, false);
        _captionText = capGO.AddComponent<TextMeshProUGUI>();
        _captionText.text = _caption;
        _captionText.fontSize = _captionFontSize;
        _captionText.color = _captionColor;
        _captionText.alignment = TextAlignmentOptions.Center;
        _captionText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var capRT = capGO.GetComponent<RectTransform>();
        capRT.anchorMin = new Vector2(0.5f, 0f);
        capRT.anchorMax = new Vector2(0.5f, 0f);
        capRT.pivot     = new Vector2(0.5f, 0f);
        capRT.anchoredPosition = new Vector2(0f, 90f);
        capRT.sizeDelta = new Vector2(1200f, 80f);
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
