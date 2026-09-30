using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class WalkingEnemy : MonoBehaviour, IStompableEnemy
{
    [SerializeField] private float speed = 1.2f;
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float stopDistance = 0.1f;
    [SerializeField] private float bounceSpeed = 11f;
    [SerializeField] private LayerMask groundLayer = 1 << 6;
    [SerializeField, Min(0.05f), Tooltip("この高さより深い段差を穴として扱います")]
    private float maxStepDown = 0.3f;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private int direction = -1;
    public bool IsDefeated { get; private set; }
    public float BounceSpeed => bounceSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        body.freezeRotation = true;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    private void FixedUpdate()
    {
        if (IsDefeated) return;
        if (target == null)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        float distance = target.position.x - body.position.x;
        float horizontalSpeed = 0f;
        if (Mathf.Abs(distance) > stopDistance)
        {
            direction = distance > 0f ? 1 : -1;
            horizontalSpeed = direction * Mathf.Min(Mathf.Max(0f, speed),
                (Mathf.Abs(distance) - stopDistance) / Time.fixedDeltaTime);
            visual.flipX = direction > 0;
        }
        // Probe beyond the next step, so even fast enemies stop before an edge.
        if (horizontalSpeed != 0f && !HasGroundAhead(horizontalSpeed)) horizontalSpeed = 0f;
        body.linearVelocity = new Vector2(horizontalSpeed, body.linearVelocity.y);
    }

    private bool HasGroundAhead(float horizontalSpeed)
    {
        Bounds bounds = hitbox.bounds;
        float sign = Mathf.Sign(horizontalSpeed);
        const float probeWidth = 0.16f;
        const float lift = 0.15f;
        float reach = bounds.extents.x + Mathf.Abs(horizontalSpeed) * Time.fixedDeltaTime + 0.05f;
        Vector2 origin = new Vector2(bounds.center.x + sign * reach, bounds.min.y + lift);
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;
        // A small box bridges tiny tile seams without accepting distant ground
        // at the bottom of a pit. Walls and other enemies are not support.
        int count = Physics2D.BoxCast(origin, new Vector2(probeWidth, 0.02f), 0f,
            Vector2.down, filter, groundHits, lift + Mathf.Max(0.05f, maxStepDown));
        for (int i = 0; i < count; i++)
        {
            var hit = groundHits[i];
            if (hit.collider == hitbox || hit.rigidbody == body) continue;
            if (hit.collider.GetComponentInParent<IStompableEnemy>() != null) continue;
            if (hit.fraction > 0f && hit.normal.y > 0.65f) return true;
        }
        return false;
    }

    public bool Stomp()
    {
        if (IsDefeated) return false;
        IsDefeated = true;
        hitbox.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(scale.x * 1.15f, scale.y * 0.25f, scale.z);
        transform.position += Vector3.down * 0.45f;
        Destroy(gameObject, 0.2f);
        return true;
    }
}
