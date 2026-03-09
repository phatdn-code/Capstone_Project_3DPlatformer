using UnityEngine;
using UnityEngine.UI;

namespace MiniGame
{
    public class ControlsSetting : MonoBehaviour
    {
        // ===============================
        // UI điều khiển cho PC / Standalone
        // ===============================
        [Header("Standalone Controls")]

        // Điều khiển kiểu classic (roll bằng phím)
        public Toggle classicControls;

        // Điều khiển bằng chuột
        public Toggle mouseControls;

        // Điều khiển đơn giản (casual)
        public Toggle casualControls;

        [Space]

        // Đảo chiều pitch (kéo lên → máy bay chúi xuống)
        public Toggle inversePitch;

        void Start()
        {
            // ===============================
            // Thiết lập trạng thái toggle ban đầu
            // ===============================

            if (ControlSettingsManager.IsMouseEnabled)
            {
                if (mouseControls) mouseControls.isOn = true;
            }
            else if (ControlSettingsManager.IsRollEnabled)
            {
                if (classicControls) classicControls.isOn = true;
            }
            else
            {
                if (casualControls) casualControls.isOn = true;
            }

            // Cập nhật trạng thái đảo chiều pitch
            if (inversePitch)
            {
                inversePitch.isOn = ControlSettingsManager.IsInversePitch;
            }
        }

        // =====================================
        // Khi người chơi bật/tắt Roll Control
        // =====================================
        public virtual void OnRollEnabledChanged(bool activated)
        {
            ControlSettingsManager.IsRollEnabled = activated;
        }

        // =====================================
        // Khi người chơi bật/tắt Mouse Control
        // =====================================
        public virtual void OnMouseEnabledChanged(bool activated)
        {
            ControlSettingsManager.IsMouseEnabled = activated;

            // Nếu bật mouse control thì bật luôn roll control
            if (activated)
            {
                ControlSettingsManager.IsRollEnabled = true;
            }
        }

        // =====================================
        // Khi người chơi bật/tắt đảo chiều Pitch
        // =====================================
        public virtual void OnInversePitchChanged(bool activated)
        {
            ControlSettingsManager.IsInversePitch = activated;
        }
    }
}