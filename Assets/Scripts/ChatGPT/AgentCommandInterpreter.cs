using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatGPT
{
    public class AgentCommandInterpreter : MonoBehaviour
    {
        private string systemPrompt =
            "Given a user command, respond with one action name from: FOLLOW, STOP, JUMP, IDLE, BACKOFF. Infer intent and choose the best action. If ambiguous, infer from context.\n\n" +
            "Output: The action word (uppercase), with no explanation or punctuation.\n" +
            "Only use the allowed actions.\n" +
            "No extra text.";
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnFollow;
        public UnityEngine.Events.UnityEvent OnStop; 
        public UnityEngine.Events.UnityEvent OnJump;
        public UnityEngine.Events.UnityEvent OnIdle;
        public UnityEngine.Events.UnityEvent OnBackOff;
        
        private Dictionary<string, UnityEngine.Events.UnityEvent> actionMap;

        // Local cache to avoid repeated LLM calls for the same phrase
        private Dictionary<string, string> commandCache = new Dictionary<string, string>();

        /// <summary>
        /// Try to parse the command locally using simple heuristics before sending to LLM.
        /// </summary>
        /// <param name="command">The user's command in natural language</param>
        /// <param name="action">The parsed action name (uppercase) if successful</param>
        /// <returns>True if a local parse was successful, false otherwise.</returns>
        private bool TryLocalParse(string command, out string action)
        {
            action = null;
            if (string.IsNullOrWhiteSpace(command)) return false;

            string cmd = command.Trim().ToLower();

            // Private method to check for keywords
            bool Has(params string[] keywords)
            {
                foreach (var keyword in keywords)
                {
                    if (cmd.Contains(keyword.ToLower()))
                        return true;
                }
                return false;
            }

            // Negation handling
            bool isNegated = Has("don't", "do not");

            // Priority: STOP, JUMP, FOLLOW, BACKOFF, IDLE
            // STOP
            if (Has("stop", "freeze", "halt", "hold", "quit", "enough"))
            {
                action = !isNegated ? "STOP" : "FOLLOW";
                return true;
            }
            // JUMP
            if (Has("jump", "leap", "hop"))
            {
                action = !isNegated ? "JUMP" : "IDLE";
                return true;
            }
            // FOLLOW
            if (Has("follow", "come", "chase", "tail me"))
            {
                action = !isNegated ? "FOLLOW" : "STOP";
                return true;
            }
            // BACKOFF
            if (Has("back off", "backoff", "back up", "backup", "step back"))
            {
                action = !isNegated ? "BACKOFF" : "FOLLOW";
                return true;

            }
            // IDLE
            if (Has("idle", "relax", "rest", "chill", "wait", "standby"))
            {
                action = !isNegated ? "IDLE" : "FOLLOW";
                return true;
            }

            return false;
        }

        private void Awake()
        {
            InitializeActionMap();
        }
        
        /// <summary>
        /// Initialize the mapping of action names to Unity events.
        /// </summary>
        private void InitializeActionMap()
        {
            actionMap = new Dictionary<string, UnityEngine.Events.UnityEvent>
            {
                { "FOLLOW", OnFollow },
                { "STOP", OnStop },
                { "JUMP", OnJump },
                { "IDLE", OnIdle },
                { "BACKOFF", OnBackOff }
            };
        }
        
        /// <summary>
        /// Process a natural language command and execute the appropriate action
        /// </summary>
        /// <param name="command">The user's command in natural language</param>
        public void ProcessCommand(string command)
        {
            if (ChatGPTClient.Instance == null)
            {
                Debug.LogError("ChatGPTClient instance not found!");
                return;
            }

            string key = (command ?? string.Empty).Trim().ToLower();

            // 1) Cache hit
            if (commandCache.TryGetValue(key, out string cachedAction))
            {
                Debug.Log($"<color=#00FF00>Cache hit:</color> {command} => {cachedAction}");
                OnCommandProcessed(cachedAction);
                return;
            }

            // 2) Try local parse
            if (TryLocalParse(command, out string localAction))
            {
                Debug.Log($"<color=#00FF00>Local parse:</color> {command} => {localAction}");
                commandCache[key] = localAction; // Cache the result
                OnCommandProcessed(localAction);
                return;
            }

            // 3) Fallback to LLM
            Debug.Log($"<color=#FFFF00>LLM parse:</color> {command}");
            ChatGPTClient.Instance.SendAgentCommand(
                command,
                systemPrompt,
                response =>
                {
                    // Normalize and cache the LLM's decision
                    var action = response?.Trim().ToUpper() ?? string.Empty;
                    if (!string.IsNullOrEmpty(action))
                    {
                        commandCache[key] = action;
                    }
                    OnCommandProcessed(response);
                },
                OnCommandError
            );
        }
        
        private void OnCommandProcessed(string response)
        {            
            string action = response.Trim().ToUpper();
            
            if (actionMap.ContainsKey(action))
            {
                Debug.Log($"Executing action: {action}");
                actionMap[action]?.Invoke();
            }
            else
            {
                Debug.LogWarning($"Unknown action received: {response}");
                OnIdle?.Invoke(); // Default to idle for unknown commands
            }
        }
        
        private void OnCommandError(string error)
        {
            Debug.LogError($"<color=#FF0000>LLM Error:</color> {error}");
            OnIdle?.Invoke(); // Default to idle on error
        }
        
        /// <summary>
        /// Add a custom action to the interpreter
        /// </summary>
        /// <param name="actionName">Name of the action (uppercase)</param>
        /// <param name="actionEvent">Unity event to invoke</param>
        public void AddCustomAction(string actionName, UnityEngine.Events.UnityEvent actionEvent)
        {
            actionMap[actionName.ToUpper()] = actionEvent;
        }
    }
}
