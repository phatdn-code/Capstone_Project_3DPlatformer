using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuTransition : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform[] panels;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private string nextSceneName;

    private Vector2[] startPositions;
    private Image[] images;

    [ContextMenu("Bắt đầu chuyển cảnh")]
    public void StartTransition()
    {
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        CacheInitialData();

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float smoothT = SmoothStep(t);

            UpdateMovement(smoothT);
            UpdateFade(t);

            yield return null;
        }

        ApplyFinalState();
        // SceneManager.LoadScene(nextSceneName);
    }

    private void CacheInitialData()
    {
        int count = panels.Length;

        startPositions = new Vector2[count];
        images = new Image[count];

        for (int i = 0; i < count; i++)
        {
            startPositions[i] = panels[i].anchoredPosition;
            panels[i].TryGetComponent(out images[i]);
        }
    }

    private void UpdateMovement(float t)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            float x = Mathf.Lerp(startPositions[i].x, 0f, t);
            panels[i].anchoredPosition = new Vector2(x, startPositions[i].y);
        }
    }

    private void UpdateFade(float t)
    {
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) 
                continue;

            Color c = images[i].color;
            c.a = t;
            images[i].color = c;
        }
    }

    private void ApplyFinalState()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].anchoredPosition =
                new Vector2(0f, panels[i].anchoredPosition.y);

            if (images[i] == null)
                continue;

            Color c = images[i].color;
            c.a = 1f;
            images[i].color = c;
        }
    }

    private float SmoothStep(float t) => t * t * (3f - 2f * t);
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
//     {
//         float elapsedTime = 0f;

//         // Lưu vị trí bắt đầu
//         Vector2[] startPositions = new Vector2[rectangles.Length];
//         for (int i = 0; i < rectangles.Length; i++)
//         {
//             startPositions[i] = rectangles[i].anchoredPosition;
//         }

//         while (elapsedTime < duration)
//         {
//             elapsedTime += Time.deltaTime;
//             float t = elapsedTime / duration;

//             // Làm mượt chuyển động (SmoothStep)
//             float smoothT = t * t * (3f - 2f * t);

//             for (int i = 0; i < rectangles.Length; i++)
//             {
//                 // 1. Di chuyển vị trí X về 0
//                 float newX = Mathf.Lerp(startPositions[i].x, 0, smoothT);
//                 rectangles[i].anchoredPosition = new Vector2(newX, startPositions[i].y);

//                 // 2. Hiệu ứng Fade In (từ 0 lên 1)
//                 if (rectangles[i].TryGetComponent<Image>(out Image img))
//                 {
//                     Color c = img.color;
//                     c.a = Mathf.Lerp(0f, 1f, t); // Fade có thể dùng t trực tiếp hoặc smoothT tùy ý bạn
//                     img.color = c;
//                 }
//             }

//             yield return null;
//         }

//         // Đảm bảo trạng thái cuối cùng chuẩn xác
//         for (int i = 0; i < rectangles.Length; i++)
//         {
//             rectangles[i].anchoredPosition = new Vector2(0, rectangles[i].anchoredPosition.y);
//             if (rectangles[i].TryGetComponent<Image>(out Image img))
//                 {
//                     Color c = img.color;
//                     c.a = 1f;
//                     img.color = c;
//                 }
//         }

//         // Chuyển Scene
//         // SceneManager.LoadScene(nextSceneName);
//     }
// }