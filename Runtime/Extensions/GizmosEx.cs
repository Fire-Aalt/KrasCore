using UnityEngine;

namespace KrasCore
{
    public static class GizmosEx
    {
        public static void DrawWireCuboid(Vector3 position, Quaternion rotation, Vector3 size)
        {
            var previousMatrix = Gizmos.matrix;
            
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = previousMatrix;
        }
    }
}
