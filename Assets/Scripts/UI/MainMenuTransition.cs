using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PLAYERTWO.PlatformerProject;

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

// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;
// using System.Collections;

// public class MainMenuTransition : MonoBehaviour
// {
//     [Header("Cài đặt UI")]
//     public RectTransform[] rectangles; // Kéo 3 Image vào đây
//     public float duration = 0.8f;      // Thời gian di chuyển (giây)
//     public string nextSceneName;       // Tên scene muốn chuyển đến

//     [ContextMenu("Bắt đầu chuyển cảnh")]
//     public void StartTransition()
//     {
//         StartCoroutine(MoveAndFadeRoutine());
//     }

//     IEnumerator MoveAndFadeRoutine()
/*     {
        float elapsedTime = 0f;

        // Lưu vị trí bắt đầu
        Vector2[] startPositions = new Vector2[rectangles.Length];
        for (int i = 0; i < rectangles.Length; i++)
        {
            startPositions[i] = rectangles[i].anchoredPosition;
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Làm mượt chuyển động (SmoothStep)
            float smoothT = t * t * (3f - 2f * t);

            for (int i = 0; i < rectangles.Length; i++)
            {
                // 1. Di chuyển vị trí X về 0
                float newX = Mathf.Lerp(startPositions[i].x, 0, smoothT);
                rectangles[i].anchoredPosition = new Vector2(newX, startPositions[i].y);

                // 2. Hiệu ứng Fade In (từ 0 lên 1)
                if (rectangles[i].TryGetComponent<Image>(out Image img))
                {
                    Color c = img.color;
                    c.a = Mathf.Lerp(0f, 1f, t); // Fade có thể dùng t trực tiếp hoặc smoothT tùy ý bạn
                    img.color = c;
                }
            }

            yield return null;
        }

        // Đảm bảo trạng thái cuối cùng chuẩn xác
        for (int i = 0; i < rectangles.Length; i++)
        {
            rectangles[i].anchoredPosition = new Vector2(0, rectangles[i].anchoredPosition.y);
            if (rectangles[i].TryGetComponent<Image>(out Image img))
                {
                    Color c = img.color;
                    c.a = 1f;
                    img.color = c;
                }
        }

        // Chuyển Scene
        // SceneManager.LoadScene(nextSceneName);
    }
} */