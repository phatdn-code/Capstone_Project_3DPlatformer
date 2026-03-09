/// <summary>
/// Chứa các kiểu delegate dùng lại cho các sự kiện (event) trong game.
/// </summary>
public class GameEventActions
{
    /// <summary>
    /// Delegate cho một hành động không có tham số
    /// và cũng không trả về giá trị nào.
    /// Thường dùng cho các sự kiện đơn giản trong game.
    /// </summary>
    public delegate void SimpleAction();
}