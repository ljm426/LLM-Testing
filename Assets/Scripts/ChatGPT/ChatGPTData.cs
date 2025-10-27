using System;

namespace ChatGPT
{
    [Serializable]
    public class ChatGPTRequest
    {
        public string model;
        public ChatMessage[] messages;
        public int max_tokens;
        public float temperature;
    }
    
    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }
    
    [Serializable]
    public class ChatGPTResponse
    {
        public Choice[] choices;
    }
    
    [Serializable]
    public class Choice
    {
        public ChatMessage message;
    }
}
