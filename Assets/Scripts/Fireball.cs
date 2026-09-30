using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Fireball : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float bounceSpeed = 5f;
    [SerializeField] private float lifetime = 3f;
    private Rigidbody2D body;
    private int direction;
    private bool spent;

    public void Launch(PlayerController owner, int facing)
    {
        direction = facing;
        body = GetComponent<Rigidbody2D>();
        var hitbox = GetComponent<Collider2D>();
        foreach (var collider in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(hitbox, collider);
        foreach (var other in FindObjectsByType<Fireball>(FindObjectsSortMode.None))
            if (other != this) Physics2D.IgnoreCollision(hitbox, other.GetComponent<Collider2D>());
        body.linearVelocity = new Vector2(direction * speed, -1f);
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (spent || body == null) return;
        body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (spent) return;
        var enemy = collision.collider.GetComponentInParent<IStompableEnemy>();
        if (enemy != null)
        {
            enemy.Stomp();
            Finish();
            return;
        }
        bool floor = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.65f) floor = true;
            else { Finish(); return; }
        }
        if (floor) body.linearVelocity = new Vector2(direction * speed, bounceSpeed);
    }

    private void Finish()
    {
        spent = true;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject);
    }
}
