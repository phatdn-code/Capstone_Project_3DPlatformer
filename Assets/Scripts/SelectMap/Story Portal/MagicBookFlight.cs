using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Sirenix.OdinInspector;

namespace PLAYERTWO.PlatformerProject
{
    public class MagicBookFlight : MonoBehaviour
    {
        private const int PathResolution = 48;
        private const float MinPointDistance = 0.05f;
        private const float MinVectorSqrMagnitude = 0.0001f;
        private const int MinOrbitSegments = 16;

        //────────────────────────────────────────────────────
        #region === EVENTS ===

        public event Action OnFlightCompleted;
        public event Action OnReturnedToStart;
        public event Action OnOpenAnimationFinished;

        #endregion

        //────────────────────────────────────────────────────
        #region === REFERENCES ===

        [Title("References")]
        [SerializeField, Required] private Transform targetPoint;

        #endregion

        //────────────────────────────────────────────────────
        #region === AURA ===

        [Title("Aura")]
        [SerializeField] private GameObject auraEffect;

        #endregion

        //────────────────────────────────────────────────────
        #region === PRE ORBIT ===

        [Title("Pre Orbit")]
        [SerializeField] private bool enablePreOrbit = true;

        [ShowIf(nameof(enablePreOrbit))]
        [SerializeField, Required]
        private Transform preOrbitCenter;

        [ShowIf(nameof(enablePreOrbit))]
        [SerializeField]
        private float preOrbitHeightOffset = 0f;

        [ShowIf(nameof(enablePreOrbit))]
        [SerializeField, MinValue(0.1f)]
        private float preOrbitRadius = 1.5f;

        [ShowIf(nameof(enablePreOrbit))]
        [SerializeField, MinValue(0.1f)]
        private float preOrbitDuration = 2.5f;

        [ShowIf(nameof(enablePreOrbit))]
        [SerializeField, MinValue(8)]
        private int preOrbitSegments = 36;

        [ShowIf(nameof(enablePreOrbit))]
        [SerializeField]
        private bool preOrbitClockwise = true;

        [ShowIf(nameof(enablePreOrbit))]
        [SerializeField]
        private Ease preOrbitEase = Ease.InOutSine;

        #endregion

        //────────────────────────────────────────────────────
        #region === MAIN FLIGHT ===

        [Title("Main Flight")]
        [SerializeField, MinValue(0.1f)] private float flyDuration = 4f;
        [SerializeField] private Ease flyEase = Ease.Linear;

        #endregion

        //────────────────────────────────────────────────────
        #region === RISE + WEAVE ===

        [Title("Rise + Weave")]
        [SerializeField] private float startLift = 1.95f;

        [SerializeField] private float weaveForward1 = -1.9f;
        [SerializeField] private float weaveForward2 = 2.75f;
        [SerializeField] private float weaveForward3 = 5.6f;
        [SerializeField] private float weaveForward4 = 5f;

        [SerializeField] private float weaveSide1 = 0.65f;
        [SerializeField] private float weaveSide2 = 2.45f;
        [SerializeField] private float weaveSide3 = 2.15f;

        [SerializeField] private float weaveHeight = 0.08f;

        #endregion

        //────────────────────────────────────────────────────
        #region === TOP LOOP ===

        [Title("Top Loop")]
        [SerializeField, MinValue(0.1f)] private float loopRadius = 1.5f;
        [SerializeField, MinValue(8)] private int loopSegments = 32;

        #endregion

        //────────────────────────────────────────────────────
        #region === FINAL GLIDE ===

        [Title("Final Glide")]
        [SerializeField] private float glideHeight = -2.2f;
        [SerializeField, MinValue(0.01f)] private float endRotateDuration = 0.45f;
        [SerializeField] private Ease endRotateEase = Ease.OutSine;

        #endregion

        //────────────────────────────────────────────────────
        #region === RETURN ===

        [Title("Return")]
        [SerializeField, MinValue(0.05f)] private float returnDuration = 0.6f;
        [SerializeField] private Ease returnEase = Ease.OutSine;

        #endregion

        //────────────────────────────────────────────────────
        #region === HEIGHT CONTROL ===

        [Title("Height Control")]
        [SerializeField, Range(0.1f, 1f)] private float heightScale = 1f;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME ===

        private Animator bookAnimator;
        private Sequence flightSequence;
        private Vector3 initialBookPosition;
        private Quaternion initialBookRotation;
        private bool hasCompletedFlight;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY EVENTS ===

        /// <summary>
        /// Lưu trạng thái ban đầu của book khi vào scene.
        /// </summary>
        private void Start()
        {
            CacheInitialBookTransform();
            CacheBookAnimator();

            SetAuraActive(false);
            SetBookOpen(false);
        }

