using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MaskHeist.Gameplay;
using MaskHeist.Loot;
using MaskHeist.Player;
using MaskHeist.Spawn;
using MaskHeist.UI;
using MaskHeist.Mask;

namespace MaskHeist.Core
{
    public enum GamePhase
    {
        Waiting,    // Oyuncular bekleniyor / Yükleniyor
        Setup,      // Malzeme ataması ve rol dağılımı (2-3 sn)
        Hiding,     // Saklama Fazı (30-45 sn)
        Briefing,   // Arayan Spawn + Brief (25-30 sn)
        Seeking,    // Arama / Çatışma Fazı (2:30 - 4:00)
        RoundEnd,   // Tur sonu / Skor
        MatchEnd    // Maç sonu
    }

    [RequireComponent(typeof(NetworkIdentity))]
    public class GameFlowManager : NetworkBehaviour
    {
        [Header("Game State")]
        [SyncVar] public GamePhase currentPhase = GamePhase.Waiting;
        [SyncVar] public double phaseEndTime; // Süre sayacı için (NetworkTime kullanacağız)

        [Header("Settings")]
        public float hidingPhaseDuration = 45f;
        public float briefingPhaseDuration = 25f;
        public float seekingPhaseDuration = 180f; // 3 dakika

        [Header("Hideable Item")]
        [Tooltip("Prefab of the item Hider needs to hide")]
        public GameObject hideableItemPrefab;
        [Tooltip("How far in front of Hider to spawn the item")]
        public float itemSpawnDistance = 2f;

        // Reference to spawned item
        private HideableItem spawnedItem;
        
        // Track spawned bonus loots for cleanup
        private List<GameObject> spawnedBonusLoots = new List<GameObject>();

        private bool itemFound = false;

        private void Start()
        {
            // Sadece sunucu oyun akışını yönetir
            if (isServer)
            {
                // Subscribe to events
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.OnItemFoundEvent += OnItemFoundHandler;
                }
                
                StartCoroutine(GameLoop());
            }
        }

        private void Update()
        {
            // Client'lar timer'ı phaseEndTime'dan hesaplar
            if (NetworkClient.active)
            {
                // Calculate remaining time from synced phaseEndTime
                float remaining = (float)(phaseEndTime - NetworkTime.time);
                if (remaining < 0) remaining = 0;
                
                // Only show timer during timed phases
                if (currentPhase == GamePhase.Hiding || 
                    currentPhase == GamePhase.Briefing || 
                    currentPhase == GamePhase.Seeking)
                {
                    UIEvents.TriggerTimerUpdated(remaining);
                }
            }
        }

