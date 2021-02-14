using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace ContiguousVsNonContiguousListTests
{
    public class Tests
    {
        private static ContiguousList<GameObjectStruct> CreateContiguousGameObjectStructs(int count)
        {
            var l = new ContiguousList<GameObjectStruct>(Unity.Collections.Allocator.Persistent);
            for (int i = 0; i < count; i++)
                l.Add(new GameObjectStruct { Velocity = new float3(1.0f, 0.0f, 1.0f), Random = new MockRandom((uint)i + 1) });
            return l;
        }

        private static NonContiguousList<GameObjectStruct> CreateNonContiguousGameObjectStructs(int count)
        {
            var l = new NonContiguousList<GameObjectStruct>(Unity.Collections.Allocator.Persistent);
            for (int i = 0; i < count; i++)
                l.Add(new GameObjectStruct { Velocity = new float3(1.0f, 0.0f, 1.0f), Random = new MockRandom((uint)i + 1) });
            return l;
        }

        public static void RandomShuffle(ContiguousList<GameObjectStruct> l, Unity.Mathematics.Random r)
        {
            for (int i = 0; i < l.Length; i++)
            {
                var idx = r.NextInt(0, l.Length);
                var tmp0 = l[idx];
                var tmp1 = l[i];
                tmp0.Random = new MockRandom((uint)i + 1);
                l[i] = tmp0;

                tmp1.Random = new MockRandom((uint)idx + 1);
                l[idx] = tmp1;
            }
        }

        public unsafe static void RandomShuffle(NonContiguousList<GameObjectStruct> l, Unity.Mathematics.Random r)
        {
            for (int i = 0; i < l.Length; i++)
            {
                var idx = r.NextInt(0, l.Length);
                var tmp0 = l.mData[idx];
                var tmp1 = l.mData[i];
                tmp0->Random = new MockRandom((uint)i + 1);
                l.mData[i] = tmp0;

                tmp1->Random = new MockRandom((uint)idx + 1);
                l.mData[idx] = tmp1;
            }
        }

        [TestFixture]
        public class PerformanceTests
        {

#if TEN_MILLION_LIST
            const int ObjectCount = 10000000;
#elif ONE_MILLION_LIST
            const int ObjectCount = 1000000;
#elif HUNDRED_THOUSAND_LIST
            const int ObjectCount = 100000;
#else
            const int ObjectCount = 10000;
#endif

            const uint RandomizerSeed = 1;

            ContiguousList<GameObjectStruct> OrderedContiguous;
            ContiguousList<GameObjectStruct> RandomizedContiguous;

            NonContiguousList<GameObjectStruct> OrderedNonContiguous;
            NonContiguousList<GameObjectStruct> RandomizedNonContiguous;

            [OneTimeSetUp]
            public void Setup()
            {
                OrderedContiguous = CreateContiguousGameObjectStructs(ObjectCount);
                RandomizedContiguous = CreateContiguousGameObjectStructs(ObjectCount);
                RandomShuffle(RandomizedContiguous, new Unity.Mathematics.Random(RandomizerSeed));

                OrderedNonContiguous = CreateNonContiguousGameObjectStructs(ObjectCount);
                RandomizedNonContiguous = CreateNonContiguousGameObjectStructs(ObjectCount);
                RandomShuffle(RandomizedNonContiguous, new Unity.Mathematics.Random(RandomizerSeed));
            }

            [OneTimeTearDown]
            public void Teardown()
            {
                OrderedContiguous.Dispose();
                RandomizedContiguous.Dispose();
                OrderedNonContiguous.Dispose();
                RandomizedNonContiguous.Dispose();
            }

            [Test, Performance]
            public void SingleUpdate()
            {
                TestUtility.Time("Reference_SingleUpdate_Sorted", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateSingleNonContiguous(OrderedNonContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Reference_SingleUpdate_Randomized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateSingleNonContiguous(RandomizedNonContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Struct_SingleUpdate_Sorted", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateSingleContiguous(OrderedContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Struct_SingleUpdate_Randomized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateSingleContiguous(RandomizedContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });
            }

            [Test, Performance]
            public void MultipleUpdateUnoptimized()
            {
                TestUtility.Time("Reference_MultipleUpdate_Sorted_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleUnoptimizedNonContiguous(OrderedNonContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Reference_MultipleUpdate_Randomize_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleUnoptimizedNonContiguous(RandomizedNonContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Struct_MultipleUpdate_Sorted_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleUnoptimizedContiguous(OrderedContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Struct_MultipleUpdate_Randomize_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleUnoptimizedContiguous(RandomizedContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });
            }

            [Test, Performance]
            public void MultipleUpdateOptimized()
            {
                TestUtility.Time("Reference_MultipleUpdate_Sorted_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleOptimizedNonContiguous(OrderedNonContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Reference_MultipleUpdate_Randomize_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleOptimizedNonContiguous(RandomizedNonContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Struct_MultipleUpdate_Sorted_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleOptimizedContiguous(OrderedContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });

                TestUtility.Time("Struct_MultipleUpdate_Randomize_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    GameObjectStruct.UpdateMultipleOptimizedContiguous(RandomizedContiguous, dt.NextFloat(), GameObjectStruct.FlightJitter, GameObjectStruct.Damping);
                });
            }
        }
    }
}