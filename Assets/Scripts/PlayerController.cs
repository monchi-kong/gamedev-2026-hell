using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private float lowJumpGravityMultiplier = 2f;
    [SerializeField, Tooltip("このワールドY座標より下に落ちると残機を失います")]
    private float fallDeathY = -20f;
    [Header("Wall Kick")]
    [SerializeField, Min(0.1f), Tooltip("壁から離れる横方向の速度")]
    private float wallKickSpeed = 7f;
    [SerializeField, Min(0.1f), Tooltip("通常ジャンプに対する壁キックの高さの速度倍率")]
    private float wallKickJumpMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("壁キック直後に横移動入力を抑える秒数")]
    private float wallKickControlLock = 0.18f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private bool jumpRequested;
    private bool jumpHeld;
    private bool isGrounded;
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[16];
    private float verticalSpeedBeforePhysics;
    private bool brokeBlockThisStep;
    private Vector2 spawnPosition;
    private float protectedUntil;
    private float nextFireTime;
    private int facingDirection = 1;
    private float wallKickLockedUntil;
    private float nextWallKickTime;
    private float wallKickHorizontalSpeed;
    public int Coins { get; private set; }
    public ChocolateEggPod CurrentVehicle { get; private set; }
    public bool IsRiding => CurrentVehicle != null;
    private int normalSortingOrder;
    private int normalSortingLayer;

    public void EnterVehicle(ChocolateEggPod vehicle)
    {
        CurrentVehicle = vehicle;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        moveInput = 0f;
        jumpRequested = jumpHeld = isGrounded = false;
        wallKickLockedUntil = nextWallKickTime = 0f;
        if (spriteRenderer != null)
        {
            normalSortingOrder = spriteRenderer.sortingOrder;
            normalSortingLayer = spriteRenderer.sortingLayerID;
            spriteRenderer.sortingLayerID = vehicle.RiderSortingLayer;
            spriteRenderer.sortingOrder = vehicle.RiderSortingOrder;
        }
        rb.position = vehicle.SeatPosition;
        transform.position = vehicle.SeatPosition;
    }

    public void ExitVehicle(Vector2 position)
    {
        CurrentVehicle = null;
        rb.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = normalSortingOrder;
            spriteRenderer.sortingLayerID = normalSortingLayer;
        }
        jumpRequested = jumpHeld = isGrounded = false;
    }

    public void FaceVehicleDirection(int direction)
    {
        facingDirection = direction;
        if (spriteRenderer != null) spriteRenderer.flipX = direction < 0;
    }

    private void LateUpdate()
    {
        if (!IsRiding) return;
        rb.position = CurrentVehicle.SeatPosition;
        transform.position = CurrentVehicle.SeatPosition;
    }

    public void AddCoin() => Coins++;
    public void RestoreCoins(int count) => Coins = Mathf.Max(0, count);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRun() => GameRunState.Reset();

    private void OnGUI()
    {
        if (SpaceBattleFlow.IsTransitioning) return;
        GUI.Box(new Rect(16, 16, 150, 32), "LIVES: " + GameRunState.Lives + " / 3");
        if (Coins > 0) GUI.Box(new Rect(16, 54, 150, 32), "COINS: " + Coins);
        if (GameRunState.HasFirePower && !IsRiding) GUI.Box(new Rect(16, 92, 190, 32), "FIRE: X / J");
        if (!GameRunState.IsGameOver) return;
        Color previousColor = GUI.color;
        GUI.color = new Color(0.025f, 0.04f, 0.06f, 0.96f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 42, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(centerX - 220, centerY - 130, 440, 70), "GAME OVER", titleStyle);
        var hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(centerX - 200, centerY - 55, 400, 30), "LIVES: 0 / 3", hintStyle);
        if (GUI.Button(new Rect(centerX - 140, centerY, 280, 48), "RETRY  [ENTER]")) RetryGame();
        if (GUI.Button(new Rect(centerX - 140, centerY + 64, 280, 48), "TITLE  [ESC]")) ReturnToTitle();
        GUI.color = previousColor;
    }

    private void RetryGame()
    {
        GameRunState.Reset();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    private void ReturnToTitle()
    {
        if (!Application.CanStreamedLevelBeLoaded("TitleScreen")) return;
        GameRunState.Reset();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnPosition = rb.position;
    }

    private void Update()
    {
        if (ReadableSign.IsReading || SpaceBattleFlow.IsTransitioning)
        {
            moveInput = 0f;
            jumpRequested = false;
            jumpHeld = false;
            return;
        }
        var keyboard = Keyboard.current;
        if (GameRunState.IsGameOver)
        {
            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) RetryGame();
            else if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ReturnToTitle();
            return;
        }
        if (IsRiding) return;
        if (rb.position.y < fallDeathY)
        {
            LoseLife();
            return;
        }
        moveInput = 0f;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) moveInput -= 1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) moveInput += 1f;
            if (keyboard.spaceKey.wasPressedThisFrame) jumpRequested = true;
            jumpHeld = keyboard.spaceKey.isPressed;
        }

        if (spriteRenderer != null && moveInput != 0f && Time.time >= wallKickLockedUntil)
        {
            spriteRenderer.flipX = moveInput < 0f;
        }
        if (moveInput != 0f && Time.time >= wallKickLockedUntil) facingDirection = moveInput < 0f ? -1 : 1;
        if (keyboard != null && (keyboard.xKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame))
            ShootFireball();

    }

    private void ShootFireball()
    {
        if (!GameRunState.HasFirePower || Time.time < nextFireTime) return;
        var prefab = Resources.Load<GameObject>("Fireball");
        if (prefab == null) return;
        // Spawn inside the player's silhouette and ignore the player, avoiding
        // spawning past a wall when standing directly against it.
        Vector3 position = transform.position + Vector3.right * facingDirection * 0.2f;
        var projectile = Instantiate(prefab, position, Quaternion.identity).GetComponent<Fireball>();
        projectile.Launch(this, facingDirection);
        nextFireTime = Time.time + 0.3f;
    }

    private void FixedUpdate()
    {
        if (GameRunState.IsGameOver || IsRiding) return;
        // Only real upward-facing contacts count: overlap queries also see the
        // underside of a one-way platform while the player passes through it.
        var groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;
        int contactCount = rb.GetContacts(groundFilter, groundContacts);
        isGrounded = false;
        int wallKickDirection = 0;
        Vector2 platformVelocity = Vector2.zero;
        for (int i = 0; i < contactCount; i++)
        {
            var contact = groundContacts[i];
            var support = contact.collider.attachedRigidbody == rb ? contact.otherCollider : contact.collider;
            // Only solid terrain side contacts qualify. One-way platforms,
            // ceilings and sloped floors must not create an extra air jump.
            if (Mathf.Abs(contact.normal.x) > 0.85f && Mathf.Abs(contact.normal.y) < 0.35f &&
                !support.usedByEffector && support.GetComponentInParent<IStompableEnemy>() == null)
            {
                int away = contact.normal.x > 0f ? 1 : -1;
                if (wallKickDirection == 0 || moveInput * away < 0f) wallKickDirection = away;
            }
            if (contact.normal.y <= 0.65f) continue;
            var platform = support.GetComponentInParent<MovingPlatform>();
            if (platform != null && !platform.isActiveAndEnabled) platform = null;
            float supportY = platform != null ? platform.PreviousVelocity.y : 0f;
            if (rb.linearVelocity.y - supportY <= 0.1f)
            {
                isGrounded = true;
                if (platform != null) platformVelocity = platform.Velocity;
            }
        }
        if (isGrounded) wallKickLockedUntil = 0f;
        float horizontalSpeed = Time.time < wallKickLockedUntil
            ? wallKickHorizontalSpeed : moveInput * moveSpeed + platformVelocity.x;
        rb.linearVelocity = new Vector2(horizontalSpeed,
            isGrounded ? platformVelocity.y : rb.linearVelocity.y);

        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce + Mathf.Max(0f, platformVelocity.y));
            isGrounded = false;
        }
        else if (jumpRequested && wallKickDirection != 0 && Time.time >= nextWallKickTime)
        {
            wallKickHorizontalSpeed = wallKickDirection * wallKickSpeed;
            rb.linearVelocity = new Vector2(wallKickHorizontalSpeed, jumpForce * wallKickJumpMultiplier);
            wallKickLockedUntil = Time.time + wallKickControlLock;
            nextWallKickTime = Time.time + Mathf.Max(0.12f, wallKickControlLock);
            facingDirection = wallKickDirection;
            if (spriteRenderer != null) spriteRenderer.flipX = wallKickDirection < 0;
        }
        jumpRequested = false;

        if (!isGrounded && rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime);
        }
        else if (!isGrounded && rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime);
        }

        // Collision callbacks see velocity after the solver has stopped the jump.
        verticalSpeedBeforePhysics = rb.linearVelocity.y;
        brokeBlockThisStep = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameRunState.IsGameOver || IsRiding || Time.time < protectedUntil) return;
        if (HandleEnemy(collision)) return;
        TryBreakBlock(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (GameRunState.IsGameOver || IsRiding || Time.time < protectedUntil) return;
        if (HandleEnemy(collision)) return;
        TryBreakBlock(collision);
    }

    private bool HandleEnemy(Collision2D collision)
    {
        var enemy = collision.collider.GetComponentInParent<IStompableEnemy>();
        if (enemy == null) return false;
        if (enemy.IsDefeated) return true;
        // Use pre-solver velocity: after landing the physics engine has already stopped the fall.
        bool onTop = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.65f) onTop = true;
        }
        if (onTop && verticalSpeedBeforePhysics < -0.01f)
        {
            if (enemy.Stomp())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, enemy.BounceSpeed);
                verticalSpeedBeforePhysics = enemy.BounceSpeed;
                isGrounded = false;
                jumpRequested = false;
            }
        }
        else if (Time.time >= protectedUntil)
        {
            LoseLife();
        }
        return true;
    }

    private void LoseLife()
    {
        if (!GameRunState.LoseLife()) return;
        if (IsRiding)
        {
            var vehicle = CurrentVehicle;
            ExitVehicle(spawnPosition);
            vehicle.ReleaseForRespawn();
        }
        wallKickLockedUntil = 0f;
        nextWallKickTime = 0f;
        wallKickHorizontalSpeed = 0f;
        rb.linearVelocity = Vector2.zero;
        verticalSpeedBeforePhysics = 0f;
        moveInput = 0f;
        jumpRequested = false;
        isGrounded = false;
        if (GameRunState.IsGameOver)
        {
            rb.simulated = false;
            Time.timeScale = 0f;
            return;
        }
        rb.position = spawnPosition;
        transform.position = new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z);
        protectedUntil = Time.time + 1f;
    }

    public void TakeEnemyDamage()
    {
        if (SpaceBattleFlow.IsTransitioning || GameRunState.IsGameOver || Time.time < protectedUntil) return;
        LoseLife();
    }

    private void TryBreakBlock(Collision2D collision)
    {
        if (verticalSpeedBeforePhysics <= 0f || brokeBlockThisStep) return;
        var itemBlock = collision.collider.GetComponent<ItemBlock>();
        if (itemBlock != null)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > -0.75f) continue;
                if (!itemBlock.TryHit(this)) return;
                brokeBlockThisStep = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(0f, rb.linearVelocity.y));
                return;
            }
            return;
        }
        var map = collision.collider.GetComponent<Tilemap>();
        if (map == null) return;

        BreakableTile selectedTile = null;
        Vector3Int selectedCell = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            // The underside pushes the player down; side and floor contacts do not qualify.
            if (contact.normal.y > -0.75f) continue;
            // Move just inside the struck surface to avoid rounding into the empty cell below it.
            Vector3Int cell = map.WorldToCell(contact.point - contact.normal * 0.02f);
            if (!(map.GetTile(cell) is BreakableTile tile)) continue;
            float distance = Mathf.Abs(map.GetCellCenterWorld(cell).x - rb.worldCenterOfMass.x);
            if (distance >= nearest) continue;
            nearest = distance;
            selectedTile = tile;
            selectedCell = cell;
        }

        if (selectedTile == null || !selectedTile.Break(map, selectedCell)) return;
        brokeBlockThisStep = true;
        // End upward motion after one block, rather than tunnelling through a column.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(0f, rb.linearVelocity.y));
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
