namespace HeadSoccer
{
    /// <summary>
    /// The complete set of states a match can be in. GameManager owns the current one.
    /// </summary>
    public enum MatchState
    {
        Menu,
        CharacterSelect,
        Kickoff,
        Playing,
        GoalScored,
        Paused,
        MatchOver
    }

    /// <summary>Which half of the pitch a thing belongs to.</summary>
    public enum Side
    {
        Left = 0,
        Right = 1
    }
}
