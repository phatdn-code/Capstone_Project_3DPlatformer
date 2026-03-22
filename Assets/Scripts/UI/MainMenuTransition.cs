using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    public class MainMenuTransition : MonoBehaviour
    {
        #region Constants
        private const int FixedSlotIndex = 0;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int PanelCountId = Shader.PropertyToID("_PanelCount");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        #endregion

        #region Inspector
        [Header("UI")]
        [SerializeField] private Image transitionImage;
        [SerializeField, Min(1)] private int panelCount = 3;
        [SerializeField] private Color panelColor = Color.black;
        [SerializeField, Min(0f)] private float duration = 0.8f;

        [Header("Scene")]
        [SerializeField] private string storySceneName;
        [SerializeField] private string selectMapSceneName;
        #endregion

        #region Runtime
        private Material runtimeMaterial;
        private Coroutine transitionCoroutine;
        private bool isTransitioning;
        #endregion

        // Khởi tạo material runtime riêng để tránh sửa trực tiếp material gốc.
        private void Awake()
        {
            InitializeRuntimeMaterial();
        }

        // Dọn material runtime khi object bị huỷ để tránh rò rỉ bộ nhớ.
        private void OnDestroy()
        {
            ReleaseRuntimeMaterial();
        }

        // Bắt đầu hiệu ứng chuyển cảnh từ main menu.
        public void StartTransition()
        {
            if (isTransitioning)
                return;

            if (!HasGameInstance())
                return;

            EnsureSaveDataLoaded();

            if (!HasRuntimeMaterial())
            {
                LoadTargetScene();
                return;
            }

            transitionCoroutine = StartCoroutine(TransitionRoutine());
        }

        // Chạy hiệu ứng tăng progress của shader rồi load scene đích.
        private IEnumerator TransitionRoutine()
        {
            isTransitioning = true;

            if (duration <= 0f)
            {
                CompleteTransitionImmediately();
                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                float smoothTime = SmoothStep(normalizedTime);

                SetTransitionProgress(smoothTime);
                yield return null;
            }

            CompleteTransition();
        }

        // Khởi tạo material runtime và áp thông số ban đầu cho shader.
        private void InitializeRuntimeMaterial()
        {
            if (transitionImage == null)
            {
                Debug.LogWarning("[MainMenuTransition] Transition image is null.");
                return;
            }

            if (transitionImage.material == null)
            {
                Debug.LogWarning("[MainMenuTransition] Transition image material is null.");
                return;
            }

            runtimeMaterial = new Material(transitionImage.material);
            transitionImage.material = runtimeMaterial;

            ApplyMaterialSettings();
        }

        // Huỷ material runtime đã clone để tránh bị giữ lại trong bộ nhớ.
        private void ReleaseRuntimeMaterial()
        {
            if (runtimeMaterial == null)
                return;

            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }

        // Áp toàn bộ giá trị cấu hình lên material của hiệu ứng.
        private void ApplyMaterialSettings()
        {
            runtimeMaterial.SetFloat(ProgressId, 0f);
            runtimeMaterial.SetFloat(PanelCountId, panelCount);
            runtimeMaterial.SetColor(ColorId, panelColor);
        }

        // Cập nhật giá trị progress cho shader transition.
        private void SetTransitionProgress(float value)
        {
            if (runtimeMaterial == null)
                return;

            runtimeMaterial.SetFloat(ProgressId, value);
        }

        // Đảm bảo save data đã được load trước khi xác định scene cần vào.
        private void EnsureSaveDataLoaded()
        {
            if (!Game.instance.dataLoaded)
                Game.instance.LoadOrCreateState(FixedSlotIndex);
        }

        // Kiểm tra Game.instance có tồn tại hay không.
        private bool HasGameInstance()
        {
            if (Game.instance != null)
                return true;

            Debug.LogWarning("[MainMenuTransition] Game.instance is null.");
            return false;
        }

        // Kiểm tra đã có material runtime để chạy hiệu ứng hay chưa.
        private bool HasRuntimeMaterial()
        {
            return runtimeMaterial != null;
        }

        // Hoàn tất ngay hiệu ứng khi duration <= 0.
        private void CompleteTransitionImmediately()
        {
            SetTransitionProgress(1f);
            FinishTransitionState();
            LoadTargetScene();
        }

        // Hoàn tất hiệu ứng ở trạng thái cuối rồi load scene.
        private void CompleteTransition()
        {
            SetTransitionProgress(1f);
            FinishTransitionState();
            LoadTargetScene();
        }

        // Reset trạng thái runtime sau khi hiệu ứng kết thúc.
        private void FinishTransitionState()
        {
            isTransitioning = false;
            transitionCoroutine = null;
        }

        // Trả về scene cần load dựa trên trạng thái intro story.
        private string GetTargetScene()
        {
            if (Game.instance != null &&
                Game.instance.dataLoaded &&
                Game.instance.introStorySeen)
            {
                return selectMapSceneName;
            }

            return storySceneName;
        }

        // Load scene đích sau khi kiểm tra dữ liệu hợp lệ.
        private void LoadTargetScene()
        {
            if (GameLoader.instance == null)
            {
                Debug.LogWarning("[MainMenuTransition] GameLoader.instance is null.");
                return;
            }

            string targetScene = GetTargetScene();

            if (string.IsNullOrWhiteSpace(targetScene))
            {
                Debug.LogWarning("[MainMenuTransition] Target scene is empty.");
                return;
            }

            GameLoader.instance.Load(targetScene);
        }

        // Làm mượt chuyển động để hiệu ứng nhìn tự nhiên hơn.
        private float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}


