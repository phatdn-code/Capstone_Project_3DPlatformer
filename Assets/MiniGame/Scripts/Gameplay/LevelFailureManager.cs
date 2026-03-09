using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

namespace MiniGame
{
    /// <summary>
    /// Quản lý các hành động khi người chơi thất bại trong level (hết thời gian).
    /// </summary>
    public class LevelFailureManager : MonoBehaviour
    {
        /// <summary>
        /// Nếu true: hiển thị menu Game Over.
        /// Nếu false: restart lại scene ngay lập tức.
        /// </summary>
        [Tooltip("Nếu bật sẽ hiển thị menu thua game, nếu tắt sẽ restart level.")]
        public bool showLevelFailMenu = true;

        /// <summary>
        /// CanvasGroup của màn hình Game Over.
        /// </summary>
        [Tooltip("UI màn hình Game Over.")]
        public CanvasGroup levelFailMenu;

        // lưu giá trị bloom ban đầu để khôi phục lại sau revive
        private float _defaultBloomIntensity;
        private float _defaultBloomThreshold;

        void OnEnable()
        {
            // đăng ký event khi hết thời gian
            CountdownController.OnTimeEmptyEvent += HandleLevelFailed;
        }

        void OnDisable()
        {
            // hủy đăng ký event
            CountdownController.OnTimeEmptyEvent -= HandleLevelFailed;
        }

        void Start()
        {
            // lưu lại giá trị bloom ban đầu
            var bloom = GameObject.FindFirstObjectByType<BloomOptimized>();

            if (bloom != null)
            {
                _defaultBloomIntensity = bloom.intensity;
                _defaultBloomThreshold = bloom.threshold;
            }
        }

        /// <summary>
        /// Được gọi khi hết thời gian → người chơi thua level.
        /// </summary>
        public virtual void HandleLevelFailed()
        {
            if (showLevelFailMenu)
            {
                // chạy hiệu ứng fade + slow motion
                StartCoroutine(FadeOutCoroutine());
            }
            else
            {
                // restart level ngay
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        /// <summary>
        /// Hiệu ứng khi thua level:
        /// - slow motion
        /// - tăng bloom
        /// - fade UI Game Over
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            yield return new WaitForSeconds(1.0f);

            var bloom = GameObject.FindFirstObjectByType<BloomOptimized>();

            // nếu không có bloom thì chỉ hiện menu
            if (bloom == null)
            {
                Time.timeScale = 0;

                if (levelFailMenu != null)
                {
                    levelFailMenu.gameObject.SetActive(true);
                }

                yield break;
            }

            float targetIntensity = 2.2f;
            float targetThreshold = 0.3f;

            var wait = new WaitForEndOfFrame();
            float tween = 1f;

            // bật menu game over
            if (levelFailMenu != null)
            {
                levelFailMenu.alpha = 0;
                levelFailMenu.gameObject.SetActive(true);
            }

            float prevTime = Time.realtimeSinceStartup;

            while (tween > 0.1f)
            {
                float deltaTime = Time.realtimeSinceStartup - prevTime;
                prevTime = Time.realtimeSinceStartup;

                // tăng hiệu ứng bloom
                bloom.intensity = Mathf.Lerp(bloom.intensity, targetIntensity, 1.5f * deltaTime);
                bloom.threshold = Mathf.Lerp(bloom.threshold, targetThreshold, 1.5f * deltaTime);

                // tween slow motion
                tween = Mathf.Lerp(tween, 0f, 1.5f * deltaTime);

                Time.timeScale = tween;
                Time.fixedDeltaTime = tween * 0.02f;

                // fade UI
                if (levelFailMenu != null)
                {
                    levelFailMenu.alpha = 1 - tween;
                }

                yield return wait;
            }

            if (levelFailMenu != null)
            {
                levelFailMenu.alpha = 1;
            }

            // dừng game
            Time.timeScale = 0;
            Time.fixedDeltaTime = 0.02f;
        }

        /// <summary>
        /// Được gọi khi người chơi revive (ví dụ xem quảng cáo).
        /// </summary>
        private void HandleReviveGranted()
        {
            Time.timeScale = 1;

            if (levelFailMenu != null)
            {
                levelFailMenu.gameObject.SetActive(false);
            }

            StartCoroutine(TweenIn());
        }

        /// <summary>
        /// Khôi phục lại hiệu ứng hình ảnh sau khi revive.
        /// </summary>
        private IEnumerator TweenIn()
        {
            var bloom = GameObject.FindFirstObjectByType<BloomOptimized>();

            if (bloom == null)
            {
                yield break;
            }

            var wait = new WaitForEndOfFrame();

            float targetIntensity = _defaultBloomIntensity;
            float tween = 1f;

            while (tween > 0.1f)
            {
                bloom.intensity = Mathf.Lerp(bloom.intensity, targetIntensity, 2f * Time.deltaTime);
                bloom.threshold = Mathf.Lerp(bloom.threshold, 0f, 2f * Time.deltaTime);

                tween = Mathf.Lerp(tween, 0f, 3f * Time.deltaTime);

                yield return wait;
            }

            // khôi phục giá trị bloom ban đầu
            bloom.intensity = _defaultBloomIntensity;
            bloom.threshold = _defaultBloomThreshold;
        }
    }
}