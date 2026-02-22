using TMPro;
using UnityEngine;

public class GameRoomManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI opponentNameText;
    private string currentMatchId;

    public void InitializeMatch(string opponent, string matchId)
    {
        opponentNameText.text = "VS " + opponent;
        currentMatchId = matchId;

        // Reset scores to 0 locally just in case
        UpdateUI(null);
    }

    public void UpdateUI(SocketMessage msg)
    {
        if (msg == null)
        {
            return;
        }
        // Make sure you save your own username to PlayerPrefs during Login!
        string myName = PlayerPrefs.GetString("MyUsername");

        if (msg.p1Name == myName)
        {
            // I entered the match first (Player 1)
            scoreText.text = $"Me ({msg.p1Name}): {msg.p1Score} | {msg.p2Name}: {msg.p2Score}";
        }
        else
        {
            // I entered the match second (Player 2)
            scoreText.text = $"Me ({msg.p2Name}): {msg.p2Score} | {msg.p1Name}: {msg.p1Score}";
        }
    }

    public void OnAddScoreClick()
    {
        string matchId = PlayerPrefs.GetString("MatchId");
        // Use the JsonUtility approach to avoid "Syntax Errors"
        SocketMessage sm = new SocketMessage();
        sm.type = "SUBMIT_SCORE";
        sm.score = 1;
        sm.matchId = matchId;

        string json = JsonUtility.ToJson(sm);
        FindObjectOfType<NativeWebSocketManager>().SendMessageToServer(json);
    }

}