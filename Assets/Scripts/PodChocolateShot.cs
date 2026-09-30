using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PodChocolateShot : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 10f;
    [SerializeField, Min(0.1f)] private float lifetime = 3f;
    private bool spent;

    public void Launch(ChocolateEggPod pod, PlayerController rider, int facing)
    {
        var hitbox = GetComponent<Collider2D>();
        foreach (var collider in pod.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(hitbox, collider);
        foreach (var collider in rider.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(hitbox, collider);
        foreach (var other in FindObjectsByType<PodChocolateShot>(FindObjectsSortMode.None))
            if (other != this) Physics2D.IgnoreCollision(hitbox, other.GetComponent<Collider2D>());
        var body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = Vector2.right * facing * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (spent) return;
        spent = true;
        collision.collider.GetComponentInParent<IStompableEnemy>()?.Stomp();
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;
        Destroy(gameObject);
    }
}
