namespace MiniGame
{
    /// <summary>
    /// Chứa các Tag dùng chung trong game.
    /// Việc đặt tag ở một nơi giúp tránh sai chính tả
    /// và dễ quản lý khi project lớn.
    /// </summary>
    public static class GameTags
    {
        /// <summary>
        /// Tag của platform dùng để máy bay cất cánh.
        /// </summary>
        public static string TakeOffPlatform = "TakeOffPlatform";

        /// <summary>
        /// Tag của nhân vật hoặc máy bay do người chơi điều khiển.
        /// </summary>
        public static string Player = "Player";
    }
}