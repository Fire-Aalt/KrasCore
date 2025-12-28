using BovineLabs.Core.Extensions;
using Unity.Entities;

#if BL_ESSENSE
using BovineLabs.Essence;
using BovineLabs.Essence.Data;
#endif

namespace KrasCore
{
    public static class ConsoleUtils
    {
        public static void GetWorld(out World world)
        {
            world = World.DefaultGameObjectInjectionWorld;
        }
        
        public static ref T GetSystem<T>(out World world) 
            where T : unmanaged, ISystem
        {
            GetWorld(out world);
            
            var systemHandle = world.GetExistingSystem<T>();
            ref var system = ref world.Unmanaged.GetUnsafeSystemRef<T>(systemHandle);
            return ref system;
        }
        
        public static T GetSystemManaged<T>(out World world) 
            where T : ComponentSystemBase
        {
            GetWorld(out world);
            
            var system = world.GetExistingSystemManaged<T>();
            return system;
        }

#if BL_ESSENSE
        public static IntrinsicWriter.Lookup GetIntrinsicLookup(ref SystemState state)
        {
            GetWorld(out var world);
            
            var lookup = new IntrinsicWriter.Lookup();
            lookup.Create(ref state);
            lookup.Update(ref state, world.EntityManager.GetSingleton<EssenceConfig>());

            return lookup;
        }
#endif
    }
}