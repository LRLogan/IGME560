
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Marks a prefab's doorway anchor. Position is the snap point and forward faces outward.
    /// Direction describes the authored socket; generated instances remap it when rotated.
    /// </summary>
    public class TileConnection : MonoBehaviour
    {
        [Tooltip("Must match the connection's direction.")]
        public Direction direction;
    }
}
