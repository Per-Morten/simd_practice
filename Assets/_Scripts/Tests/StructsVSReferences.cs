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

namespace StructVsReferences
{
    public unsafe class InvalidationProneDynamicArray
    {
        private ObjectStruct* data;
        private int size = 0;
        private int capacity = 2;

        public InvalidationProneDynamicArray()
        {
            data = (ObjectStruct*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ObjectStruct>(), UnsafeUtility.AlignOf<ObjectStruct>(), Unity.Collections.Allocator.Persistent);
        }

        public ObjectStruct* this[int key]
        {
            get => &data[key];
        }

        public void Add(ObjectStruct o)
        {
            if (size + 1 > capacity)
            {
                var newData = (ObjectStruct*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ObjectStruct>(), UnsafeUtility.AlignOf<ObjectStruct>(), Unity.Collections.Allocator.Persistent);
                UnsafeUtility.MemCpy(newData, data, size * UnsafeUtility.SizeOf<ObjectStruct>());
                UnsafeUtility.Free(data, Unity.Collections.Allocator.Persistent);
                data = newData;
            }
            data[size++] = o;
        }
    }

    public unsafe class NonInvalidationProneDynamicArray
    {
        private ObjectStruct** data;
        private int size = 0;
        private int capacity = 2;

        public NonInvalidationProneDynamicArray()
        {
            data = (ObjectStruct**)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<IntPtr>(), UnsafeUtility.AlignOf<IntPtr>(), Unity.Collections.Allocator.Persistent);
        }

        public ObjectStruct* this[int key]
        {
            get => data[key];
        }

