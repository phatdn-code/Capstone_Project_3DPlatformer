using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

namespace FlightKit
{
	public class AirplaneTouchControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		// Options for which axes to use
		public enum AxisOption
		{
			Both, // Use both
			OnlyHorizontal, // Only horizontal
			OnlyVertical // Only vertical
		}

		public AxisOption axesToUse = AxisOption.Both; // The options for the axes that the still will use
		public string horizontalAxisName = "Horizontal"; // The name given to the horizontal axis for the cross platform input
		public string verticalAxisName = "Vertical"; // The name given to the vertical axis for the cross platform input
		public float Xsensitivity = 1f;
		public float Ysensitivity = 1f;
        
        [Header ("Virtual joystick base to indicate steering center.")]
        /// <summary>
        /// Virtual joystick base to show on pointer down position.
        /// </summary>
        public Image baseImage;

        [Header ("Virtual joystick handle to indicate steering direction.")]
        /// <summary>
        /// Virtual joystick base to show on steering direction.
        /// </summary>
        public Image handleImage;
        
        [Header ("How far can the handle move from the center of the base image.")]
        /// <summary>
        /// How far can the handle move from the center of the base image.
        /// </summary>
        public float maxHandleDistance = 100;
        
		Vector3 m_StartPos;
		Vector2 m_PreviousDelta;
		Vector3 m_JoytickOutput;
		bool m_UseX; // Toggle for using the x axis
		bool m_UseY; // Toggle for using the Y axis
		CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis; // Reference to the joystick in the cross platform input
		CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis; // Reference to the joystick in the cross platform input
		bool m_Dragging;
#if !UNITY_EDITOR
		int m_Id = -1;
#endif

        private Vector3 m_Center;

		void OnEnable()
		{
			CreateVirtualAxes();
		}

		void CreateVirtualAxes()
		{
			// set axes to use
			m_UseX = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyHorizontal);
			m_UseY = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyVertical);

			// create new axes based on axes to use
			if (m_UseX)
			{
				m_HorizontalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(horizontalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_HorizontalVirtualAxis);
			}
			if (m_UseY)
			{
				m_VerticalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(verticalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_VerticalVirtualAxis);
			}
		}

		void UpdateVirtualAxes(Vector3 value)
		{
			if (m_UseX)
			{
				m_HorizontalVirtualAxis.Update(value.x);
			}

			if (m_UseY)
			{
				m_VerticalVirtualAxis.Update(value.y);
			}
		}

		void Update()
		{
			if (!m_Dragging)
			{
				return;
			}
			if (m_Dragging)
			{
#if !UNITY_EDITOR
                Vector3 pointerPos = Input.touches[m_Id].position;
#else
                Vector3 pointerPos = Input.mousePosition;
#endif

                Vector3 pointerDelta = pointerPos - m_Center;
                // Clamp pointerDelta if the joystick is on maximum distance. Otherwise, scale linearly.
                float factor = Mathf.Min(pointerDelta.magnitude / (maxHandleDistance + 0.01f), 1f);
                pointerDelta.Normalize();
                pointerDelta *= factor;
                pointerDelta.x *= Xsensitivity;
                pointerDelta.y *= -1f * Ysensitivity;
                UpdateVirtualAxes(pointerDelta);
                    
                Vector3 handlePos = pointerPos - m_Center;
                if (handlePos.magnitude > maxHandleDistance)
                {
                    handlePos = handlePos.normalized * maxHandleDistance;
                }
                handleImage.transform.localPosition = handlePos;
			}
		}

		public void OnPointerDown(PointerEventData data)
		{
			m_Dragging = true;
#if !UNITY_EDITOR
			m_Id = data.pointerId;
#endif
            
            if (baseImage != null)
            {
                baseImage.enabled = true;
                baseImage.transform.position = data.position;
                
                handleImage.enabled = true;
                handleImage.transform.position = baseImage.transform.position;
            }
            
            m_Center = data.position;
		}

		public void OnPointerUp(PointerEventData data)
		{
			m_Dragging = false;
#if !UNITY_EDITOR
			m_Id = -1;
#endif
			UpdateVirtualAxes(Vector3.zero);
            
            if (baseImage != null)
            {
                baseImage.enabled = false;
                handleImage.enabled = false;
            }
            
		}

		void OnDisable()
		{
			if (CrossPlatformInputManager.AxisExists(horizontalAxisName))
				CrossPlatformInputManager.UnRegisterVirtualAxis(horizontalAxisName);

			if (CrossPlatformInputManager.AxisExists(verticalAxisName))
				CrossPlatformInputManager.UnRegisterVirtualAxis(verticalAxisName);
		}
	}
}