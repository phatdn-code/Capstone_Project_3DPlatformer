using PLAYERTWO.PlatformerProject;
using Sirenix.OdinInspector;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class MiniGameDoorCondition : MonoBehaviour
    {
        [TitleGroup("Mini Game Door Condition")]

        [BoxGroup("Mini Game Door Condition/References")]
        [Required("Thiếu Door cần mở.")]
        [InfoBox("Door sẽ được gọi hàm OpenDoor() khi đủ điều kiện.")]
        [SerializeField] private Door targetDoor;

        [BoxGroup("Mini Game Door Condition/Condition")]
        [InfoBox("Danh sách ID minigame cần kiểm tra. ID này phải trùng với miniGameId trong MiniGame.")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, DraggableItems = false, ShowIndexLabels = true)]
        [SerializeField] private string[] requiredMiniGameIds;

        [BoxGroup("Mini Game Door Condition/Condition")]
        [LabelText("Requirement Mode")]
        [EnumToggleButtons]
        [InfoBox("All = phải hoàn thành tất cả ID mới mở cửa. Any = chỉ cần hoàn thành 1 ID là mở.")]
        [SerializeField] private RequirementMode requirementMode = RequirementMode.All;

        [BoxGroup("Mini Game Door Condition/Settings")]
        [ToggleLeft]
        [InfoBox("Bật nếu muốn kiểm tra điều kiện mở cửa ngay khi scene bắt đầu.")]
        [SerializeField] private bool checkOnStart = true;

        [BoxGroup("Mini Game Door Condition/Settings")]
        [ToggleLeft]
        [InfoBox("Bật nếu muốn nghe event load save, để sau khi Game nạp dữ liệu xong sẽ kiểm tra lại cửa.")]
        [SerializeField] private bool listenLoadState = true;

        [BoxGroup("Mini Game Door Condition/Settings")]
        [ToggleLeft]
        [InfoBox("Bật nếu chỉ muốn mở cửa đúng 1 lần, tránh gọi OpenDoor() lặp lại nhiều lần.")]
        [SerializeField] private bool onlyOpenOnce = true;

        private bool _opened;
        private bool _pendingTryOpenDoor;

        /// <summary>
        /// VN: Tự lấy Door cùng object nếu chưa gán tay.
        /// </summary>
        private void Reset()
        {
            if (targetDoor == null)
                targetDoor = GetComponent<Door>();
        }

        /// <summary>
        /// VN: Đăng ký event load save và loading finish.
        /// </summary>
        private void OnEnable()
        {
            if (listenLoadState && Game.instance != null)
                Game.instance.onLoadState.AddListener(HandleGameLoaded);

            if (GameLoader.instance != null)
                GameLoader.instance.OnLoadFinish.AddListener(HandleLoadFinished);
        }

        /// <summary>
        /// VN: Hủy đăng ký event khi object bị disable.
        /// </summary>
        private void OnDisable()
        {
            if (listenLoadState && Game.instance != null)
                Game.instance.onLoadState.RemoveListener(HandleGameLoaded);

            if (GameLoader.instance != null)
                GameLoader.instance.OnLoadFinish.RemoveListener(HandleLoadFinished);
        }

        /// <summary>
        /// VN: Khi scene bắt đầu thì yêu cầu kiểm tra mở cửa.
        /// Nếu còn đang loading thì sẽ chờ load xong mới kiểm tra.
        /// </summary>
        private void Start()
        {
            if (checkOnStart)
                RequestTryOpenDoor();
        }

        /// <summary>
        /// VN: Sau khi Game load save xong thì yêu cầu kiểm tra lại cửa.
        /// Nếu còn đang loading thì sẽ chờ load xong mới kiểm tra.
        /// </summary>
        private void HandleGameLoaded(int dataIndex)
        {
            RequestTryOpenDoor();
        }

        /// <summary>
        /// VN: Yêu cầu kiểm tra mở cửa.
        /// Nếu GameLoader còn đang loading thì đợi OnLoadFinish rồi mới chạy.
        /// </summary>
        private void RequestTryOpenDoor()
        {
            if (GameLoader.instance != null && GameLoader.instance.isLoading)
            {
                _pendingTryOpenDoor = true;
                return;
            }

            TryOpenDoor();
        }

        /// <summary>
        /// VN: Khi loading kết thúc thì mới kiểm tra mở cửa nếu đang có yêu cầu chờ.
        /// </summary>
        private void HandleLoadFinished()
        {
            if (!_pendingTryOpenDoor)
                return;

            _pendingTryOpenDoor = false;
            TryOpenDoor();
        }

        /// <summary>
        /// VN: Nếu đủ điều kiện minigame thì mở cửa.
        /// </summary>
        private void TryOpenDoor()
        {
            if (onlyOpenOnce && _opened)
                return;

            if (targetDoor == null)
                return;

            if (Game.instance == null)
                return;

            if (!IsConditionMet())
                return;

            targetDoor.OpenDoor();
            _opened = true;
        }

        /// <summary>
        /// VN: Kiểm tra điều kiện Any hoặc All theo danh sách ID.
        /// </summary>
        private bool IsConditionMet()
        {
            if (requiredMiniGameIds == null || requiredMiniGameIds.Length == 0)
                return false;

            if (requirementMode == RequirementMode.All)
                return Game.instance.AreAllMiniGamesCompleted(requiredMiniGameIds);

            return Game.instance.IsAnyMiniGameCompleted(requiredMiniGameIds);
        }
    }
}