using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using MaskHeist.Core;

namespace MaskHeist.Traps
{
    // Oyuncunun üzerindeki tuzakları tutan script
    public class PlayerTrapInventory : NetworkBehaviour
    {
        [SyncVar]
        public TrapType currentTrap = TrapType.None;

        // Prefab referansını sunucuda tutmamız lazım (SyncVar ile GameObject taşınamaz)
        // Bu yüzden basitçe bir Manager'dan veya Resource'dan çekmek daha iyi olabilir
        // Ama şimdilik basit tutalım: Her oyuncu kendi prefabını bilmez, sunucu bilir.
        
        private GameObject loadedTrapPrefab; // Sunucu tarafında tutulan prefab referansı

        [Server]
        public bool AddTrap(TrapType type, GameObject prefab)
        {
            if (currentTrap != TrapType.None)
            {
                // Zaten bir tuzağın var uyarısı
                TargetTrapError(connectionToClient, "Zaten bir tuzağın var!");
                return false;
            }

            currentTrap = type;
            loadedTrapPrefab = prefab;
            
            Debug.Log($"Trap Added: {type}");
            TargetTrapAdded(connectionToClient, type);
            return true;
        }

        [Command]
        public void CmdPlaceTrap(Vector3 position, Quaternion rotation)
        {
            if (currentTrap == TrapType.None || loadedTrapPrefab == null)
            {
                Debug.LogWarning("Tuzak yok veya prefab kayıp!");
                return;
            }

            // Tuzağı oluştur
            GameObject trapObj = Instantiate(loadedTrapPrefab, position, rotation);
            
            // Sahibini ayarla
            TrapBase trapScript = trapObj.GetComponent<TrapBase>();
            if (trapScript != null)
            {
                trapScript.ownerNetId = netId;
            }

            // Sunucuda spawn et (herkes görsün)
            NetworkServer.Spawn(trapObj);

            // Envanteri boşalt
            currentTrap = TrapType.None;
            loadedTrapPrefab = null;
            
            TargetTrapPlaced(connectionToClient);
        }

        [TargetRpc]
        private void TargetTrapAdded(NetworkConnection target, TrapType type)
        {
            Debug.Log($"Tuzak Alındı: {type} (Tuzağı kurmak için 'E' veya Ateş Tuşuna bas)");
            // UI Güncellemesi burada yapılabilir
        }

        [TargetRpc]
        private void TargetTrapPlaced(NetworkConnection target)
        {
            Debug.Log("Tuzak Kuruldu!");
            // UI Güncellemesi
        }

        [TargetRpc]
        private void TargetTrapError(NetworkConnection target, string msg)
        {
            Debug.LogWarning(msg);
        }

        // Test için Update içinde tuş kontrolü (Normalde PlayerController veya Interaction yapar)
        [ClientCallback]
        private void Update()
        {
            if (!isLocalPlayer) return;

            // O Tuşu ile tuzağı kur
            bool attemptPlace = false;

            // New Input System
            if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame) attemptPlace = true;
            
            // Legacy Input System (Eğer proje eski sistemi kullanıyorsa diye yedek)
            if (Input.GetKeyDown(KeyCode.O)) attemptPlace = true;

            if (attemptPlace)
            {
                if (currentTrap != TrapType.None)
                {
                    // Karakterin önüne koy
                    Vector3 placePos = transform.position + transform.forward * 1.5f;
                    CmdPlaceTrap(placePos, Quaternion.identity);
                }
                else
                {
                    Debug.Log("O tuşuna basıldı ama envanterde tuzak yok!");
                }
            }
        }
    }
}
