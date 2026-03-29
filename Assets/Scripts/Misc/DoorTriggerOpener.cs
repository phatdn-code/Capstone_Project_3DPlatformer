using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace PLAYERTWO.PlatformerProject
{
    public class DoorTriggerOpener : MonoBehaviour
    {
        #region Inspector

        [TitleGroup("Door Settings")]
        [HorizontalGroup("Door Settings/Row1")]
        [SerializeField] private Transform doorPivot;

        [HorizontalGroup("Door Settings/Row1")]
        [SerializeField] private float openAngle = 90f;

        [HorizontalGroup("Door Settings/Row2")]
        [SerializeField] private float rotateDuration = 0.35f;

        [HorizontalGroup("Door Settings/Row2")]
        [SerializeField] private Ease ease = Ease.OutCubic;

        [HorizontalGroup("Door Settings/Row3")]
        [SerializeField] private bool closeWhenExit = true;

        [HorizontalGroup("Door Settings/Row3")]
        [SerializeField] private bool invertOpenDirection;

        [HorizontalGroup("Door Settings/Row3")]
        [SerializeField] private SideCheckAxis sideCheckAxis = SideCheckAxis.LocalZ;

        [TitleGroup("Filter")]
        [SerializeField] private string playerTag = "Player";

        #endregion

        #region Runtime

        private Vector3 m_closedLocalEuler;
        private Tween m_rotateTween;
        private int m_playerCountInside;

        #endregion

        #region Unity Messages

        // Lưu trạng thái góc đóng ban đầu của cửa
        private void Start()
        {
            if (doorPivot == null)
                doorPivot = transform;

            m_closedLocalEuler = doorPivot.localEulerAngles;
        }

        // Khi player đi vào trigger thì mở cửa
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            m_playerCountInside++;
            OpenDoorAwayFromPlayer(other.transform);
        }

        // Khi player ra khỏi trigger thì giảm đếm và đóng cửa nếu cần
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            m_playerCountInside = Mathf.Max(0, m_playerCountInside - 1);

            if (closeWhenExit && m_playerCountInside == 0)
                CloseDoor();
        }

        // Hủy tween khi object bị disable để tránh tween rác
        private void OnDisable()
        {
            m_rotateTween?.Kill();
        }

        #endregion

        #region Door Actions

        // Mở cửa theo hướng ngược phía player đang đứng
        private void OpenDoorAwayFromPlayer(Transform player)
        {
            Vector3 localPlayerPos = doorPivot.InverseTransformPoint(player.position);

            float sideValue = sideCheckAxis == SideCheckAxis.LocalZ
                ? localPlayerPos.z
                : localPlayerPos.x;

            float direction = sideValue >= 0f ? -1f : 1f;

            if (invertOpenDirection)
                direction *= -1f;

            Vector3 targetEuler = m_closedLocalEuler + new Vector3(0f, direction * openAngle, 0f);
            RotateDoor(targetEuler);
        }

        // Đóng cửa về góc ban đầu
        private void CloseDoor()
        {
            RotateDoor(m_closedLocalEuler);
        }

        // Tween xoay cửa đến góc đích
        private void RotateDoor(Vector3 targetEuler)
        {
            m_rotateTween?.Kill();

            m_rotateTween = doorPivot
                .DOLocalRotate(targetEuler, rotateDuration)
                .SetEase(ease);
        }

        #endregion
    }
}