        public void Add(ObjectStruct o)
        {
            if (size + 1 > capacity)
            {
                var newData = (ObjectStruct**)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<IntPtr>(), UnsafeUtility.AlignOf<IntPtr>(), Unity.Collections.Allocator.Persistent);
                UnsafeUtility.MemCpy(newData, data, size * UnsafeUtility.SizeOf<IntPtr>());
                UnsafeUtility.Free(data, Unity.Collections.Allocator.Persistent);
                data = newData;
            }
            var newObject = (ObjectStruct*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ObjectStruct>(), UnsafeUtility.AlignOf<ObjectStruct>(), Unity.Collections.Allocator.Persistent);
            *newObject = o;
            data[size++] = newObject;
        }
    }


    public class ObjectReference
    {
        public static float FlightJitter;
        public static float Damping;

        public Unity.Mathematics.Random Random;
        public float3 Velocity;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Update(float deltaTime)
        {
            Velocity += Random.NextFloat3Direction() * FlightJitter * deltaTime;
            Velocity *= (1f - Damping);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateMultipleUnoptimized(ObjectReference[] objects, float deltaTime)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].Velocity += objects[i].Random.NextFloat3Direction() * FlightJitter * deltaTime;
                objects[i].Velocity *= (1f - Damping);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateMultipleOptimized(ObjectReference[] objects, float deltaTime)
        {
            var jitterMulDt = FlightJitter * deltaTime;
            var _1minDamping = (1f - Damping);
            for (int i = 0; i < objects.Length; i++)
                objects[i].Velocity = (objects[i].Velocity + objects[i].Random.NextFloat3Direction() * jitterMulDt) * _1minDamping;
        }
    }

    public struct ObjectStruct
    {
        public static float FlightJitter;
        public static float Damping;

        public Unity.Mathematics.Random Random;
        public float3 Velocity;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Update(float deltaTime)
        {
            Velocity += Random.NextFloat3Direction() * FlightJitter * deltaTime;
            Velocity *= (1f - Damping);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateMultipleUnoptimized(ObjectStruct[] objects, float deltaTime)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].Velocity += objects[i].Random.NextFloat3Direction() * FlightJitter * deltaTime;
                objects[i].Velocity *= (1f - Damping);
                objects[i].Velocity.x = 2.0f;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateMultipleOptimized(ObjectStruct[] objects, float deltaTime)
        {
            var jitterMulDt = FlightJitter * deltaTime;
            var _1minDamping = (1f - Damping);
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].Velocity = (objects[i].Velocity + objects[i].Random.NextFloat3Direction() * jitterMulDt) * _1minDamping;
            }
        }
    }


    public class Tests
    {
        private static ObjectStruct[] CreateStructs(int count)
        {
            var array = new ObjectStruct[count];
            for (int i = 0; i < count; i++)
                array[i] = new ObjectStruct { Velocity = new float3(1.0f, 0.0f, 1.0f), Random = new Unity.Mathematics.Random((uint)i + 1) };

            return array;
        }

        private static ObjectReference[] CreateReferences(int count)
        {
            var array = new ObjectReference[count];
            for (int i = 0; i < count; i++)
                array[i] = new ObjectReference { Velocity = new float3(1.0f, 0.0f, 1.0f), Random = new Unity.Mathematics.Random((uint)i + 1) };

            return array;
        }

        public static void RandomShuffle<T>(List<T> list, Unity.Mathematics.Random randomizer)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var idx = randomizer.NextInt(0, list.Count);
                var tmp = list[idx];
                list[idx] = list[i];
                list[i] = tmp;
            }
        }

        public static void RandomShuffle<T>(T[] list, Unity.Mathematics.Random randomizer)
        {
            for (int i = 0; i < list.Length; i++)
            {
                var idx = randomizer.NextInt(0, list.Length);
                var tmp = list[idx];
                list[idx] = list[i];
                list[i] = tmp;
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

            ObjectStruct[] OrderedStructs;
            ObjectStruct[] RandomizedStructs;

            ObjectReference[] OrderedReferences;
            ObjectReference[] RandomizedReferences;

            [OneTimeSetUp]
            public void Setup()
            {
                OrderedStructs = CreateStructs(ObjectCount);
                RandomizedStructs = CreateStructs(ObjectCount);
                RandomShuffle(RandomizedStructs, new Unity.Mathematics.Random(RandomizerSeed));

                OrderedReferences = CreateReferences(ObjectCount);
                RandomizedReferences = CreateReferences(ObjectCount);
                RandomShuffle(RandomizedReferences, new Unity.Mathematics.Random(RandomizerSeed));
            }

            [Test, Performance]
            public void SingleUpdate()
            {
                TestUtility.Time("Reference_SingleUpdate_Sorted", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    for (int i = 0; i < OrderedReferences.Length; i++)
                        OrderedReferences[i].Update(dt.NextFloat());
                });

                TestUtility.Time("Reference_SingleUpdate_Randomized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    for (int i = 0; i < RandomizedReferences.Length; i++)
                        RandomizedReferences[i].Update(dt.NextFloat());
                });

                TestUtility.Time("Struct_SingleUpdate_Sorted", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    for (int i = 0; i < OrderedStructs.Length; i++)
                        OrderedStructs[i].Update(dt.NextFloat());
                });

                TestUtility.Time("Struct_SingleUpdate_Randomized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    for (int i = 0; i < RandomizedStructs.Length; i++)
                        RandomizedStructs[i].Update(dt.NextFloat());
                });
            }

            [Test, Performance]
            public void MultipleUpdateUnoptimized()
            {
                TestUtility.Time("Reference_MultipleUpdate_Sorted_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectReference.UpdateMultipleUnoptimized(OrderedReferences, dt.NextFloat());
                });

                TestUtility.Time("Reference_MultipleUpdate_Randomize_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectReference.UpdateMultipleUnoptimized(RandomizedReferences, dt.NextFloat());
                });

                TestUtility.Time("Struct_MultipleUpdate_Sorted_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectStruct.UpdateMultipleUnoptimized(OrderedStructs, dt.NextFloat());
                });

                TestUtility.Time("Struct_MultipleUpdate_Randomize_Unoptimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectStruct.UpdateMultipleUnoptimized(RandomizedStructs, dt.NextFloat());
                });
            }

            [Test, Performance]
            public void MultipleUpdateOptimized()
            {
                TestUtility.Time("Reference_MultipleUpdate_Sorted_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectReference.UpdateMultipleOptimized(OrderedReferences, dt.NextFloat());
                });

                TestUtility.Time("Reference_MultipleUpdate_Randomize_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectReference.UpdateMultipleOptimized(RandomizedReferences, dt.NextFloat());
                });

                TestUtility.Time("Struct_MultipleUpdate_Sorted_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectStruct.UpdateMultipleOptimized(OrderedStructs, dt.NextFloat());
                });

                TestUtility.Time("Struct_MultipleUpdate_Randomize_Optimized", () =>
                {
                    var dt = new Unity.Mathematics.Random(RandomizerSeed);
                    ObjectStruct.UpdateMultipleOptimized(RandomizedStructs, dt.NextFloat());
                });
            }
        }
    }
}