using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200)]
public class ReadableSign : MonoBehaviour
{
    [SerializeField] private string heading = "看板";
    [SerializeField, TextArea(3, 12)] private string message = "この先に気をつけて！";
    [SerializeField, Min(0.5f)] private float readDistance = 2f;
    private static readonly List<ReadableSign> signs = new List<ReadableSign>();
    private static ReadableSign active;
    private static int lastInteractionFrame = -1;
    public static bool IsReading => active != null;
    private PlayerController player;
    private Font font;
    private Vector2 scroll;
    private float previousTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        signs.Clear();
        active = null;
        lastInteractionFrame = -1;
    }

    private void OnEnable() => signs.Add(this);
    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial" }, 24);
    }

    private bool IsNearest()
    {
        if (player == null || player.IsRiding) return false;
        ReadableSign nearest = null;
        float best = float.PositiveInfinity;
        foreach (var sign in signs)
        {
            float distance = Vector2.Distance(player.transform.position, sign.transform.position);
            if (distance > sign.readDistance || distance >= best) continue;
            best = distance;
            nearest = sign;
        }
        return nearest == this;
    }

    private void Update()
    {
        if (SpaceBattleFlow.IsTransitioning) return;
        if (GameRunState.IsGameOver) { if (active == this) Close(); return; }
        var keyboard = Keyboard.current;
        if (keyboard == null || lastInteractionFrame == Time.frameCount) return;
        if (active == this)
        {
            if (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) Close();
        }
        else if (active == null && Time.timeScale > 0f && IsNearest() && keyboard.eKey.wasPressedThisFrame)
        {
            active = this;
            lastInteractionFrame = Time.frameCount;
            scroll = Vector2.zero;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    private void Close()
    {
        if (active != this) return;
        active = null;
        lastInteractionFrame = Time.frameCount;
        if (!GameRunState.IsGameOver) Time.timeScale = previousTimeScale;
    }

    private void OnDisable() { Close(); signs.Remove(this); }
    private void OnDestroy() { if (font != null) Destroy(font); }

    private void OnGUI()
    {
        if (GameRunState.IsGameOver || player == null) return;
        var style = new GUIStyle(GUI.skin.label) { font = font, fontSize = 22, wordWrap = true, richText = false };
        style.normal.textColor = Color.white;
        if (active == null && IsNearest())
        {
            var hint = new Rect((Screen.width - 320) / 2f, Screen.height - 70, 320, 44);
            GUI.Box(hint, GUIContent.none);
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(hint, "E：看板を読む", style);
        }
        if (active != this) return;
        Color saved = GUI.color;
        GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float width = Mathf.Min(760, Screen.width - 32);
        float height = Mathf.Min(460, Screen.height - 32);
        var panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x + 24, panel.y + 16, width - 48, 36), heading, style);
        var viewport = new Rect(panel.x + 24, panel.y + 64, width - 48, height - 138);
        float contentWidth = viewport.width - 24;
        float contentHeight = Mathf.Max(viewport.height, style.CalcHeight(new GUIContent(message), contentWidth));
        scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, contentWidth, contentHeight));
        GUI.Label(new Rect(0, 0, contentWidth, contentHeight), message, style);
        GUI.EndScrollView();
        var buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = 20 };
        if (GUI.Button(new Rect(panel.center.x - 130, panel.yMax - 58, 260, 40), "閉じる（E / Esc）", buttonStyle)) Close();
        GUI.color = saved;
    }
}
