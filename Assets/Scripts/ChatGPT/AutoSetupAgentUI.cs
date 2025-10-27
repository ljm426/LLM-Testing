using UnityEngine;

namespace ChatGPT
{
    /// <summary>
    /// Simple component to automatically setup the Agent Command UI
    /// Add this to any GameObject in your scene to create the UI system
    /// </summary>
    public class AutoSetupAgentUI : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private Canvas targetCanvas;
        
        [Header("Agent Reference")]
        [SerializeField] private AgentCommandInterpreter agentInterpreter;
        
        private AgentCommandUI createdUI;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupUI();
            }
        }
        
        /// <summary>
        /// Create the Agent Command UI
        /// </summary>
        [ContextMenu("Setup Agent UI")]
        public void SetupUI()
        {
            if (createdUI != null)
            {
                Debug.LogWarning("Agent Command UI already exists!");
                return;
            }
            
            // Create the UI
            createdUI = AgentCommandUISetup.CreateAgentCommandUI(targetCanvas);
            
            // Auto-find agent interpreter if not assigned
            if (agentInterpreter == null)
            {
                agentInterpreter = FindFirstObjectByType<AgentCommandInterpreter>();
            }
            
            // Link the agent interpreter
            if (agentInterpreter != null)
            {
                createdUI.SetAgentInterpreter(agentInterpreter);
                Debug.Log("Agent Command UI setup complete and linked to AgentCommandInterpreter!");
            }
            else
            {
                Debug.LogWarning("No AgentCommandInterpreter found in scene. Please assign one manually or add it to your agent.");
            }
        }
        
        /// <summary>
        /// Remove the created UI
        /// </summary>
        [ContextMenu("Remove Agent UI")]
        public void RemoveUI()
        {
            if (createdUI != null)
            {
                DestroyImmediate(createdUI.gameObject);
                createdUI = null;
                Debug.Log("Agent Command UI removed.");
            }
        }
    }
}
