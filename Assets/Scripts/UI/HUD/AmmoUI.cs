using UnityEngine;
using TMPro;

namespace MaskHeist.UI
{
    /// <summary>
    /// Displays current ammo count for Hider's weapon.
    /// Shows "Silah Yok" when weapon not equipped.
    /// </summary>
    public class AmmoUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private GameObject ammoPanel;

        [Header("Settings")]
        [SerializeField] private string noWeaponText = "Silah Yok";
        [SerializeField] private string ammoFormat = "{0} / {1}";
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lowAmmoColor = Color.red;
        [SerializeField] private int lowAmmoThreshold = 2;

        private bool hasWeapon = false;

        private void OnEnable()
        {
            UIEvents.OnAmmoChanged += HandleAmmoChanged;
            UIEvents.OnWeaponEquipped += HandleWeaponEquipped;
        }

        private void OnDisable()
        {
            UIEvents.OnAmmoChanged -= HandleAmmoChanged;
            UIEvents.OnWeaponEquipped -= HandleWeaponEquipped;
        }

        private void Start()
        {
            // Initially hide or show "no weapon"
            UpdateDisplay(0, 0);
        }

        private void HandleAmmoChanged(int current, int max)
        {
            UpdateDisplay(current, max);
        }

        private void HandleWeaponEquipped(bool equipped)
        {
            hasWeapon = equipped;
            
            if (ammoPanel != null)
            {
                ammoPanel.SetActive(equipped);
            }

            if (!equipped)
            {
                if (ammoText != null)
                {
                    ammoText.text = noWeaponText;
                    ammoText.color = normalColor;
                }
            }
        }

        private void UpdateDisplay(int current, int max)
        {
            if (ammoText == null) return;

            if (!hasWeapon)
            {
                ammoText.text = noWeaponText;
                ammoText.color = normalColor;
                return;
            }

            ammoText.text = string.Format(ammoFormat, current, max);

            // Change color when low on ammo
            if (current <= lowAmmoThreshold)
            {
                ammoText.color = lowAmmoColor;
            }
            else
            {
                ammoText.color = normalColor;
            }
        }
    }
}
