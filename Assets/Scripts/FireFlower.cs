using UnityEngine;

public class FireFlower : MonoBehaviour
{
    private bool collected;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || GameRunState.IsGameOver) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<ChocolateEggPod>()?.Rider;
        if (player == null) return;
        collected = true;
        GameRunState.HasFirePower = true;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject);
    }
}
