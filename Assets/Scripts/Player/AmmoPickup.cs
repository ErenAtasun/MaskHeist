using Mirror;
using UnityEngine;
using MaskHeist.Interaction;

namespace MaskHeist.Player
{
    /// <summary>
    /// Ammo pickup that can be collected by Hider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AmmoPickup : NetworkBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private int ammoAmount = 2;

        [Header("Visuals")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private float rotateSpeed = 50f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;

        private Vector3 startPos;

        public int AmmoAmount => ammoAmount;
        public string InteractionPrompt => $"[Sağ Tık] Mermi Al (+{ammoAmount})";

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            // Visual effects - rotate and bob
            if (visualModel != null)
            {
                visualModel.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            }

            // Bob up and down
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        public bool CanInteract(GameObject interactor)
        {
            // Only Hider can pick up ammo
            var gamePlayer = interactor.GetComponent<MaskHeist.Core.MaskHeistGamePlayer>();
            if (gamePlayer != null && gamePlayer.role == MaskHeist.Core.PlayerRole.Hider)
            {
                var weapon = interactor.GetComponent<WeaponController>();
                return weapon != null && weapon.CurrentAmmo < weapon.MaxAmmo;
            }
            return false;
        }

        public void OnInteract(GameObject interactor)
        {
            // Handled by WeaponController.CmdPickupAmmo
        }
    }
}
