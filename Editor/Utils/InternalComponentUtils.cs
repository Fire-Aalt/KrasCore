using System.Reflection;
using UnityEngine;

namespace KrasCore.Editor
{
    public static class InternalComponentUtils
    {
        private delegate bool MoveComponentDelegate(Component aTarget, Component aRelative, bool aMoveAbove);
        private static readonly MoveComponentDelegate _moveComponent;
        
        static InternalComponentUtils()
        {
            var componentUtilityType = typeof(UnityEditorInternal.ComponentUtility);
            var moveComponentMI = componentUtilityType.GetMethod(
                "MoveComponentRelativeToComponent",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] {
                    typeof(Component),
                    typeof(Component),
                    typeof(bool)
                }, null);
            if (moveComponentMI == null)
                throw new System.Exception("Internal method MoveComponentRelativeToComponent was not found");
            _moveComponent = (MoveComponentDelegate)System.Delegate.CreateDelegate(typeof(MoveComponentDelegate), moveComponentMI);
        }
        public static bool MoveComponent(this Component aTarget, Component aRelative, bool aMoveAbove)
        {
            if (_moveComponent == null)
                throw new System.Exception("Internal method MoveComponentRelativeToComponent was not found");
            return _moveComponent(aTarget, aRelative, aMoveAbove);
        }
    }
}