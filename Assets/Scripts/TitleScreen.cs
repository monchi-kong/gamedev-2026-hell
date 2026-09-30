using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private string gameTitle = "GAME TITLE";
    [SerializeField] private string firstScene = "Scene1";
    [SerializeField] private Texture2D background;

    private Font font;
    private CanvasGroup menu;
    private GameObject help;
    private Button startButton;
    private Text status;
    private bool loading;
    private readonly Color cream = new Color(0.94f, 0.94f, 0.81f);

    private void Start()
    {
        Time.timeScale = 1f;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasObject = new GameObject("Title Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasObject.transform;

        var backdrop = new GameObject("Forest", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        backdrop.transform.SetParent(root, false);
        backdrop.GetComponent<RawImage>().texture = background;
        backdrop.GetComponent<RawImage>().raycastTarget = false;
        var fit = backdrop.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = background != null ? (float)background.width / background.height : 16f / 9f;
        var shade = Panel("Shade", root, new Color(0.025f, 0.055f, 0.09f, 0.57f));
        Stretch(shade.rectTransform);
        shade.raycastTarget = false;

        var content = new GameObject("Menu", typeof(RectTransform), typeof(CanvasGroup));
        content.transform.SetParent(root, false);
        Stretch(content.GetComponent<RectTransform>());
        menu = content.GetComponent<CanvasGroup>();
        Label(content.transform, "2D PLATFORMER", 16, 178, new Color(0.65f, 0.8f, 0.65f));
        Label(content.transform, gameTitle, 76, 102, cream);
        startButton = MakeButton(content.transform, "START", -72, BeginGame);
        MakeButton(content.transform, "HOW TO PLAY", -140, ToggleHelp);
        status = Label(content.transform, "ENTER / CLICK TO START", 14, -218, new Color(0.72f, 0.79f, 0.75f));

        var panel = Panel("Controls", root, new Color(0.035f, 0.075f, 0.10f, 0.98f));
        Place(panel.rectTransform, 680, 440, 0);
        help = panel.gameObject;
        Label(help.transform, "HOW TO PLAY", 36, 142, cream);
        Label(help.transform, "A / D   or   LEFT / RIGHT     Move\n\nSPACE     Jump\n\nReach the goal to enter the next stage.", 22, 15, cream);
        MakeButton(help.transform, "BACK", -150, ToggleHelp);
        help.SetActive(false);

        if (EventSystem.current == null)
        {
            var events = new GameObject("Title EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || loading) return;
        if (keyboard.escapeKey.wasPressedThisFrame && help.activeSelf) ToggleHelp();
        if (!help.activeSelf && keyboard.enterKey.wasPressedThisFrame &&
            (EventSystem.current.currentSelectedGameObject == null || EventSystem.current.currentSelectedGameObject == startButton.gameObject))
            BeginGame();
    }

    private void ToggleHelp()
    {
        if (loading) return;
        help.SetActive(!help.activeSelf);
        menu.interactable = !help.activeSelf;
        menu.blocksRaycasts = !help.activeSelf;
        EventSystem.current.SetSelectedGameObject(help.activeSelf ? help.GetComponentInChildren<Button>().gameObject : startButton.gameObject);
    }

    private void BeginGame()
    {
        if (loading || help.activeSelf) return;
        if (!Application.CanStreamedLevelBeLoaded(firstScene))
        {
            status.text = "Scene not available: " + firstScene;
            Debug.LogError("Add the first scene to Build Settings: " + firstScene, this);
            return;
        }
        loading = true;
        menu.interactable = false;
        status.text = "LOADING...";
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        GameRunState.Reset();
        yield return null;
        yield return SceneManager.LoadSceneAsync(firstScene);
    }

    private Text Label(Transform parent, string value, int size, float y, Color color)
    {
        var obj = new GameObject(value.Split('\n')[0], typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        Place(text.rectTransform, help != null && parent == help.transform ? 640 : 1100, size > 50 ? 110 : 190, y);
        return text;
    }

    private Button MakeButton(Transform parent, string caption, float y, UnityEngine.Events.UnityAction action)
    {
        var image = Panel(caption, parent, Color.white);
        Place(image.rectTransform, 320, 54, y);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.19f, 0.29f, 0.26f, 0.96f);
        colors.highlightedColor = new Color(0.36f, 0.49f, 0.36f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.13f, 0.23f, 0.19f);
        button.colors = colors;
        button.onClick.AddListener(action);
        var label = Label(image.transform, caption, 20, 0, cream);
        Stretch(label.rectTransform);
        return button;
    }

    private static Image Panel(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Place(RectTransform rect, float width, float height, float y)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0, y);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
