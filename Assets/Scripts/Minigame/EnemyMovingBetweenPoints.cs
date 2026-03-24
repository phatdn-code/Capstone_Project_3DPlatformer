using UnityEngine;
using PLAYERTWO.PlatformerProject;

public class EnemyMovingBetweenPoints : Enemy
{
    [Header("Patrol Points")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float reachDistance = 0.2f;

    private Vector3 currentTarget;
    private bool initialized = false;

    protected override void Awake()
    {
        base.Awake();

        // 🔥 Tắt AI để tránh conflict
        if (states != null)
            states.enabled = false;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        Patrol();
    }

    private void Patrol()
    {
        // ❗ Check null an toàn
        if (pointA == null || pointB == null)
            return;

        // 🔰 Init lần đầu
        if (!initialized)
        {
            currentTarget = pointA.position;
            initialized = true;
        }

        Vector3 direction = currentTarget - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        // 🎯 Đổi hướng
        if (distance <= reachDistance)
        {
            currentTarget = (currentTarget == pointA.position)
                ? pointB.position
                : pointA.position;

            return;
        }

        direction.Normalize();

        // 🚀 Movement KHÔNG phụ thuộc stats
        Accelerate(direction,
            10f,          // turningDrag
            acceleration, // acceleration riêng
            speed);       // topSpeed

        FaceDirection(direction, rotationSpeed);
    }

    // 💀 Chạm player = chết (optional)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player != null)
                player.ApplyDamage(1, transform.position);
        }
    }
}