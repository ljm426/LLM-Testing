using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace ChatGPT
{
    public class ChatGPTClient : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string apiKey = "";
        [SerializeField] private string model = "gpt-4o-mini";
        [SerializeField] private int maxTokens = 4;
        [SerializeField] private float temperature = 0.0f;
        
        private const string API_URL = "https://api.openai.com/v1/chat/completions";
    private const string TRANSCRIBE_URL = "https://api.openai.com/v1/audio/transcriptions";
        
        public static ChatGPTClient Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Send a message to ChatGPT and get a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="onResponse">Callback when response is received</param>
        /// <param name="onError">Callback when an error occurs</param>
        public void SendMessage(string message, Action<string> onResponse, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("API key is not set");
                return;
            }
            
            StartCoroutine(SendRequestCoroutine(message, onResponse, onError));
        }
        
        /// <summary>
        /// Send a message with system context for agent commands
        /// </summary>
        /// <param name="userCommand">The user's command</param>
        /// <param name="systemContext">System context for the AI</param>
        /// <param name="onResponse">Callback when response is received</param>
        /// <param name="onError">Callback when an error occurs</param>
        public void SendAgentCommand(string userCommand, string systemContext, Action<string> onResponse, Action<string> onError = null)
        {
            var request = new ChatGPTRequest
            {
                model = model,
                messages = new ChatMessage[]
                {
                    new ChatMessage { role = "system", content = systemContext },
                    new ChatMessage { role = "user", content = userCommand }
                },
                max_tokens = maxTokens,
                temperature = temperature
            };
            
            StartCoroutine(SendRequestCoroutine(request, onResponse, onError));
        }
        
        private IEnumerator SendRequestCoroutine(string message, Action<string> onResponse, Action<string> onError)
        {
            var request = new ChatGPTRequest
            {
                model = model,
                messages = new ChatMessage[]
                {
                    new ChatMessage { role = "user", content = message }
                },
                max_tokens = maxTokens,
                temperature = temperature
            };
            
            yield return SendRequestCoroutine(request, onResponse, onError);
        }
        
        private IEnumerator SendRequestCoroutine(ChatGPTRequest request, Action<string> onResponse, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest webRequest = new UnityWebRequest(API_URL, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<ChatGPTResponse>(webRequest.downloadHandler.text);
                        string content = response.choices[0].message.content.Trim();
                        onResponse?.Invoke(content);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse response: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {webRequest.error}");
                }
            }
        }
        
        /// <summary>
        /// Set the API key programmatically
        /// </summary>
        public void SetApiKey(string key)
        {
            apiKey = key;
        }

        /// <summary>
        /// Transcribe WAV audio bytes to text using OpenAI Whisper (model: whisper-1)
        /// </summary>
        public void TranscribeAudio(byte[] wavBytes, Action<string> onText, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("API key is not set");
                return;
            }
            StartCoroutine(TranscribeCoroutine(wavBytes, onText, onError));
        }

        private IEnumerator TranscribeCoroutine(byte[] wavBytes, Action<string> onText, Action<string> onError)
        {
            // Build multipart/form-data payload
            string boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] headerBytes(string name, string fileName = null, string contentType = null)
            {
                string header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"" +
                                (fileName != null ? $"; filename=\"{fileName}\"" : "") + "\r\n" +
                                (contentType != null ? $"Content-Type: {contentType}\r\n" : "") +
                                "\r\n";
                return Encoding.UTF8.GetBytes(header);
            }

            using (var mem = new MemoryStream())
            {
                // model field
                mem.Write(headerBytes("model"));
                var modelBytes = Encoding.UTF8.GetBytes("whisper-1\r\n");
                mem.Write(modelBytes, 0, modelBytes.Length);

                // file field
                mem.Write(headerBytes("file", "audio.wav", "audio/wav"));
                mem.Write(wavBytes, 0, wavBytes.Length);
                var crlf = Encoding.UTF8.GetBytes("\r\n");
                mem.Write(crlf, 0, crlf.Length);

                // Close boundary
                var endBoundary = Encoding.UTF8.GetBytes($"--{boundary}--\r\n");
                mem.Write(endBoundary, 0, endBoundary.Length);

                byte[] body = mem.ToArray();

                using (UnityWebRequest req = new UnityWebRequest(TRANSCRIBE_URL, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    req.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");

                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            // The response has a field "text" with transcription
                            var json = req.downloadHandler.text;
                            // Minimal JSON parse
                            string text = ExtractJsonStringValue(json, "text");
                            if (string.IsNullOrEmpty(text))
                            {
                                onError?.Invoke("Empty transcription response");
                            }
                            else
                            {
                                onText?.Invoke(text);
                            }
                        }
                        catch (Exception e)
                        {
                            onError?.Invoke($"Failed to parse transcription: {e.Message}");
                        }
                    }
                    else
                    {
                        onError?.Invoke($"Transcription failed: {req.error}\n{req.downloadHandler.text}");
                    }
                }
            }
        }

        // Very small helper to pull a string value out of a flat JSON object
        private static string ExtractJsonStringValue(string json, string key)
        {
            // This is intentionally simple to avoid adding JSON libs; expects \"key\":\"value\"
            string pattern = "\\\"" + key + "\\\"\\s*:\\s*\\\"";
            var idx = System.Text.RegularExpressions.Regex.Match(json, pattern);
            if (!idx.Success) return null;
            int start = idx.Index + idx.Length;
            int end = json.IndexOf('"', start);
            if (end < 0) return null;
            return json.Substring(start, end - start);
        }
    }
}
