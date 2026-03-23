using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    public class UI_HUD : MonoBehaviour
    {
        [Header("Format Settings")]
        public string retriesFormat = "00";
        public string coinsFormat = "000";
        public string healthFormat = "0";

        [Header("UI References")]
        public TMP_Text retriesText;
        public TMP_Text coinsText;
        public TMP_Text healthText;
        public Image[] starImages;

        protected Game game;
        protected LevelScore score;
        protected Player currentPlayer;

        /// <summary>
        /// VN: Khởi tạo reference, đăng ký listener và cập nhật HUD lần đầu.
        /// </summary>
        protected virtual void Start()
        {
            CacheReferences();
            RegisterListeners();
            Refresh();
        }

        /// <summary>
        /// VN: Gỡ listener khi object bị hủy để tránh trùng callback.
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnregisterListeners();
        }

        /// <summary>
        /// VN: Lấy các reference cần thiết từ hệ thống game.
        /// </summary>
        protected virtual void CacheReferences()
        {
            game = Game.instance;
            score = LevelScore.instance;
            currentPlayer = Level.instance != null ? Level.instance.player : null;
        }

        /// <summary>
        /// VN: Đăng ký các sự kiện để HUD tự cập nhật khi dữ liệu thay đổi.
        /// </summary>
        protected virtual void RegisterListeners()
        {
            if (score != null)
            {
                score.OnCoinsSet.AddListener(UpdateCoins);
                score.OnStarsSet.AddListener(UpdateStars);
            }

            if (game != null)
                game.OnRetriesSet.AddListener(UpdateRetries);

            RegisterPlayerHealthListener(currentPlayer);

            if (Level.instance != null)
                Level.instance.onPlayerChanged.AddListener(OnPlayerChanged);
        }

        /// <summary>
        /// VN: Gỡ các sự kiện đã đăng ký trước đó.
        /// </summary>
        protected virtual void UnregisterListeners()
        {
            if (score != null)
            {
                score.OnCoinsSet.RemoveListener(UpdateCoins);
                score.OnStarsSet.RemoveListener(UpdateStars);
            }

            if (game != null)
                game.OnRetriesSet.RemoveListener(UpdateRetries);

            UnregisterPlayerHealthListener(currentPlayer);

            if (Level.instance != null)
                Level.instance.onPlayerChanged.RemoveListener(OnPlayerChanged);
        }

        /// <summary>
        /// VN: Gắn listener theo dõi máu của player hiện tại.
        /// </summary>
        protected virtual void RegisterPlayerHealthListener(Player player)
        {
            if (player == null || player.health == null)
                return;

            player.health.onChange.AddListener(UpdateHealth);
        }

        /// <summary>
        /// VN: Gỡ listener máu của player hiện tại.
        /// </summary>
        protected virtual void UnregisterPlayerHealthListener(Player player)
        {
            if (player == null || player.health == null)
                return;

            player.health.onChange.RemoveListener(UpdateHealth);
        }

        /// <summary>
        /// VN: Xử lý khi player trong level bị thay đổi.
        /// </summary>
        protected virtual void OnPlayerChanged(Player newPlayer)
        {
            UnregisterPlayerHealthListener(currentPlayer);

            currentPlayer = newPlayer;

            RegisterPlayerHealthListener(currentPlayer);
            UpdateHealth();
        }

        /// <summary>
        /// VN: Cập nhật text số coin.
        /// </summary>
        protected virtual void UpdateCoins(int value)
        {
            if (coinsText == null)
                return;

            coinsText.text = value.ToString(coinsFormat);
        }

        /// <summary>
        /// VN: Cập nhật text số lượt chơi lại.
        /// </summary>
        protected virtual void UpdateRetries(int value)
        {
            if (retriesText == null)
                return;

            retriesText.text = value.ToString(retriesFormat);
        }

        /// <summary>
        /// VN: Cập nhật text máu hiện tại của player.
        /// </summary>
        protected virtual void UpdateHealth()
        {
            if (healthText == null || currentPlayer == null || currentPlayer.health == null)
                return;

            healthText.text = currentPlayer.health.current.ToString(healthFormat);
        }

        /// <summary>
        /// VN: Cập nhật trạng thái hiển thị sao theo dữ liệu nhận vào.
        /// </summary>
        protected virtual void UpdateStars(bool[] values)
        {
            if (starImages == null || values == null)
                return;

            int length = Mathf.Min(starImages.Length, values.Length);

            for (int i = 0; i < length; i++)
            {
                if (starImages[i] != null)
                    starImages[i].enabled = values[i];
            }
        }

        /// <summary>
        /// VN: Cập nhật toàn bộ HUD theo dữ liệu hiện tại.
        /// </summary>
        public virtual void Refresh()
        {
            if (score != null)
            {
                UpdateCoins(score.coins);
                UpdateStars(score.stars);
            }

            if (game != null) UpdateRetries(game.retries);

            UpdateHealth();
        }
    }
}