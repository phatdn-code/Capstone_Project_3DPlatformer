using UnityEngine;
using UnityEngine.Splines;

namespace PLAYERTWO.PlatformerProject
{
    public class RailGrindPlayerState : PlayerState
    {
        protected bool m_backwards;
        protected float m_speed;
        protected float m_lastDahTime;

        protected const int k_splineResolution = 128;

        protected override void OnEnter(Player player)
        {
            Evaluate(player, out var point, out var forward, out var upward, out _);
            UpdatePosition(player, point, upward);

            m_backwards = Vector3.Dot(player.transform.forward, forward) < 0;
            m_speed = Mathf.Max(
                player.lateralVelocity.magnitude,
                player.stats.current.minGrindInitialSpeed
            );
            player.velocity = Vector3.zero;
            player.UseCustomCollision(player.stats.current.useCustomCollision);
        }

        protected override void OnExit(Player player)
        {
            // Trả player về tư thế thẳng khi rời rail
            ResetRotation(player);

            player.ExitRail();
            player.UseCustomCollision(false);
        }

        protected override void OnStep(Player player)
        {
            int jumpsBefore = player.jumpCounter;
            player.Jump();

            // Nếu vừa nhảy khỏi rail thì dừng luôn logic grind của frame này
            // tránh bị rail xoay thêm 1 lần nữa.
            if (player.jumpCounter > jumpsBefore)
            {
                ResetRotation(player);
                return;
            }

            if (player.onRails)
            {
                Evaluate(player, out var point, out var forward, out var upward, out var t);

                var direction = m_backwards ? -forward : forward;
                var worldUp = player.CurrentWorldUp();
                var factor = Vector3.Dot(worldUp, direction);
                var multiplier =
                    factor <= 0
                        ? player.stats.current.slopeDownwardForce
                        : player.stats.current.slopeUpwardForce;

                HandleDeceleration(player);
                HandleDash(player);

                if (player.stats.current.applyGrindingSlopeFactor)
                    m_speed -= factor * multiplier * Time.deltaTime;

                m_speed = Mathf.Clamp(
                    m_speed,
                    player.stats.current.minGrindSpeed,
                    player.stats.current.grindTopSpeed
                );

                Rotate(player, direction, upward);
                player.velocity = direction * m_speed;

                if (player.rails.Spline.Closed || (t > 0 && t < 0.9f))
                    UpdatePosition(player, point, upward);
            }
            else
            {
                player.states.Change<FallPlayerState>();
            }
        }

        public override void OnContact(Player player, Collider other) { }

        protected virtual void Evaluate(
            Player player,
            out Vector3 point,
            out Vector3 forward,
            out Vector3 upward,
            out float t
        )
        {
            var origin = player.rails.transform.InverseTransformPoint(player.transform.position);

            SplineUtility.GetNearestPoint(
                player.rails.Spline,
                origin,
                out var nearest,
                out t,
                k_splineResolution
            );

            point = player.rails.transform.TransformPoint(nearest);
            forward = Vector3.Normalize(player.rails.EvaluateTangent(t));
            upward = Vector3.Normalize(player.rails.EvaluateUpVector(t));
        }

        protected virtual void HandleDeceleration(Player player)
        {
            if (player.stats.current.canGrindBrake && player.inputs.GetGrindBrake())
            {
                var decelerationDelta =
                    player.stats.current.grindBrakeDeceleration * Time.deltaTime;
                m_speed = Mathf.MoveTowards(m_speed, 0, decelerationDelta);
            }
        }

        protected virtual void HandleDash(Player player)
        {
            if (
                player.stats.current.canGrindDash
                && player.inputs.GetDashDown()
                && Time.time >= m_lastDahTime + player.stats.current.grindDashCoolDown
            )
            {
                m_lastDahTime = Time.time;
                m_speed = player.stats.current.grindDashForce;
                player.playerEvents.OnDashStarted.Invoke();
            }
        }

        protected virtual void UpdatePosition(Player player, Vector3 point, Vector3 upward) =>
            player.transform.position = point + upward * GetDistanceToRail(player);

        protected virtual void Rotate(Player player, Vector3 forward, Vector3 upward)
        {
            if (forward.sqrMagnitude == 0f || upward.sqrMagnitude == 0f)
                return;

            // Xoay trực tiếp theo tangent + up của rail, đỡ bị lệch roll tích lũy
            player.transform.rotation = Quaternion.LookRotation(
                forward.normalized,
                upward.normalized
            );
        }

        protected virtual void ResetRotation(Player player)
        {
            Vector3 worldUp = player.CurrentWorldUp();
            if (worldUp.sqrMagnitude < 0.0001f)
                worldUp = Vector3.up;

            // Giữ hướng nhìn nhưng bỏ toàn bộ độ nghiêng theo rail
            Vector3 flatForward = Vector3.ProjectOnPlane(player.transform.forward, worldUp);

            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.ProjectOnPlane(player.pathForward, worldUp);

            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.Cross(player.transform.right, worldUp);

            player.transform.rotation = Quaternion.LookRotation(flatForward.normalized, worldUp);
        }

        protected virtual float GetDistanceToRail(Player player) =>
            player.originalHeight * 0.5f + player.stats.current.grindRadiusOffset;
    }
}