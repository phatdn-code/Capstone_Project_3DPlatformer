using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    public class MainMenuTransition : MonoBehaviour
    {
        private const int FixedSlotIndex = 0;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int PanelCountId = Shader.PropertyToID("_PanelCount");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("UI")]
        [SerializeField] private Image transitionImage;
        [SerializeField, Min(1)] private int panelCount = 3;
        [SerializeField] private Color panelColor = Color.black;
        [SerializeField, Min(0f)] private float duration = 0.8f;

        [Header("Scene")]
        [SerializeField] private string storySceneName;
        [SerializeField] private string selectMapSceneName;

        private Material runtimeMaterial;

        private void Awake()
        {
            if (transitionImage == null)
            {
                Debug.LogWarning("[MainMenuTransition] Transition image is null.");
                return;
            }

            runtimeMaterial = new Material(transitionImage.material);
            transitionImage.material = runtimeMaterial;

            runtimeMaterial.SetFloat(ProgressId, 0f);
            runtimeMaterial.SetFloat(PanelCountId, panelCount);
            runtimeMaterial.SetColor(ColorId, panelColor);
        }

        public void StartTransition()
        {
            if (Game.instance == null)
            {
                Debug.LogWarning("[MainMenuTransition] Game.instance is null.");
                return;
            }

            if (!Game.instance.dataLoaded)
                Game.instance.LoadOrCreateState(FixedSlotIndex);

            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            if (runtimeMaterial == null)
                yield break;

            if (duration <= 0f)
            {
                runtimeMaterial.SetFloat(ProgressId, 1f);
                LoadTargetScene();
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = SmoothStep(t);

                runtimeMaterial.SetFloat(ProgressId, smooth);
                yield return null;
            }

            runtimeMaterial.SetFloat(ProgressId, 1f);
            LoadTargetScene();
        }

        private string GetStartScene()
        {
            return Game.instance != null &&
                   Game.instance.dataLoaded &&
                   Game.instance.introStorySeen
                ? selectMapSceneName
                : storySceneName;
        }

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