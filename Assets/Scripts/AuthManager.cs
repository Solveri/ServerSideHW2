using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    private ApiClient _apiClient;

    [Header("Settings")]
    // 1. SET THIS TO TRUE FOR THE LIVE SERVER
    public bool isProduction = true;

    // 2. Replace this with your actual Render URL (keep the https://)
    private string productionUrl = "https://serversidefinal-1.onrender.com";
    private string localUrl = "http://localhost:3000";

    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI feedbackText;

    public event System.Action<string> OnLoginSuccess;

    private void Awake()
    {
        // Automatically picks the right URL based on the checkbox
        string activeUrl = isProduction ? productionUrl : localUrl;
        _apiClient = new ApiClient(activeUrl);

        Debug.Log($"AuthManager initialized. Target: {activeUrl}");
    }

    private void Start()
    {
        if (isProduction)
        {
            StartCoroutine(WakeUpServer());
        }
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

        var response = await _apiClient.PostAsync<string>("/api/auth/register", requestData);

        if (response.Success)
        {
            // Updated text to reflect cloud storage
            feedbackText.text = "<color=green>User Registered!</color> Check MongoDB Atlas.";
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
            string token = response.Data.token;
            _apiClient.SetAuthToken(token);

            feedbackText.text = "<color=green>Logged In!</color>";

            // 3. IMPORTANT: Pass the token to the WebSocket manager
            // Ensure your WebSocket manager uses "wss://" for production
            OnLoginSuccess?.Invoke(token);

            Debug.Log("JWT Token Received: " + token);
        }
        else
        {
            // If it fails, it might be because the server is still 'waking up'
            feedbackText.text = "<color=red>Login Failed: " + response.ErrorMessage + "</color>";
            Debug.LogWarning("Check Render logs if this persists.");
        }
    }
    private IEnumerator WakeUpServer()
    {
        // Clean the URL to ensure no double slashes
        string cleanUrl = productionUrl.TrimEnd('/');
        Debug.Log($"[Wakeup] Pinging: {cleanUrl}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(cleanUrl))
        {
            webRequest.timeout = 30; // 30s is plenty if the browser already sees it

            // This tells the server we are a standard client
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("User-Agent", "UnityPlayer");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(" [Wakeup] Unity successfully reached the server!");
                feedbackText.text = "<color=green>Connected to Cloud</color>";
            }
            else
            {
                // Even if it "fails" with a 404, the server IS awake if it responds at all.
                Debug.LogWarning($" [Wakeup] Response: {webRequest.responseCode} - {webRequest.error}");

                if (webRequest.responseCode == 404 || webRequest.responseCode == 200)
                {
                    Debug.Log(" Server responded! We are good to go.");
                }
            }
        }
    }
}
