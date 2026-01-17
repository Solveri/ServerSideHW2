using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ApiResponse<T>
{
    public bool Success;
    public T Data;
    public string ErrorMessage;
    public long StatusCode;
}

public class ApiClient
{
    private readonly string _baseUrl;
    private string _authToken;

    public ApiClient(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        using (var request = UnityWebRequest.Get(_baseUrl + endpoint))
        {
            return await SendRequestAsync<T>(request);
        }
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object body)
    {
        string json = JsonUtility.ToJson(body);

        using (var request = new UnityWebRequest(_baseUrl + endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequestAsync<T>(request);
        }
    }

    private async Task<ApiResponse<T>> SendRequestAsync<T>(UnityWebRequest request)
    {
        request.SetRequestHeader("Accept", "application/json");
        if (!string.IsNullOrEmpty(_authToken))
        {
            request.SetRequestHeader("Authorization", "Bearer " + _authToken);
        }

        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        var result = new ApiResponse<T>
        {
            StatusCode = request.responseCode
        };

        if (request.result == UnityWebRequest.Result.Success)
        {
            result.Success = true;
            try
            {
                result.Data = JsonUtility.FromJson<T>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                result.Success = false;
                result.ErrorMessage = e.Message;
            }
        }
        else
        {
            result.Success = false;
            result.ErrorMessage = request.error + ": " + request.downloadHandler.text;
        }

        return result;
    }
}
[Serializable] public class LoginRequest { public string username; public string password; }
[Serializable] public class LoginResponse { public string token; public int userId; }

[Serializable] public class PlayerProfile { public string username; public int level; public int gold; }

[Serializable] public class InventoryAction { public string itemId; public int quantity; }
[Serializable] public class InventoryResponse { public bool success; public int remainingQuantity; }

[Serializable] public class ScoreSubmission { public int score; public string levelId; }
[Serializable] public class ScoreResponse { public int newRank; }