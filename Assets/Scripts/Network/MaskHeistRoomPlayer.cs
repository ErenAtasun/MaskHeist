using Mirror;
using UnityEngine;

namespace MaskHeist.Network
{
    public class MaskHeistRoomPlayer : NetworkRoomPlayer
    {
        [SyncVar]
        public string displayName = "Player";

        public override void OnStartClient()
        {
            // Oyuncu lobiye girdiğinde yapılacaklar
            // Örn: UI'da ismini güncelle
            Debug.Log($"Oyuncu lobiye girdi: {index}");
        }

        public override void OnClientEnterRoom()
        {
            // UI'ı aktif et
            Debug.Log("Lobiye giriş yapıldı.");
        }

        public override void OnClientExitRoom()
        {
            // UI'ı kapat
            Debug.Log("Lobiden çıkıldı.");
        }

        // UI Butonuna bağlanacak fonksiyon
        public void ToggleReadyState()
        {
            if (isLocalPlayer)
            {
                CmdChangeReadyState(!readyToBegin);
            }
        }
    }
}
