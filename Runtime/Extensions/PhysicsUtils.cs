using Unity.Mathematics;
using Unity.Physics.Authoring;

namespace KrasCore
{
    public static class PhysicsUtils
    {
        public static AABB GetPhysicsAABB(PhysicsShapeAuthoring shapeAuthoring)
        {
            var aabb = new AABB();

            switch (shapeAuthoring.ShapeType)
            {
                case ShapeType.Box:
                    var boxProps = shapeAuthoring.GetBoxProperties();
                    aabb.Center = boxProps.Center;
                    aabb.Extents = boxProps.Size / 2f + boxProps.BevelRadius;
                    break;
                case ShapeType.Capsule:
                    var cylinderProps = shapeAuthoring.GetCylinderProperties();
                    aabb.Center = cylinderProps.Center;

                    var r = cylinderProps.Radius;
                    var h =  cylinderProps.Height;
                    var q = new float3x3(cylinderProps.Orientation);
                    
                    var localHalfExtents = new float3(r, r, h / 2f);

                    var absRotatedX = math.abs(math.mul(new float3(1, 0, 0), q));
                    var absRotatedY = math.abs(math.mul(new float3(0, 1, 0), q));
                    var absRotatedZ = math.abs(math.mul(new float3(0, 0, 1), q));

                    var worldHalfExtents = absRotatedX * localHalfExtents.x +
                                           absRotatedY * localHalfExtents.y +
                                           absRotatedZ * localHalfExtents.z;
                        
                    aabb.Extents = worldHalfExtents;
                    break;
                default:
                    throw new System.NotImplementedException();
            }
            return aabb;
        }
    }
}