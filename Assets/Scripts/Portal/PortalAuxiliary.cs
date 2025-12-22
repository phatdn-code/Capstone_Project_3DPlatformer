using EPOOutline;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class PortalAuxiliary : MonoBehaviour
    {
        //─────────────────────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Correct Portal Indicator")]
        [SerializeField] private GameObject arrowIndicator;   // Mũi tên chỉ portal đúng
        [SerializeField] private Outlinable outline;          // Outline viền của portal

        [Header("Energy Effect")]
        [SerializeField] private GameObject innerEnergyEffect; // Hiệu ứng năng lượng trong portal

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            // Tắt mũi tên ban đầu
            if (arrowIndicator != null)
                arrowIndicator.SetActive(false);

            // Tắt outline ban đầu
            if (outline != null)
                outline.enabled = false;

            // Tắt năng lượng ban đầu
            if (innerEnergyEffect != null)
                innerEnergyEffect.SetActive(false);
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === HIGHLIGHT API ===
        /// <summary>
        /// Hiện arrow + outline để báo portal đúng.
        /// </summary>
        public void ShowHighlight()
        {
            if (arrowIndicator != null)
                arrowIndicator.SetActive(true);

            if (outline != null)
                outline.enabled = true;
        }

        /// <summary>
        /// Ẩn arrow + outline.
        /// </summary>
        public void HideHighlight()
        {
            if (arrowIndicator != null)
                arrowIndicator.SetActive(false);

            if (outline != null)
                outline.enabled = false;
        }
        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === ENERGY API ===
        /// <summary>
        /// Hiện hiệu ứng năng lượng bên trong portal.
        /// </summary>
        public void ShowEnergy()
        {
            if (innerEnergyEffect != null)
                innerEnergyEffect.SetActive(true);
        }

        /// <summary>
        /// Ẩn năng lượng bên trong portal.
        /// </summary>
        public void HideEnergy()
        {
            if (innerEnergyEffect != null)
                innerEnergyEffect.SetActive(false);
        }
        #endregion
        //─────────────────────────────────────────────────────────────
    }
}
