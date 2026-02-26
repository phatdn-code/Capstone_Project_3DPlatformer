using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/States/Follow Enemy State")]
    public class FollowEnemyState : EnemyState
    {
        #region ===== RUNTIME =====

        protected float m_returnStateTimer;

        #endregion

        #region ===== STATE LIFECYCLE =====

        /// <summary>
        /// Khi vào state Follow: reset timer dùng cho cơ chế "mất mục tiêu -> quay lại state trước".
        /// </summary>
        protected override void OnEnter(Enemy enemy)
        {
            m_returnStateTimer = 0f;
        }

        /// <summary>
        /// Khi thoát state Follow: hiện không cần xử lý gì thêm.
        /// </summary>
        protected override void OnExit(Enemy enemy) { }

        /// <summary>
        /// Mỗi frame: xử lý trọng lực/dính đất, mất mục tiêu, roll attack, extra attack, rồi mới chase.
        /// </summary>
        protected override void OnStep(Enemy enemy)
        {
            ApplyBasicForces(enemy);

            if (TryReturnToLastStateWhenLostTarget(enemy))
                return;

            if (TryHandleRollAttack(enemy))
                return;

            if (TryHandleExtraAttack(enemy))
                return;

            HandleChase(enemy);
        }

        /// <summary>
        /// Va chạm trong Follow state: hiện không cần xử lý gì thêm.
        /// </summary>
        public override void OnContact(Enemy enemy, Collider other) { }

        #endregion

        #region ===== BASIC PHYSICS =====

        /// <summary>
        /// Áp dụng các lực cơ bản cho enemy (trọng lực + dính đất) mỗi frame.
        /// </summary>
        protected virtual void ApplyBasicForces(Enemy enemy)
        {
            enemy.Gravity();
            enemy.SnapToGround();
        }

        #endregion

        #region ===== LOST TARGET / RETURN =====

        /// <summary>
        /// Nếu mất mục tiêu và bật option return: giảm tốc, đếm timer, đủ thời gian thì quay lại state trước.
        /// </summary>
        protected virtual bool TryReturnToLastStateWhenLostTarget(Enemy enemy)
        {
            if (enemy.stats == null || enemy.stats.current == null) return false;

            if (enemy.player == null && enemy.stats.current.returnToLastStateWhenLostTarget)
            {
                m_returnStateTimer += Time.deltaTime;

                enemy.Decelerate(enemy.stats.current.deceleration);

                if (m_returnStateTimer >= enemy.stats.current.returnToLastStateDelay)
                    enemy.states.Change(enemy.states.last);

                return true;
            }

            return false;
        }

        #endregion

        #region ===== ROLL ATTACK =====

        /// <summary>
        /// Ưu tiên xử lý roll attack:
        /// - Nếu đang roll: StepRollAttack và kết thúc frame.
        /// - Nếu đủ điều kiện bắt đầu roll: StartRollAttack và kết thúc frame.
        /// </summary>
        protected virtual bool TryHandleRollAttack(Enemy enemy)
        {
            // Đang roll -> chỉ step roll (không chase, không extra attack)
            if (enemy.IsRollAttacking())
            {
                enemy.StepRollAttack();
                return true;
            }

            // Chỉ start roll khi đang có player
            if (enemy.player != null && enemy.CanStartRollAttack())
            {
                enemy.StartRollAttack();
                return true;
            }

            return false;
        }

        #endregion

        #region ===== EXTRA ATTACK (ANIMATION) =====

        /// <summary>
        /// Xử lý extra attack dạng animation:
        /// - Nếu đang đánh: giảm tốc, xoay mặt về player, kết thúc frame.
        /// - Nếu đủ gần: TryStartExtraAttack và đứng lại đánh ngay.
        /// </summary>
        protected virtual bool TryHandleExtraAttack(Enemy enemy)
        {
            if (enemy.player == null) return false;
            if (enemy.extraAttackMode != ExtraAttackMode.Animated) return false;
            if (enemy.stats == null || enemy.stats.current == null) return false;

            if (enemy.IsExtraAttacking())
            {
                enemy.Decelerate(enemy.stats.current.deceleration);
                FacePlayerSmooth(enemy, enemy.player.position);
                return true;
            }

            float dist = Vector3.Distance(enemy.position, enemy.player.position);
            if (dist <= enemy.extraAttackRange)
            {
                enemy.Decelerate(enemy.stats.current.deceleration);
                FacePlayerSmooth(enemy, enemy.player.position);
                enemy.TryStartExtraAttack();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Xoay enemy nhìn về phía player (chuẩn hoá hướng trên mặt phẳng).
        /// </summary>
        protected virtual void FacePlayerSmooth(Enemy enemy, Vector3 targetWorldPos)
        {
            var look = targetWorldPos - enemy.position;

            // Loại bỏ thành phần theo trục up để tránh ngước lên/xuống
            var lookUpOffset = Vector3.Dot(enemy.transform.up, look);
            var flat = look - enemy.transform.up * lookUpOffset;

            // Đưa về local "up = Vector3.up" để xử lý ổn định
            var localLook = Quaternion.FromToRotation(enemy.transform.up, Vector3.up) * flat;

            if (localLook.sqrMagnitude > 0.0001f)
                enemy.FaceDirectionSmooth(localLook.normalized);
        }

        #endregion

        #region ===== CHASE =====

        /// <summary>
        /// Đuổi theo player theo followAcceleration/followTopSpeed trong stats.
        /// </summary>
        protected virtual void HandleChase(Enemy enemy)
        {
            // Nếu không bật return-to-last mà player bị null thì tránh NullRef
            if (enemy.player == null) return;
            if (enemy.stats == null || enemy.stats.current == null) return;

            Vector3 localDirection = GetLocalFlatDirectionToTarget(enemy, enemy.player.position);
            if (localDirection.sqrMagnitude <= 0.0001f) return;

            localDirection = localDirection.normalized;

            enemy.Accelerate(localDirection, enemy.stats.current.followAcceleration, enemy.stats.current.followTopSpeed);
            enemy.FaceDirectionSmooth(localDirection);
        }

        /// <summary>
        /// Tính hướng đến mục tiêu trên mặt phẳng (loại trục up) và đổi sang hệ local "up = Vector3.up".
        /// </summary>
        protected virtual Vector3 GetLocalFlatDirectionToTarget(Enemy enemy, Vector3 targetWorldPos)
        {
            var head = targetWorldPos - enemy.position;

            var upOffset = Vector3.Dot(enemy.transform.up, head);
            var flat = head - enemy.transform.up * upOffset;

            return Quaternion.FromToRotation(enemy.transform.up, Vector3.up) * flat;
        }

        #endregion
    }
}