#if BL_BRIDGE
using BovineLabs.Bridge.Camera;
using BovineLabs.Bridge.Data;
using BovineLabs.Bridge.Data.Camera;
using Unity.Entities;

namespace KrasCore
{
    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    [UpdateAfter(typeof(CameraMatrixShiftSyncSystem))]
    public partial class CameraDistortBridgeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (cameraComponent, entity) in SystemAPI.Query<RefRW<CameraBridge>>().WithEntityAccess())
            {
                var camera = cameraComponent.ValueRW.Value.Value;
                if (camera == null || !camera.TryGetComponent(out CameraDistort cameraDistort))
                {
                    continue;
                }
                cameraDistort.DistortProjectionMatrix(camera, !EntityManager.HasComponent<CameraViewSpaceOffset>(entity));
            }
        }
    }
}
#endif