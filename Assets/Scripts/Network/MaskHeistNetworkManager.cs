using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaskHeist.Network
{
    // GDD'deki 8 kişilik lobi ve round sistemi için NetworkRoomManager kullanıyoruz.
    // Bu sınıf; oyuncuların bağlanmasını, odaya girmesini ve herkes hazır olunca oyunu başlatmasını yönetir.
    public class MaskHeistNetworkManager : NetworkRoomManager
    {
        [Header("MaskHeist Settings")]
        [Tooltip("Minimum oyuncu sayısı (GDD: 6-10 arası, varsayılan 8)")]
        public int minPlayersToStart = 2; // Test için 2 yapıyoruz, sonra 6-8 yaparız.

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
            
            /* Örnek:
            MaskHeistGamePlayer gamePlayerScript = gamePlayer.GetComponent<MaskHeistGamePlayer>();
            MaskHeistRoomPlayer roomPlayerScript = roomPlayer.GetComponent<MaskHeistRoomPlayer>();
            
            gamePlayerScript.displayName = roomPlayerScript.displayName;
            gamePlayerScript.role = ... (Burada Hider/Seeker ataması yapacağız)
            */

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
