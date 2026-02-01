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
        [SerializeField] private GameObject weaponModelPrefab;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform weaponHolder;

        private GameObject currentWeaponInstance;

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

            // Only Hider can use weapon
            if (gamePlayer == null || gamePlayer.role != PlayerRole.Hider) return;

            // If no weapon yet, SPACE tries to pick up weapon
            if (!hasWeapon)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    TryPickupWeapon();
                }
                return;
            }

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

        private void TryPickupWeapon()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, pickupDistance))
            {
                WeaponPickup weaponPickup = hit.collider.GetComponent<WeaponPickup>();
                if (weaponPickup != null)
                {
                    CmdPickupWeapon(weaponPickup.gameObject);
                }
            }
        }

        [Command]
        private void CmdPickupWeapon(GameObject weaponObject)
        {
            if (weaponObject == null) return;

            WeaponPickup pickup = weaponObject.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                // Trigger pickup through IInteractable
                pickup.OnInteract(gameObject);
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
            
            // Calculate muzzle position for visuals
            Vector3 muzzlePos = currentWeaponInstance != null ? currentWeaponInstance.transform.position : (cameraTransform.position + cameraTransform.forward * 0.5f);
            
            CmdShoot(muzzlePos);
        }

        [Command]
        private void CmdShoot(Vector3 muzzlePos)
        {
            if (currentAmmo <= 0) return;

            currentAmmo--;

            // Use camera transform for direction (Server Safe)
            if (cameraTransform != null)
            {
                Vector3 rayOrigin = cameraTransform.position;
                Vector3 rayDir = cameraTransform.forward;
                
                Ray ray = new Ray(rayOrigin, rayDir);
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
                    
                    // Spawn bullet trail using provided muzzle pos
                    RpcSpawnBulletTrail(muzzlePos, hit.point);
                }
                else
                {
                    // Missed everything, shoot into distance
                    Vector3 target = rayOrigin + rayDir * range;
                    RpcSpawnBulletTrail(muzzlePos, target);
                }
            }

            // Show muzzle flash for all
            RpcShowMuzzleFlash();
        }

        [ClientRpc]
        private void RpcSpawnBulletTrail(Vector3 start, Vector3 end)
        {
            if (bulletPrefab != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, start, Quaternion.identity);
                bullet.transform.localScale = Vector3.one * 3f; // Make bullet bigger to be visible
                bullet.transform.LookAt(end);
                
                // Add velocity if it has RB, or just move it
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = (end - start).normalized * 50f; // Fast speed
                }
                else
                {
                    // Simple move coroutine or script
                    StartCoroutine(MoveBullet(bullet, start, end));
                }
                
                Destroy(bullet, 2f);
            }
        }

        private System.Collections.IEnumerator MoveBullet(GameObject bullet, Vector3 start, Vector3 end)
        {
            float duration = 0.1f;
            float elapsed = 0f;
            while (elapsed < duration && bullet != null)
            {
                elapsed += Time.deltaTime;
                bullet.transform.position = Vector3.Lerp(start, end, elapsed / duration);
                yield return null;
            }
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
            
            // Trigger ammo UI update
            if (isLocalPlayer)
            {
                UIEvents.TriggerAmmoChanged(newVal, maxAmmo);
            }
        }

        private void OnWeaponEquipped(bool wasEquipped, bool isEquipped)
        {
            hasWeapon = isEquipped;
            UpdateWeaponVisuals();
            
            // Trigger weapon UI update
            if (isLocalPlayer)
            {
                UIEvents.TriggerWeaponEquipped(isEquipped);
                
                // Also update ammo display
                if (isEquipped)
                {
                    UIEvents.TriggerAmmoChanged(currentAmmo, maxAmmo);
                }
            }
        }

        private void UpdateWeaponVisuals()
        {
            // FPS View: Only show for local player if they are Hider
            if (isLocalPlayer)
            {
                if (hasWeapon)
                {
                    if (currentWeaponInstance == null && weaponModelPrefab != null)
                    {
                        // Use assigned holder or fallback to camera
                        Transform parent = weaponHolder != null ? weaponHolder : cameraTransform;
                        
                        // Clean up any existing children (like placeholder models) in the holder
                        // This fixes the issue where a placeholder weapon (e.g. M4) remains visible
                        if (parent.childCount > 0)
                        {
                            foreach (Transform child in parent)
                            {
                                Destroy(child.gameObject);
                            }
                        }
                        
                        currentWeaponInstance = Instantiate(weaponModelPrefab, parent);
                        
                        // Set layer to match camera's culling mask (usually Default)
                        SetLayerRecursively(currentWeaponInstance, gameObject.layer);

                        // Reset transform to look good in FPS view
                        // Adjusted values for better visibility
                        currentWeaponInstance.transform.localPosition = new Vector3(0.25f, -0.25f, 0.5f); 
                        currentWeaponInstance.transform.localRotation = Quaternion.identity; // Reset rotation first
                        currentWeaponInstance.transform.localScale = Vector3.one; // Reset scale
                    }
                    if (currentWeaponInstance != null) currentWeaponInstance.SetActive(true);
                }
                else
                {
                    if (currentWeaponInstance != null) currentWeaponInstance.SetActive(false);
                }
            }
            // TPS View (for others): Could handle 3rd person model here if we had one
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UpdateWeaponVisuals();
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
        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

    }
}
