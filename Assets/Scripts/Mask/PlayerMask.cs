using UnityEngine;
using Mirror;
using MaskHeist.Core;
using MaskHeist.Mask.Abilities;
using MaskHeist.UI;

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
        
        // Invisibility state (Energy Bar system)
        private InvisibilityEffect invisibilityEffect;
        private float invisibilityMaxEnergy = 5f;  // Total seconds of invisibility
        [SyncVar] private float invisibilityEnergy = 5f;  // Current energy remaining
        private float invisibilityDrainRate = 1f;   // Energy drain per second (1 = real-time)
        private float invisibilityRechargeRate = 0.5f; // Energy recharge per second
        private float minEnergyToActivate = 0.5f;  // Minimum energy needed to toggle on
        
        // Sprint state
        private float sprintDuration = 5f;
        private float sprintCooldown = 30f;
        [SyncVar] private float sprintCooldownEndTime = 0f;
        private float sprintSpeedMultiplier = 1.5f;
        private float sprintDeactivateTime = -1f; // Server-side timer (replaces Invoke)
        private PlayerController playerController;
        private MaskHeistGamePlayer gamePlayer;
        
        // Decoy state
        [SyncVar(hook = nameof(OnDecoyChanged))]
        private bool isDecoyActive = false;
        private float decoyCooldown = 60f;
        [SyncVar] private float decoyCooldownEndTime = 0f;
        private float decoyLifetime = 4f;
        private float decoySpeed = 7f;
        private float decoyDeactivateTime = -1f; // Server-side timer (replaces Invoke)
        
        [Header("Decoy Settings")]
        [SerializeField] private GameObject decoyClonePrefab;
        private GameObject activeDecoyObj; // Track active decoy to manage/destroy it
        
        // Jumper/Dash state
        private float dashForce = 10f;
        private float dashUpwardForce = 12f;
        private float dashCooldown = 4f;
        [SyncVar] private float dashCooldownEndTime = 0f;
        
        private GameObject currentMaskModel;
        private MaskPickup currentMaskPickup;
        
        // Properties
        public MaskData CurrentMask { get; private set; }
        public bool IsInvisible => isInvisible;
        public bool IsSprinting => isSprinting;
        public bool HasMask => CurrentMask != null;
        public MaskPickup CurrentMaskPickup => currentMaskPickup;
        
        public bool IsInvisibilityOnCooldown => invisibilityEnergy < minEnergyToActivate;
        public float InvisibilityCooldownRemaining => 0; // No cooldown, energy-based now
        public float InvisibilityEnergy => invisibilityEnergy;
        public float InvisibilityMaxEnergy => invisibilityMaxEnergy;
        public float InvisibilityEnergyPercent => invisibilityMaxEnergy > 0 ? invisibilityEnergy / invisibilityMaxEnergy : 0;
        
        public bool IsSprintOnCooldown => NetworkTime.time < sprintCooldownEndTime;
        public float SprintCooldownRemaining => Mathf.Max(0, sprintCooldownEndTime - (float)NetworkTime.time);
        
        public bool IsDecoyOnCooldown => NetworkTime.time < decoyCooldownEndTime;
        public float DecoyCooldownRemaining => Mathf.Max(0, decoyCooldownEndTime - (float)NetworkTime.time);
        public bool IsDecoyActive => isDecoyActive;
        
        public bool IsDashOnCooldown => NetworkTime.time < dashCooldownEndTime;
        public float DashCooldownRemaining => Mathf.Max(0, dashCooldownEndTime - (float)NetworkTime.time);
        
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
            
            // Client-side safety: if server says we're invisible but energy is 0, force deactivate
            if (isInvisible && invisibilityEnergy <= 0f)
            {
                Debug.LogWarning("[PlayerMask] Client safety: Enerji 0 ama hala görünmez! Zorla kapatılıyor...");
                CmdDeactivateInvisibility();
            }
            
            // Log Q/E presses BEFORE any guard checks
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log($"[PlayerMask] Q BASILDI! CurrentMask: {CurrentMask?.maskName ?? "NULL"}, " +
                          $"Role: {gamePlayer?.role.ToString() ?? "NULL"}, " +
                          $"AbilityType: {CurrentMask?.uniqueAbilityType.ToString() ?? "N/A"}");
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[PlayerMask] E BASILDI! CurrentMask: {CurrentMask?.maskName ?? "NULL"}, " +
                          $"Role: {gamePlayer?.role.ToString() ?? "NULL"}, Energy: {invisibilityEnergy:F1}/{invisibilityMaxEnergy}");
            }
            
            if (CurrentMask == null)
            {
                if (Time.frameCount % 300 == 0)
                    Debug.Log($"[PlayerMask] CurrentMask is NULL - maske henüz alınmadı veya client'a sync olmadı");
                return;
            }
            
            // Only Seeker can use mask abilities
            if (gamePlayer == null || gamePlayer.role != PlayerRole.Seeker)
            {
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
                    Debug.LogWarning($"[PlayerMask] Yetenek ENGELLENDI! role: {gamePlayer?.role}, sadece Seeker kullanabilir");
                return;
            }
            
            // E = Toggle Invisibility (all masks)
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleInvisibility();
            }
            
            // Q = Unique ability (Sprinter, DecoyMaster, etc.)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log($"[PlayerMask] Q tuşuna basıldı - Unique ability denenecek (Type: {CurrentMask.uniqueAbilityType})");
                TryUseUniqueAbility();
            }
        }
        
        #region Invisibility (Toggle + Energy Bar)
        
        private void ToggleInvisibility()
        {
            if (isInvisible)
            {
                // Turn OFF invisibility
                Debug.Log($"[PlayerMask] Görünmezlik KAPATILIYOR (Kalan enerji: {invisibilityEnergy:F1}s)");
                CmdDeactivateInvisibility();
            }
            else
            {
                // Turn ON invisibility
                if (invisibilityEnergy < minEnergyToActivate)
                {
                    Debug.Log($"[PlayerMask] Yeterli enerji yok! ({invisibilityEnergy:F1}/{minEnergyToActivate}s gerekli)");
                    return;
                }
                Debug.Log($"[PlayerMask] Görünmezlik AÇILIYOR (Enerji: {invisibilityEnergy:F1}/{invisibilityMaxEnergy}s)");
                CmdActivateInvisibility();
            }
        }
        
        [Command]
        public void CmdActivateInvisibility()
        {
            if (isInvisible) return;
            if (invisibilityEnergy < minEnergyToActivate) return;
            
            // Destroy active decoy when going invisible to prevent overlap
            if (activeDecoyObj != null)
            {
                NetworkServer.Destroy(activeDecoyObj);
                activeDecoyObj = null;
                isDecoyActive = false;
                Debug.Log($"[Server] {gameObject.name} decoy destroyed (invisibility activated)");
            }
            
            isInvisible = true;
            Debug.Log($"[Server] {gameObject.name} invisibility ON! Energy: {invisibilityEnergy:F1}");
        }
        
        [Command]
        private void CmdDeactivateInvisibility()
        {
            if (!isInvisible) return;
            
            isInvisible = false;
            Debug.Log($"[Server] {gameObject.name} invisibility OFF! Energy: {invisibilityEnergy:F1}");
        }
        
        /// <summary>
        /// Server-side: drain energy while invisible, recharge when visible.
        /// </summary>
        [ServerCallback]
        private void FixedUpdate()
        {
            // === Invisibility Energy (uses fixedDeltaTime, stays here) ===
            if (isInvisible)
            {
                invisibilityEnergy -= invisibilityDrainRate * Time.fixedDeltaTime;
                
                if (invisibilityEnergy <= 0f)
                {
                    invisibilityEnergy = 0f;
                    isInvisible = false;
                    Debug.Log($"[Server] {gameObject.name} invisibility EXPIRED - enerji bitti!");
                }
            }
            else
            {
                if (invisibilityEnergy < invisibilityMaxEnergy)
                {
                    invisibilityEnergy += invisibilityRechargeRate * Time.fixedDeltaTime;
                    invisibilityEnergy = Mathf.Min(invisibilityEnergy, invisibilityMaxEnergy);
                }
            }
        }
        
        /// <summary>
        /// Server-side: check ability timers.
        /// MUST be in LateUpdate (not FixedUpdate) because timers are set using
        /// Time.time in Command methods which run in Update context.
        /// FixedUpdate uses Time.fixedTime which can diverge from Time.time.
        /// </summary>
        private void LateUpdate()
        {
            if (!isServer) return;
            
            // === Sprint Timer ===
            if (isSprinting && sprintDeactivateTime > 0f && Time.time >= sprintDeactivateTime)
            {
                sprintDeactivateTime = -1f;
                isSprinting = false;
                Debug.Log($"[Server] {gameObject.name} sprint ended!");
            }
            
            // === Decoy Timer ===
            if (isDecoyActive && decoyDeactivateTime > 0f && Time.time >= decoyDeactivateTime)
            {
                decoyDeactivateTime = -1f;
                isDecoyActive = false;
                activeDecoyObj = null;
                Debug.Log($"[Server] {gameObject.name} decoy ended! Cooldown remaining: {DecoyCooldownRemaining:F1}s");
            }
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
                case MaskAbilityType.DecoyMaster:
                    TryUseDecoy();
                    break;
                case MaskAbilityType.Jumper:
                    TryUseDash();
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
            sprintDeactivateTime = Time.time + sprintDuration;
            
            Debug.Log($"[Server] {gameObject.name} activated sprint! Deactivate at: {sprintDeactivateTime}");
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
        
        #region Decoy Ability
        
        private void TryUseDecoy()
        {
            if (isDecoyActive)
            {
                Debug.Log("Decoy already active!");
                return;
            }
            
            if (IsDecoyOnCooldown)
            {
                Debug.Log($"Decoy on cooldown: {DecoyCooldownRemaining:F1}s");
                return;
            }
            
            Debug.Log($"Activating decoy... ({decoyLifetime}s lifetime)");
            CmdActivateDecoy();
        }
        
        [Command]
        public void CmdActivateDecoy()
        {
            if (isDecoyActive) return;
            if (NetworkTime.time < decoyCooldownEndTime) return;
            
            // Destroy any existing decoy first
            if (activeDecoyObj != null)
            {
                NetworkServer.Destroy(activeDecoyObj);
                activeDecoyObj = null;
            }
            
            isDecoyActive = true;
            decoyCooldownEndTime = (float)NetworkTime.time + decoyCooldown;
            decoyDeactivateTime = Time.time + decoyLifetime + 0.5f;
            
            // Spawn decoy clone further in front of the player to prevent overlap
            Vector3 spawnPos = transform.position + transform.forward * 3f;
            Quaternion spawnRot = transform.rotation;
            
            if (decoyClonePrefab != null)
            {
                GameObject decoyObj = Instantiate(decoyClonePrefab, spawnPos, spawnRot);
                NetworkServer.Spawn(decoyObj);
                activeDecoyObj = decoyObj; // Track the decoy
                
                DecoyClone decoy = decoyObj.GetComponent<DecoyClone>();
                if (decoy != null)
                {
                    decoy.Initialize(transform.forward, decoySpeed, decoyLifetime);
                }
                
                Debug.Log($"[Server] {gameObject.name} spawned decoy! Deactivate at: {decoyDeactivateTime}, Cooldown: {decoyCooldown}s");
            }
            else
            {
                Debug.LogWarning("[PlayerMask] Decoy clone prefab not assigned!");
            }
        }
        
        private void OnDecoyChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"OnDecoyChanged: {oldValue} -> {newValue}");
            UIEvents.TriggerDecoyActivated(newValue);
        }
        
        #endregion
        
        #region Jumper/Dash Ability
        
        private void TryUseDash()
        {
            if (IsDashOnCooldown)
            {
                Debug.Log($"Dash on cooldown: {DashCooldownRemaining:F1}s");
                return;
            }
            
            Debug.Log($"Activating dash! (Force: {dashForce}, Up: {dashUpwardForce})");
            CmdActivateDash();
        }
        
        [Command]
        public void CmdActivateDash()
        {
            if (NetworkTime.time < dashCooldownEndTime) return;
            
            dashCooldownEndTime = (float)NetworkTime.time + dashCooldown;
            
            Debug.Log($"[Server] {gameObject.name} activated dash!");
            
            // Apply dash on all clients
            RpcOnDash(dashForce, dashUpwardForce);
        }
        
        [ClientRpc]
        private void RpcOnDash(float force, float upForce)
        {
            Debug.Log($"[RpcOnDash] Dash! force={force}, upForce={upForce}, isLocalPlayer={isLocalPlayer}");
            
            if (playerController != null)
            {
                playerController.ApplyDash(force, upForce);
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
            invisibilityMaxEnergy = maskData.invisibilityDuration;
            invisibilityEnergy = invisibilityMaxEnergy; // Start full
            
            // Configure sprint settings (if applicable)
            if (maskData.uniqueAbilityType == MaskAbilityType.Sprinter)
            {
                sprintDuration = maskData.uniqueAbilityDuration;
                sprintCooldown = maskData.uniqueAbilityCooldown;
                sprintSpeedMultiplier = maskData.speedMultiplier;
            }
            
            // Configure decoy settings (if applicable)
            if (maskData.uniqueAbilityType == MaskAbilityType.DecoyMaster)
            {
                decoyLifetime = maskData.decoyLifetime;
                decoySpeed = maskData.decoySpeed;
                decoyCooldown = maskData.uniqueAbilityCooldown;
            }
            
            // Configure jumper/dash settings (if applicable)
            if (maskData.uniqueAbilityType == MaskAbilityType.Jumper)
            {
                dashForce = maskData.dashForce;
                dashUpwardForce = maskData.dashUpwardForce;
                dashCooldown = maskData.uniqueAbilityCooldown;
            }
            
            SpawnMaskModel(maskData);
            
            Debug.Log($"Applied mask: {maskData.maskName} (InvisEnergy: {invisibilityMaxEnergy}s, Unique: {maskData.uniqueAbilityType})");
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
            // Now returns energy fill percent (1 = full, 0 = empty)
            return InvisibilityEnergyPercent;
        }
        
        public bool IsInvisibilityReady()
        {
            return invisibilityEnergy >= minEnergyToActivate;
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
        
        public float GetDecoyCooldownPercent()
        {
            if (decoyCooldown <= 0) return 0;
            return Mathf.Clamp01(DecoyCooldownRemaining / decoyCooldown);
        }
        
        public bool IsDecoyReady()
        {
            return !isDecoyActive && !IsDecoyOnCooldown;
        }
        
        #endregion
    }
}
