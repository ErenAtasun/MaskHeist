using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaskHeist.Core;
using kcp2k;

namespace MaskHeist.Network
{
    // GDD'deki 8 kişilik lobi ve round sistemi için NetworkRoomManager kullanıyoruz.
    // Bu sınıf; oyuncuların bağlanmasını, odaya girmesini ve herkes hazır olunca oyunu başlatmasını yönetir.
    public class MaskHeistNetworkManager : NetworkRoomManager
    {
        [Header("MaskHeist Settings")]
        [Tooltip("Minimum oyuncu sayısı (GDD: 6-10 arası, varsayılan 8)")]
        public int minPlayersToStart = 1; // Test için 1 yaptık (Normalde 2 olmalı).

        public override void Awake()
        {
            base.Awake();
            
            // Transport portunu 25565 olarak zorla (Kullanıcı tercihi/Forum önerisi)
            if (transport is KcpTransport kcp)
            {
                kcp.Port = 25565;
                // Sahne yüklemelerinde bağlantı kopmaması için Timeout süresini uzat (30sn)
                kcp.Timeout = 30000; 
                Debug.Log($"[MaskHeist] KCP Transport Port set to: {kcp.Port}, Timeout: {kcp.Timeout}");
            }
            else
            {
                Debug.LogWarning($"[MaskHeist] Transport is not KCP! It is: {transport?.GetType().Name}");
            }
        }

        /// <summary>
        /// Sadece bilgilendirme amaçlı log. Bağlantı reddi kaldırıldı.
        /// </summary>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            string activeScenePath = SceneManager.GetActiveScene().path;
            Debug.Log($"[MaskHeist] Yeni bağlantı: ConnId={conn.connectionId}, ActiveScene='{activeScenePath}'");
            
            base.OnServerConnect(conn);
        }

        /// <summary>
        /// Sahne değişimi öncesi çağrılır - client'ların hazır olmasını bekleriz
        /// </summary>
        public override void OnServerChangeScene(string newSceneName)
        {
            Debug.Log($"[MaskHeist] Sahne değişiyor: {SceneManager.GetActiveScene().name} -> {newSceneName}");
            base.OnServerChangeScene(newSceneName);
        }

        /// <summary>
        /// Sahne yüklendikten sonra çağrılır
        /// </summary>
        public override void OnServerSceneChanged(string sceneName)
        {
            Debug.Log($"[MaskHeist] Yeni sahne yüklendi: {sceneName}, Bağlı oyuncu sayısı: {NetworkServer.connections.Count}");
            base.OnServerSceneChanged(sceneName);
        }

        private bool IsSameScene(string activePath, string targetScene)
        {
            if (string.IsNullOrEmpty(targetScene)) return false;
            if (activePath == targetScene) return true;
            
            // Biri path, biri isim olabilir. İkisinin de sadece ismini karşılaştır.
            string activeName = System.IO.Path.GetFileNameWithoutExtension(activePath);
            string targetName = System.IO.Path.GetFileNameWithoutExtension(targetScene);
            
            return activeName == targetName;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // RoomPlayer Prefab kontrolü
            if (roomPlayerPrefab == null)
            {
                Debug.LogError("[MaskHeist] CRITICAL: Room Player Prefab is MISSING in NetworkManager! Clients cannot join.");
            }
            
            Debug.Log($"[MaskHeist] Sunucu Başladı! Min Player: {minPlayersToStart}");
        }

        // Sunucu başladığında çalışır
        public override void OnRoomStartServer()
        {
            base.OnRoomStartServer();
            Debug.Log("Lobi Sunucusu Başlatıldı. Oyuncular bekleniyor...");
        }

        // Bir oyuncu lobiye bağlandığında (Room Player oluşturulduğunda)
        public override void OnRoomServerPlayersReady()
        {
            // Orijinalinde herkes hazır olunca otomatik başlar,
            // burada ekstra kontrol ekleyebiliriz (örn: harita seçimi vs).
            
            // Şimdilik ebeveyn mantığına bırakıyoruz (tüm oyuncular Ready olunca sahne değişir).
            base.OnRoomServerPlayersReady();
        }

        // Oyun sahnesine geçildiğinde çalışır
        // Burası GamePlayer prefab'inin yaratılacağı yerdir.
        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            // Burası kritik: Oyuncu lobiden oyun sahnesine geçtiğinde,
            // RoomPlayer'daki bilgileri (seçilen maske, isim vb.) GamePlayer'a aktaracağız.
            
            MaskHeistGamePlayer gamePlayerScript = gamePlayer.GetComponent<MaskHeistGamePlayer>();
            MaskHeistRoomPlayer roomPlayerScript = roomPlayer.GetComponent<MaskHeistRoomPlayer>();
            
            if (gamePlayerScript != null && roomPlayerScript != null)
            {
                gamePlayerScript.displayName = roomPlayerScript.displayName;
                // Rol daha sonra GameFlowManager tarafından atanacak
                gamePlayerScript.role = PlayerRole.None; 
            }

            return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer);
        }

        // Oyun bittiğinde veya oyuncu çıktığında temizlik
        public override void OnRoomStopClient()
        {
            base.OnRoomStopClient();
            Debug.Log("İstemci lobiden ayrıldı.");
        }
    }
}
