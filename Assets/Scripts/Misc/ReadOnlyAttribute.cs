using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Attribute để hiển thị field trong Inspector nhưng không cho phép edit
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyAttribute() { }
    }
}
