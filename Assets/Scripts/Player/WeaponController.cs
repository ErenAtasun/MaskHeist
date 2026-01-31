using Mirror;
using UnityEngine;
using MaskHeist.Core;
using MaskHeist.UI;

namespace MaskHeist.Player
{
    /// <summary>
    /// Weapon controller for Hider - shoot Seekers with limited ammo.
    /// SPACE to shoot, Right-click to pickup ammo.
    /// </summary>
    public class WeaponController : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private int startingAmmo = 3;
        [SerializeField] private int maxAmmo = 10;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private float range = 50f;
        [SerializeField] private float pickupDistance = 3f;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private LayerMask shootableLayer;
        [SerializeField] private LayerMask ammoLayer;

        [Header("Effects")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject hitEffectPrefab;

        [SyncVar(hook = nameof(OnAmmoChanged))]
        private int currentAmmo;

        [SyncVar(hook = nameof(OnWeaponEquipped))]
        private bool hasWeapon = false;

        private float nextFireTime;
        private Camera playerCamera;
        private MaskHeistGamePlayer gamePlayer;
        private ItemPlacementController itemPlacement;

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public bool HasWeapon => hasWeapon;

        private void Awake()
        {
            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;

            playerCamera = cameraTransform?.GetComponent<Camera>();
            gamePlayer = GetComponent<MaskHeistGamePlayer>();
            itemPlacement = GetComponent<ItemPlacementController>();

            if (shootableLayer == 0)
                shootableLayer = LayerMask.GetMask("Default", "Player");
            if (ammoLayer == 0)
                ammoLayer = LayerMask.GetMask("Ammo", "Default");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Don't give ammo at start - needs to pick up weapon first
            currentAmmo = 0;
            hasWeapon = false;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // Only Hider can shoot
            if (gamePlayer == null || gamePlayer.role != PlayerRole.Hider) return;

            // Must have weapon to use
            if (!hasWeapon) return;

            // Check for ammo pickup (Right-click)
            if (Input.GetMouseButtonDown(1))
            {
                TryPickupAmmo();
            }

            // Shoot (SPACE) - only if not holding item
            bool isHoldingItem = itemPlacement != null && itemPlacement.IsHoldingItem;
            if (!isHoldingItem && Input.GetKeyDown(KeyCode.Space))
            {
                TryShoot();
            }
        }

        private void TryShoot()
        {
            if (Time.time < nextFireTime) return;
            if (currentAmmo <= 0)
            {
                Debug.Log("[Weapon] Mermi yok!");
                return;
            }

            nextFireTime = Time.time + fireRate;
            CmdShoot();
        }

        [Command]
        private void CmdShoot()
        {
            if (currentAmmo <= 0) return;

            currentAmmo--;

            // Raycast from camera
            if (playerCamera != null)
            {
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, range, shootableLayer))
                {
                    Debug.Log($"[Weapon] Hit: {hit.collider.name}");

                    // Check if hit a player
                    PlayerHealth health = hit.collider.GetComponent<PlayerHealth>();
                    if (health == null)
                        health = hit.collider.GetComponentInParent<PlayerHealth>();

                    if (health != null)
                    {
                        // Check if it's a Seeker
                        MaskHeistGamePlayer targetPlayer = health.GetComponent<MaskHeistGamePlayer>();
                        if (targetPlayer != null && targetPlayer.role == PlayerRole.Seeker)
                        {
                            health.ServerDie();
                            Debug.Log($"[Weapon] Killed Seeker: {targetPlayer.displayName}");
                        }
                    }

                    // Spawn hit effect
                    RpcShowHitEffect(hit.point, hit.normal);
                }
            }

            // Show muzzle flash for all
            RpcShowMuzzleFlash();
        }

        [ClientRpc]
        private void RpcShowMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && cameraTransform != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, cameraTransform.position + cameraTransform.forward * 0.5f, cameraTransform.rotation);
                Destroy(flash, 0.1f);
            }

            // Play sound here if needed
            Debug.Log("[Weapon] BANG!");
        }

        [ClientRpc]
        private void RpcShowHitEffect(Vector3 position, Vector3 normal)
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(effect, 2f);
            }
        }

        private void TryPickupAmmo()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, pickupDistance, ammoLayer))
            {
                AmmoPickup ammo = hit.collider.GetComponent<AmmoPickup>();
                if (ammo != null)
                {
                    CmdPickupAmmo(ammo.gameObject);
                }
            }
        }

        [Command]
        private void CmdPickupAmmo(GameObject ammoObject)
        {
            if (ammoObject == null) return;

            AmmoPickup ammo = ammoObject.GetComponent<AmmoPickup>();
            if (ammo != null && currentAmmo < maxAmmo)
            {
                int ammoToAdd = ammo.AmmoAmount;
                currentAmmo = Mathf.Min(currentAmmo + ammoToAdd, maxAmmo);
                
                Debug.Log($"[Weapon] Picked up {ammoToAdd} ammo. Total: {currentAmmo}");
                
                NetworkServer.Destroy(ammoObject);
            }
        }

        [Server]
        public void AddAmmo(int amount)
        {
            currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        }

        private void OnAmmoChanged(int oldVal, int newVal)
        {
            Debug.Log($"[Weapon] Ammo: {newVal}/{maxAmmo}");
            // Update UI here if needed
            UIEvents.TriggerScoreChanged(newVal, newVal - oldVal); // Temporary - use ammo UI later
        }

        private void OnWeaponEquipped(bool oldVal, bool newVal)
        {
            if (newVal && isLocalPlayer)
            {
                Debug.Log("[Weapon] ========== SİLAH ALINDI! SPACE ile ateş et ==========");
            }
        }

        /// <summary>
        /// Called by WeaponPickup when player picks up the weapon.
        /// </summary>
        [Server]
        public void ServerEquipWeapon(int startAmmo)
        {
            hasWeapon = true;
            currentAmmo = startAmmo;
            Debug.Log($"[Weapon] Weapon equipped with {startAmmo} ammo");
        }
    }
}
