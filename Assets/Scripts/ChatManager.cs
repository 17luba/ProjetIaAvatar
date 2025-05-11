using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ChatManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField inputField;
    public TMP_Text displayResponse;
    public ScrollRect scrollView;
    public Button sendButton;
    public Button newChatButton;

    List<string> converdationHistory = new List<string>();

    [Header("API Settings")]
    public string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
    public string apiKey = "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"; // clé API GroqCloud.
    public string systemMessage = "Tu es un clone amusant qui raconte des blagues et des anecdotes rigolotes.";

    [Header("TTS Settings")]
    public string playHTApiUrl = "https://api.play.ht/v1/convert";
    public string playHTApiKey = "iI4O2BhnORdUm9lkG2eUtB3ncRP2";
    public string voiceId = "s3://Voice-IDs/en_us/Atlas/female";

    [Header("Animation")]
    public Animator npcAnimator;

    [Header("Animation Setting")]
    public float thinkingAnimationDuration = 4.06f;

    void Start()
    {
        sendButton.onClick.AddListener(SendMessageToAPI);
        newChatButton.onClick.AddListener(ClearChat);
    }

    void SetThinkingState(bool thinking)
    {
        npcAnimator.SetBool("isThinking", thinking);
    }

    void SetTalkingState(bool talking)
    {
        npcAnimator.SetBool("isTalking", talking);
    }

    
    void DisplayMessage(string message)
    {
        converdationHistory.Add(message);
        displayResponse.text += "\n" + message;
    }

    // Effet frappe de message
    IEnumerator TypeMessage(string message)
    {
        displayResponse.text += "\nAI :";
        foreach (char letter in message)
        {
            displayResponse.text += letter;
            yield return new WaitForSeconds(0.07f);
        }
    }

    void SendMessageToAPI()
    {
        string userMessage = inputField.text;
        if (!string.IsNullOrEmpty(userMessage))
        {
            SetThinkingState(true); // Activation de l'etat thinking
            StartCoroutine(PostRequest(userMessage));
        }
    }

    IEnumerator PostRequest(string message)
    {

        // Ajouter le message de l'utilisateur à l'historique
        converdationHistory.Add($"{{\"role\": \"user\", \"content\": \"{message}\"}}");

        // Construire le Payload JSON avec le rôle du système et l'historique de la conversation
        string messagePayload = $"{{\"role\": \"system\", \"content\": \"{systemMessage}\"}}";

        foreach (string pastMessage in converdationHistory)
        {
            messagePayload += $" ,{pastMessage}";
        }

        // Construire le payload JSON.
        string jsonData = $"{{\"model\": \"llama3-8b-8192\", \"messages\": [{messagePayload}]}}";

        // Configurer la requête HTTP POST.
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        Debug.Log(apiUrl);
        Debug.Log(jsonData);
        Debug.Log(apiKey);

        yield return request.SendWebRequest();


        if (request.result == UnityWebRequest.Result.Success)
        {
            // Extraire et afficher la réponse.
            ResponseData response = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
            string aiResponse = response.choices[0].message.content;

            // Ajout de la reponse de l'avatar à l'historique
            converdationHistory.Add($"{{\"role\": \"assistant\", \"content\": \"{aiResponse}\"}}");

            // Désactivation de l'état thinking après la durée de l'animation
            yield return new WaitForSeconds(thinkingAnimationDuration);
            SetThinkingState(false);

            // Activer l'état talking et commencer l'affichage
            SetTalkingState(true);
            StartCoroutine(TypeMessage(aiResponse));


            // Attente de la fin de l'affichage avant de retourner à l'etat Idle
            yield return new WaitForSeconds(aiResponse.Length * 0.07f + 1.0f);

            SetTalkingState(false); // Retour à l'état Idle

        }
        else
        {
            Debug.LogError($"Erreur : {request.error}");
            displayResponse.text += "\nErreur de communication avec l'API.";
            SetThinkingState(false);
        }

        // Effacer le champ de texte.
        inputField.text = "";
    }

    // Audio de l'avatar

    void ClearChat()
    {
        displayResponse.text = "";
        inputField.text = "";
        converdationHistory.Clear();
    }

    // Classes pour désérialiser la réponse JSON.
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