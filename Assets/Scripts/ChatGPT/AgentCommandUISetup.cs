using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChatGPT
{
    /// <summary>
    /// Utility class to programmatically create the Agent Command UI
    /// </summary>
    public static class AgentCommandUISetup
    {
        /// <summary>
        /// Creates a complete Agent Command UI in the scene
        /// </summary>
        /// <param name="canvas">The canvas to create the UI under (if null, will find or create one)</param>
        /// <returns>The created AgentCommandUI component</returns>
        public static AgentCommandUI CreateAgentCommandUI(Canvas canvas = null)
        {
            // Find or create canvas
            if (canvas == null)
            {
                canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasGO = new GameObject("Canvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                    
                    // Create EventSystem if it doesn't exist
                    if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                    {
                        GameObject eventSystemGO = new GameObject("EventSystem");
                        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }
                }
            }
            
            // Create main UI manager object
            GameObject uiManagerGO = new GameObject("AgentCommandUI");
            uiManagerGO.transform.SetParent(canvas.transform, false);
            AgentCommandUI uiManager = uiManagerGO.AddComponent<AgentCommandUI>();
            
            // Create command panel
            GameObject panelGO = new GameObject("CommandPanel");
            panelGO.transform.SetParent(uiManagerGO.transform, false);
            
            // Setup panel RectTransform
            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 150);
            
            // Add panel background (transparent)
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
            
            // Create input field background
            GameObject inputBG = new GameObject("InputBackground");
            inputBG.transform.SetParent(panelGO.transform, false);
            RectTransform inputBGRect = inputBG.AddComponent<RectTransform>();
            inputBGRect.anchorMin = new Vector2(0.05f, 0.4f);
            inputBGRect.anchorMax = new Vector2(0.95f, 0.8f);
            inputBGRect.offsetMin = Vector2.zero;
            inputBGRect.offsetMax = Vector2.zero;
            
            Image inputBGImage = inputBG.AddComponent<Image>();
            inputBGImage.color = new Color(1, 1, 1, 0.9f); // Semi-transparent white
            
            // Create input field
            GameObject inputFieldGO = new GameObject("CommandInputField");
            inputFieldGO.transform.SetParent(inputBG.transform, false);
            
            RectTransform inputRect = inputFieldGO.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(10, 5);
            inputRect.offsetMax = new Vector2(-10, -5);
            
            TMP_InputField inputField = inputFieldGO.AddComponent<TMP_InputField>();
            
            // Create text component for input field
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(inputFieldGO.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = 18;
            textComponent.color = Color.black;
            
            // Create placeholder
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputFieldGO.transform, false);
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Type your command here... (Press T to open/close)";
            placeholderText.fontSize = 18;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            // Setup input field references
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;
            
            // Create send button
            GameObject sendButtonGO = CreateButton("SendButton", panelGO.transform, "Send", new Vector2(0.7f, 0.1f), new Vector2(0.95f, 0.35f));
            Button sendButton = sendButtonGO.GetComponent<Button>();
            
            // Create close button
            GameObject closeButtonGO = CreateButton("CloseButton", panelGO.transform, "Close", new Vector2(0.05f, 0.1f), new Vector2(0.3f, 0.35f));
            Button closeButton = closeButtonGO.GetComponent<Button>();
            
            // Assign references to UI manager
            var uiManagerType = typeof(AgentCommandUI);
            var commandPanelField = uiManagerType.GetField("commandPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var inputFieldField = uiManagerType.GetField("commandInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sendButtonField = uiManagerType.GetField("sendButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var closeButtonField = uiManagerType.GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            commandPanelField?.SetValue(uiManager, panelGO);
            inputFieldField?.SetValue(uiManager, inputField);
            sendButtonField?.SetValue(uiManager, sendButton);
            closeButtonField?.SetValue(uiManager, closeButton);
            
            Debug.Log("Agent Command UI created successfully! Press 'T' to open the command interface.");
            
            return uiManager;
        }
        
        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            
            // Create button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return buttonGO;
        }
    }
}
