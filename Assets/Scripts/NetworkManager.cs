using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NativeWebSocketManager : MonoBehaviour
{
    private ClientWebSocket _webSocket = null;
    private CancellationTokenSource _cts;

    // Thread-safe queue to move messages from the background thread to Unity's Main Thread
    private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

    [Header("Dependencies")]
    public LobbyUI lobbyUI;
    [SerializeField] GameObject loginPanel;
    [SerializeField] AuthManager AuthManager;
    public int playerCount;

    private void Update()
    {
        // Check the queue every frame and update UI components on the Main Thread
        while (_messageQueue.TryDequeue(out string json))
        {
            HandleMessage(json);
        }
    }
    private void OnEnable()
    {
        AuthManager.OnLoginSuccess += ConnectToLobby;   
    }
    

    public async void ConnectToLobby(string jwtToken)
    {
        lobbyUI.gameObject.SetActive(true);
        loginPanel.SetActive(false);
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        // Pass the JWT in the URL for the server to verify (Requirement: Auth via Token)
        string url = $"ws://127.0.0.1:3000/?token={jwtToken}";
        Uri uri = new Uri(url);

        try
        {
            Debug.Log("Connecting to WebSocket...");
            await _webSocket.ConnectAsync(uri, _cts.Token);
            Debug.Log(" WebSocket Connected!");

            // Start the background loop to listen for messages
            playerCount++;
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket Connection Error: {e.Message}");
        }
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024 * 4];

        while (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _cts.Token);
                }
                else
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // Add message to queue to be handled in Update()
                    _messageQueue.Enqueue(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Receive Error: {e.Message}");
                break;
            }
        }
    }

    public async void SendMessageToServer(string json)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            Debug.LogError("Cannot send message: WebSocket is not open.");
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
    }

    private void HandleMessage(string json)
    {
        Debug.Log($"Message Received: {json}");
        var msg = JsonUtility.FromJson<SocketMessage>(json);

        if (lobbyUI == null) lobbyUI = FindObjectOfType<LobbyUI>();

        // Route the message to the correct UI function
        switch (msg.type)
        {
            case "LOBBY_UPDATE":
                lobbyUI.UpdatePlayerCount(msg.count);
                break;
            case "CHAT_MSG":
                lobbyUI.AddChatMessage(msg.sender, msg.text);
                break;
            case "PLAYER_JOINED":
                lobbyUI.AddChatMessage("System", $"{msg.username} joined the lobby.");
                lobbyUI.UpdatePlayerCount(playerCount);
                break;
        }
    }

    private async void OnDestroy()
    {
        if (_webSocket != null)
        {
            _cts.Cancel();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App Destroyed", CancellationToken.None);
            _webSocket.Dispose();
        }
    }
}

// Data model for JSON communication
[Serializable]
public class SocketMessage
{
    public string type;
    public string username;
    public string sender;
    public string text;
    public int count;
}