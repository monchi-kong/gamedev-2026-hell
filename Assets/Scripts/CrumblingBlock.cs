using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class CrumblingBlock : MonoBehaviour
{
    [SerializeField, Min(0.05f), Tooltip("崩れるまでの秒数")]
    private float crumbleDelay = 1.5f;
    [SerializeField, Tooltip("ON: ゲーム開始時から計測。OFF: プレイヤーが乗ってから計測")]
    private bool startOnEnable;

    private SpriteRenderer blockRenderer;
    private BoxCollider2D blockCollider;
    private Color originalColor;
    private float elapsed;
    private bool countingDown;
    private bool crumbled;

    private void Awake()
    {
        blockRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<BoxCollider2D>();
        originalColor = blockRenderer.color;
    }

    private void OnEnable()
    {
        elapsed = 0f;
        crumbled = false;
        countingDown = startOnEnable;
        blockRenderer.color = originalColor;
        blockRenderer.enabled = true;
        blockCollider.enabled = true;
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryStartCountdown(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryStartCountdown(collision);

    private void TryStartCountdown(Collision2D collision)
    {
        if (countingDown || crumbled) return;
        if (collision.rigidbody == null || !collision.rigidbody.CompareTag("Player")) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            // On the block, a player standing on top produces a downward normal.
            // Touching the sides or hitting the underside must not start the timer.
            if (collision.GetContact(i).normal.y >= -0.65f) continue;
            countingDown = true;
            break;
        }
    }

    private void Update()
    {
        if (!countingDown || crumbled) return;
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, crumbleDelay));
        Color warningColor = new Color(1f, 0.3f, 0.15f, originalColor.a);
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 24f);
        blockRenderer.color = Color.Lerp(originalColor, warningColor, progress * pulse);

        if (progress < 1f) return;
        crumbled = true;
        // Disable collision immediately so the player falls on the next physics step.
        blockCollider.enabled = false;
        blockRenderer.enabled = false;
        Destroy(gameObject);
    }
}
