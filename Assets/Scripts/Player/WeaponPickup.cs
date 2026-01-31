using Mirror;
using UnityEngine;
using MaskHeist.Core;
using MaskHeist.Interaction;

namespace MaskHeist.Player
{
    /// <summary>
    /// Weapon pickup on the ground - only Hider can pick up.
    /// Press E to collect. Gives weapon + starting ammo.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : NetworkBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private int ammoOnPickup = 3;

        [Header("Visuals")]
        [SerializeField] private GameObject weaponModel;
        [SerializeField] private float rotateSpeed = 30f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.15f;

        [SyncVar(hook = nameof(OnPickedUpChanged))]
        private bool isPickedUp = false;

        private Vector3 startPos;
        private Collider col;

        public string InteractionPrompt => "[SPACE] Silahı Al";

        private void Awake()
        {
            col = GetComponent<Collider>();
        }

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            if (isPickedUp) return;

            // Visual effects - rotate and bob
            if (weaponModel != null)
            {
                weaponModel.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            }

            // Bob up and down
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPos.x, newY, startPos.z);
        }

        public bool CanInteract(GameObject interactor)
        {
            if (isPickedUp) return false;

            // Only Hider can pick up weapon
            var gamePlayer = interactor.GetComponent<MaskHeistGamePlayer>();
            if (gamePlayer == null || gamePlayer.role != PlayerRole.Hider) return false;

            // Check if already has weapon
            var weapon = interactor.GetComponent<WeaponController>();
            if (weapon != null && weapon.HasWeapon) return false;

            return true;
        }

        public void OnInteract(GameObject interactor)
        {
            // This is called from server (via PlayerInteraction.CmdInteract)
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[WeaponPickup] OnInteract called on client!");
                return;
            }

            if (isPickedUp) return;

            var weapon = interactor.GetComponent<WeaponController>();
            if (weapon == null)
            {
                Debug.LogWarning("[WeaponPickup] No WeaponController on interactor!");
                return;
            }

            // Give weapon to player
            weapon.ServerEquipWeapon(ammoOnPickup);
            isPickedUp = true;

            Debug.Log($"[WeaponPickup] {interactor.name} silahı aldı!");

            // Destroy after short delay (for sync)
            Invoke(nameof(ServerDestroy), 0.1f);
        }

        [Server]
        private void ServerDestroy()
        {
            NetworkServer.Destroy(gameObject);
        }

        private void OnPickedUpChanged(bool oldVal, bool newVal)
        {
            // Hide visuals when picked up
            if (weaponModel != null)
                weaponModel.SetActive(!newVal);

            if (col != null)
                col.enabled = !newVal;
        }
    }
}
