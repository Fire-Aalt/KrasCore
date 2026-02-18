# Tips

## 1) Unity Physics `BoxCast` Half-Extents Gotcha

`BoxCast(..., halfExtents, ...)` expects values equivalent to `AABB.Extents`, not `AABB.Extents / 2f`.

- Correct mental model: `halfExtents == halfSize` (distance from center to each face).
- Practical fix: if you already have full size, pass `size / 2f`.
- Passing `AABB.Extents / 2f` makes the cast box too small and can cause confusing misses/interactions.

Reference signature:

```csharp
public bool BoxCast(
    float3 center,
    quaternion orientation,
    float3 halfExtents,
    float3 direction,
    float maxDistance,
    CollisionFilter filter,
    QueryInteraction queryInteraction = QueryInteraction.Default)
    => QueryWrappers.BoxCast(
        in this,
        center,
        orientation,
        halfExtents,
        direction,
        maxDistance,
        filter,
        queryInteraction);
```

## 2) Generic Job Pattern: Propagate Default Job Stub

For generic jobs, propagate a default job-stub parameter from caller to callee. This can avoid needing `[RegisterGenericJobType<T>]` for each concrete generic case.

```csharp
public JobHandle CopyParallelToListSingle(
    JobHandle dependency,
    ParallelList<T>.UnsafeParallelListToArraySingleThreaded jobStud = default)
{
    return ParallelList.CopyToArraySingle(ref List, dependency, jobStud);
}

public JobHandle CopyToArraySingle(
    ref NativeList<T> nativeList,
    JobHandle dependency,
    UnsafeParallelListToArraySingleThreaded jobStud = default)
{
    return new UnsafeParallelListToArraySingleThreaded
    {
        ParallelList = _unsafeParallelList,
        List = nativeList.m_ListData
    }.Schedule(dependency);
}
```
