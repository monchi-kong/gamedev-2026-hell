using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class ItemBlock : MonoBehaviour
{
    [SerializeField, Tooltip("空欄ならコインを1枚獲得。指定するとそのPrefabを出します")]
    private GameObject itemPrefab;
    [SerializeField, Min(0f), Tooltip("ブロック上面から出現位置までの高さ")]
    private float spawnHeight = 0.5f;
    [SerializeField] private Color usedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    private bool used;

    public bool TryHit(PlayerController player)
    {
        if (used || player == null || !isActiveAndEnabled) return false;
        used = true;
        var bounds = GetComponent<BoxCollider2D>().bounds;
        Vector3 position = new Vector3(bounds.center.x, bounds.max.y + spawnHeight, transform.position.z);
        if (itemPrefab != null)
        {
            Instantiate(itemPrefab, position, Quaternion.identity);
        }
        else
        {
            CoinReward.Show(position, GetComponent<SpriteRenderer>());
            player.AddCoin();
        }
        GetComponent<SpriteRenderer>().color = usedColor;
        return true;
    }
}