        public override void OnStopServer()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnItemFoundEvent -= OnItemFoundHandler;
            }
            base.OnStopServer();
        }

        private void OnItemFoundHandler()
        {
            itemFound = true;
        }

        IEnumerator GameLoop()
        {
            yield return new WaitForSeconds(2f); // Herkesin sahneye girmesini bekle

            // 1. Setup Fazı
            currentPhase = GamePhase.Setup;
            Debug.Log("Faz: Setup - Roller Dağıtılıyor...");
            AssignRoles(); // Hider ve Seeker'ları seç
            TeleportPlayersToSpawnPoints(); // Spawn noktalarına taşı
            RpcUpdatePhase(currentPhase);
            yield return new WaitForSeconds(3f);

            // 2. Hiding Fazı
            currentPhase = GamePhase.Hiding;
            phaseEndTime = NetworkTime.time + hidingPhaseDuration;
            Debug.Log("Faz: Hiding - Saklayan saklanıyor...");
            
            // Spawn hideable item for Hider
            SpawnHideableItem();
            
            RpcUpdatePhase(currentPhase);
            // Burada Arayanları kör et veya spawn'da kilitle
            yield return new WaitForSeconds(hidingPhaseDuration);

            // 3. Briefing Fazı
            currentPhase = GamePhase.Briefing;
            phaseEndTime = NetworkTime.time + briefingPhaseDuration;
            Debug.Log("Faz: Briefing - Arayanlar hazırlanıyor...");
            RpcUpdatePhase(currentPhase);
            
            // Show mask selection UI for Seekers
            RpcShowMaskSelection();
            
            yield return new WaitForSeconds(briefingPhaseDuration);
            
            // Hide mask selection UI (auto-select default if not chosen)
            RpcHideMaskSelection();

            // 4. Seeking Fazı
            currentPhase = GamePhase.Seeking;
            phaseEndTime = NetworkTime.time + seekingPhaseDuration;
            Debug.Log("Faz: Seeking - Oyun Başladı!");
            
            // Spawn bonus loots on the map
            SpawnBonusLoots();
            
            RpcUpdatePhase(currentPhase);
            
            // Wait for time to run out OR item to be found
            // Also award Hider survival points every N seconds
            float survivalTimer = 0f;
            float survivalInterval = ScoreManager.Instance != null ? ScoreManager.Instance.survivalInterval : 5f;
            
            while (NetworkTime.time < phaseEndTime && !itemFound)
            {
                survivalTimer += Time.deltaTime;
                
                // Award survival points periodically
                if (survivalTimer >= survivalInterval)
                {
                    survivalTimer = 0f;
                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.AwardSurvivalPoints();
                    }
                }
                
                yield return null;
            }

            // Determine winner
            PlayerRole winnerRole;
            if (itemFound)
            {
                Debug.Log("Oyun Bitti: Eşya Bulundu! (Seeker Kazandı)");
                winnerRole = PlayerRole.Seeker;
            }
            else
            {
                Debug.Log("Oyun Bitti: Süre Doldu! (Hider Kazandı)");
                winnerRole = PlayerRole.Hider;
            }

            EndRound(winnerRole);
        }

        [Server]
        private void EndRound(PlayerRole winnerRole)
        {
            currentPhase = GamePhase.RoundEnd;
            RpcUpdatePhase(currentPhase);
            
            // Award round-end bonuses to winning team
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AwardRoundEndBonus(winnerRole);
            }
            
            RpcGameOver(winnerRole);

            // Wait and restart round (Soft Reset)
            StartCoroutine(RestartRoundRoutine());
        }

        [Server]
        private IEnumerator RestartRoundRoutine()
        {
            yield return new WaitForSeconds(10f); // Show scoreboard for 10s

            // Reset Game State
            itemFound = false;
            if (spawnedItem != null)
            {
                NetworkServer.Destroy(spawnedItem.gameObject);
                spawnedItem = null;
            }
            
            // Clean up bonus loots
            CleanupBonusLoots();

            // Clean up traps (Optional - requires tracking traps)
            // ...

            // Restart Loop
            StartCoroutine(GameLoop());
        }

        [ClientRpc]
        private void RpcGameOver(PlayerRole winnerRole)
        {
            // Find local player to check if we won
            var localPlayer = NetworkClient.localPlayer?.GetComponent<MaskHeistGamePlayer>();
            if (localPlayer != null)
            {
                bool amIWinner = localPlayer.role == winnerRole;
                
                // Seeker'lar takım olduğu için herhangi biri kazanınca hepsi kazanır
                if (winnerRole == PlayerRole.Seeker && localPlayer.role == PlayerRole.Seeker)
                    amIWinner = true;

                // ScoreManager'dan oyuncunun gerçek skorunu al
                int displayScore = 0;
                if (ScoreManager.Instance != null && NetworkClient.localPlayer != null)
                {
                    displayScore = ScoreManager.Instance.GetPlayerScore(NetworkClient.localPlayer.netId);
                }
                
                Debug.Log($"[GameOver] Role: {localPlayer.role}, Winner: {winnerRole}, Score: {displayScore}, Won: {amIWinner}");
                UIEvents.TriggerGameOver(displayScore, amIWinner);
            }
        }

        [Server]
        void SpawnHideableItem()
        {
            // Try to get a random prefab from LootPool
            GameObject prefabToSpawn = null;
            
            if (LootPool.Instance != null)
            {
                prefabToSpawn = LootPool.Instance.GetRandomHideableItemPrefab();
            }
            
            // Fallback to hardcoded prefab
            if (prefabToSpawn == null)
            {
                prefabToSpawn = hideableItemPrefab;
            }
            
            if (prefabToSpawn == null)
            {
                Debug.LogWarning("[GameFlowManager] No hideable item prefab available!");
                return;
            }

            // Find the Hider
            var hider = FindObjectsOfType<MaskHeistGamePlayer>()
                .FirstOrDefault(p => p.role == PlayerRole.Hider);

            if (hider == null)
            {
                Debug.LogWarning("[GameFlowManager] No Hider found to spawn item for!");
                return;
            }

            // Calculate spawn position in front of Hider
            Vector3 spawnPos = hider.transform.position 
                             + hider.transform.forward * itemSpawnDistance 
                             + Vector3.up * 1f;

            // Spawn the item
            GameObject itemObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(itemObj);

            spawnedItem = itemObj.GetComponent<HideableItem>();

            Debug.Log($"[GameFlowManager] Hideable item spawned: {spawnedItem?.ItemName ?? prefabToSpawn.name} for {hider.displayName}");

            // Notify the Hider
            var placementController = hider.GetComponent<ItemPlacementController>();
            if (placementController != null)
            {
                placementController.TargetNotifyItemSpawned(hider.connectionToClient, itemObj);
            }
        }

        /// <summary>
        /// Spawn bonus loot items on the map during Seeking phase.
        /// </summary>
        [Server]
        void SpawnBonusLoots()
        {
            if (LootPool.Instance == null)
            {
                Debug.Log("[GameFlowManager] No LootPool — bonus loot spawn skipped");
                return;
            }

            var spawns = LootPool.Instance.GetBonusLootSpawns();
            
            foreach (var spawnInfo in spawns)
            {
                GameObject lootObj = Instantiate(spawnInfo.prefab, spawnInfo.position, spawnInfo.rotation);
                NetworkServer.Spawn(lootObj);
                
                // Set loot data if it has a LootItem component
                var lootItem = lootObj.GetComponent<LootItem>();
                if (lootItem != null && spawnInfo.lootData != null)
                {
                    lootItem.SetLootData(spawnInfo.lootData);
                }
                
                spawnedBonusLoots.Add(lootObj);
                Debug.Log($"[GameFlowManager] Bonus loot spawned: {spawnInfo.lootData?.lootName} at {spawnInfo.position}");
            }
            
            Debug.Log($"[GameFlowManager] Total bonus loots spawned: {spawnedBonusLoots.Count}");
        }

        /// <summary>
        /// Clean up all bonus loots between rounds.
        /// </summary>
        [Server]
        void CleanupBonusLoots()
        {
            foreach (var lootObj in spawnedBonusLoots)
            {
                if (lootObj != null)
                {
                    NetworkServer.Destroy(lootObj);
                }
            }
            spawnedBonusLoots.Clear();
            Debug.Log("[GameFlowManager] Bonus loots cleaned up");
        }

        [Server]
        void AssignRoles()
        {
            // Tüm oyuncuları bul
            var players = FindObjectsOfType<MaskHeistGamePlayer>().ToList();
            
            if (players.Count == 0)
            {
                Debug.LogWarning("Rol dağıtılacak oyuncu bulunamadı!");
                return;
            }

            Debug.Log($"Roller dağıtılıyor. Toplam Oyuncu: {players.Count}");

            // Listeyi karıştır (Shuffle)
            for (int i = 0; i < players.Count; i++)
            {
                MaskHeistGamePlayer temp = players[i];
                int randomIndex = Random.Range(i, players.Count);
                players[i] = players[randomIndex];
                players[randomIndex] = temp;
            }

            // GDD Madde 3: Her tur 1 Hider, diğerleri Seeker
            // İlk oyuncuyu Hider yap
            players[0].role = PlayerRole.Hider;
            Debug.Log($"Hider Seçildi: {players[0].displayName} (NetID: {players[0].netId})");

            // Geri kalanları Seeker yap
            for (int i = 1; i < players.Count; i++)
            {
                players[i].role = PlayerRole.Seeker;
                Debug.Log($"Seeker Atandı: {players[i].displayName} (NetID: {players[i].netId})");
            }
        }

        [ClientRpc]
        void RpcUpdatePhase(GamePhase newPhase)
        {
            // Client'larda UI güncelleme, ses çalma vb.
            Debug.Log($"Client Faz Güncellemesi: {newPhase}");
        }
        
        /// <summary>
        /// Show mask selection panel on Seeker clients during Briefing.
        /// </summary>
        [ClientRpc]
        private void RpcShowMaskSelection()
        {
            // Only Seekers can select masks
            var localPlayer = NetworkClient.localPlayer?.GetComponent<MaskHeistGamePlayer>();
            if (localPlayer != null && localPlayer.role == PlayerRole.Seeker)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMaskSelection();
                }
            }
        }
        
        /// <summary>
        /// Hide mask selection panel on all clients.
        /// </summary>
        [ClientRpc]
        private void RpcHideMaskSelection()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideMaskSelection();
            }
        }

        [Server]
        void TeleportPlayersToSpawnPoints()
        {
            if (SpawnPointManager.Instance == null)
            {
                Debug.LogWarning("[GameFlowManager] SpawnPointManager bulunamadı!");
                return;
            }

            var players = FindObjectsOfType<MaskHeistGamePlayer>();
            foreach (var player in players)
            {
                Vector3 spawnPos = SpawnPointManager.Instance.GetSpawnPosition(player.role);
                Quaternion spawnRot = SpawnPointManager.Instance.GetSpawnRotation(player.role);

                // Teleport player
                player.transform.position = spawnPos;
                player.transform.rotation = spawnRot;

                // Notify client to update position
                RpcTeleportPlayer(player.netIdentity, spawnPos, spawnRot);

                Debug.Log($"[GameFlowManager] {player.displayName} ({player.role}) spawned at {spawnPos}");
            }
        }

        [ClientRpc]
        void RpcTeleportPlayer(NetworkIdentity playerIdentity, Vector3 position, Quaternion rotation)
        {
            if (playerIdentity == null) return;

            playerIdentity.transform.position = position;
            playerIdentity.transform.rotation = rotation;

            // Reset velocity if has rigidbody
            var rb = playerIdentity.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Reset CharacterController if present
            var cc = playerIdentity.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                playerIdentity.transform.position = position;
                cc.enabled = true;
            }
        }
    }
}
