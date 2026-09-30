using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class ToppoEnemy : MonoBehaviour, IStompableEnemy
{
    [SerializeField] private Transform target;
    [SerializeField] private ChocolateShot chocolatePrefab;
    [SerializeField, Min(0.5f)] private float detectionRange = 8f;
    [SerializeField, Min(1)] private int ammunition = 5;
    [SerializeField, Min(0.1f)] private float shotInterval = 0.65f;
    [SerializeField, Min(0.1f)] private float chargeDelay = 0.6f;
    [SerializeField, Min(0f)] private float chargeSpeed = 4.5f;
    [SerializeField] private float bounceSpeed = 11f;
    [SerializeField] private LayerMask groundLayer = 1 << 6;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private Color originalColor;
    private ToppoAttackCycle cycle;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
    public bool IsDefeated { get; private set; }
    public float BounceSpeed => bounceSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        originalColor = visual.color;
        body.freezeRotation = true;
        cycle = new ToppoAttackCycle(ammunition, shotInterval, chargeDelay);
        if (chocolatePrefab == null)
        {
            Debug.LogError("ToppoEnemy: Chocolate Prefab を指定してください。", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (target != null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }

    private void FixedUpdate()
    {
        if (IsDefeated || GameRunState.IsGameOver) return;
        if (target == null) { body.linearVelocity = new Vector2(0f, body.linearVelocity.y); return; }
        float dx = target.position.x - body.position.x;
        int direction = dx >= 0f ? 1 : -1;
        visual.flipX = direction < 0;
        bool inRange = Vector2.Distance(target.position, body.position) <= detectionRange;
        if (cycle.Tick(Time.fixedDeltaTime, inRange))
        {
            // Spawn inside the shell to avoid skipping through an adjacent wall.
            Vector2 muzzle = new Vector2(hitbox.bounds.center.x, hitbox.bounds.max.y - 0.3f);
            var shot = Instantiate(chocolatePrefab, muzzle, Quaternion.identity);
            Vector2 aim = (Vector2)target.position - muzzle;
            shot.Launch(this, aim.sqrMagnitude > 0.001f ? aim.normalized : Vector2.right * direction);
        }
        float horizontal = 0f;
        if (cycle.Phase == ToppoAttackCycle.AttackPhase.Charging)
        {
            horizontal = direction * Mathf.Min(Mathf.Max(0f, chargeSpeed), Mathf.Abs(dx) / Time.fixedDeltaTime);
            if (!HasGroundAhead(horizontal)) horizontal = 0f;
            visual.color = new Color(1f, 0.65f, 0.5f, originalColor.a);
        }
        else if (cycle.Phase == ToppoAttackCycle.AttackPhase.Windup)
        {
            visual.color = Color.Lerp(originalColor, Color.red, 0.35f + 0.25f * Mathf.Sin(Time.time * 30f));
        }
        body.linearVelocity = new Vector2(horizontal, body.linearVelocity.y);
    }

    private bool HasGroundAhead(float horizontal)
    {
        if (Mathf.Abs(horizontal) < 0.001f) return false;
        Bounds bounds = hitbox.bounds;
        float reach = bounds.extents.x + Mathf.Abs(horizontal) * Time.fixedDeltaTime + 0.05f;
        Vector2 origin = new Vector2(bounds.center.x + Mathf.Sign(horizontal) * reach, bounds.min.y + 0.15f);
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;
        int count = Physics2D.BoxCast(origin, new Vector2(0.16f, 0.02f), 0f, Vector2.down, filter, groundHits, 0.45f);
        for (int i = 0; i < count; i++)
        {
            var hit = groundHits[i];
            if (hit.rigidbody == body || hit.collider.GetComponentInParent<IStompableEnemy>() != null) continue;
            if (hit.fraction > 0f && hit.normal.y > 0.65f) return true;
        }
        return false;
    }

    public bool Stomp()
    {
        if (IsDefeated) return false;
        IsDefeated = true;
        float bottom = hitbox.bounds.min.y;
        hitbox.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        var scale = transform.localScale;
        transform.localScale = new Vector3(scale.x * 1.2f, scale.y * 0.2f, scale.z);
        transform.position += Vector3.up * (bottom - visual.bounds.min.y);
        Destroy(gameObject, 0.2f);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
