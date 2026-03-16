using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Game/Game")]
    public class Game : Singleton<Game>
    {
        public UnityEvent<int> OnRetriesSet;
        public UnityEvent OnSavingRequested;
        public UnityEvent<int> onLoadState;

        [Tooltip("Amount of stars existing in each level.")]
        public int starsPerLevel = 3;

        [Tooltip("Amount of retries the player has when starting a new game.")]
        public int initialRetries = 3;

        [Tooltip("Name of the scene to load when the player exits any level.")]
        public string levelExitScene;

        [Tooltip("The list of playable levels.")]
        public List<GameLevel> levels;

        protected int m_retries;
        protected int m_dataIndex = -1;
        protected DateTime m_createdAt;
        protected DateTime m_updatedAt;

        protected bool m_introStorySeen;
        public bool introStorySeen => m_introStorySeen;

        //==================================================
        // VN: Lưu tạm điểm quay lại khi chuyển scene
        protected string m_pendingReturnScene;
        protected string m_pendingReturnPointId;
        //==================================================

        public int retries
        {
            get { return m_retries; }
            set
            {
                m_retries = value;
                OnRetriesSet?.Invoke(m_retries);
            }
        }

        public bool dataLoaded => m_dataIndex >= 0;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            InitializeRetries();
            InitializeFrameRate();
        }

        protected virtual void InitializeRetries()
        {
            retries = initialRetries;
        }

        protected virtual void InitializeFrameRate()
        {
#if UNITY_ANDROID || UNITY_IOS
            Application.targetFrameRate = 60;
#endif
        }

        public static void LockCursor(bool value = true)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            Cursor.visible = !value;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
#endif
        }

        public virtual void LoadOrCreateState(int index)
        {
            var data = GameSaver.instance.Load(index);
            data ??= GameData.Create();
            LoadState(index, data);
        }

        public virtual void LoadState(int index, GameData data)
        {
            m_dataIndex = index;
            m_retries = data.retries;
            m_createdAt = DateTime.Parse(data.createdAt);
            m_updatedAt = DateTime.Parse(data.updatedAt);
            m_introStorySeen = data.introStorySeen;

            for (int i = 0; i < data.levels.Length; i++)
            {
                if (i >= levels.Count)
                    break;

                levels[i].LoadState(data.levels[i]);
            }

            onLoadState.Invoke(index);
        }

        public virtual LevelData[] LevelsData()
        {
            return levels.Select(level => level.ToData()).ToArray();
        }

        public virtual GameLevel GetCurrentLevel()
        {
            var scene = GameLoader.instance.currentScene;
            return levels.Find((level) => level.scene == scene);
        }

        public virtual int GetCurrentLevelIndex()
        {
            var scene = GameLoader.instance.currentScene;
            return levels.FindIndex((level) => level.scene == scene);
        }

        public virtual int GetTotalStars() =>
            levels.Aggregate(0, (acc, level) => acc + level.CollectedStarsCount());

        public virtual void RequestSaving()
        {
            GameSaver.instance.Save(ToData(), m_dataIndex);
            OnSavingRequested?.Invoke();
        }

        public virtual void UnlockLevelBySceneName(string sceneName)
        {
            var level = levels.Find((level) => level.scene == sceneName);

            if (level != null && level.requiredStars <= 0)
            {
                level.locked = false;
            }
        }

        public virtual void UnlockNextLevel()
        {
            var index = GetCurrentLevelIndex() + 1;

            if (index >= 0 && index < levels.Count)
            {
                if (levels[index].requiredStars > 0)
                    return;

                levels[index].locked = false;
            }
        }

        public virtual void ResetRetries() => m_retries = initialRetries;

        public virtual bool HasAnyBeatenLevel() => levels.Any((level) => level.beatenTimes > 0);

        public virtual GameData ToData()
        {
            return new GameData()
            {
                retries = m_retries,
                levels = LevelsData(),
                createdAt = m_createdAt.ToString(),
                updatedAt = DateTime.UtcNow.ToString(),
                introStorySeen = m_introStorySeen,
            };
        }

        public virtual void MarkIntroStoryAsSeen(bool saveImmediately = true)
        {
            if (m_introStorySeen)
                return;

            m_introStorySeen = true;

            if (saveImmediately && dataLoaded)
                RequestSaving();
        }

        /// <summary>
        /// VN: Ghi nhớ scene map và ID điểm quay lại để khi quay về sẽ spawn đúng portal.
        /// </summary>
        public virtual void SetPendingReturnPoint(string sceneName, string pointId)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            if (string.IsNullOrEmpty(pointId))
                return;

            m_pendingReturnScene = sceneName;
            m_pendingReturnPointId = pointId;
        }

        /// <summary>
        /// VN: Lấy ID điểm quay lại nếu scene hiện tại đúng scene đã lưu.
        /// </summary>
        public virtual bool TryConsumePendingReturnPoint(string currentScene, out string pointId)
        {
            pointId = null;

            if (string.IsNullOrEmpty(currentScene))
                return false;

            if (string.IsNullOrEmpty(m_pendingReturnScene))
                return false;

            if (string.IsNullOrEmpty(m_pendingReturnPointId))
                return false;

            if (m_pendingReturnScene != currentScene)
                return false;

            pointId = m_pendingReturnPointId;

            m_pendingReturnScene = null;
            m_pendingReturnPointId = null;

            return true;
        }

        /// <summary>
        /// VN: Xóa dữ liệu điểm quay lại đang chờ.
        /// </summary>
        public virtual void ClearPendingReturnPoint()
        {
            m_pendingReturnScene = null;
            m_pendingReturnPointId = null;
        }

        /// <summary>
        /// VN: Chỉ xóa điểm quay lại đang chờ nếu nó thuộc đúng scene được truyền vào.
        /// </summary>
        public virtual void ClearPendingReturnPoint(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            if (m_pendingReturnScene != sceneName)
                return;

            ClearPendingReturnPoint();
        }
    }
}