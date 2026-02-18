using UnityEngine;
using Mirror;
using MaskHeist.Core;

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
        private MaskHeistGamePlayer gamePlayer;
        
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
            gamePlayer = GetComponent<MaskHeistGamePlayer>();
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
            
            if (CurrentMask == null)
            {
                // Sadece ara sıra logla (spam önleme)
                if (Time.frameCount % 300 == 0)
                    Debug.Log($"[PlayerMask] CurrentMask is NULL - maske henüz alınmadı veya client'a sync olmadı");
                return;
            }
            
            // Only Seeker can use mask abilities
            if (gamePlayer == null || gamePlayer.role != PlayerRole.Seeker)
            {
                if (Time.frameCount % 300 == 0)
                    Debug.Log($"[PlayerMask] Yetenek kullanılamaz - role: {gamePlayer?.role}, sadece Seeker kullanabilir");
                return;
            }
            
            // E = Invisibility (all masks)
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[PlayerMask] E tuşuna basıldı - Invisibility denenecek (Mask: {CurrentMask.maskName})");
                TryUseInvisibility();
            }
            
            // Q = Unique ability (Sprinter, etc.)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log($"[PlayerMask] Q tuşuna basıldı - Unique ability denenecek (Type: {CurrentMask.uniqueAbilityType})");
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
            Debug.Log($"EquipMaskDirect called - maskData: {maskData?.maskName}, pickup: {pickup}, isServer: {isServer}");
            
            if (maskData != null)
            {
                ReturnCurrentMask();
                currentMaskPickup = pickup;
                
                // Sunucuda maske uygula
                ApplyMask(maskData);
                
                // Tüm client'lara maskeyi sync et (isim üzerinden)
                if (isServer)
                {
                    RpcApplyMask(maskData.maskName);
                }
            }
            else
            {
                Debug.LogWarning("EquipMaskDirect: maskData is NULL!");
            }
        }
        
        /// <summary>
        /// Client'larda maskeyi isim üzerinden bulup uygular.
        /// Server zaten ApplyMask çağırdığı için server'da tekrar çağırmayız.
        /// </summary>
        [ClientRpc]
        private void RpcApplyMask(string maskName)
        {
            Debug.Log($"[PlayerMask] RpcApplyMask called - maskName: {maskName}, isServer: {isServer}, CurrentMask: {CurrentMask?.maskName}");
            
            // Server zaten EquipMaskDirect içinde ApplyMask çağırdı, tekrar çağırmaya gerek yok
            if (isServer) return;
            
            // Client tarafında maskeyi bul ve uygula
            MaskData maskToApply = null;
            
            // 1. Önce MaskRegistry'den ara
            if (MaskRegistry.Instance != null)
            {
                maskToApply = MaskRegistry.Instance.GetMaskByName(maskName);
            }
            
            // 2. Registry'de bulunamadıysa, sahnedeki MaskPickup'lardan ara
            if (maskToApply == null)
            {
                foreach (var pickup in FindObjectsOfType<MaskPickup>())
                {
                    if (pickup.MaskData != null && pickup.MaskData.maskName == maskName)
                    {
                        maskToApply = pickup.MaskData;
                        break;
                    }
                }
            }
            
            // 3. Son çare: defaultMask kullan
            if (maskToApply == null && defaultMask != null)
            {
                Debug.LogWarning($"[PlayerMask] Mask '{maskName}' not found, using defaultMask");
                maskToApply = defaultMask;
            }
            
            if (maskToApply != null)
            {
                ApplyMask(maskToApply);
                Debug.Log($"[PlayerMask] Client mask applied: {maskToApply.maskName}");
            }
            else
            {
                Debug.LogError($"[PlayerMask] Could not find any mask to apply on client! maskName: {maskName}");
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
            Debug.Log($"SpawnMaskModel - maskAttachPoint: {maskAttachPoint}");
            
            // Önce eski maskeyi temizle
            if (currentMaskModel != null)
            {
                currentMaskModel.SetActive(false);
            }
            
            if (maskAttachPoint == null)
            {
                Debug.LogWarning("SpawnMaskModel: maskAttachPoint is NULL! Set it in PlayerMask component.");
                return;
            }
            
            // maskAttachPoint'te zaten bir child maske var mı kontrol et
            if (maskAttachPoint.childCount > 0)
            {
                // Mevcut child'ı kullan (başta gizli olan maske)
                currentMaskModel = maskAttachPoint.GetChild(0).gameObject;
                currentMaskModel.SetActive(true);
                Debug.Log($"Mask model activated: {currentMaskModel.name}");
            }
            else if (maskData.maskPrefab != null)
            {
                // Child yoksa prefab'dan oluştur
                currentMaskModel = Instantiate(maskData.maskPrefab, maskAttachPoint);
                currentMaskModel.transform.localPosition = Vector3.zero;
                currentMaskModel.transform.localRotation = Quaternion.identity;
                Debug.Log($"Mask model spawned: {currentMaskModel.name}");
            }
            else
            {
                Debug.LogWarning("SpawnMaskModel: No child mask and maskPrefab is NULL in MaskData!");
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
