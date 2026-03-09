using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;

namespace MiniGame
{
    /// <summary>
    /// Theo dõi tiến trình thu thập pickup trong level.
    /// - Hiển thị số pickup đã nhặt
    /// - Cộng thêm thời gian
    /// - Kích hoạt hoàn thành level khi nhặt đủ
    /// </summary>
    public class LevelPickupProgressTracker : MonoBehaviour
    {
        [Header("UI")]
        public Text pickupsText;
        public Image pickupIconImage;

        [Header("Gameplay")]
        /// <summary>
        /// Số giây được cộng thêm mỗi khi nhặt pickup
        /// </summary>
        public float pickupBonusTime = 10f;

        private int _numPickupsCollected = 0;
        private int _numPickupsTotal;

        private CountdownController timer;
        private CountdownUIText timerUI;

        void Start()
        {
            // tìm UI nếu chưa gán
            if (pickupsText == null)
            {
                var obj = GameObject.Find("PickupsText");
                if (obj) pickupsText = obj.GetComponent<Text>();
            }

            if (pickupIconImage == null)
            {
                var icon = GameObject.Find("PickupIcon");
                if (icon) pickupIconImage = icon.GetComponent<Image>();
            }

            // tìm timer
            timer = FindFirstObjectByType<CountdownController>();
            timerUI = FindFirstObjectByType<CountdownUIText>();

            // đếm tổng pickup trong scene
            var pickups = FindObjectsByType<CollectableStar>(FindObjectsSortMode.None);
            _numPickupsTotal = pickups.Length;

            // ẩn UI lúc bắt đầu
            if (pickupIconImage) pickupIconImage.enabled = false;
            if (pickupsText) pickupsText.enabled = false;

            UpdatePickupText();

            // đăng ký event khi pickup được nhặt
            CollectableStar.OnCollectEvent += RegisterPickup;
        }

        void OnDestroy()
        {
            CollectableStar.OnCollectEvent -= RegisterPickup;
        }

        /// <summary>
        /// Được gọi khi player nhặt pickup
        /// </summary>
        private void RegisterPickup()
        {
            // hiển thị UI nếu là pickup đầu tiên
            if (_numPickupsCollected == 0)
                ShowPickupCounter();

            _numPickupsCollected++;

            // cộng thời gian
            if (timer != null)
                timer.AddTime(pickupBonusTime);

            // hiển thị +time
            if (timerUI != null)
                timerUI.ShowBonusTime(pickupBonusTime);

            UpdatePickupText();

            // hoàn thành level nếu nhặt đủ
            if (_numPickupsCollected >= _numPickupsTotal)
            {
                RegisterLevelComplete();
            }
        }

        /// <summary>
        /// Cập nhật UI hiển thị số pickup
        /// </summary>
        private void UpdatePickupText()
        {
            if (pickupsText)
                pickupsText.text = $"{_numPickupsCollected} / {_numPickupsTotal}";
        }

        /// <summary>
        /// Gọi khi hoàn thành level
        /// </summary>
        public virtual void RegisterLevelComplete()
        {
            if (timer != null)
                timer.enabled = false;

            StartCoroutine(FadeOutCoroutine());
        }

        /// <summary>
        /// Hiệu ứng cinematic khi hoàn thành level
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            var bloom = FindFirstObjectByType<BloomOptimized>();

            float targetIntensity = 2.3f;
            float targetThreshold = 0.4f;

            var musicController = FindFirstObjectByType<MusicController>();
            bool tweenMusic = musicController && musicController.gameplay;

            float tween = 1f;
            float tweenSpeed = 0.5f;

            float fixedDelta = Time.fixedDeltaTime;

            while (tween > 0.1f)
            {
                float deltaTime = Time.unscaledDeltaTime;

                // bloom effect
                if (bloom)
                {
                    bloom.intensity =
                        Mathf.Lerp(bloom.intensity, targetIntensity, tweenSpeed * deltaTime);

                    bloom.threshold =
                        Mathf.Lerp(bloom.threshold, targetThreshold, tweenSpeed * deltaTime);
                }

                // giảm âm lượng nhạc
                if (tweenMusic)
                {
                    musicController.gameplay.volume =
                        Mathf.Lerp(musicController.gameplay.volume, 0f, tweenSpeed * deltaTime);
                }

                // slow motion
                Time.timeScale = Mathf.Lerp(Time.timeScale, 0f, tweenSpeed * deltaTime);
                Time.fixedDeltaTime = fixedDelta * Time.timeScale;

                tween = Mathf.Lerp(tween, 0f, tweenSpeed * deltaTime);

                yield return null;
            }

            // khôi phục time
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedDelta;

            var lcc = FindFirstObjectByType<LevelCompletionManager>();

            if (lcc)
                lcc.HandleLevelComplete();
        }

        /// <summary>
        /// Hiển thị UI pickup lần đầu
        /// </summary>
        private void ShowPickupCounter()
        {
            if (pickupIconImage)
            {
                pickupIconImage.enabled = true;
                pickupIconImage.canvasRenderer.SetAlpha(0f);
                pickupIconImage.CrossFadeAlpha(1f, 1f, false);
            }

            if (pickupsText)
            {
                pickupsText.enabled = true;
                pickupsText.canvasRenderer.SetAlpha(0f);
                pickupsText.CrossFadeAlpha(1f, 1f, false);
            }
        }
    }
}