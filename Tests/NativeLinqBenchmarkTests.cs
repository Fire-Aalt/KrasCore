using System;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.PerformanceTesting;

namespace KrasCore.Tests
{
    [BurstCompile]
    public class NativeLinqBenchmarkTests
    {
        private const int WARMUP_RUNS = 2;
        private const int MEASURE_RUNS = 10;

        [Test]
        [Performance]
        [Explicit("Benchmark test. Run manually.")]
        [Category("Benchmark")]
        [TestCase(128)]
        [TestCase(1_024)]
        [TestCase(8_192)]
        [TestCase(65_536)]
        [TestCase(262_144)]
        public void SimpleQuery_CompareLinqNativeLinq(int elementCount)
        {
            MeasureLinq(
                $"LINQ.Simple/{elementCount}",
                elementCount,
                QueryLinqSimple,
                QueryLinqSimple);

            MeasureNativeLinq(
                $"NativeLINQ.NoBurst.Simple/{elementCount}",
                elementCount,
                QueryNativeLinqSimple,
                QueryLinqSimple);

            MeasureBurstNativeLinq(
                $"NativeLINQ.Burst.Simple/{elementCount}",
                elementCount,
                array => QueryNativeLinqSimpleBurst(array),
                QueryLinqSimple);
        }

        [Test]
        [Performance]
        [Explicit("Benchmark test. Run manually.")]
        [Category("Benchmark")]
        [TestCase(128)]
        [TestCase(1_024)]
        [TestCase(8_192)]
        [TestCase(65_536)]
        [TestCase(262_144)]
        public void ComplexQuery_CompareLinqNativeLinq(int elementCount)
        {
            MeasureLinq(
                $"LINQ.Complex/{elementCount}",
                elementCount,
                QueryLinqComplex,
                QueryLinqComplex);

            MeasureNativeLinq(
                $"NativeLINQ.NoBurst.Complex/{elementCount}",
                elementCount,
                QueryNativeLinqComplex,
                QueryLinqComplex);

            MeasureBurstNativeLinq(
                $"NativeLINQ.Burst.Complex/{elementCount}",
                elementCount,
                array => QueryNativeLinqComplexBurst(array),
                QueryLinqComplex);
        }

        private static void MeasureLinq(
            string sampleGroupName,
            int elementCount,
            Func<int[], int> query,
            Func<int[], int> expectedQuery)
        {
            var values = Array.Empty<int>();
            var expected = 0;
            var result = 0;

            Measure.Method(() => result = query(values))
                .SetUp(() =>
                {
                    values = CreateInput(elementCount);
                    expected = expectedQuery(values);
                })
                .CleanUp(() =>
                {
                    Assert.That(result, Is.EqualTo(expected));
                    values = Array.Empty<int>();
                })
                .WarmupCount(WARMUP_RUNS)
                .MeasurementCount(MEASURE_RUNS)
                .SampleGroup(new SampleGroup(sampleGroupName, SampleUnit.Millisecond))
                .Run();
        }

        private static void MeasureNativeLinq(
            string sampleGroupName,
            int elementCount,
            Func<NativeArray<int>, int> query,
            Func<int[], int> expectedQuery)
        {
            var managedValues = Array.Empty<int>();
            var values = default(NativeArray<int>);
            var expected = 0;
            var result = 0;

            Measure.Method(() => result = query(values))
                .SetUp(() =>
                {
                    managedValues = CreateInput(elementCount);
                    values = new NativeArray<int>(managedValues, Allocator.TempJob);
                    expected = expectedQuery(managedValues);
                })
                .CleanUp(() =>
                {
                    Assert.That(result, Is.EqualTo(expected));
                    values.Dispose();
                    managedValues = Array.Empty<int>();
                })
                .WarmupCount(WARMUP_RUNS)
                .MeasurementCount(MEASURE_RUNS)
                .SampleGroup(new SampleGroup(sampleGroupName, SampleUnit.Millisecond))
                .Run();
        }

        private static void MeasureBurstNativeLinq(
            string sampleGroupName,
            int elementCount,
            Func<NativeArray<int>, int> query,
            Func<int[], int> expectedQuery)
        {
            var managedValues = Array.Empty<int>();
            var values = default(NativeArray<int>);
            var expected = 0;
            var result = 0;

            Measure.Method(() => result = query.Invoke(values))
                .SetUp(() =>
                {
                    managedValues = CreateInput(elementCount);
                    values = new NativeArray<int>(managedValues, Allocator.TempJob);
                    expected = expectedQuery(managedValues);
                })
                .CleanUp(() =>
                {
                    Assert.That(result, Is.EqualTo(expected));
                    values.Dispose();
                    managedValues = Array.Empty<int>();
                })
                .WarmupCount(WARMUP_RUNS)
                .MeasurementCount(MEASURE_RUNS)
                .SampleGroup(new SampleGroup(sampleGroupName, SampleUnit.Millisecond))
                .Run();
        }

        private static int[] CreateInput(int elementCount)
        {
            var values = new int[elementCount];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = i;
            }

            return values;
        }

