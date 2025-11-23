using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class PortalAuxiliary : MonoBehaviour
    {
        //─────────────────────────────────────────────────────────────
        #region === Inspector Fields ===

        [Header("Effects")]
        [SerializeField] private GameObject highlightEffect;      // Viền ngoài portal
        [SerializeField] private GameObject innerEnergyEffect;    // Năng lượng bên trong

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Unity Lifecycle ===

        private void Start()
        {
            // Tắt highlight ngay khi vào scene
            if (highlightEffect != null)
                highlightEffect.SetActive(false);

            // Tắt hiệu ứng năng lượng ngay khi vào scene
            if (innerEnergyEffect != null)
                innerEnergyEffect.SetActive(false);
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Highlight API ===

        /// <summary>
        /// Bật viền highlight bên ngoài portal.
        /// </summary>
        public void ShowHighlight()
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(true);
        }

        /// <summary>
        /// Tắt viền highlight bên ngoài portal.
        /// </summary>
        public void HideHighlight()
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(false);
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Energy Effect API ===

        /// <summary>
        /// Bật hiệu ứng năng lượng bên trong portal.
        /// </summary>
        public void ShowEnergy()
        {
            if (innerEnergyEffect != null)
                innerEnergyEffect.SetActive(true);
        }

        /// <summary>
        /// Tắt hiệu ứng năng lượng bên trong portal.
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