        /// <summary>
        /// Dọn tween khi object bị tắt.
        /// </summary>
        private void OnDisable()
        {
            KillAllTweens();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PUBLIC METHODS ===

        /// <summary>
        /// Chạy toàn bộ chuyển động của sách.
        /// </summary>
        [Button("Fly Book"), GUIColor(0.3f, 0.8f, 1f)]
        public void FlyBook()
        {
            if (!HasValidReferences())
                return;

            KillAllTweens();

            hasCompletedFlight = false;
            SetAuraActive(true);
            SetBookOpen(false);

            flightSequence = DOTween.Sequence();

            Vector3 finalEnd = targetPoint.position;
            Vector3 currentStart = transform.position;

            if (ShouldPlayPreOrbit())
            {
                Vector3[] preOrbitPath = BuildHorizontalPreOrbitPath(transform.position, GetPreOrbitCenterPosition());

                if (HasValidPath(preOrbitPath))
                {
                    currentStart = preOrbitPath[preOrbitPath.Length - 1];
                    AppendPathTween(preOrbitPath, preOrbitDuration, preOrbitEase);
                }
            }

            Vector3[] mainPath = BuildMainFlightPath(currentStart, finalEnd);
            AppendPathTween(mainPath, flyDuration, flyEase);

            flightSequence.OnComplete(CompleteFlight);
        }

        /// <summary>
        /// Đưa sách về vị trí và góc xoay ban đầu.
        /// </summary>
        [Button("Return Book"), GUIColor(1f, 0.75f, 0.3f)]
        public void ReturnBookToStart()
        {
            KillAllTweens();
            SetBookOpen(false);

            Sequence returnSequence = DOTween.Sequence();

            returnSequence.Join(
                transform.DOMove(initialBookPosition, returnDuration)
                    .SetEase(returnEase)
            );

            returnSequence.Join(
                transform.DORotateQuaternion(initialBookRotation, returnDuration)
                    .SetEase(returnEase)
            );

            returnSequence.OnComplete(() =>
            {
                transform.position = initialBookPosition;
                transform.rotation = initialBookRotation;

                hasCompletedFlight = false;
                SetAuraActive(false);

                OnReturnedToStart?.Invoke();
            });

            flightSequence = returnSequence;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === VALIDATION ===

        /// <summary>
        /// Kiểm tra các reference bắt buộc.
        /// </summary>
        private bool HasValidReferences()
        {
            if (targetPoint != null)
                return true;

            Debug.LogWarning("MagicBookFlight: thiếu Target Point.", this);
            return false;
        }

        /// <summary>
        /// Kiểm tra có nên chạy vòng bay đầu hay không.
        /// </summary>
        private bool ShouldPlayPreOrbit()
        {
            return enablePreOrbit && preOrbitCenter != null;
        }

        /// <summary>
        /// Kiểm tra path có đủ điểm để chạy hay không.
        /// </summary>
        private bool HasValidPath(Vector3[] path)
        {
            return path != null && path.Length > 1;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === FLIGHT FLOW ===

        /// <summary>
        /// Lấy Animator trên chính book.
        /// </summary>
        private void CacheBookAnimator()
        {
            bookAnimator = GetComponent<Animator>();
        }

        /// <summary>
        /// Thêm một đoạn bay theo path vào sequence.
        /// </summary>
        private void AppendPathTween(Vector3[] path, float duration, Ease ease)
        {
            if (!HasValidPath(path))
                return;

            flightSequence.Append(
                transform.DOPath(path, duration, PathType.CatmullRom, PathMode.Full3D, PathResolution)
                    .SetEase(ease)
            );
        }

        /// <summary>
        /// Kết thúc bay, snap vị trí và xoay theo target.
        /// </summary>
        private void CompleteFlight()
        {
            hasCompletedFlight = true;
            transform.position = targetPoint.position;

            transform.DORotateQuaternion(targetPoint.rotation, endRotateDuration)
                .SetEase(endRotateEase)
                .OnComplete(() =>
                {
                    transform.rotation = targetPoint.rotation;
                    SetBookOpen(true);
                    OnFlightCompleted?.Invoke();
                });
        }

        /// <summary>
        /// VN: Được gọi bởi Animation Event ở cuối clip mở sách.
        /// </summary>
        public void NotifyOpenAnimationFinished()
        {
            OnOpenAnimationFinished?.Invoke();
        }

        /// <summary>
        /// Bật hoặc tắt effect aura của book.
        /// </summary>
        private void SetAuraActive(bool isActive)
        {
            if (auraEffect != null)
                auraEffect.SetActive(isActive);
        }

        /// <summary>
        /// Bật/tắt trạng thái mở sách trong Animator.
        /// </summary>
        private void SetBookOpen(bool isOpen)
        {
            if (bookAnimator != null)
                bookAnimator.SetBool("isOpen", isOpen);
        }

        /// <summary>
        /// Lưu transform ban đầu của book.
        /// </summary>
        private void CacheInitialBookTransform()
        {
            initialBookPosition = transform.position;
            initialBookRotation = transform.rotation;
        }

        /// <summary>
        /// Hủy toàn bộ tween đang chạy của book.
        /// </summary>
        private void KillAllTweens()
        {
            flightSequence?.Kill();
            flightSequence = null;

            transform.DOKill();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PATH BUILDING ===

        /// <summary>
        /// Tạo vòng tròn ngang quanh tâm pre orbit.
        /// </summary>
        private Vector3[] BuildHorizontalPreOrbitPath(Vector3 start, Vector3 center)
        {
            List<Vector3> points = new();

            int segments = Mathf.Max(MinOrbitSegments, preOrbitSegments);

            Vector3 axisX = preOrbitCenter.right.normalized;
            Vector3 axisZ = preOrbitCenter.forward.normalized;

            Vector3 projected = ProjectOnHorizontalOrbitPlane(start - center, axisX, axisZ);

            if (projected.sqrMagnitude < MinVectorSqrMagnitude)
                projected = axisX * preOrbitRadius;

            Vector3 startDir = projected.normalized;
            float startAngle = Mathf.Atan2(Vector3.Dot(startDir, axisZ), Vector3.Dot(startDir, axisX));

            points.Add(start);

            Vector3 entryPoint = GetHorizontalOrbitPoint(center, axisX, axisZ, startAngle, preOrbitRadius);

            if (Vector3.Distance(start, entryPoint) > MinPointDistance)
            {
                points.Add(Vector3.Lerp(start, entryPoint, 0.5f));
                points.Add(entryPoint);
            }

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = preOrbitClockwise
                    ? startAngle - Mathf.PI * 2f * t
                    : startAngle + Mathf.PI * 2f * t;

                points.Add(GetHorizontalOrbitPoint(center, axisX, axisZ, angle, preOrbitRadius));
            }

            return points.ToArray();
        }

        /// <summary>
        /// Tạo quỹ đạo bay chính của sách.
        /// </summary>
        private Vector3[] BuildMainFlightPath(Vector3 start, Vector3 end)
        {
            List<Vector3> points = new();

            Vector3 up = Vector3.up;
            Vector3 travelDir = GetTravelDirection(start, end, up);
            Vector3 sideDir = GetSideDirection(travelDir, up);

            float scaledStartLift = startLift * heightScale;
            float scaledWeaveHeight = weaveHeight * heightScale;
            float scaledLoopRadius = loopRadius * heightScale;
            float scaledGlideHeight = glideHeight * heightScale;

            Vector3 p1 = start + up * (scaledStartLift * 0.45f) + travelDir * 0.7f;
            Vector3 p2 = start + up * scaledStartLift + travelDir * weaveForward1 - sideDir * weaveSide1;
            Vector3 p3 = start + up * (scaledStartLift + scaledWeaveHeight) + travelDir * weaveForward2 + sideDir * weaveSide2;
            Vector3 p4 = start + up * (scaledStartLift + scaledWeaveHeight * 0.7f) + travelDir * weaveForward3 - sideDir * weaveSide3;
            Vector3 p5 = start + up * (scaledStartLift + scaledWeaveHeight * 0.35f) + travelDir * weaveForward4 + sideDir * (weaveSide3 * 0.5f);

            points.Add(start);
            points.Add(p1);
            points.Add(p2);
            points.Add(p3);
            points.Add(p4);
            points.Add(p5);

            AddTopLoop(points, p5, travelDir, up, scaledLoopRadius);
            AddFinalGlide(points, end, travelDir, up, scaledGlideHeight);

            return points.ToArray();
        }

        /// <summary>
        /// Thêm vòng tròn phía trên vào path chính.
        /// </summary>
        private void AddTopLoop(List<Vector3> points, Vector3 startPoint, Vector3 travelDir, Vector3 up, float scaledLoopRadius)
        {
            Vector3 loopEntry = startPoint + travelDir * 1.2f;
            Vector3 loopCenter = loopEntry + up * scaledLoopRadius;

            points.Add(loopEntry);

            for (int i = 1; i <= loopSegments; i++)
            {
                float t = i / (float)loopSegments;
                float angle = t * Mathf.PI * 2f;

                Vector3 circlePoint =
                    loopCenter
                    + travelDir * Mathf.Sin(angle) * scaledLoopRadius
                    - up * Mathf.Cos(angle) * scaledLoopRadius;

                points.Add(circlePoint);
            }
        }

        /// <summary>
        /// Thêm đoạn lướt cuối về target.
        /// </summary>
        private void AddFinalGlide(List<Vector3> points, Vector3 end, Vector3 travelDir, Vector3 up, float scaledGlideHeight)
        {
            Vector3 loopEntry = points[points.Count - (loopSegments + 1)];

            Vector3 exit1 = loopEntry + travelDir * 2.2f + up * scaledGlideHeight;
            Vector3 exit2 = Vector3.Lerp(exit1, end, 0.35f) + up * (scaledGlideHeight * 0.75f);
            Vector3 exit3 = Vector3.Lerp(exit1, end, 0.7f) + up * (scaledGlideHeight * 0.35f);

            points.Add(exit1);
            points.Add(exit2);
            points.Add(exit3);
            points.Add(end);
        }

        /// <summary>
        /// Lấy hướng bay chính từ start tới end.
        /// </summary>
        private Vector3 GetTravelDirection(Vector3 start, Vector3 end, Vector3 up)
        {
            Vector3 direction = Vector3.ProjectOnPlane(end - start, up).normalized;

            if (direction.sqrMagnitude < MinVectorSqrMagnitude)
                direction = Vector3.ProjectOnPlane(transform.forward, up).normalized;

            if (direction.sqrMagnitude < MinVectorSqrMagnitude)
                direction = transform.forward.normalized;

            return direction;
        }

        /// <summary>
        /// Lấy hướng ngang vuông góc với hướng bay.
        /// </summary>
        private Vector3 GetSideDirection(Vector3 travelDir, Vector3 up)
        {
            Vector3 side = Vector3.Cross(up, travelDir).normalized;

            if (side.sqrMagnitude < MinVectorSqrMagnitude)
                side = transform.right.normalized;

            return side;
        }

        /// <summary>
        /// Lấy vị trí tâm pre orbit sau khi cộng offset độ cao.
        /// </summary>
        private Vector3 GetPreOrbitCenterPosition()
        {
            if (preOrbitCenter == null)
                return Vector3.zero;

            return preOrbitCenter.position + Vector3.up * preOrbitHeightOffset;
        }

        /// <summary>
        /// Chiếu vector lên mặt phẳng vòng ngang.
        /// </summary>
        private Vector3 ProjectOnHorizontalOrbitPlane(Vector3 offset, Vector3 axisX, Vector3 axisZ)
        {
            return Vector3.Project(offset, axisX) + Vector3.Project(offset, axisZ);
        }

        /// <summary>
        /// Lấy điểm trên vòng tròn ngang.
        /// </summary>
        private Vector3 GetHorizontalOrbitPoint(Vector3 center, Vector3 axisX, Vector3 axisZ, float angle, float radius)
        {
            return center
                 + axisX * Mathf.Cos(angle) * radius
                 + axisZ * Mathf.Sin(angle) * radius;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === GIZMOS ===

#if UNITY_EDITOR
        /// <summary>
        /// Vẽ preview path trong Scene.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (targetPoint == null)
                return;

            Vector3 drawStart = Application.isPlaying ? initialBookPosition : transform.position;
            Vector3 finalEnd = targetPoint.position;

            if (ShouldPlayPreOrbit())
                DrawPreOrbitPreview(ref drawStart);

            DrawMainPathPreview(drawStart, finalEnd);
        }

        /// <summary>
        /// Vẽ preview vòng bay đầu.
        /// </summary>
        private void DrawPreOrbitPreview(ref Vector3 drawStart)
        {
            Vector3 orbitCenter = GetPreOrbitCenterPosition();
            Vector3[] prePreview = BuildHorizontalPreOrbitPath(drawStart, orbitCenter);

            Gizmos.color = Color.yellow;
            DrawPathGizmos(prePreview, 0.04f);

            drawStart = prePreview[prePreview.Length - 1];

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(orbitCenter, 0.08f);
        }

        /// <summary>
        /// Vẽ preview path chính.
        /// </summary>
        private void DrawMainPathPreview(Vector3 start, Vector3 end)
        {
            Vector3[] preview = BuildMainFlightPath(start, end);

            Gizmos.color = Color.cyan;
            DrawPathGizmos(preview, 0.05f);

            Gizmos.DrawSphere(preview[preview.Length - 1], 0.06f);
        }

        /// <summary>
        /// Vẽ line và điểm cho một path.
        /// </summary>
        private void DrawPathGizmos(Vector3[] path, float pointSize)
        {
            if (!HasValidPath(path))
                return;

            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
                Gizmos.DrawSphere(path[i], pointSize);
            }
        }
#endif

        #endregion
    }
}