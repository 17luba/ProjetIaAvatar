using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;

public class ChatManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField inputField;
    public TMP_Text displayResponse;
    public ScrollRect scrollView;
    public Button sendButton;
    public Button newChatButton;

    [Header("API Settings")]
    public string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private string apiKey; // Chargée dynamiquement
    public string systemMessage = "Tu es un clone amusant qui raconte des blagues et des anecdotes rigolotes.";

    [Header("Animation")]
    public Animator npcAnimator;

    [Header("Animation Setting")]
    public float thinkingAnimationDuration = 4.06f;

    private List<string> conversationHistory = new List<string>();

    private void Start()
    {
        sendButton.onClick.AddListener(SendMessageToAPI);
        newChatButton.onClick.AddListener(ClearChat);

        StartCoroutine(LoadApiKey());
    }

    private IEnumerator LoadApiKey()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "apiKey.txt");

        if (File.Exists(path))
        {
            if (path.Contains("://") || path.Contains(":///"))
            {
                UnityWebRequest www = UnityWebRequest.Get(path);
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                    apiKey = www.downloadHandler.text.Trim();
                else
                    Debug.LogError("Erreur de chargement de la clé API : " + www.error);
            }
            else
            {
                apiKey = File.ReadAllText(path).Trim();
            }
        }
        else
        {
            Debug.LogError("Fichier apiKey.txt introuvable dans StreamingAssets.");
        }
    }

    private void SetThinkingState(bool thinking)
    {
        npcAnimator.SetBool("isThinking", thinking);
    }

    private void SetTalkingState(bool talking)
    {
        npcAnimator.SetBool("isTalking", talking);
    }

    private void DisplayMessage(string message)
    {
        conversationHistory.Add(message);
        displayResponse.text += "\n" + message;
    }

    private IEnumerator TypeMessage(string message)
    {
        displayResponse.text += "\nAI :";
        foreach (char letter in message)
        {
            displayResponse.text += letter;
            yield return new WaitForSeconds(0.07f);
        }
    }

    private void SendMessageToAPI()
    {
        string userMessage = inputField.text;
        if (!string.IsNullOrEmpty(userMessage) && !string.IsNullOrEmpty(apiKey))
        {
            SetThinkingState(true);
            StartCoroutine(PostRequest(userMessage));
        }
        else
        {
            displayResponse.text += "\nErreur : clé API non chargée.";
        }
    }

    private IEnumerator PostRequest(string message)
    {
        conversationHistory.Add($"{{\"role\": \"user\", \"content\": \"{message}\"}}");

        string messagePayload = $"{{\"role\": \"system\", \"content\": \"{systemMessage}\"}}";
        foreach (string pastMessage in conversationHistory)
        {
            messagePayload += $" ,{pastMessage}";
        }

        string jsonData = $"{{\"model\": \"llama3-8b-8192\", \"messages\": [{messagePayload}]}}";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ResponseData response = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
            string aiResponse = response.choices[0].message.content;

            conversationHistory.Add($"{{\"role\": \"assistant\", \"content\": \"{aiResponse}\"}}");

            yield return new WaitForSeconds(thinkingAnimationDuration);
            SetThinkingState(false);

            SetTalkingState(true);
            yield return StartCoroutine(TypeMessage(aiResponse));
            yield return new WaitForSeconds(aiResponse.Length * 0.07f + 1.0f);
            SetTalkingState(false);
        }
        else
        {
            Debug.LogError($"Erreur : {request.error}");
            displayResponse.text += "\nErreur de communication avec l'API.";
            SetThinkingState(false);
        }

        inputField.text = "";
    }

    private void ClearChat()
    {
        displayResponse.text = "";
        inputField.text = "";
        conversationHistory.Clear();
    }

    [System.Serializable]
    public class ResponseData
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
    }
}
