using UnityEngine;

public class GameManager : MonoBehaviour
{
    private ApiClient _apiClient;
    private AuthService _authService;
    private PlayerService _playerService;

    void Start()
    {

        _apiClient = new ApiClient("http://localhost:5222/api");

        _authService = new AuthService(_apiClient);
        _playerService = new PlayerService(_apiClient);

        RunGameLoop();
    }

    private async void RunGameLoop()
    {
        Debug.Log("Starting Game Loop...");

        bool loginSuccess = await _authService.Login("admin", "1234");
        if (!loginSuccess)
        {
            Debug.LogError("Stopping Game Loop because Login failed.");
            return;
        }

        Debug.Log("Getting Profile...");
        var profile = await _playerService.GetProfile();
        Debug.Log($"Profile Loaded: {profile.username}");

        await _playerService.UpdateProfile(profile.level + 1, profile.gold + 50);
        Debug.Log("Profile Updated on Server.");
    }
}