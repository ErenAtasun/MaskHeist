using UnityEngine;
using Mirror;
using MaskHeist.Core;

namespace MaskHeist.Traps
{
    public class ProximityMine : TrapBase
    {
        [Header("Mine Settings")]
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private GameObject explosionPrefab; // Patlama efekti

        [Server]
        protected override void TriggerTrap(MaskHeistGamePlayer victim)
        {
            Debug.Log($"BOMB! Player {victim.displayName} stepped on mine!");

            // Patlama efekti
            RpcExplode(transform.position);

            // Etraftaki oyuncuları bul ve etkile
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider col in colliders)
            {
                MaskHeistGamePlayer player = col.GetComponent<MaskHeistGamePlayer>();
                if (player != null)
                {
                    // Eğer Seeker ise kör et / yavaşlat
                    if (player.role == PlayerRole.Seeker)
                    {
                        // TargetRpc ile o oyuncuya özel efekt gönderilebilir
                        TargetApplyStun(player.connectionToClient, stunDuration);
                    }
                }
            }

            // Tuzağı yok et
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcExplode(Vector3 pos)
        {
            // Varsa patlama partikülü oluştur
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, pos, Quaternion.identity);
            }
            Debug.Log("BOOM! Visual effect playing.");
        }

        [TargetRpc]
        private void TargetApplyStun(NetworkConnection target, float duration)
        {
            Debug.Log($"STUNNED for {duration} seconds!");
            // UI kör etme veya hareket kısıtlama kodu buraya gelecek
            // Örn: PlayerController.Instance.Freeze(duration);
        }
    }
}
