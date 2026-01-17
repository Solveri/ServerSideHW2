using System.Threading.Tasks;
using UnityEngine;

public class AuthService
{
    private ApiClient _api;

    public AuthService(ApiClient api) => _api = api;

    public async Task<bool> Login(string username, string password)
    {
        var req = new LoginRequest { username = username, password = password };
        var response = await _api.PostAsync<LoginResponse>("/auth/login", req);

        if (response.Success)
        {
            _api.SetAuthToken(response.Data.token);
            return true;
        }

        return false;
    }
}

public class PlayerService
{
    private ApiClient _api;
    public PlayerService(ApiClient api) => _api = api;

    public async Task<PlayerProfile> GetProfile()
    {
        var response = await _api.GetAsync<PlayerProfile>("/player/profile");
        return response.Success ? response.Data : null;
    }

    public async Task<bool> UpdateProfile(int level, int gold)
    {
        var req = new PlayerProfile { level = level, gold = gold };
        var response = await _api.PostAsync<PlayerProfile>("/player/update", req);
        return response.Success;
    }
}

public class GameplayMetaService
{
    private ApiClient _api;
    public GameplayMetaService(ApiClient api) => _api = api;

    public async Task<int> ConsumeItem(string itemId)
    {
        var req = new InventoryAction { itemId = itemId, quantity = 1 };
        var response = await _api.PostAsync<InventoryResponse>("/inventory/consume", req);

        if (response.Success && response.Data.success)
            return response.Data.remainingQuantity;

        return -1;
    }

    public async Task<int> SubmitScore(string levelId, int score)
    {
        var req = new ScoreSubmission { levelId = levelId, score = score };
        var response = await _api.PostAsync<ScoreResponse>("/leaderboard/submit", req);

        return response.Success ? response.Data.newRank : 0;
    }
}