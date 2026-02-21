using UnityEngine;
using TMPro;
using UnityEngine.UI; // Needed for ScrollRect
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    public TextMeshProUGUI chatText;      // The actual text component
    public TextMeshProUGUI Count;      // The actual text component
    public ScrollRect chatScrollRect;    // The Scroll View's ScrollRect component
    public TMP_InputField chatInput;     // Where the player types
    int playercount;
    public void AddChatMessage(string sender, string message)
    {
        // 1. Append the message with some basic formatting
        chatText.text += $"\n<color=#5865F2><b>{sender}:</b></color> {message}";

        // 2. Wait for the end of the frame, then scroll to the bottom
        StartCoroutine(SnapToBottom());
    }

    private IEnumerator SnapToBottom()
    {
        // We wait for the UI to rebuild the text layout
        yield return new WaitForEndOfFrame();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

    // Called when the "Send" button is clicked
    public void OnSendButtonClick()
    {
        if (string.IsNullOrEmpty(chatInput.text)) return;

        string json = "{\"type\":\"CHAT\", \"text\":\"" + chatInput.text + "\"}";
        FindObjectOfType<NativeWebSocketManager>().SendMessageToServer(json);

        chatInput.text = ""; // JUST clear the input, don't update the text area here!
    }
    public void OnRoomClick(string roomName)
    {
        // Clear the chat locally so it feels like a new room
        chatText.text = $"<color=yellow>--- Switched to {roomName} ---</color>";

        // Tell the server we are moving
        string json = "{\"type\":\"JOIN_ROOM\", \"roomId\":\"" + roomName + "\"}";
        FindObjectOfType<NativeWebSocketManager>().SendMessageToServer(json);
    }
    public void UpdatePlayerCount(int count)
    {
        playercount = count;
        Count.text = count.ToString();
    }
}