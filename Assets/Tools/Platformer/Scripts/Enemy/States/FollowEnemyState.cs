using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/States/Follow Enemy State")]
    public class FollowEnemyState : EnemyState
    {
        protected float m_returnStateTimer;

        protected override void OnEnter(Enemy enemy)
        {
            m_returnStateTimer = 0f;
        }

        protected override void OnExit(Enemy enemy) { }

        protected override void OnStep(Enemy enemy)
        {
            enemy.Gravity();
            enemy.SnapToGround();

            if (!enemy.player && enemy.stats.current.returnToLastStateWhenLostTarget)
            {
                m_returnStateTimer += Time.deltaTime;
                enemy.Decelerate(enemy.stats.current.deceleration);

                if (m_returnStateTimer >= enemy.stats.current.returnToLastStateDelay)
                    enemy.states.Change(enemy.states.last);

                return;
            }

            // ===== Extra Attack (Animation) =====
            if (enemy.extraAttackMode == Enemy.ExtraAttackMode.Animated && enemy.player != null)
            {
                if (enemy.IsExtraAttacking())
                {
                    enemy.Decelerate(enemy.stats.current.deceleration);

                    var look = enemy.player.position - enemy.position;
                    var lookUpOffset = Vector3.Dot(enemy.transform.up, look);
                    var flat = look - enemy.transform.up * lookUpOffset;
                    var localLook = Quaternion.FromToRotation(enemy.transform.up, Vector3.up) * flat;

                    if (localLook.sqrMagnitude > 0.0001f)
                        enemy.FaceDirectionSmooth(localLook.normalized);

                    return;
                }

                float dist = Vector3.Distance(enemy.position, enemy.player.position);
                if (dist <= enemy.extraAttackRange)
                {
                    enemy.TryStartExtraAttack();
                    // return; // bật nếu muốn đứng lại đánh ngay
                }
            }

            var head = enemy.player.position - enemy.position;
            var upOffset = Vector3.Dot(enemy.transform.up, head);
            var direction = head - enemy.transform.up * upOffset;
            var localDirection = Quaternion.FromToRotation(enemy.transform.up, Vector3.up) * direction;

            localDirection = localDirection.normalized;

            enemy.Accelerate(localDirection, enemy.stats.current.followAcceleration, enemy.stats.current.followTopSpeed);
            enemy.FaceDirectionSmooth(localDirection);
        }

        public override void OnContact(Enemy enemy, Collider other) { }
    }
}