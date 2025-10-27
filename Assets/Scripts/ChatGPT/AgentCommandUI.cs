using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace ChatGPT
{
    public class AgentCommandUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject commandPanel;
        [SerializeField] private TMP_InputField commandInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;
        
        [Header("Settings")]
        [SerializeField] private Key activationKey = Key.T;
        [SerializeField] private bool pauseGameWhenOpen = false;
        
        [Header("Agent Reference")]
        [SerializeField] private AgentCommandInterpreter agentInterpreter;
        
        [Header("Player Input")]
        [Tooltip("Reference to the player's StarterAssetsInputs component")]
        [SerializeField] private StarterAssets.StarterAssetsInputs playerInput;
        
        private bool isUIOpen = false;
        private float originalTimeScale;
        private Vector2 originalMoveInput;
        private Vector2 originalLookInput;
        
        private void Start()
        {
            SetupUI();
            CloseCommandUI();
        }
        
        private void Update()
        {
            // Only process the toggle key if the input field is not focused
            bool isInputFieldFocused = commandInputField != null && commandInputField.isFocused;
            
            // Check for activation key (only when input field is not focused)
            if (Keyboard.current[activationKey].wasPressedThisFrame && !isInputFieldFocused)
            {
                if (isUIOpen)
                {
                    CloseCommandUI();
                }
                else
                {
                    OpenCommandUI();
                }
            }
            
            // Submit command with Enter key when UI is open
            if (isUIOpen && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                SendCommand();
            }
            
            // Close with Escape key
            if (isUIOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseCommandUI();
            }
        }
        
        private void SetupUI()
        {
            // Find agent interpreter if not assigned
            if (agentInterpreter == null)
            {
                agentInterpreter = FindFirstObjectByType<AgentCommandInterpreter>();
            }
            
            // Find player input if not assigned
            if (playerInput == null)
            {
                playerInput = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
            }
            
            // Setup button events
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(SendCommand);
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseCommandUI);
            }
            
            // Setup input field
            if (commandInputField != null)
            {
                commandInputField.onSubmit.AddListener(OnInputSubmit);
            }
        }
        
        private void OpenCommandUI()
        {
            if (commandPanel != null)
            {
                commandPanel.SetActive(true);
                isUIOpen = true;
                
                // Focus the input field
                if (commandInputField != null)
                {
                    commandInputField.Select();
                    commandInputField.ActivateInputField();
                }
                
                // Pause game if enabled
                if (pauseGameWhenOpen)
                {
                    originalTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }
                
                // Disable player movement input while typing
                if (playerInput != null)
                {
                    // Store current input values
                    originalMoveInput = playerInput.move;
                    originalLookInput = playerInput.look;
                    
                    // Clear input to prevent movement
                    playerInput.MoveInput(Vector2.zero);
                    playerInput.LookInput(Vector2.zero);
                    
                    // Temporarily set ignore input flag
                    typeof(StarterAssets.StarterAssetsInputs)
                        .GetField("m_IgnoreInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(playerInput, true);
                }
                
                // Unlock cursor
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        private void CloseCommandUI()
        {
            if (commandPanel != null)
            {
                commandPanel.SetActive(false);
                isUIOpen = false;
                
                // Clear input field
                if (commandInputField != null)
                {
                    commandInputField.text = "";
                }
                
                // Restore game time
                if (pauseGameWhenOpen)
                {
                    Time.timeScale = originalTimeScale;
                }
                
                // Re-enable player movement
                if (playerInput != null)
                {
                    // Reset the ignore input flag
                    typeof(StarterAssets.StarterAssetsInputs)
                        .GetField("m_IgnoreInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(playerInput, false);
                }
                
                // Lock cursor back (adjust based on your game's cursor needs)
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        private void SendCommand()
        {
            if (commandInputField != null && !string.IsNullOrEmpty(commandInputField.text.Trim()))
            {
                string command = commandInputField.text.Trim();
                
                if (agentInterpreter != null)
                {
                    Debug.Log($"Sending command to agent: {command}");
                    agentInterpreter.ProcessCommand(command);
                }
                else
                {
                    Debug.LogWarning("No AgentCommandInterpreter found!");
                }
                
                CloseCommandUI();
            }
        }
        
        private void OnInputSubmit(string value)
        {
            SendCommand();
        }
        
        /// <summary>
        /// Set the agent interpreter reference
        /// </summary>
        /// <param name="interpreter">The agent interpreter to use</param>
        public void SetAgentInterpreter(AgentCommandInterpreter interpreter)
        {
            agentInterpreter = interpreter;
        }
        
        /// <summary>
        /// Set the activation key
        /// </summary>
        /// <param name="key">Key to open/close the UI</param>
        public void SetActivationKey(Key key)
        {
            activationKey = key;
        }
        
        /// <summary>
        /// Open the command UI programmatically
        /// </summary>
        public void ShowCommandUI()
        {
            OpenCommandUI();
        }
        
        /// <summary>
        /// Close the command UI programmatically
        /// </summary>
        public void HideCommandUI()
        {
            CloseCommandUI();
        }
    }
}
