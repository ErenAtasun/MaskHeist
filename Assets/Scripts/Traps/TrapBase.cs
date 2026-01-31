using UnityEngine;
using Mirror;
using MaskHeist.Core;

namespace MaskHeist.Traps
{
    public abstract class TrapBase : NetworkBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField] protected float armingDelay = 2f;
        [SerializeField] protected bool triggerOnSeeker = true;
        [SerializeField] protected bool triggerOnHider = false;
        
        [SyncVar]
        protected bool isArmed = false;

        [SyncVar]
        public uint ownerNetId; // Tuzağı kuran oyuncunun ID'si

        public override void OnStartServer()
        {
            base.OnStartServer();
            Invoke(nameof(ArmTrap), armingDelay);
        }

        [Server]
        protected virtual void ArmTrap()
        {
            isArmed = true;
            // Görsel efekt vs. eklenebilir (Rpc ile)
            Debug.Log($"Trap Armed: {gameObject.name}");
        }

        [ServerCallback]
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!isArmed) return;
            if (!NetworkServer.active) return;

            MaskHeistGamePlayer player = other.GetComponent<MaskHeistGamePlayer>();
            if (player == null) return;

            // Kendi tuzağına basma kontrolü (opsiyonel)
            // if (player.netId == ownerNetId) return;

            bool shouldTrigger = false;
            if (player.role == PlayerRole.Seeker && triggerOnSeeker) shouldTrigger = true;
            if (player.role == PlayerRole.Hider && triggerOnHider) shouldTrigger = true;

            if (shouldTrigger)
            {
                TriggerTrap(player);
            }
        }

        // Abstract metodlarda [Server] attribute kullanılamaz, override edenlerde kullanılır.
        // O yüzden burada sadece protected abstract tanım yapıyoruz.
        protected abstract void TriggerTrap(MaskHeistGamePlayer victim);

        [ClientRpc]
        protected void RpcPlayTriggerEffect(Vector3 position)
        {
            // Efekt oynatma mantığı (Ses, partikül vb.)
        }
    }
}
