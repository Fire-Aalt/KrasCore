using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Jobs;

namespace FireAlt.Core.Collections
{
    public struct ReusableTransformAccessArray : IDisposable
    {
        public TransformAccessArray Array => _array;
        public NativeList<Entity> AlignedEntities => _alignedEntities;

        private TransformAccessArray _array;
        private NativeList<Entity> _alignedEntities;
        private NativeHashSet<int> _freeIds;
        
        public ReusableTransformAccessArray(int initialCapacity, Allocator allocator)
        {
            _array = new TransformAccessArray(initialCapacity);
            _alignedEntities = new NativeList<Entity>(initialCapacity, allocator);
            _freeIds = new NativeHashSet<int>(initialCapacity, allocator);
        }
        
        /// <summary>
        /// Adds transform to the transform container,
        /// and assigns an id for the referencing
        /// </summary>
        public int AddTransform(Entity entity, Transform trm) 
        {
            int refId;

            // If there's a free id -> use it
            if (!_freeIds.IsEmpty) 
            {
                var enumerator = _freeIds.GetEnumerator();
            
                enumerator.MoveNext();
                refId = enumerator.Current;
                enumerator.Dispose();

                _freeIds.Remove(refId);
            
                _array[refId] = trm;
                _alignedEntities[refId] = entity;
            
                return refId;
            }

            // Otherwise generate id / add transform and return new refId
            refId = _array.length;
         
            _array.Add(trm);
            _alignedEntities.Add(entity);

            return refId;
        }

        /// <summary>
        /// Releases transform reference from the transform container. 
        /// <remarks>Cannot be used in Burst context.</remarks>
        /// </summary>
        public Transform ReleaseTransformManaged(int id)
        {
            var transform = _array[id];
            
            _freeIds.Add(id);
            _array.SetTransformHandle(id, default);
            _alignedEntities[id] = Entity.Null;
            
            return transform;
        }

        /// <summary>
        /// Releases transform reference from the transform container. 
        /// <remarks>Can be used in Burst context.</remarks>
        /// </summary>
        public void ReleaseTransform(int id)
        {
            _freeIds.Add(id);
            _array.SetTransformHandle(id, default);
            _alignedEntities[id] = Entity.Null;
        }
        
        public void Dispose()
        {
            _array.Dispose();
            _alignedEntities.Dispose();
            _freeIds.Dispose();
        }
    }
}