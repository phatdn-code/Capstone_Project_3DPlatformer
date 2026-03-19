using PLAYERTWO.PlatformerProject;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PLAYERTWO.PlatformerProject
{
    public class StoryPortalTrigger : MonoBehaviour, IPortalReturnPoint
    {
        //────────────────────────────────────────────────────
        #region === REFERENCES ===

        [Title("References")]
        [SerializeField, Required] private MagicBookFlight bookFlight;
        [SerializeField] private string storySceneName;
        [SerializeField] private string playerTag = "Player";

        #endregion

        //────────────────────────────────────────────────────
        #region === RETURN SPAWN ===

        [Title("Return Spawn")]
        [SerializeField] private bool useAsReturnPoint = true;

        [SerializeField, ShowIf(nameof(useAsReturnPoint))]
        private string returnPointId;

        [SerializeField, ShowIf(nameof(useAsReturnPoint))]
        private Transform returnPoint;

        #endregion

        //────────────────────────────────────────────────────
        #region === OPTIONS ===

        [Title("Options")]
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private bool returnToStartOnExitTrigger = true;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME ===

        private bool _playerInside;
        private bool _hasTriggered;
        private bool _canEnterStory;
        private bool _isLoading;

        #endregion

        //────────────────────────────────────────────────────
        #region === INTERFACE ===

        public Transform ReturnPoint => returnPoint != null ? returnPoint : transform;

        public string ReturnPointId => string.IsNullOrEmpty(returnPointId) ? storySceneName : returnPointId;

        public bool UseAsReturnPoint => useAsReturnPoint;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY EVENTS ===

        /// <summary>
        /// VN: Đăng ký event từ book.
        /// </summary>
        private void OnEnable()
        {
            if (bookFlight == null)
                return;

            bookFlight.OnOpenAnimationFinished += HandleBookOpenFinished;
            bookFlight.OnReturnedToStart += HandleBookReturned;
        }

        /// <summary>
        /// VN: Hủy đăng ký event từ book.
        /// </summary>
        private void OnDisable()
        {
            if (bookFlight == null)
                return;

            bookFlight.OnOpenAnimationFinished -= HandleBookOpenFinished;
            bookFlight.OnReturnedToStart -= HandleBookReturned;
        }

        /// <summary>
        /// VN: Chỉ cho bấm nút vào story khi book đã mở xong.
        /// </summary>
        private void Update()
        {
            if (!_playerInside || !_canEnterStory || _isLoading)
                return;

            if (PlayerHub.Instance == null || PlayerHub.Instance.InputManager == null)
                return;

            if (PlayerHub.Instance.InputManager.GetStompDown())
                ConfirmEnter();
        }

        /// <summary>
        /// VN: Player vào vùng trigger thì cho book bắt đầu bay.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            if (triggerOnce && _hasTriggered)
                return;

            _playerInside = true;
            _hasTriggered = true;
            _canEnterStory = false;

            bookFlight.FlyBook();
        }

        /// <summary>
        /// VN: Player ra khỏi vùng trigger thì reset trạng thái và cho book quay về.
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            _playerInside = false;
            _canEnterStory = false;

            if (!returnToStartOnExitTrigger)
                return;

            bookFlight.ReturnBookToStart();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === BOOK EVENTS ===

        /// <summary>
        /// VN: Khi animation mở sách chạy xong thì mới cho phép bấm nút vào story.
        /// </summary>
        private void HandleBookOpenFinished()
        {
            if (!_playerInside)
                return;

            _canEnterStory = true;
        }

        /// <summary>
        /// VN: Khi book quay về ban đầu thì reset lại quyền vào story.
        /// </summary>
        private void HandleBookReturned()
        {
            _canEnterStory = false;
            _hasTriggered = false;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === ENTER STORY ===

        /// <summary>
        /// VN: Ghi nhớ điểm quay lại trước khi rời scene hiện tại.
        /// </summary>
        private void RememberReturnPoint()
        {
            if (!UseAsReturnPoint)
                return;

            if (Game.instance == null)
                return;

            string currentScene = SceneManager.GetActiveScene().name;
            Game.instance.SetPendingReturnPoint(currentScene, ReturnPointId);
        }

        /// <summary>
        /// VN: Xác nhận đi vào scene story.
        /// </summary>
        private void ConfirmEnter()
        {
            if (!CanEnterStory())
                return;

            _isLoading = true;

            PrepareForEnter();
            LoadStoryScene();
            RememberReturnPoint();

            AudioManager.Instance?.PlaySound(SoundCategory.Normal, 1);
        }

        /// <summary>
        /// VN: Kiểm tra đã đủ điều kiện vào story chưa.
        /// </summary>
        private bool CanEnterStory()
        {
            return !_isLoading
                && _playerInside
                && _canEnterStory
                && !string.IsNullOrEmpty(storySceneName);
        }

        /// <summary>
        /// VN: Khóa player và pause trước khi chuyển scene.
        /// </summary>
        private void PrepareForEnter()
        {
            if (LevelPauser.instance != null)
            {
                LevelPauser.instance.Pause(false);
                LevelPauser.instance.canPause = false;
            }

            PlayerHub.Instance?.LockPlayer(true);
        }

        /// <summary>
        /// VN: Load scene story, ưu tiên GameLoader rồi Fader.
        /// </summary>
        private void LoadStoryScene()
        {
            if (GameLoader.instance != null)
            {
                GameLoader.instance.Load(storySceneName);
                return;
            }

            if (Fader.instance != null)
            {
                Fader.instance.FadeOut(() => SceneManager.LoadScene(storySceneName));
                return;
            }

            SceneManager.LoadScene(storySceneName);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === VALIDATION ===

        /// <summary>
        /// VN: Kiểm tra collider có phải player hay không.
        /// </summary>
        private bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            if (other.CompareTag(playerTag))
                return true;

            if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
                return true;

            return false;
        }

        #endregion
    }
}