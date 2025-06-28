using System.Net.Http.Json;
#pragma warning disable CA1031

namespace Game.Bots;

public interface IBotService
{
    Task<BotInfo?> GetBotInfoAsync(Uri externalBotUrl);
}

public class BotService : IBotService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    public async Task<BotInfo?> GetBotInfoAsync(Uri externalBotUrl)
    {
        try
        {
            var botInfo = await HttpClient.GetFromJsonAsync<BotInfo>(externalBotUrl);
            return botInfo;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}