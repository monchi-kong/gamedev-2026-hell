using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpaceBattleFlow : MonoBehaviour
{
    public const string SceneName = "SpaceBoss";
    public static bool IsTransitioning { get; private set; }
    public static int CarriedCoins { get; private set; }
    private float progress;
    private bool loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { IsTransitioning = false; CarriedCoins = 0; }
    public static int ConsumeCoins() { int result = CarriedCoins; CarriedCoins = 0; return result; }

    public static bool Begin(ChocolateFountainBoss boss, ChocolateEggPod fallbackPod)
    {
        if (GameRunState.IsGameOver || SceneManager.GetActiveScene().name == SceneName || IsTransitioning) return false;
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null || !Application.CanStreamedLevelBeLoaded(SceneName))
        { Debug.LogError("SpaceBoss scene or player is missing; add SpaceBoss to the build scene list."); return false; }
        var pod = player.CurrentVehicle;
        if (pod == null) pod = FindFirstObjectByType<ChocolateEggPod>();
        if (pod == null && fallbackPod != null)
            pod = Instantiate(fallbackPod, player.transform.position - Vector3.up, Quaternion.identity);
        if (pod == null) { Debug.LogError("Space transition requires a ChocolateEggPod."); return false; }
        IsTransitioning = true;
        CarriedCoins = player.Coins;
        var flow = new GameObject("Space Battle Launch").AddComponent<SpaceBattleFlow>();
        flow.StartCoroutine(flow.Launch(boss, pod, player));
        return true;
    }

    private IEnumerator Launch(ChocolateFountainBoss boss, ChocolateEggPod pod, PlayerController player)
    {
        // Freeze combat, but animate with unscaled time. No extra lives are lost
        // to queued projectiles or triggers while the actors leave the arena.
        Time.timeScale = 0f;
        pod.BoardForSequence(player);
        pod.ControlsLocked = true;
        pod.GetComponent<Rigidbody2D>().simulated = false;
        boss.GetComponent<SpriteRenderer>().color = Color.white;
        var camera = Camera.main;
        if (camera != null)
        {
            var follow = camera.GetComponent<CameraFollow>();
            if (follow != null) follow.enabled = false;
        }
        Vector3 bossStart = boss.transform.position;
        Vector3 podStart = pod.transform.position;
        Vector3 cameraStart = camera != null ? camera.transform.position : Vector3.zero;
        float originalSize = camera != null ? camera.orthographicSize : 5f;
        Color originalColor = camera != null ? camera.backgroundColor : Color.black;
        float riseDistance = 40f;
        if (camera != null)
        {
            float lowestPoint = Mathf.Min(boss.GetComponent<SpriteRenderer>().bounds.min.y,
                pod.GetComponent<SpriteRenderer>().bounds.min.y);
            float screenTop = camera.ViewportToWorldPoint(new Vector3(0.5f, 1f,
                Mathf.Abs(bossStart.z - cameraStart.z))).y;
            // Both silhouettes must clear the top before the final fade starts.
            riseDistance = Mathf.Max(riseDistance, screenTop - lowestPoint + 2f);
        }
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            elapsed += Time.unscaledDeltaTime;
            progress = Mathf.Clamp01(elapsed / 5f);
            float flight = Mathf.Clamp01((elapsed - 0.6f) / 3.2f);
            float rise = flight * flight * riseDistance;
            boss.transform.position = bossStart + Vector3.up * rise;
            pod.transform.position = podStart + Vector3.up * rise;
            if (camera != null)
            {
                camera.transform.position = cameraStart;
                camera.orthographicSize = originalSize;
                camera.backgroundColor = Color.Lerp(originalColor, new Color(0.015f, 0.02f, 0.065f), progress);
            }
            yield return null;
        }
        // Keep the fade and combat lock until the scene activation completes.
        var operation = SceneManager.LoadSceneAsync(SceneName);
        if (operation != null)
        {
            loading = true;
            while (!operation.isDone) yield return null;
        }
    }

    private void OnGUI()
    {
        GUI.depth = -100;
        Color saved = GUI.color;
        GUI.color = new Color(0.02f, 0.03f, 0.09f, progress * 0.45f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(0, 0, 0, loading ? 1f : Mathf.InverseLerp(0.84f, 1f, progress));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = saved;
    }

    private void OnDestroy()
    {
        IsTransitioning = false;
        Time.timeScale = 1f;
    }
}
