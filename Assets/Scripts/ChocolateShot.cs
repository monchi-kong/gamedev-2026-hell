using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ChocolateShot : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 6f;
    [SerializeField, Min(0.1f)] private float lifetime = 4f;
    private bool spent;

    public void Launch(Component owner, Vector2 direction)
    {
        var hitbox = GetComponent<Collider2D>();
        foreach (var collider in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(hitbox, collider);
        foreach (var shot in FindObjectsByType<ChocolateShot>(FindObjectsSortMode.None))
            if (shot != this) Physics2D.IgnoreCollision(hitbox, shot.GetComponent<Collider2D>());
        var body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = direction.normalized * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (spent) return;
        spent = true;
        var player = collision.collider.GetComponentInParent<PlayerController>();
        if (player == null) player = collision.collider.GetComponentInParent<ChocolateEggPod>()?.Rider;
        if (player != null) player.TakeEnemyDamage();
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;
        Destroy(gameObject);
    }
}
