using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using PLAYERTWO.PlatformerProject;
using BossBomb = PLAYERTWO.PlatformerProject.BossBomb;
using BossFireball = PLAYERTWO.PlatformerProject.BossFireball;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Soldier Robot Boss - Đơn giản hóa hoàn toàn
    /// </summary>
    public class SoldierRobot : BaseBoss
    {
        [Header("Bomb Settings")]
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private Transform rightHandSpawnPoint;
        [SerializeField] private Transform leftHandSpawnPoint;
        [SerializeField] private float bombThrowForce = 10f;
        [SerializeField] private float bombDamage = 50f;
        [SerializeField] private float bombFuseTime = 3f;
        [SerializeField] private float bombExplosionRadius = 5f;
        [SerializeField] private float bombExplosionForce = 15f;

        [Header("Fireball Settings")]
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private Transform fireballSpawnPoint;
        [SerializeField] private float fireballDamage = 30f;
        [SerializeField] private float fireballSpeed = 8f;
        [SerializeField] private float fireballLifetime = 5f;

        [Header("Movement Settings")]
        [SerializeField] private Transform centerPoint;
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float movementRestTime = 2f;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float rotationThreshold = 5f; // Góc tối thiểu để bắt đầu xoay

        [Header("Animation")]
        [SerializeField] private Animator skinAnimator;
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("Effects")]
        [SerializeField] private GameObject bombExplosionEffect;
        [SerializeField] private GameObject fireballEffect;

        [Header("Player")]
        [SerializeField] private new Player player;
        [SerializeField] private bool autoFindPlayer = true;

        // Components
        private NavMeshAgent agent;

        // Attack sequence state
        private bool m_isInAttackSequence = false;
        private int m_currentStep = 0; // 0: bomb right, 1: bomb left, 2: fireball, 3: move
        private bool m_isMoving = false;

        protected override void Start()
        {
            base.Start();

            // Tự động lấy NavMeshAgent
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError($"❌ NavMeshAgent component not found!");
            }

            // Tự động tìm Player
            if (player == null && autoFindPlayer)
            {
                player = Object.FindFirstObjectByType<Player>();
            }

            // Bắt đầu attack sequence
            StartAttackSequence();
        }

        private void StartAttackSequence()
        {
            if (m_isInAttackSequence) return;

            m_isInAttackSequence = true;
            m_currentStep = 0;

            Debug.Log($"🎯 Starting attack sequence");

            // Bắt đầu với bomb tay phải
            StartCoroutine(ExecuteAttackSequence());
        }

        private IEnumerator ExecuteAttackSequence()
        {
            // Bước 1: Bắn bomb tay phải
            Debug.Log($"💣 Step 1: Shooting bomb with right hand");
            ShootBomb(true);
            yield return new WaitForSeconds(1f);

            // Bước 2: Bắn bomb tay trái
            Debug.Log($"💣 Step 2: Shooting bomb with left hand");
            ShootBomb(false);
            yield return new WaitForSeconds(3f);

            // Bước 3: Bắn fireball (1-2 quả)
            int fireballCount = Random.Range(1, 3);
            Debug.Log($"🔥 Step 3: Shooting {fireballCount} fireballs");

            for (int i = 0; i < fireballCount; i++)
            {
                ShootFireball();
                if (i < fireballCount - 1) // Không nghỉ sau quả cuối
                    yield return new WaitForSeconds(2f);
            }

            yield return new WaitForSeconds(5f);

            // Bước 4: Di chuyển
            Debug.Log($"🚶 Step 4: Moving to new position");
            yield return StartCoroutine(MoveToNewPosition());

            // Reset và lặp lại
            m_isInAttackSequence = false;
            StartAttackSequence();
        }

        private void ShootBomb(bool useRightHand)
        {
            if (m_isMoving) return;

            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null) return;

            // Xoay về hướng player trước khi bắn
            StartCoroutine(RotateTowardsPlayer(() =>
            {
                // Trigger animation sau khi đã xoay
                if (skinAnimator != null)
                {
                    if (useRightHand)
                        skinAnimator.SetTrigger("RightHandShoot");
                    else
                        skinAnimator.SetTrigger("LeftHandShoot");
                }
            }));
        }

        private void ShootFireball()
        {
            if (m_isMoving) return;

            // Xoay về hướng player trước khi bắn
            StartCoroutine(RotateTowardsPlayer(() =>
            {
                // Trigger animation sau khi đã xoay
                if (skinAnimator != null)
                    skinAnimator.SetTrigger("FireballShoot");
            }));
        }

        // Animation Events
        public void ShootBombFromAnimation(bool useRightHand)
        {
            if (m_isMoving) return;

            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null) return;

            GameObject bomb = Instantiate(bombPrefab, spawnPoint.position, spawnPoint.rotation);
            SetupBomb(bomb);
        }

        public void CreateFireballFromAnimation()
        {
            if (m_isMoving) return;

            if (fireballPrefab == null || fireballSpawnPoint == null) return;

            GameObject fireball = Instantiate(fireballPrefab, fireballSpawnPoint.position, fireballSpawnPoint.rotation);
            SetupFireball(fireball);
        }

        private void SetupBomb(GameObject bomb)
        {
            var bombComponent = bomb.GetComponent<BossBomb>();
            if (bombComponent == null)
            {
                bombComponent = bomb.AddComponent<BossBomb>();
            }

            if (player == null && autoFindPlayer)
                player = Object.FindFirstObjectByType<Player>();

            if (player != null)
                bombComponent.SetupFromPool(player, bombThrowForce, (int)bombDamage, bombFuseTime, bombExplosionRadius, bombExplosionForce);
        }

        private void SetupFireball(GameObject fireball)
        {
            var fireballComponent = fireball.GetComponent<BossFireball>();

            if (fireballComponent == null)
                fireballComponent = fireball.AddComponent<BossFireball>();

            fireballComponent.SetupFromPool((int)fireballDamage, fireballSpeed, fireballLifetime);
        }

        private IEnumerator MoveToNewPosition()
        {
            if (m_isMoving) yield break;

            m_isMoving = true;

            // Tìm vị trí mới cách xa vị trí hiện tại
            Vector3 newPosition = GetNewPosition();
            agent.SetDestination(newPosition);

            if (skinAnimator != null)
                skinAnimator.SetBool("isMoving", true);

            // Chờ đến nơi
            while (agent.pathPending || agent.remainingDistance > 0.2f)
                yield return null;

            if (skinAnimator != null)
                skinAnimator.SetBool("isMoving", false);

            yield return new WaitForSeconds(movementRestTime);

            m_isMoving = false;
        }

        private Vector3 GetNewPosition()
        {
            Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
            Vector3 currentPos = transform.position;

            // Tìm vị trí cách xa vị trí hiện tại
            Vector3 randomDirection;
            int attempts = 0;
            float minDistance = wanderRadius * 0.5f;

            do
            {
                randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += center;
                attempts++;
            }
            while (Vector3.Distance(currentPos, randomDirection) < minDistance && attempts < 10);

            NavMeshHit hit;
            bool found = NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1);

            if (!found)
                found = NavMesh.SamplePosition(randomDirection, out hit, wanderRadius * 2f, 1);

            if (!found)
                return transform.position;

            return hit.position;
        }

        /// <summary>
        /// Xoay boss về hướng player một cách mượt mà
        /// </summary>
        private IEnumerator RotateTowardsPlayer(System.Action onComplete = null)
        {
            if (player == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            directionToPlayer.y = 0; // Chỉ xoay trên trục Y

            if (directionToPlayer.magnitude < 0.1f)
            {
                onComplete?.Invoke();
                yield break;
            }

            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

            // Chỉ xoay nếu góc khác biệt đủ lớn
            if (angleDifference > rotationThreshold)
            {
                float rotationTime = angleDifference / (rotationSpeed * 90f); // Tính thời gian xoay

                Quaternion startRotation = transform.rotation;
                float elapsedTime = 0f;

                while (elapsedTime < rotationTime)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / rotationTime;

                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                    yield return null;
                }

                transform.rotation = targetRotation;
            }

            onComplete?.Invoke();
        }

        // Public methods for animation events
        public void TriggerRightHandShoot()
        {
            if (m_isMoving) return;
            if (skinAnimator != null)
                skinAnimator.SetTrigger("RightHandShoot");
        }

        public void TriggerLeftHandShoot()
        {
            if (m_isMoving) return;
            if (skinAnimator != null)
                skinAnimator.SetTrigger("LeftHandShoot");
        }

        public void TriggerFireballShoot()
        {
            if (m_isMoving) return;
            if (skinAnimator != null)
                skinAnimator.SetTrigger("FireballShoot");
        }

        // Public properties and methods for other classes
        public Animator SkinAnimator => skinAnimator;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public GameObject BombPrefab => bombPrefab;
        public GameObject FireballPrefab => fireballPrefab;
        public GameObject BombExplosionEffect => bombExplosionEffect;
        public GameObject FireballEffect => fireballEffect;
        public Transform RightHandSpawnPoint => rightHandSpawnPoint;
        public Transform LeftHandSpawnPoint => leftHandSpawnPoint;
        public Transform FireballSpawnPoint => fireballSpawnPoint;

        // Setter methods for factory
        public void SetBombPrefab(GameObject newPrefab) => bombPrefab = newPrefab;
        public void SetFireballPrefab(GameObject newPrefab) => fireballPrefab = newPrefab;
        public void SetBombExplosionEffect(GameObject newEffect) => bombExplosionEffect = newEffect;
        public void SetFireballEffect(GameObject newEffect) => fireballEffect = newEffect;
        public void SetRightHandSpawnPoint(Transform newPoint) => rightHandSpawnPoint = newPoint;
        public void SetLeftHandSpawnPoint(Transform newPoint) => leftHandSpawnPoint = newPoint;
        public void SetFireballSpawnPoint(Transform newPoint) => fireballSpawnPoint = newPoint;
        public void SetSkinAnimator(Animator newAnimator) => skinAnimator = newAnimator;
        public void SetAnimatorController(RuntimeAnimatorController newController) => animatorController = newController;

        // Attack state info
        public string GetAttackStateInfo()
        {
            return $"AttackSequence: {m_isInAttackSequence}, Step: {m_currentStep}, Moving: {m_isMoving}";
        }
    }
}