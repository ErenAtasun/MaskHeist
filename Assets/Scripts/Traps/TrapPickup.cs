using UnityEngine;
using Mirror;
using MaskHeist.Core;

namespace MaskHeist.Traps
{
    // Yerde duran ve toplanabilir tuzak kutusu
    public class TrapPickup : NetworkBehaviour
    {
        [Header("Settings")]
        public TrapType trapType;
        public GameObject trapPrefab; // Bu tuzağın gerçek prefab'i (yere kurulacak olan)

        [Header("Visuals")]
        [SerializeField] private GameObject visualModel;

        // Oyuncu buna yaklaşıp 'E' ye basınca (veya üzerine basınca) bu çalışacak
        [Server]
        public void OnPickedUp(MaskHeistGamePlayer player)
        {
            // Oyuncunun envanterine ekle
            PlayerTrapInventory inventory = player.GetComponent<PlayerTrapInventory>();
            if (inventory != null)
            {
                bool success = inventory.AddTrap(trapType, trapPrefab);
                if (success)
                {
                    // Yerden sil
                    NetworkServer.Destroy(gameObject);
                }
            }
        }

        // Basit tetiklenme (üzerine yürüyünce alma)
        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            MaskHeistGamePlayer player = other.GetComponent<MaskHeistGamePlayer>();
            
            // Eğer direkt objede yoksa, parent'larında ara
            if (player == null)
            {
                player = other.GetComponentInParent<MaskHeistGamePlayer>();
            }

            if (player == null)
            {
                 // Oyuncu değilse ilgilenme
                 return;
            }

            // TEST BİTTİ: Rol kontrolünü geri açtık. Sadece Hider alabilir.
            if (player.role == PlayerRole.Hider)
            {
                OnPickedUp(player);
            }
            else
            {
                // Opsiyonel: Seeker almaya çalışırsa uyarı verilebilir
                // Debug.Log("Sadece Hider'lar tuzak alabilir!");
            }
        }
    }
}
