using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using TMPro;

public class ChatClient : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject chatPanel;

    [Header("Login UI")]
    public TMP_InputField userIn;
    public TextMeshProUGUI loginStatus;

    [Header("Chat UI")]
    public TMP_InputField msgIn;
    public TextMeshProUGUI chatDisplay;
    public TextMeshProUGUI headerText;

    private ClientWebSocket ws;
    private CancellationTokenSource cts;

    // Queue to hold messages received on background threads
    private ConcurrentQueue<string> mainThreadMessageQueue = new ConcurrentQueue<string>();

    void Update()
    {
        // Check the queue every frame and update the UI on the Main Thread
        while (mainThreadMessageQueue.TryDequeue(out string message))
        {
            chatDisplay.text += $"\n{message}";
        }
    }

    public async void OnLoginClick()
    {
        if (string.IsNullOrEmpty(userIn.text))
        {
            loginStatus.text = "Enter a username!";
            return;
        }

        loginStatus.text = "Connecting...";
        await Connect(userIn.text);
    }

    async Task Connect(string username)
    {
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();

        // Ensure this matches your server's IP and Port
        Uri uri = new Uri($"ws://localhost:8080?user={username}");

        try
        {
            await ws.ConnectAsync(uri, cts.Token);

            loginPanel.SetActive(false);
            chatPanel.SetActive(true);
            headerText.text = $"Logged in as: {username}";

            // Start the background listening loop
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            loginStatus.text = "Connection Failed!";
            Debug.LogError($"WebSocket Connection Error: {e.Message}");
        }
    }

    async Task ReceiveLoop()
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    mainThreadMessageQueue.Enqueue("<i>Server closed the connection.</i>");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                mainThreadMessageQueue.Enqueue(message);
            }
        }
        catch (Exception e)
        {
            if (ws.State != WebSocketState.Aborted)
            {
                Debug.LogWarning($"Receive loop stopped: {e.Message}");
                // Use a special string or flag to handle disconnect on main thread
                mainThreadMessageQueue.Enqueue("SYSTEM_DISCONNECT");
            }
        }
    }

    public async void SendMsg()
    {
        if (string.IsNullOrEmpty(msgIn.text) || ws == null || ws.State != WebSocketState.Open) return;

        try
        {
            string messageToSend = msgIn.text;
            byte[] buffer = Encoding.UTF8.GetBytes(messageToSend);

            // Optimistically show your own message
            chatDisplay.text += $"\nYou: {messageToSend}";

            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
            msgIn.text = "";
        }
        catch (Exception e)
        {
            Debug.LogError($"Send Error: {e.Message}");
        }
    }

    public async void Disconnect()
    {
        if (ws != null)
        {
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "User Logged Out", new CancellationTokenSource(2000).Token);
                }
                catch (Exception e) { Debug.Log($"Close error: {e.Message}"); }
            }
            ws.Dispose();
            ws = null;
        }

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        HandleDisconnectUI("Logged Out");
    }

    private void HandleDisconnectUI(string status)
    {
        chatPanel.SetActive(false);
        loginPanel.SetActive(true);
        chatDisplay.text = "";
        loginStatus.text = status;

        // Clear the queue
        while (mainThreadMessageQueue.TryDequeue(out _)) { }
    }

    private void OnApplicationQuit()
    {
        if (ws != null)
        {
            ws.Abort(); // Force kill on quit
        }
    }
}