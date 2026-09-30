using System;

// Kept independent of physics so ammunition and phase transitions can be tested.
public sealed class ToppoAttackCycle
{
    public enum AttackPhase { Waiting, Shooting, Windup, Charging }
    public AttackPhase Phase { get; private set; }
    public int RemainingShots { get; private set; }
    private readonly float shotInterval;
    private readonly float chargeDelay;
    private float timer;

    public ToppoAttackCycle(int shots, float interval, float delay)
    {
        RemainingShots = Math.Max(1, shots);
        shotInterval = Math.Max(0.1f, interval);
        chargeDelay = Math.Max(0.1f, delay);
    }

    // Returns true exactly once for each shot. No reload after the magazine empties.
    public bool Tick(float deltaTime, bool playerInRange)
    {
        if (Phase == AttackPhase.Waiting)
        {
            if (!playerInRange) return false;
            Phase = AttackPhase.Shooting;
            timer = shotInterval;
            return false;
        }
        if (Phase == AttackPhase.Charging) return false;
        timer -= Math.Max(0f, deltaTime);
        if (timer > 0f) return false;
        if (Phase == AttackPhase.Windup)
        {
            Phase = AttackPhase.Charging;
            return false;
        }
        RemainingShots--;
        if (RemainingShots == 0)
        {
            Phase = AttackPhase.Windup;
            timer = chargeDelay;
        }
        else timer = shotInterval;
        return true;
    }
}
