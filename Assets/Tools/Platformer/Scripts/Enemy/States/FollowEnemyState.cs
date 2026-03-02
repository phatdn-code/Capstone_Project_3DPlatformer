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

        /// <summary>Vào Follow: reset timer dùng cho cơ chế "mất mục tiêu -> quay lại state trước".</summary>
        protected override void OnEnter(Enemy enemy)
        {
            m_returnStateTimer = 0f;
        }

        /// <summary>Thoát Follow: hiện không cần xử lý thêm.</summary>
        protected override void OnExit(Enemy enemy) { }

        /// <summary>Update mỗi frame: physics cơ bản -> mất mục tiêu -> skill -> chase.</summary>
        protected override void OnStep(Enemy enemy)
        {
            ApplyBasicForces(enemy);

            if (TryReturnToLastStateWhenLostTarget(enemy))
                return;

            if (TryHandleRollAttack(enemy))
                return;

            if (TryHandleRangedAttack(enemy))
                return;

            if (TryHandleExtraAttack(enemy))
                return;

            // Ưu tiên thấp để không đổi thứ tự logic hiện có
            if (TryHandleSprayAttack(enemy))
                return;

            HandleChase(enemy);
        }

        /// <summary>Va chạm trong Follow: hiện không xử lý thêm.</summary>
        public override void OnContact(Enemy enemy, Collider other) { }

        #endregion

        #region ===== BASIC PHYSICS =====

        /// <summary>Áp dụng trọng lực + dính đất mỗi frame.</summary>
        protected virtual void ApplyBasicForces(Enemy enemy)
        {
            enemy.Gravity();
            enemy.SnapToGround();
        }

        #endregion

        #region ===== LOST TARGET / RETURN =====

        /// <summary>Nếu mất mục tiêu và bật return: giảm tốc + đếm timer, đủ thời gian thì quay lại state trước.</summary>
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

        /// <summary>Ưu tiên xử lý roll: đang roll thì step, đủ điều kiện thì start.</summary>
        protected virtual bool TryHandleRollAttack(Enemy enemy)
        {
            if (enemy.IsRollAttacking())
            {
                enemy.StepRollAttack();
                return true;
            }

            if (enemy.player != null && enemy.CanStartRollAttack())
            {
                enemy.StartRollAttack();
                return true;
            }

            return false;
        }

        #endregion

        #region ===== RANGED ATTACK (PROJECTILE) =====

        /// <summary>Xử lý ranged: đang bắn thì đứng lại + nhìn player, đủ điều kiện thì start bắn.</summary>
        protected virtual bool TryHandleRangedAttack(Enemy enemy)
        {
            if (enemy.player == null) return false;
            if (enemy.stats == null || enemy.stats.current == null) return false;

            if (enemy.IsRangedAttacking())
            {
                enemy.Decelerate(enemy.stats.current.deceleration);
                FacePlayerSmooth(enemy, enemy.player.position);
                return true;
            }

            if (enemy.CanStartRangedAttack())
            {
                enemy.Decelerate(enemy.stats.current.deceleration);
                FacePlayerSmooth(enemy, enemy.player.position);
                enemy.TryStartRangedAttack();
                return true;
            }

            return false;
        }

        #endregion

        #region ===== EXTRA ATTACK (ANIMATION) =====

        /// <summary>Xử lý extra: đang đánh thì đứng lại + nhìn player, đủ gần thì TryStartExtraAttack.</summary>
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

        #endregion

        #region ===== SPRAY ATTACK =====

        /// <summary>Xử lý SprayAttack: đang spray thì step, đủ điều kiện thì start.</summary>
        protected virtual bool TryHandleSprayAttack(Enemy enemy)
        {
            // Nếu Enemy.cs chưa có SprayAttack thì bạn xoá region này
            if (enemy.IsSprayAttacking())
            {
                enemy.StepSprayAttack();
                return true;
            }

            if (enemy.player != null && enemy.CanStartSprayAttack())
            {
                enemy.StartSprayAttack();
                return true;
            }

            return false;
        }

        #endregion

        #region ===== CHASE =====

        /// <summary>Đuổi theo player theo followAcceleration/followTopSpeed trong stats.</summary>
        protected virtual void HandleChase(Enemy enemy)
        {
            if (enemy.player == null) return;
            if (enemy.stats == null || enemy.stats.current == null) return;

            Vector3 localDirection = GetLocalFlatDirectionToTarget(enemy, enemy.player.position);
            if (localDirection.sqrMagnitude <= 0.0001f) return;

            localDirection = localDirection.normalized;

            enemy.Accelerate(localDirection, enemy.stats.current.followAcceleration, enemy.stats.current.followTopSpeed);
            enemy.FaceDirectionSmooth(localDirection);
        }

        #endregion

        #region ===== AIM HELPERS =====

        /// <summary>Xoay enemy nhìn về phía mục tiêu (loại trục up, rồi đổi về local up = Vector3.up).</summary>
        protected virtual void FacePlayerSmooth(Enemy enemy, Vector3 targetWorldPos)
        {
            var look = targetWorldPos - enemy.position;

            var upOffset = Vector3.Dot(enemy.transform.up, look);
            var flat = look - enemy.transform.up * upOffset;

            var localLook = Quaternion.FromToRotation(enemy.transform.up, Vector3.up) * flat;

            if (localLook.sqrMagnitude > 0.0001f)
                enemy.FaceDirectionSmooth(localLook.normalized);
        }

        /// <summary>Tính hướng đến mục tiêu trên mặt phẳng và đổi sang hệ local up = Vector3.up.</summary>
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