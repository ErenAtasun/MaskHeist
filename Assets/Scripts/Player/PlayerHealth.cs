using Mirror;
using UnityEngine;
using MaskHeist.Core;

namespace MaskHeist.Player
{
    /// <summary>
    /// Player health component - handles death and spectator transition.
    /// Seekers have 1 HP - instant death when shot.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnDeadChanged))]
        private bool isDead = false;

        [Header("References")]
        [SerializeField] private GameObject[] disableOnDeath;
        [SerializeField] private MonoBehaviour[] disableScriptsOnDeath;

        private MaskHeistGamePlayer gamePlayer;
        private SpectatorController spectator;
        private PlayerController playerController;

        public bool IsDead => isDead;

        private void Awake()
        {
            gamePlayer = GetComponent<MaskHeistGamePlayer>();
            spectator = GetComponent<SpectatorController>();
            playerController = GetComponent<PlayerController>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            // Ensure spectator starts disabled
            if (spectator != null)
                spectator.enabled = false;
        }

        /// <summary>
        /// Called by WeaponController when player is shot.
        /// </summary>
        [Server]
        public void ServerDie()
        {
            if (isDead) return;

            // Only Seekers can die
            if (gamePlayer != null && gamePlayer.role != PlayerRole.Seeker) return;

            isDead = true;
            Debug.Log($"[PlayerHealth] {gamePlayer?.displayName ?? "Player"} öldü!");

            // Notify all clients
            RpcOnDeath();
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            Debug.Log($"[PlayerHealth] Ölüm bildirimi alındı: {gameObject.name}");
        }

        private void OnDeadChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            Debug.Log($"[PlayerHealth] HandleDeath called for {gameObject.name}, isLocalPlayer: {isLocalPlayer}");

            // Disable movement and visuals
            if (playerController != null)
                playerController.enabled = false;

            // Disable objects
            foreach (var obj in disableOnDeath)
            {
                if (obj != null) obj.SetActive(false);
            }

            // Disable scripts
            foreach (var script in disableScriptsOnDeath)
            {
                if (script != null) script.enabled = false;
            }

            // Hide player model (for others)
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }

            // Enable spectator mode for local player
            if (isLocalPlayer)
            {
                EnableSpectatorMode();
            }
        }

        private void EnableSpectatorMode()
        {
            Debug.Log("[PlayerHealth] Seyirci moduna geçiliyor...");

            if (spectator != null)
            {
                spectator.enabled = true;
                spectator.StartSpectating();
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] SpectatorController bulunamadı!");
            }
        }

        /// <summary>
        /// Respawn player (for next round).
        /// </summary>
        [Server]
        public void ServerRespawn()
        {
            isDead = false;
        }
    }
}
