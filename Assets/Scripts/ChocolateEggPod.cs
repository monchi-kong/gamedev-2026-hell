using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class ChocolateEggPod : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float boardDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float flightSpeed = 5f;
    [SerializeField, Min(0.1f)] private float shotInterval = 0.3f;
    [SerializeField] private PodChocolateShot shotPrefab;
    [SerializeField] private Vector2 seatOffset = new Vector2(0f, 0.9f);
    [SerializeField] private float fallDeathY = -20f;
    private static readonly List<ChocolateEggPod> pods = new List<ChocolateEggPod>();
    private static int interactionFrame = -1;
    private Rigidbody2D body;
    private BoxCollider2D hull;
    private BoxCollider2D riderClearance;
    private SpriteRenderer visual;
    private PlayerController player;
    private Vector2 home;
    private Vector2 movement;
    private Vector2 riderSize;
    private Vector2 riderCenterOffset;
    private readonly Collider2D[] obstacles = new Collider2D[32];
    private readonly RaycastHit2D[] pathHits = new RaycastHit2D[32];
    private int facing = 1;
    private float nextShotTime;
    private float messageUntil;
    public bool ControlsLocked { get; set; }
    private bool spaceMode;
    private Rect flightBounds;
    public PlayerController Rider { get; private set; }
    public Vector3 SeatPosition => transform.TransformPoint(seatOffset);
    public int RiderSortingOrder => visual.sortingOrder - 1;
    public int RiderSortingLayer => visual.sortingLayerID;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { pods.Clear(); interactionFrame = -1; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        hull = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        home = body.position;
        // Protect the exposed rider's head from clipping through ceilings.
        riderClearance = gameObject.AddComponent<BoxCollider2D>();
        riderClearance.enabled = false;
    }

    private void OnEnable() => pods.Add(this);
    private void Start() => player = FindFirstObjectByType<PlayerController>();

    private bool IsNearest()
    {
        if (player == null || player.IsRiding) return false;
        ChocolateEggPod nearest = null;
        float best = float.PositiveInfinity;
        foreach (var pod in pods)
        {
            if (pod.Rider != null) continue;
            float distance = Vector2.Distance(player.transform.position, pod.transform.position);
            if (distance > pod.boardDistance || distance >= best) continue;
            best = distance;
            nearest = pod;
        }
        return nearest == this;
    }

    private void Update()
    {
        movement = Vector2.zero;
        if (ControlsLocked || SpaceBattleFlow.IsTransitioning || GameRunState.IsGameOver || ReadableSign.IsReading || Time.timeScale <= 0f) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (Rider != null)
        {
            if (body.position.y < fallDeathY) { Rider.TakeEnemyDamage(); return; }
            if (!spaceMode && keyboard.eKey.wasPressedThisFrame && interactionFrame != Time.frameCount)
            {
                interactionFrame = Time.frameCount;
                TryExit();
                return;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x--;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x++;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y++;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y--;
            movement = Vector2.ClampMagnitude(movement, 1f);
            if (movement.x != 0f) facing = movement.x > 0f ? 1 : -1;
            Rider.FaceVehicleDirection(facing);
            if ((keyboard.xKey.isPressed || keyboard.jKey.isPressed) && Time.time >= nextShotTime && shotPrefab != null)
            {
                var shot = Instantiate(shotPrefab, body.position + Vector2.right * facing * 0.6f, Quaternion.identity);
                shot.Launch(this, Rider, facing);
                nextShotTime = Time.time + Mathf.Max(0.1f, shotInterval);
            }
        }
        else if (keyboard.eKey.wasPressedThisFrame && interactionFrame != Time.frameCount && IsNearest())
        {
            interactionFrame = Time.frameCount;
            var playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider == null) return;
            riderSize = playerCollider.bounds.size;
            riderCenterOffset = playerCollider.bounds.center - player.transform.position;
            if (!SpaceAvailable(SeatPosition) || !PathAvailable(player.transform.position, SeatPosition))
            { messageUntil = Time.unscaledTime + 2f; return; }
            BoardForSequence(player);
        }
    }

    public void BoardForSequence(PlayerController passenger)
    {
        if (Rider == passenger) return;
        player = passenger;
        var playerCollider = passenger.GetComponent<Collider2D>();
        riderSize = playerCollider.bounds.size;
        riderCenterOffset = playerCollider.bounds.center - passenger.transform.position;
        Rider = passenger;
        riderClearance.size = new Vector2(riderSize.x / Mathf.Abs(transform.lossyScale.x), riderSize.y / Mathf.Abs(transform.lossyScale.y));
        riderClearance.offset = transform.InverseTransformPoint(SeatPosition + (Vector3)riderCenterOffset);
        riderClearance.enabled = true;
        passenger.EnterVehicle(this);
    }

    public void ConfigureSpace(Rect bounds)
    {
        spaceMode = true;
        flightBounds = bounds;
        home = body.position;
    }

    private void FixedUpdate()
    {
        Vector2 velocity = Rider != null && !ControlsLocked && !GameRunState.IsGameOver ? movement * flightSpeed : Vector2.zero;
        if (spaceMode)
        {
            Vector2 next = body.position + velocity * Time.fixedDeltaTime;
            next.x = Mathf.Clamp(next.x, flightBounds.xMin, flightBounds.xMax);
            next.y = Mathf.Clamp(next.y, flightBounds.yMin, flightBounds.yMax);
            velocity = (next - body.position) / Time.fixedDeltaTime;
        }
        body.linearVelocity = velocity;
    }

    private bool SpaceAvailable(Vector2 position)
    {
        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int count = Physics2D.OverlapBox(position + riderCenterOffset, riderSize + Vector2.one * 0.06f, 0f, filter, obstacles);
        if (count == obstacles.Length) return false;
        for (int i = 0; i < count; i++)
        {
            if (obstacles[i].attachedRigidbody == body) continue;
            if (obstacles[i].GetComponentInParent<PlayerController>() == player) continue;
            return false;
        }
        return true;
    }

    private bool PathAvailable(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < 0.001f) return true;
        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int count = Physics2D.BoxCast(from + riderCenterOffset, riderSize, 0f,
            delta.normalized, filter, pathHits, delta.magnitude);
        if (count == pathHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var collider = pathHits[i].collider;
            if (collider.attachedRigidbody == body || collider.GetComponentInParent<PlayerController>() == player) continue;
            // Initial floor contact is harmless when moving away from it.
            if (pathHits[i].distance == 0f && Vector2.Dot(delta, (Vector2)collider.bounds.center - from) <= 0f) continue;
            return false;
        }
        return true;
    }

    private void TryExit()
    {
        Bounds bounds = hull.bounds;
        Vector2 above = new Vector2(bounds.center.x, bounds.max.y + riderSize.y * 0.5f + 0.15f) - riderCenterOffset;
        Vector2 side = new Vector2(bounds.extents.x + riderSize.x * 0.5f + 0.15f, 0f);
        Vector2[] candidates = { above, (Vector2)SeatPosition + side * facing, (Vector2)SeatPosition - side * facing };
        foreach (var position in candidates)
        {
            if (!SpaceAvailable(position) || !PathAvailable(SeatPosition, position)) continue;
            var previousRider = Rider;
            Rider = null;
            riderClearance.enabled = false;
            movement = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            previousRider.ExitVehicle(position);
            return;
        }
        messageUntil = Time.unscaledTime + 2f;
    }

    public void ReleaseForRespawn()
    {
        Rider = null;
        riderClearance.enabled = false;
        movement = Vector2.zero;
        body.linearVelocity = Vector2.zero;
        body.position = home;
        transform.position = new Vector3(home.x, home.y, transform.position.z);
        if (spaceMode && player != null && !GameRunState.IsGameOver) BoardForSequence(player);
    }

    private void OnCollisionEnter2D(Collision2D collision) => TouchEnemy(collision);
    private void OnCollisionStay2D(Collision2D collision) => TouchEnemy(collision);
    private void TouchEnemy(Collision2D collision)
    {
        var enemy = collision.collider.GetComponentInParent<IStompableEnemy>();
        if (Rider != null && enemy != null && !enemy.IsDefeated) Rider.TakeEnemyDamage();
    }

    private void OnDisable()
    {
        pods.Remove(this);
        if (Rider != null)
        {
            var previousRider = Rider;
            Rider = null;
            previousRider.ExitVehicle(SeatPosition + Vector3.up * 2f);
        }
        if (riderClearance != null) riderClearance.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void OnGUI()
    {
        if (ControlsLocked || SpaceBattleFlow.IsTransitioning || GameRunState.IsGameOver || ReadableSign.IsReading) return;
        string hint = Rider != null ? "WASD / ARROWS: FLY    X / J: SHOOT    E: EXIT" : IsNearest() ? "E: BOARD CHOCOLATE EGG" : null;
        if (Rider != null && spaceMode) hint = "ROUND 2    WASD / ARROWS: FLY    X / J: SHOOT";
        if (Time.unscaledTime < messageUntil) hint = "NOT ENOUGH SPACE - MOVE TO AN OPEN AREA";
        if (hint != null) GUI.Box(new Rect((Screen.width - 540) * 0.5f, Screen.height - 62, 540, 38), hint);
    }
}
