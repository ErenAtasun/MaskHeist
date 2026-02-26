using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MaskHeist.Mask;
using Mirror;

namespace MaskHeist.UI
{
    /// <summary>
    /// UI Panel that allows Seeker players to select a mask during Briefing phase.
    /// Dynamically creates mask buttons from MaskRegistry and lets the player
    /// confirm a selection, which is sent to the server via PlayerMask.CmdSelectMask().
    /// </summary>
    public class MaskSelectionPanel : BaseUIPanel
    {
        [Header("Mask Selection UI")]
        [SerializeField] private Transform maskButtonContainer;
        [SerializeField] private GameObject maskButtonPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI abilityText;
        [SerializeField] private Image selectedMaskIcon;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 0.8f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.5f, 0.9f);
        
        private int selectedMaskIndex = -1;
        private List<GameObject> spawnedButtons = new List<GameObject>();
        private List<Image> buttonBackgrounds = new List<Image>();
        
        protected override void Awake()
        {
            base.Awake();
            
            // Subscribe to events
            UIEvents.OnShowMaskSelection += HandleShowMaskSelection;
            
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
                confirmButton.interactable = false;
            }
        }
        
        private void OnDestroy()
        {
            UIEvents.OnShowMaskSelection -= HandleShowMaskSelection;
        }
        
        private void HandleShowMaskSelection(bool show)
        {
            if (show)
                Show();
            else
                Hide();
        }
        
        public override void Show()
        {
            base.Show();
            PopulateMasks();
            
            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (titleText != null)
                titleText.text = "MASKE SEÇ";
            
            if (descriptionText != null)
                descriptionText.text = "Tur için bir maske seçin. Her maskenin görünmezlik yeteneği ortaktır ve ek özel bir yeteneği vardır.";
            
            OnShow();
        }
        
        public override void Hide()
        {
            base.Hide();
            
            // Re-lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            OnHide();
        }
        
        /// <summary>
        /// Dynamically create buttons for each available mask from MaskRegistry.
        /// </summary>
        private void PopulateMasks()
        {
            // Clear existing buttons
            ClearButtons();
            
            if (MaskRegistry.Instance == null)
            {
                Debug.LogWarning("[MaskSelectionPanel] MaskRegistry not found!");
                return;
            }
            
            var masks = MaskRegistry.Instance.AvailableMasks;
            
            for (int i = 0; i < masks.Count; i++)
            {
                CreateMaskButton(masks[i], i);
            }
            
            // Reset selection
            selectedMaskIndex = -1;
            if (confirmButton != null)
                confirmButton.interactable = false;
            
            UpdateDetailPanel(null);
        }
        
        private void CreateMaskButton(MaskData maskData, int index)
        {
            if (maskButtonPrefab == null || maskButtonContainer == null)
            {
                Debug.LogWarning("[MaskSelectionPanel] Button prefab or container not assigned!");
                return;
            }
            
            GameObject buttonObj = Instantiate(maskButtonPrefab, maskButtonContainer);
            spawnedButtons.Add(buttonObj);
            
            // Setup button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = maskData.maskName;
            }
            
            // Setup button icon (if there's an Image child named "Icon")
            Transform iconTransform = buttonObj.transform.Find("Icon");
            if (iconTransform != null && maskData.icon != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = maskData.icon;
                    iconImage.color = maskData.maskColor;
                }
            }
            
            // Track background for highlighting
            Image bg = buttonObj.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = normalColor;
                buttonBackgrounds.Add(bg);
            }
            
            // Setup click handler
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                int capturedIndex = index; // Capture for lambda
                btn.onClick.AddListener(() => OnMaskButtonClicked(capturedIndex));
            }
        }
        
        private void OnMaskButtonClicked(int index)
        {
            selectedMaskIndex = index;
            
            // Update visual highlight
            for (int i = 0; i < buttonBackgrounds.Count; i++)
            {
                if (buttonBackgrounds[i] != null)
                {
                    buttonBackgrounds[i].color = (i == index) ? selectedColor : normalColor;
                }
            }
            
            // Enable confirm button
            if (confirmButton != null)
                confirmButton.interactable = true;
            
            // Update detail panel
            if (MaskRegistry.Instance != null)
            {
                MaskData mask = MaskRegistry.Instance.GetMask(index);
                UpdateDetailPanel(mask);
            }
            
            Debug.Log($"[MaskSelectionPanel] Mask selected: index={index}");
        }
        
        private void UpdateDetailPanel(MaskData mask)
        {
            if (mask != null)
            {
                if (descriptionText != null)
                    descriptionText.text = mask.description;
                
                if (abilityText != null)
                    abilityText.text = GetAbilityDescription(mask.uniqueAbilityType);
                
                if (selectedMaskIcon != null && mask.icon != null)
                {
                    selectedMaskIcon.sprite = mask.icon;
                    selectedMaskIcon.color = mask.maskColor;
                    selectedMaskIcon.enabled = true;
                }
            }
            else
            {
                if (descriptionText != null)
                    descriptionText.text = "Tur için bir maske seçin. Her maskenin görünmezlik yeteneği ortaktır ve ek özel bir yeteneği vardır.";
                
                if (abilityText != null)
                    abilityText.text = "";
                
                if (selectedMaskIcon != null)
                    selectedMaskIcon.enabled = false;
            }
        }
        
        private string GetAbilityDescription(MaskAbilityType abilityType)
        {
            switch (abilityType)
            {
                case MaskAbilityType.None:
                    return "Özel yetenek yok — Sadece görünmezlik";
                case MaskAbilityType.Tracker:
                    return "İz Sürücü — Saklayanın ayak izlerini gösterir";
                case MaskAbilityType.Scanner:
                    return "Tarayıcı — Eşya ve tuzakları kısa menzilde tarar";
                case MaskAbilityType.Sprinter:
                    return "Koşucu — Kısa süreli hız patlaması sağlar";
                case MaskAbilityType.Silent:
                    return "Sessiz — Ayak sesi menzilini azaltır";
                case MaskAbilityType.Disruptor:
                    return "Bozucu — Yakındaki tuzakları devre dışı bırakır";
                case MaskAbilityType.DecoyMaster:
                    return "Yanıltıcı — Holografik kopya gönderir";
                default:
                    return "Bilinmeyen yetenek";
            }
        }
        
        private void OnConfirmClicked()
        {
            if (selectedMaskIndex < 0) return;
            
            // Find local player's PlayerMask and send selection
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer != null)
            {
                PlayerMask playerMask = localPlayer.GetComponent<PlayerMask>();
                if (playerMask != null)
                {
                    playerMask.CmdSelectMask(selectedMaskIndex);
                    Debug.Log($"[MaskSelectionPanel] Mask confirmed: index={selectedMaskIndex}");
                    
                    // Also equip directly
                    MaskData selectedMask = MaskRegistry.Instance?.GetMask(selectedMaskIndex);
                    if (selectedMask != null)
                    {
                        playerMask.EquipMaskDirect(selectedMask);
                    }
                }
            }
            
            // Fire UI event
            UIEvents.TriggerMaskSelected(selectedMaskIndex);
            
            // Hide panel after selection
            Hide();
        }
        
        /// <summary>
        /// Auto-select first mask if player doesn't choose (called when Briefing ends).
        /// </summary>
        public void AutoSelectDefault()
        {
            if (selectedMaskIndex < 0 && MaskRegistry.Instance != null)
            {
                var masks = MaskRegistry.Instance.AvailableMasks;
                if (masks.Count > 0)
                {
                    selectedMaskIndex = 0;
                    OnConfirmClicked();
                    Debug.Log("[MaskSelectionPanel] Auto-selected default mask (index 0)");
                }
            }
        }
        
        private void ClearButtons()
        {
            foreach (var btn in spawnedButtons)
            {
                if (btn != null) Destroy(btn);
            }
            spawnedButtons.Clear();
            buttonBackgrounds.Clear();
        }
    }
}
