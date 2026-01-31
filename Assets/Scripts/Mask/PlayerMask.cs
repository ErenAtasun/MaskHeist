using UnityEngine;
using Mirror;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Main component that handles player's mask selection and abilities.
    /// All network logic for abilities is handled here.
    /// </summary>
    public class PlayerMask : NetworkBehaviour
    {
        [Header("Mask Settings")]
        [SerializeField] private MaskData defaultMask;
        
        [Header("References")]
        [SerializeField] private Transform maskAttachPoint;
        
        [Header("Current State")]
        [SyncVar(hook = nameof(OnMaskChanged))]
        private int selectedMaskIndex = -1;
        
        [SyncVar(hook = nameof(OnInvisibilityChanged))]
        private bool isInvisible = false;
        
        [SyncVar(hook = nameof(OnSprintChanged))]
        private bool isSprinting = false;
        
        // Invisibility state
        private InvisibilityEffect invisibilityEffect;
        private float invisibilityDuration = 10f;
        private float invisibilityCooldown = 45f;
        private float invisibilityCooldownEndTime = 0f;
        
        // Sprint state
        private float sprintDuration = 5f;
        private float sprintCooldown = 30f;
        private float sprintCooldownEndTime = 0f;
        private float sprintSpeedMultiplier = 1.5f;
        private PlayerController playerController;
        
        private GameObject currentMaskModel;
        private MaskPickup currentMaskPickup;
        
        // Properties
        public MaskData CurrentMask { get; private set; }
        public bool IsInvisible => isInvisible;
        public bool IsSprinting => isSprinting;
        public bool HasMask => CurrentMask != null;
        public MaskPickup CurrentMaskPickup => currentMaskPickup;
        
        public bool IsInvisibilityOnCooldown => NetworkTime.time < invisibilityCooldownEndTime;
        public float InvisibilityCooldownRemaining => Mathf.Max(0, invisibilityCooldownEndTime - (float)NetworkTime.time);
        
        public bool IsSprintOnCooldown => NetworkTime.time < sprintCooldownEndTime;
        public float SprintCooldownRemaining => Mathf.Max(0, sprintCooldownEndTime - (float)NetworkTime.time);
        
        private void Awake()
        {
            invisibilityEffect = gameObject.AddComponent<InvisibilityEffect>();
            playerController = GetComponent<PlayerController>();
        }
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            invisibilityEffect?.Initialize(true);
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isLocalPlayer)
            {
                invisibilityEffect?.Initialize(false);
            }
        }
        
        private void Update()
        {
            if (netIdentity == null) return;
            if (!isLocalPlayer) return;
            if (CurrentMask == null) return;
            
            // E = Invisibility (all masks)
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryUseInvisibility();
            }
            
            // Q = Unique ability (Sprinter, etc.)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TryUseUniqueAbility();
            }
        }
        
        #region Invisibility
        
        private void TryUseInvisibility()
        {
            if (isInvisible)
            {
                Debug.Log("Already invisible!");
                return;
            }
            
            if (IsInvisibilityOnCooldown)
            {
                Debug.Log($"Invisibility on cooldown: {InvisibilityCooldownRemaining:F1}s");
                return;
            }
            
            Debug.Log($"Activating invisibility... ({invisibilityDuration}s)");
            CmdActivateInvisibility();
        }
        
        [Command]
        public void CmdActivateInvisibility()
        {
            if (isInvisible) return;
            if (NetworkTime.time < invisibilityCooldownEndTime) return;
            
            isInvisible = true;
            invisibilityCooldownEndTime = (float)NetworkTime.time + invisibilityCooldown;
            
            Debug.Log($"[Server] {gameObject.name} activated invisibility!");
            Invoke(nameof(ServerDeactivateInvisibility), invisibilityDuration);
        }
        
        [Server]
        private void ServerDeactivateInvisibility()
        {
            isInvisible = false;
            Debug.Log($"[Server] {gameObject.name} invisibility ended!");
        }
        
        private void OnInvisibilityChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"OnInvisibilityChanged: {oldValue} -> {newValue}");
            invisibilityEffect?.SetInvisible(newValue);
        }
        
        #endregion
        
        #region Unique Ability (Sprinter)
        
        private void TryUseUniqueAbility()
        {
            if (CurrentMask == null) return;
            
            if (CurrentMask.uniqueAbilityType == MaskAbilityType.None)
            {
                Debug.Log("This mask has no unique ability (Q)");
                return;
            }
            
            switch (CurrentMask.uniqueAbilityType)
            {
                case MaskAbilityType.Sprinter:
                    TryUseSprint();
                    break;
                default:
                    Debug.Log($"Ability {CurrentMask.uniqueAbilityType} not implemented yet");
                    break;
            }
        }
        
        private void TryUseSprint()
        {
            if (isSprinting)
            {
                Debug.Log("Already sprinting!");
                return;
            }
            
            if (IsSprintOnCooldown)
            {
                Debug.Log($"Sprint on cooldown: {SprintCooldownRemaining:F1}s");
                return;
            }
            
            Debug.Log($"Activating sprint... ({sprintDuration}s at {sprintSpeedMultiplier}x speed)");
            CmdActivateSprint();
        }
        
        [Command]
        public void CmdActivateSprint()
        {
            if (isSprinting) return;
            if (NetworkTime.time < sprintCooldownEndTime) return;
            
            isSprinting = true;
            sprintCooldownEndTime = (float)NetworkTime.time + sprintCooldown;
            
            Debug.Log($"[Server] {gameObject.name} activated sprint!");
            Invoke(nameof(ServerDeactivateSprint), sprintDuration);
        }
        
        [Server]
        private void ServerDeactivateSprint()
        {
            isSprinting = false;
            Debug.Log($"[Server] {gameObject.name} sprint ended!");
        }
        
        private void OnSprintChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"OnSprintChanged: {oldValue} -> {newValue}");
            
            if (playerController != null)
            {
                if (newValue)
                {
                    playerController.SetSpeedMultiplier(sprintSpeedMultiplier);
                }
                else
                {
                    playerController.ResetSpeedMultiplier();
                }
            }
        }
        
        #endregion
        
        #region Mask Selection
        
        [Command]
        public void CmdSelectMask(int maskIndex)
        {
            selectedMaskIndex = maskIndex;
        }
        
        public void EquipMaskDirect(MaskData maskData, MaskPickup pickup = null)
        {
            if (maskData != null)
            {
                ReturnCurrentMask();
                currentMaskPickup = pickup;
                ApplyMask(maskData);
            }
        }
        
        public void ReturnCurrentMask()
        {
            if (currentMaskPickup != null)
            {
                currentMaskPickup.CmdResetMask();
                currentMaskPickup = null;
            }
        }
        
        private void OnMaskChanged(int oldIndex, int newIndex)
        {
            MaskData maskToApply = null;
            
            if (MaskRegistry.Instance != null && newIndex >= 0)
            {
                maskToApply = MaskRegistry.Instance.GetMask(newIndex);
            }
            
            if (maskToApply == null && defaultMask != null)
            {
                maskToApply = defaultMask;
            }
            
            if (maskToApply != null)
            {
                ApplyMask(maskToApply);
            }
        }
        
        private void ApplyMask(MaskData maskData)
        {
            CurrentMask = maskData;
            
            // Configure invisibility settings
            invisibilityDuration = maskData.invisibilityDuration;
            invisibilityCooldown = maskData.invisibilityCooldown;
            
            // Configure sprint settings (if applicable)
            if (maskData.uniqueAbilityType == MaskAbilityType.Sprinter)
            {
                sprintDuration = maskData.uniqueAbilityDuration;
                sprintCooldown = maskData.uniqueAbilityCooldown;
                sprintSpeedMultiplier = maskData.speedMultiplier;
            }
            
            SpawnMaskModel(maskData);
            
            Debug.Log($"Applied mask: {maskData.maskName} (Invis: {invisibilityDuration}s, Unique: {maskData.uniqueAbilityType})");
        }
        
        private void SpawnMaskModel(MaskData maskData)
        {
            if (currentMaskModel != null)
            {
                Destroy(currentMaskModel);
            }
            
            if (maskData.maskPrefab != null && maskAttachPoint != null)
            {
                currentMaskModel = Instantiate(maskData.maskPrefab, maskAttachPoint);
                currentMaskModel.transform.localPosition = Vector3.zero;
                currentMaskModel.transform.localRotation = Quaternion.identity;
            }
        }
        
        #endregion
        
        #region UI Helpers
        
        public float GetInvisibilityCooldownPercent()
        {
            if (invisibilityCooldown <= 0) return 0;
            return Mathf.Clamp01(InvisibilityCooldownRemaining / invisibilityCooldown);
        }
        
        public bool IsInvisibilityReady()
        {
            return !isInvisible && !IsInvisibilityOnCooldown;
        }
        
        public float GetSprintCooldownPercent()
        {
            if (sprintCooldown <= 0) return 0;
            return Mathf.Clamp01(SprintCooldownRemaining / sprintCooldown);
        }
        
        public bool IsSprintReady()
        {
            return !isSprinting && !IsSprintOnCooldown;
        }
        
        #endregion
    }
}
