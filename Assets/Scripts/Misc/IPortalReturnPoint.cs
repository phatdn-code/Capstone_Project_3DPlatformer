using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public interface IPortalReturnPoint
    {
        Transform ReturnPoint { get; }
        string ReturnPointId { get; }
        bool UseAsReturnPoint { get; }
    }
}