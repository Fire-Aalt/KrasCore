// <copyright file="BurstUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using Unity.Burst;
using Unity.Entities;

namespace FireAlt.Core.Utility
{
    [BurstCompile]
    public static class BurstUtils
    {
        [BurstCompile]
        public static bool IsEmpty(ref EntityQuery query)
        {
            return query.IsEmpty;
        }

        [BurstDiscard]
        public static void SetNotBurstCompiled(ref bool isBurstCompiled)
        {
            isBurstCompiled = false;
        }
    }
}