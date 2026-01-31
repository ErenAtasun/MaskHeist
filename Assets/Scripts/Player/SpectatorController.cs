using Mirror;
using UnityEngine;
using System.Collections.Generic;
using MaskHeist.Core;

namespace MaskHeist.Player
{
    /// <summary>
    /// Spectator controller - allows dead players to watch other Seekers.
    /// Tab or Arrow keys to switch between players.
    /// </summary>
    public class SpectatorController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float followDistance = 5f;
        [SerializeField] private float followHeight = 2f;
        [SerializeField] private float followSmoothness = 5f;

        [Header("UI")]
        [SerializeField] private string spectatingPrefix = "İzleniyor: ";

        private List<MaskHeistGamePlayer> aliveSeekersCache = new List<MaskHeistGamePlayer>();
        private int currentTargetIndex = 0;
        private Transform currentTarget;
        private Camera spectatorCamera;
        private bool isSpectating = false;

        private void Awake()
        {
            spectatorCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (!isLocalPlayer || !isSpectating) return;

            // Handle input to switch targets
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                SwitchToNextTarget();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SwitchToPreviousTarget();
            }

            // Follow current target
            FollowTarget();
        }

        /// <summary>
        /// Start spectating other players.
        /// </summary>
        public void StartSpectating()
        {
            if (!isLocalPlayer) return;

            isSpectating = true;
            Debug.Log("[Spectator] Seyirci modu başladı");

            // Find alive Seekers
            RefreshAliveSeekersCache();

            // Start following first target
            if (aliveSeekersCache.Count > 0)
            {
                currentTargetIndex = 0;
                SetTarget(aliveSeekersCache[0].transform);
            }
            else
            {
                Debug.Log("[Spectator] İzlenecek oyuncu bulunamadı!");
            }

            // Show cursor for UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StopSpectating()
        {
            isSpectating = false;
            currentTarget = null;
        }

        private void RefreshAliveSeekersCache()
        {
            aliveSeekersCache.Clear();

            foreach (var player in FindObjectsOfType<MaskHeistGamePlayer>())
            {
                // Only add alive Seekers (not us)
                if (player.role == PlayerRole.Seeker && player != GetComponent<MaskHeistGamePlayer>())
                {
                    var health = player.GetComponent<PlayerHealth>();
                    if (health == null || !health.IsDead)
                    {
                        aliveSeekersCache.Add(player);
                    }
                }
            }

            Debug.Log($"[Spectator] {aliveSeekersCache.Count} izlenebilir oyuncu bulundu");
        }

        private void SwitchToNextTarget()
        {
            RefreshAliveSeekersCache();
            if (aliveSeekersCache.Count == 0) return;

            currentTargetIndex = (currentTargetIndex + 1) % aliveSeekersCache.Count;
            SetTarget(aliveSeekersCache[currentTargetIndex].transform);
        }

        private void SwitchToPreviousTarget()
        {
            RefreshAliveSeekersCache();
            if (aliveSeekersCache.Count == 0) return;

            currentTargetIndex--;
            if (currentTargetIndex < 0) currentTargetIndex = aliveSeekersCache.Count - 1;
            SetTarget(aliveSeekersCache[currentTargetIndex].transform);
        }

        private void SetTarget(Transform target)
        {
            currentTarget = target;

            var gamePlayer = target.GetComponent<MaskHeistGamePlayer>();
            string playerName = gamePlayer != null ? gamePlayer.displayName : target.name;

            Debug.Log($"[Spectator] Şu an izleniyor: {playerName}");

            // Update UI
            UI.UIEvents.TriggerInteractableChanged($"{spectatingPrefix}{playerName}\n[Tab/←/→] Oyuncu Değiştir");
        }

        private void FollowTarget()
        {
            if (currentTarget == null)
            {
                RefreshAliveSeekersCache();
                if (aliveSeekersCache.Count > 0)
                {
                    SetTarget(aliveSeekersCache[0].transform);
                }
                return;
            }

            // Calculate target camera position (behind and above target)
            Vector3 targetPos = currentTarget.position 
                              - currentTarget.forward * followDistance 
                              + Vector3.up * followHeight;

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmoothness);

            // Look at target
            if (spectatorCamera != null)
            {
                spectatorCamera.transform.LookAt(currentTarget.position + Vector3.up * 1.5f);
            }
        }
    }
}
