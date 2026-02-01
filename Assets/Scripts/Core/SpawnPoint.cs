using UnityEngine;
using MaskHeist.Core;

namespace MaskHeist.Spawn
{
    /// <summary>
    /// Marks a spawn point location in the scene.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Which role can spawn here")]
        public PlayerRole spawnRole = PlayerRole.None;

        [Header("Gizmo")]
        [SerializeField] private Color gizmoColor = Color.green;
        [SerializeField] private float gizmoSize = 1f;

        private void OnDrawGizmos()
        {
            // Draw spawn point in editor
            Gizmos.color = spawnRole switch
            {
                PlayerRole.Hider => Color.red,
                PlayerRole.Seeker => Color.blue,
                _ => gizmoColor
            };

            Gizmos.DrawWireSphere(transform.position, gizmoSize);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

            // Draw label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, spawnRole.ToString());
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
