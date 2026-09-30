public interface IStompableEnemy
{
    bool IsDefeated { get; }
    float BounceSpeed { get; }
    bool Stomp();
}
