using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    public class GameFlowManager : NetworkBehaviour
    {
        [Header("Game State")]
        [SyncVar] public GamePhase currentPhase = GamePhase.Waiting;
        [SyncVar] public double phaseEndTime; // Süre sayacı için (NetworkTime kullanacağız)

        [Header("Settings")]
        public float hidingPhaseDuration = 45f;
        public float briefingPhaseDuration = 25f;
        public float seekingPhaseDuration = 180f; // 3 dakika

        private void Start()
        {
            // Sadece sunucu oyun akışını yönetir
            if (isServer)
            {
                StartCoroutine(GameLoop());
            }
        }

        IEnumerator GameLoop()
        {
            yield return new WaitForSeconds(2f); // Herkesin sahneye girmesini bekle

            // 1. Setup Fazı
            currentPhase = GamePhase.Setup;
            Debug.Log("Faz: Setup - Roller Dağıtılıyor...");
            AssignRoles(); // Hider ve Seeker'ları seç
            RpcUpdatePhase(currentPhase);
            yield return new WaitForSeconds(3f);

            // 2. Hiding Fazı
            currentPhase = GamePhase.Hiding;
            phaseEndTime = NetworkTime.time + hidingPhaseDuration;
            Debug.Log("Faz: Hiding - Saklayan saklanıyor...");
            RpcUpdatePhase(currentPhase);
            // Burada Arayanları kör et veya spawn'da kilitle
            yield return new WaitForSeconds(hidingPhaseDuration);

            // 3. Briefing Fazı
            currentPhase = GamePhase.Briefing;
            phaseEndTime = NetworkTime.time + briefingPhaseDuration;
            Debug.Log("Faz: Briefing - Arayanlar hazırlanıyor...");
            RpcUpdatePhase(currentPhase);
            yield return new WaitForSeconds(briefingPhaseDuration);

            // 4. Seeking Fazı
            currentPhase = GamePhase.Seeking;
            phaseEndTime = NetworkTime.time + seekingPhaseDuration;
            Debug.Log("Faz: Seeking - Oyun Başladı!");
            RpcUpdatePhase(currentPhase);
            
            // Seeking fazı süre bitene kadar veya eşya bulunana kadar sürer
            // Buradaki kontrolü Update içinde veya event ile yapacağız.
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
    }
}
