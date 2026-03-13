using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Panel")]
    public class Panel : MonoBehaviour, IEntityContact
    {
        public enum ActivationEntity
        {
            Player,
            Other,
            Any,
            None,
        }

        public enum ActivationCollider
        {
            Any,
            None,
        }

        [Header("Activation Settings")]
        public bool autoToggle;
        public ActivationEntity activationEntity = ActivationEntity.Player;
        public ActivationCollider activationCollider = ActivationCollider.None;
        public bool requireStomp;

        // Bật cái này thì chỉ cần chạm là kích hoạt ngay.
        // Khi bật, sẽ bỏ qua check đạp từ trên xuống và stomp.
        public bool activateOnTouch;

        [Header("Audio Settings")]
        public AudioClip activateClip;
        public AudioClip deactivateClip;

        [Header("Events")]
        public UnityEvent OnActivate;
        public UnityEvent OnDeactivate;

        protected Collider m_collider;
        protected AudioSource m_audio;

        protected Collider m_entityActivator;
        protected Collider m_otherActivator;

        public bool activated { get; protected set; }

        // Lấy component cần dùng.
        protected virtual void Awake()
        {
            gameObject.tag = GameTags.Panel;
            m_collider = GetComponent<Collider>();
            m_audio = GetComponent<AudioSource>();
        }

        // Bật panel.
        public virtual void Activate()
        {
            if (activated)
                return;

            if (activateClip)
                m_audio.PlayOneShot(activateClip);

            activated = true;
            OnActivate?.Invoke();
        }

        // Tắt panel.
        public virtual void Deactivate()
        {
            if (!activated)
                return;

            if (deactivateClip)
                m_audio.PlayOneShot(deactivateClip);

            activated = false;
            OnDeactivate?.Invoke();
        }

        // Giữ trạng thái khi còn đang chạm.
        protected virtual void Update()
        {
            if (m_entityActivator == null && m_otherActivator == null)
                return;

            var center = m_collider.bounds.center;
            var contactOffset = Physics.defaultContactOffset + 0.1f;
            var size = m_collider.bounds.size + Vector3.up * contactOffset;
            var bounds = new Bounds(center, size);

            var intersectsEntity =
                m_entityActivator != null && bounds.Intersects(m_entityActivator.bounds);
            var intersectsOther =
                m_otherActivator != null && bounds.Intersects(m_otherActivator.bounds);

            if (intersectsEntity || intersectsOther)
            {
                if (!activated)
                    Activate();
            }
            else
            {
                if (!intersectsEntity)
                    m_entityActivator = null;

                if (!intersectsOther)
                    m_otherActivator = null;

                if (autoToggle)
                    Deactivate();
            }
        }

        // Nhận tiếp xúc từ hệ Entity của project.
        public void OnEntityContact(Entity entity)
        {
            if (entity == null || activationEntity == ActivationEntity.None)
                return;

            if (!CanEntityActivate(entity))
                return;

            m_entityActivator = entity.controller;

            if (activateOnTouch)
                Activate();
        }

        // Va chạm vật lý - chạm lần đầu.
        protected virtual void OnCollisionEnter(Collision collision)
        {
            HandleColliderContact(collision.collider);
        }

        // Va chạm vật lý - đang chạm liên tục.
        protected virtual void OnCollisionStay(Collision collision)
        {
            HandleColliderContact(collision.collider);
        }

        // Trigger - chạm lần đầu.
        protected virtual void OnTriggerEnter(Collider other)
        {
            HandleColliderContact(other);
        }

        // Trigger - đang chạm liên tục.
        protected virtual void OnTriggerStay(Collider other)
        {
            HandleColliderContact(other);
        }

        // Trigger rời khỏi.
        protected virtual void OnTriggerExit(Collider other)
        {
            TryClearActivator(other);
        }

        // Collision rời khỏi.
        protected virtual void OnCollisionExit(Collision collision)
        {
            TryClearActivator(collision.collider);
        }

        // Xử lý khi có collider chạm vào panel.
        protected virtual void HandleColliderContact(Collider other)
        {
            if (other == null)
                return;

            var entity = other.GetComponentInParent<Entity>();

            // Nếu là Entity thì ưu tiên dùng nhánh entity.
            if (entity != null)
            {
                if (activationEntity == ActivationEntity.None)
                    return;

                if (!CanEntityActivate(entity))
                    return;

                m_entityActivator = entity.controller;

                if (activateOnTouch)
                    Activate();

                return;
            }

            // Nếu không phải entity thì dùng nhánh collider thường.
            if (activationCollider == ActivationCollider.None)
                return;

            m_otherActivator = other;

            if (activateOnTouch)
                Activate();
        }

        // Xóa activator khi rời panel.
        protected virtual void TryClearActivator(Collider other)
        {
            if (other == null)
                return;

            var entity = other.GetComponentInParent<Entity>();

            if (entity != null && entity.controller == m_entityActivator)
                m_entityActivator = null;

            if (m_otherActivator == other)
                m_otherActivator = null;

            if (autoToggle && m_entityActivator == null && m_otherActivator == null)
                Deactivate();
        }

        // Kiểm tra entity có được phép kích hoạt không.
        protected virtual bool CanEntityActivate(Entity entity)
        {
            if (entity == null)
                return false;

            switch (activationEntity)
            {
                case ActivationEntity.Player:
                    if (!(entity is Player))
                        return false;
                    break;

                case ActivationEntity.Other:
                    if (entity is Player)
                        return false;
                    break;

                case ActivationEntity.Any:
                    break;

                case ActivationEntity.None:
                    return false;
            }

            // Mode chạm là ăn ngay: bỏ qua check hướng rơi và stomp.
            if (activateOnTouch)
                return true;

            // Mode cũ: phải tiếp xúc từ phía trên.
            if (entity.verticalVelocity.y > 0)
                return false;

            if (!BoundsHelper.IsBellowPoint(m_collider, entity.stepPosition))
                return false;

            // Nếu yêu cầu stomp thì chỉ Player đang stomp mới kích hoạt được.
            if (requireStomp && entity is Player player)
            {
                if (!player.states.IsCurrentOfType(typeof(StompPlayerState)))
                    return false;
            }

            return true;
        }

        // Reset trạng thái bằng tay.
        public void SetActivatedFalse()
        {
            activated = false;
        }
    }
}