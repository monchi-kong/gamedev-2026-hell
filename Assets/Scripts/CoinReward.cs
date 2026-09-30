using UnityEngine;

// A coin pops out and is credited immediately, like a classic coin block.
public class CoinReward : MonoBehaviour
{
    private Vector3 origin;
    private float elapsed;
    private SpriteRenderer coinRenderer;
    private Texture2D texture;
    private Sprite sprite;

    public static void Show(Vector3 position, SpriteRenderer blockRenderer)
    {
        var obj = new GameObject("CoinReward");
        obj.transform.position = position;
        var reward = obj.AddComponent<CoinReward>();
        reward.origin = position;
        reward.coinRenderer = obj.AddComponent<SpriteRenderer>();
        reward.coinRenderer.sortingLayerID = blockRenderer.sortingLayerID;
        reward.coinRenderer.sortingOrder = blockRenderer.sortingOrder + 1;
        reward.texture = new Texture2D(16, 16);
        reward.texture.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            float radius = Mathf.Pow((x - 7.5f) / 5.5f, 2f) + Mathf.Pow((y - 7.5f) / 7f, 2f);
            Color color = radius > 1f ? Color.clear : radius > 0.65f
                ? new Color(0.8f, 0.4f, 0.05f) : new Color(1f, 0.85f, 0.15f);
            if (radius < 0.65f && x == 7 && y > 3 && y < 12) color = new Color(1f, 1f, 0.7f);
            reward.texture.SetPixel(x, y, color);
        }
        reward.texture.Apply();
        reward.sprite = Sprite.Create(reward.texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 24f);
        reward.coinRenderer.sprite = reward.sprite;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / 0.65f);
        transform.position = origin + Vector3.up * (4f * progress * (1f - progress));
        coinRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(0.65f, 1f, progress));
        if (progress >= 1f) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }
}
