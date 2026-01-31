using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaskHeist.Core;

namespace MaskHeist.Network
{
    // GDD'deki 8 kişilik lobi ve round sistemi için NetworkRoomManager kullanıyoruz.
    // Bu sınıf; oyuncuların bağlanmasını, odaya girmesini ve herkes hazır olunca oyunu başlatmasını yönetir.
    public class MaskHeistNetworkManager : NetworkRoomManager
    {
        [Header("MaskHeist Settings")]
        [Tooltip("Minimum oyuncu sayısı (GDD: 6-10 arası, varsayılan 8)")]
        public int minPlayersToStart = 1; // Test için 1 yaptık (Normalde 2 olmalı).

        public override void OnStartServer()
        {
            base.OnStartServer();
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