        private static int QueryLinqSimple(int[] values)
        {
            return values
                .Where(SimpleWhere)
                .Select(SimpleSelect)
                .Sum();
        }

        private static int QueryNativeLinqSimple(NativeArray<int> values)
        {
            return values
                .AsQuery()
                .Where(new SimpleWherePredicate())
                .Select(new SimpleSelectSelector())
                .Sum();
        }

        [BurstCompile]
        private static int QueryNativeLinqSimpleBurst(in NativeArray<int> values)
        {
            return QueryNativeLinqSimple(values);
        }

        private static int QueryLinqComplex(int[] values)
        {
            return values
                .Where(ComplexWhere0)
                .Select(ComplexSelect0)
                .Where(ComplexWhere1)
                .Select(ComplexSelect1)
                .Where(ComplexWhere2)
                .Select(ComplexSelect2)
                .Where(ComplexWhere3)
                .Select(ComplexSelect3)
                .Where(ComplexWhere4)
                .Select(ComplexSelect4)
                .Where(ComplexWhere5)
                .Select(ComplexSelect5)
                .Sum();
        }

        private static int QueryNativeLinqComplex(NativeArray<int> values)
        {
            return values
                .AsQuery()
                .Where(new ComplexWhere0Predicate())
                .Select(new ComplexSelect0Selector())
                .Where(new ComplexWhere1Predicate())
                .Select(new ComplexSelect1Selector())
                .Where(new ComplexWhere2Predicate())
                .Select(new ComplexSelect2Selector())
                .Where(new ComplexWhere3Predicate())
                .Select(new ComplexSelect3Selector())
                .Where(new ComplexWhere4Predicate())
                .Select(new ComplexSelect4Selector())
                .Where(new ComplexWhere5Predicate())
                .Select(new ComplexSelect5Selector())
                .Sum();
        }

        [BurstCompile]
        private static int QueryNativeLinqComplexBurst(in NativeArray<int> values)
        {
            return QueryNativeLinqComplex(values);
        }

        private static bool SimpleWhere(int value)
        {
            return (value & 1) == 0;
        }

        private static int SimpleSelect(int value)
        {
            return (value & 1023) + 1;
        }

        private static bool ComplexWhere0(int value)
        {
            return (value & 1) == 0;
        }

        private static int ComplexSelect0(int value)
        {
            return ((value * 3) + 7) & 4095;
        }

        private static bool ComplexWhere1(int value)
        {
            return value % 3 != 1;
        }

        private static int ComplexSelect1(int value)
        {
            return (value ^ 0x5A5) & 4095;
        }

        private static bool ComplexWhere2(int value)
        {
            return (value & 7) != 0;
        }

        private static int ComplexSelect2(int value)
        {
            return ((value * 5) - 11) & 4095;
        }

        private static bool ComplexWhere3(int value)
        {
            return value % 5 != 2;
        }

        private static int ComplexSelect3(int value)
        {
            return (value + (value >> 1) + 17) & 4095;
        }

        private static bool ComplexWhere4(int value)
        {
            return (value & 15) < 12;
        }

        private static int ComplexSelect4(int value)
        {
            return ((value * 7) + 3) & 4095;
        }

        private static bool ComplexWhere5(int value)
        {
            return value % 11 != 0;
        }

        private static int ComplexSelect5(int value)
        {
            return (value & 255) + 1;
        }

        private struct SimpleWherePredicate : IPredicate<int>
        {
            public bool Match(in int value)
            {
                return SimpleWhere(value);
            }
        }

        private struct SimpleSelectSelector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return SimpleSelect(value);
            }
        }

        private struct ComplexWhere0Predicate : IPredicate<int>
        {
            public bool Match(in int value)
            {
                return ComplexWhere0(value);
            }
        }

        private struct ComplexSelect0Selector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return ComplexSelect0(value);
            }
        }

        private struct ComplexWhere1Predicate : IPredicate<int>
        {
            public bool Match(in int value)
            {
                return ComplexWhere1(value);
            }
        }

        private struct ComplexSelect1Selector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return ComplexSelect1(value);
            }
        }

        private struct ComplexWhere2Predicate : IPredicate<int>
        {
            public bool Match(in int value)
            {
                return ComplexWhere2(value);
            }
        }

        private struct ComplexSelect2Selector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return ComplexSelect2(value);
            }
        }

        private struct ComplexWhere3Predicate : IPredicate<int>
        {
            public bool Match(in int value)
            {
                return ComplexWhere3(value);
            }
        }

        private struct ComplexSelect3Selector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return ComplexSelect3(value);
            }
        }

        private struct ComplexWhere4Predicate : IPredicate<int>
        {
            public bool Match(in int value)
            {
                return ComplexWhere4(value);
            }
        }

        private struct ComplexSelect4Selector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return ComplexSelect4(value);
            }
        }

        private struct ComplexWhere5Predicate : IPredicate<int>
        {
            public bool Match(in int value)
            {
                return ComplexWhere5(value);
            }
        }

        private struct ComplexSelect5Selector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return ComplexSelect5(value);
            }
        }
    }
}
