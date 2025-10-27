using UnityEngine;

namespace ChatGPT
{
    /// <summary>
    /// Simple utility class for quick ChatGPT integration
    /// </summary>
    public static class ChatGPTUtility
    {
        /// <summary>
        /// Quick setup method to create a ChatGPT client in the scene
        /// </summary>
        /// <param name="apiKey">Your OpenAI API key</param>
        /// <returns>The created ChatGPTClient instance</returns>
        public static ChatGPTClient SetupClient(string apiKey)
        {
            GameObject clientObject = new GameObject("ChatGPT Client");
            ChatGPTClient client = clientObject.AddComponent<ChatGPTClient>();
            client.SetApiKey(apiKey);
            return client;
        }
        
        /// <summary>
        /// Quick setup method to create an agent command interpreter
        /// </summary>
        /// <param name="target">The GameObject to attach the interpreter to</param>
        /// <returns>The created AgentCommandInterpreter instance</returns>
        public static AgentCommandInterpreter SetupAgent(GameObject target)
        {
            return target.AddComponent<AgentCommandInterpreter>();
        }
        
        /// <summary>
        /// Send a quick message to ChatGPT (requires ChatGPTClient.Instance to exist)
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="onResponse">Callback for response</param>
        public static void QuickMessage(string message, System.Action<string> onResponse)
        {
            if (ChatGPTClient.Instance != null)
            {
                ChatGPTClient.Instance.SendMessage(message, onResponse);
            }
            else
            {
                Debug.LogError("No ChatGPTClient instance found. Use SetupClient first.");
            }
        }
    }
}
