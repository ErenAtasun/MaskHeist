using UnityEngine;
using Mirror;
using MaskHeist.Core;

namespace MaskHeist.Traps
{
    public class LaserTrap : TrapBase
    {
        [Header("Laser Settings")]
        [SerializeField] private float revealDuration = 5f;
        [SerializeField] private LineRenderer laserLine;

        private void Awake()
        {
            if (laserLine == null)
            {
                laserLine = GetComponent<LineRenderer>();
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Lazer görselini ayarla
            if (laserLine != null)
            {
                laserLine.enabled = true;
            }
        }

        [Server]
        protected override void TriggerTrap(MaskHeistGamePlayer victim)
        {
            Debug.Log($"ALARM! Player {victim.displayName} crossed the laser!");

            // Ses ve görsel uyarı
            RpcTriggerAlarm(victim.transform.position);

            // Oyuncuyu ifşa et (Outline veya UI ikonu)
            // Bu kısım ScoreManager veya GameFlowManager üzerinden yönetilebilir
            // Şimdilik sadece log
            TargetAlertPlayer(victim.connectionToClient);
            
            // Tuzağı yok et
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcTriggerAlarm(Vector3 pos)
        {
            Debug.Log("WEE-WOO! Laser Alarm sounding at " + pos);
            // Ses çalma kodu buraya
        }

        [TargetRpc]
        private void TargetAlertPlayer(NetworkConnection target)
        {
            Debug.Log("You have been detected by a laser!");
            // Ekranda "DETECTED" yazısı çıkartılabilir
        }
    }
}
