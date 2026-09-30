using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class ChocolateFountainBoss : MonoBehaviour, IStompableEnemy
{
    [Header("Boss")]
    [SerializeField] private Transform target;
    [SerializeField, Min(1)] private int maxHealth = 12;
    [SerializeField, Min(1f)] private float detectionRange = 15f;
    [SerializeField, Min(0f)] private float damageInvulnerability = 0.35f;
    [SerializeField] private float bounceSpeed = 16f;
    [Header("Hover")]
    [SerializeField, Min(0f)] private float horizontalRange = 3f;
    [SerializeField, Min(0f)] private float hoverSpeed = 0.65f;
    [SerializeField, Min(0f)] private float bobHeight = 0.35f;
    [Header("Chocolate Fan")]
    [SerializeField] private ChocolateShot chocolatePrefab;
    [SerializeField, Min(0.3f)] private float attackInterval = 2.4f;
    [SerializeField, Min(0.1f)] private float warningTime = 0.6f;
    [SerializeField, Range(2, 15)] private int normalShotCount = 5;
    [SerializeField, Range(2, 15)] private int enragedShotCount = 9;
    [SerializeField, Range(10f, 160f)] private float fanAngle = 70f;
    [SerializeField] private UnityEvent onDefeated = new UnityEvent();
    [Header("Second Round")]
    [SerializeField] private bool launchSpaceRound = true;
    [SerializeField] private ChocolateEggPod escapePodPrefab;
    public event System.Action FinalDefeated;

    public void ConfigureSpaceRound(Transform player)
    {
        launchSpaceRound = false;
        target = player;
        maxHealth = 18;
        health = new FountainBossHealth(maxHealth);
        horizontalRange = 2f;
        hoverSpeed = 0.8f;
        bobHeight = 1f;
        normalShotCount = 7;
        enragedShotCount = 11;
        attackInterval = 1.8f;
        origin = body.position;
        Activate();
        nextAttackTime = Time.time + 3f;
    }

    private Rigidbody2D body;
    private SpriteRenderer visual;
    private Collider2D[] colliders;
    private Color baseColor;
    private Vector2 origin;
    private FountainBossHealth health;
    private readonly List<ChocolateShot> shots = new List<ChocolateShot>();
    private bool activated;
    private float motionTime;
    private float nextAttackTime;
    private float flashUntil;
    public bool IsDefeated => health != null && health.Defeated;
    public float BounceSpeed => bounceSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        visual = GetComponent<SpriteRenderer>();
        baseColor = visual.color;
        colliders = GetComponentsInChildren<Collider2D>();
        health = new FountainBossHealth(maxHealth);
        origin = body.position;
        if (chocolatePrefab == null)
        {
            Debug.LogError("ChocolateFountainBoss: Chocolate Prefab を指定してください。", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (target != null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }

    private void Activate()
    {
        if (activated) return;
        activated = true;
        nextAttackTime = Time.time + Mathf.Max(attackInterval, warningTime + 0.1f);
    }

    private void FixedUpdate()
    {
        if (IsDefeated || GameRunState.IsGameOver || target == null) return;
        if (!activated)
        {
            if (Vector2.Distance(body.position, target.position) > detectionRange) return;
            Activate();
        }
        motionTime += Time.fixedDeltaTime * hoverSpeed * (health.Enraged ? 1.35f : 1f);
        body.MovePosition(origin + new Vector2(Mathf.Sin(motionTime) * horizontalRange,
            Mathf.Sin(motionTime * 2f) * bobHeight));
        bool warning = Time.time >= nextAttackTime - warningTime;
        if (Time.time < flashUntil) visual.color = new Color(1f, 0.3f, 0.3f, baseColor.a);
        else if (warning) visual.color = Color.Lerp(baseColor, new Color(1f, 0.5f, 0.15f),
            0.3f + 0.25f * Mathf.Sin(Time.time * 24f));
        else visual.color = baseColor;
        if (Time.time < nextAttackTime) return;
        FireFan();
        float interval = attackInterval * (health.Enraged ? 0.6f : 1f);
        nextAttackTime = Time.time + Mathf.Max(warningTime + 0.15f, interval);
    }

    private void FireFan()
    {
        shots.RemoveAll(shot => shot == null);
        // Start inside the lower saucer; owner collisions are ignored, so shots
        // cannot skip a nearby wall by spawning on its far side.
        Vector2 muzzle = body.position + Vector2.down * 1.5f;
        Vector2 aim = (Vector2)target.position - muzzle;
        if (aim.sqrMagnitude < 0.01f) aim = Vector2.down;
        aim.Normalize();
        int count = Mathf.Clamp(health.Enraged ? enragedShotCount : normalShotCount, 2, 15);
        float angle = Mathf.Clamp(fanAngle, 10f, 160f);
        for (int i = 0; i < count; i++)
        {
            float offset = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, (float)i / (count - 1));
            Vector2 direction = Quaternion.Euler(0, 0, offset) * aim;
            var shot = Instantiate(chocolatePrefab, muzzle, Quaternion.identity);
            shot.Launch(this, direction);
            shots.Add(shot);
        }
    }

    public bool Stomp()
    {
        if (IsDefeated || !enabled) return false;
        Activate();
        if (health.TryDamage(Time.time, damageInvulnerability))
        {
            flashUntil = Time.time + 0.15f;
            if (IsDefeated) StartCoroutine(Defeat());
        }
        // Still bounce the player during the brief hit cooldown.
        return true;
    }

    private IEnumerator Defeat()
    {
        foreach (var collider in colliders) collider.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        foreach (var shot in shots)
        {
            if (shot == null) continue;
            shot.gameObject.SetActive(false);
            Destroy(shot.gameObject);
        }
        shots.Clear();
        if (launchSpaceRound && SpaceBattleFlow.Begin(this, escapePodPrefab)) yield break;
        onDefeated.Invoke();
        FinalDefeated?.Invoke();
        float elapsed = 0f;
        while (elapsed < 2.5f)
        {
            elapsed += Time.deltaTime;
            visual.color = new Color(1f, 0.7f, 0.4f, Mathf.Clamp01(1f - elapsed / 2.5f));
            transform.position += Vector3.down * Time.deltaTime * 0.4f;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnGUI()
    {
        if (SpaceBattleFlow.IsTransitioning || !activated || health == null || GameRunState.IsGameOver || ReadableSign.IsReading) return;
        float width = Mathf.Min(520f, Screen.width - 40f);
        float x = (Screen.width - width) * 0.5f;
        var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
        labelStyle.normal.textColor = Color.white;
        GUI.Box(new Rect(x - 8, 12, width + 16, 75), GUIContent.none);
        GUI.Label(new Rect(x, 16, width, 32), IsDefeated ? "BOSS DEFEATED!" : "CHOCOLATE FOUNTAIN UFO", labelStyle);
        Color saved = GUI.color;
        GUI.color = new Color(0.12f, 0.06f, 0.04f);
        GUI.DrawTexture(new Rect(x, 54, width, 20), Texture2D.whiteTexture);
        GUI.color = health.Enraged ? new Color(1f, 0.25f, 0.1f) : new Color(0.95f, 0.65f, 0.2f);
        GUI.DrawTexture(new Rect(x, 54, width * health.Current / health.Maximum, 20), Texture2D.whiteTexture);
        GUI.color = saved;
        GUI.Label(new Rect(x, 51, width, 26), health.Current + " / " + health.Maximum,
            new GUIStyle(labelStyle) { fontSize = 16 });
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)origin : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + Vector3.left * horizontalRange, center + Vector3.right * horizontalRange);
    }
}
