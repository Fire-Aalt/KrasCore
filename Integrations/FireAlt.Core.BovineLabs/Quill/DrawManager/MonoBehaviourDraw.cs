#if BL_QUILL
using UnityEngine;

namespace KrasCore.Quill
{
    public abstract class MonoBehaviourDraw : MonoBehaviour, IDraw
    {
        protected MonoBehaviourDraw()
        {
#if UNITY_EDITOR
            DrawManager.Register(this);
#endif
        }
        
        /// <summary>
        /// This is needed because only objects with an OnDrawGizmos/OnDrawGizmosSelected method will show up in Unity's menu for enabling/disabling
        /// the gizmos per object type (upper right corner of the scene view).
        /// By using OnDrawGizmosSelected instead of OnDrawGizmos we minimize the overhead of Unity calling this empty method.
        /// </summary>
        private void OnDrawGizmosSelected() 
        {
            // An empty OnDrawGizmosSelected method
        }
        
        public virtual void Draw()
        {
            // Override to implement
        }
    
        public virtual void DrawSelected()
        {
            // Override to implement
        }
    }
}
#endif