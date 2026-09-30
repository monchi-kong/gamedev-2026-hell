using System;

public sealed class FountainBossHealth
{
    public int Maximum { get; }
    public int Current { get; private set; }
    public bool Defeated => Current == 0;
    public bool Enraged => !Defeated && Current <= Maximum / 2;
    private float nextDamageTime = float.NegativeInfinity;

    public FountainBossHealth(int maximum)
    {
        Maximum = Math.Max(1, maximum);
        Current = Maximum;
    }

    public bool TryDamage(float time, float invulnerability)
    {
        if (Defeated || time < nextDamageTime) return false;
        Current--;
        nextDamageTime = time + Math.Max(0f, invulnerability);
        return true;
    }
}
