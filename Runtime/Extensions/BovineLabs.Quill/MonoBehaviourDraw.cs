#if BL_QUILL
using UnityEngine;

namespace KrasCore.Quill
{
    public class MonoBehaviourDraw : MonoBehaviour
    {
        public MonoBehaviourDraw()
        {
            Debug.Log("Created");
            DrawManager.Register(this);
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