/*using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    public class MainMenuTransition : MonoBehaviour
    {
        private const int FixedSlotIndex = 0;

        [Header("UI")]
        [SerializeField] private RectTransform[] panels;
        [SerializeField] private float duration = 0.8f;

        [Header("Scene")]
        [SerializeField] private string storySceneName;
        [SerializeField] private string selectMapSceneName;

        [Header("Runtime Cache")]
        private Vector2[] startPositions;
        private Image[] images;

        // Bắt đầu hiệu ứng chuyển cảnh và nạp save slot cố định.
        public void StartTransition()
        {
            if (Game.instance == null)
            {
                Debug.LogWarning("[MainMenuTransition] Game.instance is null.");
                return;
            }

            if (!Game.instance.dataLoaded)
                Game.instance.LoadOrCreateState(FixedSlotIndex);

            if (!HasPanels())
            {
                LoadTargetScene();
                return;
            }

            StartCoroutine(TransitionRoutine());
        }

        // Chạy toàn bộ hiệu ứng di chuyển và fade.
        private IEnumerator TransitionRoutine()
        {
            CachePanelData();

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                float smoothTime = SmoothStep(normalizedTime);

                UpdateMovement(smoothTime);
                UpdateFade(normalizedTime);

                yield return null;
            }

            ApplyFinalState();
            LoadTargetScene();
        }

        // Kiểm tra có panel để chạy hiệu ứng hay không.
        private bool HasPanels()
        {
            return panels != null && panels.Length > 0;
        }

        // Xác định scene cần vào sau khi bấm Start.
        private string GetStartScene()
        {
            if (Game.instance != null &&
                Game.instance.dataLoaded && Game.instance.introStorySeen)
                return selectMapSceneName;

            return storySceneName;
        }

        // Load scene đích sau khi xác định trạng thái story.
        private void LoadTargetScene()
        {
            string targetScene = GetStartScene();

            if (string.IsNullOrWhiteSpace(targetScene))
            {
                Debug.LogWarning("[MainMenuTransition] Target scene is empty.");
                return;
            }

            GameLoader.instance.Load(targetScene);
        }

        // Cache vị trí ban đầu và Image của từng panel.
        private void CachePanelData()
        {
            int panelCount = panels.Length;

            startPositions = new Vector2[panelCount];
            images = new Image[panelCount];

            for (int i = 0; i < panelCount; i++)
            {
                if (panels[i] == null)
                    continue;

                startPositions[i] = panels[i].anchoredPosition;
                panels[i].TryGetComponent(out images[i]);
            }
        }

        // Cập nhật vị trí X của các panel.
        private void UpdateMovement(float t)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null)
                    continue;

                float targetX = Mathf.Lerp(startPositions[i].x, 0f, t);
                panels[i].anchoredPosition = new Vector2(targetX, startPositions[i].y);
            }
        }

        // Cập nhật alpha của Image từ 0 đến 1.
        private void UpdateFade(float t)
        {
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                    continue;

                Color color = images[i].color;
                color.a = t;
                images[i].color = color;
            }
        }

        // Ép trạng thái cuối cùng về đúng vị trí và alpha.
        private void ApplyFinalState()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                    panels[i].anchoredPosition = new Vector2(0f, panels[i].anchoredPosition.y);

                if (images[i] == null)
                    continue;

                Color color = images[i].color;
                color.a = 1f;
                images[i].color = color;
            }
        }

        // Làm mượt giá trị nội suy.
        private float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
*/