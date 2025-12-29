namespace BotSharp.Abstraction.Conversations;

public interface ITokenStatistics
{
    int Total { get; }
    float AccumulatedCost { get; }
    float Cost { get; }
    void StartTimer();
    void StopTimer();
    Task AddToken(TokenStatsModel stats, RoleDialogModel message);
    void PrintStatistics();
}