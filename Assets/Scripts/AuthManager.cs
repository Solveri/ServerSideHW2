using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    private ApiClient _apiClient;

    [Header("Settings")]
    public string serverUrl = "http://localhost:3000"; // Ensure this matches your Node server

    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI feedbackText;

    public event System.Action<string> OnLoginSuccess; // Event to notify successful login with JWT token

    private void Awake()
    {
        // Initialize your ApiClient with the base URL
        _apiClient = new ApiClient(serverUrl);
    }

    // --- ASSIGNMENT 1: REGISTER ---
    public async void OnRegisterClick()
    {
        feedbackText.text = "Registering...";

        var requestData = new LoginRequest
        {
            username = usernameInput.text,
            password = passwordInput.text
        };

        // Note: Using /api/auth/register based on our previous server code
        var response = await _apiClient.PostAsync<string>("/api/auth/register", requestData);

        if (response.Success)
        {
            feedbackText.text = "<color=green>User Registered!</color> Check MongoDB Compass.";
            
            Debug.Log("Registration Success!");
        }
        else
        {
            feedbackText.text = "<color=red>Error: " + response.ErrorMessage + "</color>";
        }
    }

    // --- ASSIGNMENT 1: LOGIN ---
    public async void OnLoginClick()
    {
        feedbackText.text = "Logging in...";

        var requestData = new LoginRequest
        {
            username = usernameInput.text,
            password = passwordInput.text
        };

        var response = await _apiClient.PostAsync<LoginResponse>("/api/auth/login", requestData);

        if (response.Success)
        {
            // REQUIREMENT: Connects to WebSocket using token
            string token = response.Data.token;
            _apiClient.SetAuthToken(token); // Save it in the client for future REST calls

            feedbackText.text = "<color=green>Logged In!</color>";
            OnLoginSuccess?.Invoke(token); // Notify listeners about successful login with the token
            Debug.Log("JWT Token Received: " + token);

            // This is where you would trigger your NativeWebSocketManager.Connect(token)
        }
        else
        {
            feedbackText.text = "<color=red>Login Failed: " + response.ErrorMessage + "</color>";
        }
    }
}