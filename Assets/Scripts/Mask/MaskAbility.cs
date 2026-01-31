using UnityEngine;
using Mirror;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Base class for all mask abilities.
    /// Handles cooldown logic and activation state.
    /// </summary>
    public abstract class MaskAbility : NetworkBehaviour
    {
        [Header("Base Ability Settings")]
        [SyncVar] protected float cooldownEndTime;
        [SyncVar] protected bool isActive;
        
        protected float duration;
        protected float cooldown;
        protected PlayerMask playerMask;
        
        public bool IsActive => isActive;
        public bool IsOnCooldown => NetworkTime.time < cooldownEndTime;
        public float CooldownRemaining => Mathf.Max(0, (float)(cooldownEndTime - NetworkTime.time));
        public float CooldownPercent => cooldown > 0 ? CooldownRemaining / cooldown : 0;
        
        public virtual void Initialize(PlayerMask mask, float abilityDuration, float abilityCooldown)
        {
            playerMask = mask;
            duration = abilityDuration;
            cooldown = abilityCooldown;
        }
        
        /// <summary>
        /// Try to activate the ability. Returns true if successful.
        /// </summary>
        public bool TryActivate()
        {
            // Use playerMask's netIdentity since this component is added dynamically
            if (playerMask == null)
            {
                Debug.Log("TryActivate failed: playerMask is null");
                return false;
            }
            
            // Check netIdentity before accessing isLocalPlayer (Mirror requirement)
            try
            {
                if (playerMask.netIdentity == null)
                {
                    Debug.Log("TryActivate failed: playerMask.netIdentity is null");
                    return false;
                }
                
                if (!playerMask.isLocalPlayer)
                {
                    Debug.Log("TryActivate failed: not local player");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"TryActivate failed with exception: {e.Message}");
                return false;
            }
            
            if (IsOnCooldown || isActive)
            {
                Debug.Log($"TryActivate failed: IsOnCooldown={IsOnCooldown}, isActive={isActive}");
                return false;
            }
            
            // Note: For unique abilities, activation should be done through PlayerMask
            // This base implementation just returns true to indicate the ability is ready
            Debug.Log("MaskAbility.TryActivate - ability ready for activation");
            return true;
        }
        
        /// <summary>
        /// Called by PlayerMask.CmdActivateAbility on the server
        /// </summary>
        [Server]
        public void ServerActivate()
        {
            if (IsOnCooldown || isActive) return;
            
            isActive = true;
            cooldownEndTime = (float)NetworkTime.time + cooldown;
            
            OnAbilityActivated();
            RpcOnAbilityActivated();
            
            // Schedule deactivation
            Invoke(nameof(ServerDeactivate), duration);
        }
        
        [Command]
        protected virtual void CmdActivate()
        {
            if (IsOnCooldown || isActive) return;
            
            isActive = true;
            cooldownEndTime = (float)NetworkTime.time + cooldown;
            
            OnAbilityActivated();
            RpcOnAbilityActivated();
            
            // Schedule deactivation
            Invoke(nameof(ServerDeactivate), duration);
        }
        
        [Server]
        protected virtual void ServerDeactivate()
        {
            isActive = false;
            OnAbilityDeactivated();
            RpcOnAbilityDeactivated();
        }
        
        [ClientRpc]
        protected virtual void RpcOnAbilityActivated()
        {
            // Override in derived classes for client-side effects
        }
        
        [ClientRpc]
        protected virtual void RpcOnAbilityDeactivated()
        {
            // Override in derived classes for client-side effects
        }
        
        /// <summary>
        /// Called on server when ability activates
        /// </summary>
        protected abstract void OnAbilityActivated();
        
        /// <summary>
        /// Called on server when ability deactivates
        /// </summary>
        protected abstract void OnAbilityDeactivated();
    }
}
