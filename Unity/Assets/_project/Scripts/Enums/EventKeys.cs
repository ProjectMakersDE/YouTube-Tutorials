namespace PM.Enums
{
    public enum EventKeys
    {
        // Change events
        ChangeGold = 100,
        ChangeReputation = 101,

        // Load events
        LoadGold = 200,
        LoadReputation = 201,

        // User events
        UserLoadScoreBoard = 300,
        UserResolveOrder = 301,
        CompleteOrder = 302,
        RunPromotion  = 303,
        ResetProgress  = 304,
        
        // UiEvent
        ShowUi = 400,
        
        // System
        LoadingFinish = 1000,

    }
}