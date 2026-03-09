using UnityEngine;

namespace MiniGame
{
    public class AeroplanePropellerAnimator : MonoBehaviour
    {
        [SerializeField]
        private Transform m_PropellerModel;
        // Model cánh quạt thật của máy bay

        [SerializeField]
        private Transform m_PropellerBlur;
        // Mặt phẳng dùng để hiển thị hiệu ứng cánh quạt bị mờ khi quay nhanh

        [SerializeField]
        private Texture2D[] m_PropellerBlurTextures;
        // Mảng texture cánh quạt mờ (mờ dần theo tốc độ quay)

        [SerializeField]
        [Range(0f, 1f)]
        private float m_ThrottleBlurStart = 0.25f;
        // Mức ga bắt đầu áp dụng hiệu ứng mờ

        [SerializeField]
        [Range(0f, 1f)]
        private float m_ThrottleBlurEnd = 0.5f;
        // Mức ga mà hiệu ứng mờ đạt tối đa và không thay đổi thêm

        [SerializeField]
        private float m_MaxRpm = 2000;
        // Tốc độ quay tối đa của cánh quạt (vòng/phút)

        /// <summary>
        /// Quay cánh quạt quanh trục X thay vì trục Y.
        /// Hữu ích cho các model import từ Blender.
        /// </summary>
        public bool rotateAroundX = false;

        private FlightDynamicsController m_Plane;
        // Tham chiếu tới script điều khiển máy bay

        private int m_PropellorBlurState = -1;
        // Lưu trạng thái hiện tại của mức độ mờ cánh quạt

        private const float k_RpmToDps = 60f;
        // Hằng số chuyển đổi từ vòng/phút sang độ/giây

        private Renderer m_PropellorModelRenderer;
        // Renderer của model cánh quạt thật

        private Renderer m_PropellorBlurRenderer;
        // Renderer của cánh quạt hiệu ứng mờ


        private void Awake()
        {
            // Lấy reference tới AeroplaneController trên cùng GameObject
            m_Plane = GetComponent<FlightDynamicsController>();

            // Lấy Renderer của cánh quạt thật và cánh quạt mờ
            m_PropellorModelRenderer = m_PropellerModel.GetComponent<Renderer>();
            m_PropellorBlurRenderer = m_PropellerBlur.GetComponent<Renderer>();

            // Gán cánh quạt mờ làm con của cánh quạt thật
            m_PropellerBlur.parent = m_PropellerModel;
        }


        private void Update()
        {
            // Tính tốc độ quay của cánh quạt dựa trên ga (Throttle)
            float rotation = m_MaxRpm * m_Plane.Throttle * Time.deltaTime * k_RpmToDps;

            // Quay cánh quạt theo trục phù hợp
            if (rotateAroundX)
            {
                m_PropellerModel.Rotate(rotation, 0, 0);
            }
            else
            {
                m_PropellerModel.Rotate(0, rotation, 0);
            }

            // Xác định mức độ mờ mới
            int newBlurState = 0;

            // Nếu ga đủ lớn thì bắt đầu áp dụng hiệu ứng mờ
            if (m_Plane.Throttle > m_ThrottleBlurStart)
            {
                // Tính tỉ lệ mờ dựa trên mức ga
                float throttleBlurProportion =
                    Mathf.InverseLerp(m_ThrottleBlurStart, m_ThrottleBlurEnd, m_Plane.Throttle);

                // Xác định index texture mờ cần dùng
                newBlurState = Mathf.FloorToInt(
                    throttleBlurProportion * (m_PropellerBlurTextures.Length - 1)
                );
            }

            // Nếu trạng thái mờ thay đổi
            if (newBlurState != m_PropellorBlurState)
            {
                m_PropellorBlurState = newBlurState;

                if (m_PropellorBlurState == 0)
                {
                    // Hiện cánh quạt thật, tắt hiệu ứng mờ
                    m_PropellorModelRenderer.enabled = true;
                    m_PropellorBlurRenderer.enabled = false;
                }
                else
                {
                    // Ẩn cánh quạt thật, bật hiệu ứng mờ
                    m_PropellorModelRenderer.enabled = false;
                    m_PropellorBlurRenderer.enabled = true;

                    // Gán texture mờ tương ứng
                    m_PropellorBlurRenderer.material.mainTexture =
                        m_PropellerBlurTextures[m_PropellorBlurState];
                }
            }
        }
    }
}
