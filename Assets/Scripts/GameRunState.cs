public static class GameRunState
{
    public const int StartingLives = 3;
    public static int Lives { get; private set; } = StartingLives;
    public static bool IsGameOver => Lives == 0;
    public static bool HasFirePower { get; set; }

    public static void Reset()
    {
        Lives = StartingLives;
        HasFirePower = false;
    }

    public static bool LoseLife()
    {
        if (IsGameOver) return false;
        Lives--;
        HasFirePower = false;
        return true;
    }
}
