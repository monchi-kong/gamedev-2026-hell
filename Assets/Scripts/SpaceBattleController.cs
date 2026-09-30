using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SpaceBattleController : MonoBehaviour
{
    [SerializeField] private PlayerController playerPrefab;
    [SerializeField] private ChocolateEggPod podPrefab;
    [SerializeField] private ChocolateFountainBoss bossPrefab;
    private ChocolateEggPod pod;
    private bool victory;
    private float introUntil;
    private Texture2D starTexture;
    private Sprite starSprite;
    private Texture2D planetTexture;
    private Sprite planetSprite;

    private void Start()
    {
        Time.timeScale = 1f;
        var camera = GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(9f, 17f / camera.aspect);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.012f, 0.018f, 0.055f);
        CreateSpace(camera);

        pod = Instantiate(podPrefab, new Vector3(-9, -3, 0), Quaternion.identity);
        var player = Instantiate(playerPrefab, pod.SeatPosition, Quaternion.identity);
        player.RestoreCoins(SpaceBattleFlow.ConsumeCoins());
        Physics2D.SyncTransforms();
        pod.ConfigureSpace(new Rect(-14f, -6.5f, 28f, 11.5f));
        pod.BoardForSequence(player);
        pod.ControlsLocked = true;
        var boss = Instantiate(bossPrefab, new Vector3(6, 2, 0), Quaternion.identity);
        boss.ConfigureSpaceRound(player.transform);
        boss.FinalDefeated += Win;
        introUntil = Time.time + 3f;
    }

    private void Update()
    {
        if (pod == null) return;
        if (!victory)
        {
            pod.ControlsLocked = Time.time < introUntil;
            return;
        }
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.enterKey.wasPressedThisFrame) Restart();
        else if (keyboard.escapeKey.wasPressedThisFrame) Title();
    }

    private void Win()
    {
        victory = true;
        if (pod != null) pod.ControlsLocked = true;
    }

    private void Restart()
    {
        GameRunState.Reset();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SpaceBattleFlow.SceneName);
    }

    private void Title()
    {
        GameRunState.Reset();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }

    private void CreateSpace(Camera camera)
    {
        var material = bossPrefab.GetComponent<SpriteRenderer>().sharedMaterial;
        starTexture = new Texture2D(2, 2);
        starTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        starTexture.Apply();
        starSprite = Sprite.Create(starTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 2f);
        var random = new System.Random(20260920);
        for (int i = 0; i < 180; i++)
        {
            var star = new GameObject("Star").AddComponent<SpriteRenderer>();
            star.transform.SetParent(transform, false);
            star.transform.position = new Vector3(((float)random.NextDouble() * 2f - 1f) * camera.orthographicSize * camera.aspect,
                ((float)random.NextDouble() * 2f - 1f) * camera.orthographicSize, 5f);
            star.transform.localScale = Vector3.one * (0.025f + (float)random.NextDouble() * 0.07f);
            star.sprite = starSprite;
            star.sharedMaterial = material;
            star.sortingOrder = -100;
            star.color = Color.Lerp(new Color(0.3f, 0.45f, 0.8f), Color.white, (float)random.NextDouble());
        }
        planetTexture = new Texture2D(96, 96);
        planetTexture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 96; y++) for (int x = 0; x < 96; x++)
        {
            float dx = (x - 47.5f) / 46f, dy = (y - 47.5f) / 46f;
            float radius = dx * dx + dy * dy;
            Color color = Color.clear;
            if (radius < 1f)
            {
                color = Color.Lerp(new Color(0.035f, 0.06f, 0.18f), new Color(0.15f, 0.5f, 0.7f),
                    Mathf.Clamp01((1f - radius) * 0.7f + (dx + dy + 2f) * 0.2f));
                if (Mathf.Sin(dx * 14f + Mathf.Sin(dy * 18f)) > 0.6f)
                    color = Color.Lerp(color, new Color(0.28f, 0.45f, 0.42f), 0.45f);
            }
            planetTexture.SetPixel(x, y, color);
        }
        planetTexture.Apply();
        planetSprite = Sprite.Create(planetTexture, new Rect(0, 0, 96, 96), Vector2.one * 0.5f, 12f);
        var planet = new GameObject("Distant Planet").AddComponent<SpriteRenderer>();
        planet.transform.position = new Vector3(-11, -7, 4);
        planet.transform.SetParent(transform, true);
        planet.sprite = planetSprite;
        planet.sharedMaterial = material;
        planet.sortingOrder = -90;
    }

    private void OnGUI()
    {
        if (GameRunState.IsGameOver) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 38, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.white;
        if (!victory && Time.time < introUntil)
            GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 100), "ROUND 2\nSPACE BATTLE", style);
        if (!victory) return;
        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;
        GUI.Box(new Rect(x - 260, y - 130, 520, 270), GUIContent.none);
        GUI.Label(new Rect(x - 250, y - 110, 500, 80), "SPACE BOSS DEFEATED!", new GUIStyle(style) { fontSize = 30 });
        if (GUI.Button(new Rect(x - 140, y, 280, 48), "PLAY AGAIN  [ENTER]")) Restart();
        if (GUI.Button(new Rect(x - 140, y + 64, 280, 48), "TITLE  [ESC]")) Title();
    }

    private void OnDestroy()
    {
        if (starSprite != null) Destroy(starSprite);
        if (starTexture != null) Destroy(starTexture);
        if (planetSprite != null) Destroy(planetSprite);
        if (planetTexture != null) Destroy(planetTexture);
    }
}
