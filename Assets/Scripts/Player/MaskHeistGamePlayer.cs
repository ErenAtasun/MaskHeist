using Mirror;
using UnityEngine;

namespace MaskHeist.Core
{
    public enum PlayerRole
    {
        None,
        Hider,
        Seeker
    }

    public class MaskHeistGamePlayer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnRoleChanged))]
        public PlayerRole role = PlayerRole.None;

        [SyncVar]
        public string displayName = "Player";

        public override void OnStartClient()
        {
            // Add to a static list if needed, or just let FindObjectsOfType find it
            base.OnStartClient();
        }

        private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
        {
            Debug.Log($"Rol Değişti: {oldRole} -> {newRole}");
            
            // Burada UI güncellemesi veya karakter model değişimi tetiklenebilir
            if (isLocalPlayer)
            {
                // Local player için HUD güncelle
                Debug.Log($"Benim yeni rolüm: {newRole}");
            }
        }
    }
}
