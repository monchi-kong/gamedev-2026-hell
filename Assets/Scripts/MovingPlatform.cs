using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField, Tooltip("配置位置からの移動量（ワールド座標）。X: 横、Y: 縦")]
    private Vector2 travelOffset = new Vector2(4f, 0f);
    [SerializeField, Min(0f), Tooltip("移動速度（単位/秒）")]
    private float speed = 2f;

    private Rigidbody2D body;
    private Vector2 startPosition;
    private float distanceTravelled;
    public Vector2 Velocity { get; private set; }
    public Vector2 PreviousVelocity { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        startPosition = body.position;
        distanceTravelled = 0f;
        Velocity = PreviousVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        PreviousVelocity = Velocity;
        float distance = travelOffset.magnitude;
        if (distance < 0.001f || speed <= 0f)
        {
            Velocity = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            return;
        }
        distanceTravelled = Mathf.Repeat(distanceTravelled + speed * Time.fixedDeltaTime, distance * 2f);
        Vector2 destination = startPosition + travelOffset * (Mathf.PingPong(distanceTravelled, distance) / distance);
        Velocity = (destination - body.position) / Time.fixedDeltaTime;
        body.MovePosition(destination);
    }

    private void OnDisable()
    {
        Velocity = PreviousVelocity = Vector2.zero;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + (Vector3)travelOffset);
        Gizmos.DrawWireSphere(origin + (Vector3)travelOffset, 0.15f);
    }
}
