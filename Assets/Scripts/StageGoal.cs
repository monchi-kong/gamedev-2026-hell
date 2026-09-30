using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class StageGoal : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private string playerTag = "Player";
    private bool loading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (SpaceBattleFlow.IsTransitioning || GameRunState.IsGameOver || loading) return;
        var rider = other.GetComponentInParent<ChocolateEggPod>()?.Rider;
        if (!other.CompareTag(playerTag) && (rider == null || !rider.CompareTag(playerTag))) return;
        if (string.IsNullOrEmpty(nextSceneName)) return;

        loading = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
