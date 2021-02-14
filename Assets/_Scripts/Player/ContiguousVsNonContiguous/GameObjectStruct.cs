using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[BurstCompile]
public struct GameObjectStruct
{
    public static float FlightJitter;
    public static float Damping;

    public MockRandom Random;
    public float3 Velocity;

    [BurstCompile]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BurstUpdateImpl(ref GameObjectStruct obj, float deltaTime, float flightJitter, float damping)
    {
        obj.Velocity += obj.Random.NextFloat3Direction() * flightJitter * deltaTime;
        obj.Velocity *= (1f - damping);
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpdateSingleContiguous(in ContiguousList<GameObjectStruct> objects, float deltaTime, float flightJitter, float damping)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            ref var o = ref objects.AsRef(i);
            BurstUpdateImpl(ref o, deltaTime, flightJitter, damping);
        }
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpdateSingleNonContiguous(in NonContiguousList<GameObjectStruct> objects, float deltaTime, float flightJitter, float damping)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            ref var o = ref objects.AsRef(i);
            BurstUpdateImpl(ref o, deltaTime, flightJitter, damping);
        }
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpdateMultipleUnoptimizedContiguous(in ContiguousList<GameObjectStruct> objects, float deltaTime, float flightJitter, float damping)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            ref var o =  ref objects.AsRef(i);
            o.Velocity += o.Random.NextFloat3Direction() * flightJitter * deltaTime;
            o.Velocity *= (1f - damping);
        }
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpdateMultipleUnoptimizedNonContiguous(in NonContiguousList<GameObjectStruct> objects, float deltaTime, float flightJitter, float damping)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            ref var o = ref objects.AsRef(i);
            o.Velocity += o.Random.NextFloat3Direction() * flightJitter * deltaTime;
            o.Velocity *= (1f - damping);
        }
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpdateMultipleOptimizedContiguous(in ContiguousList<GameObjectStruct> objects, float deltaTime, float flightJitter, float damping)
    {
        var jitterMulDt = flightJitter * deltaTime;
        var _1minDamping = (1f - damping);
        for (int i = 0; i < objects.Length; i++)
        {
            ref var o = ref objects.AsRef(i);
            o.Velocity += o.Random.NextFloat3Direction() * jitterMulDt;
            o.Velocity *= _1minDamping;
        }
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpdateMultipleOptimizedNonContiguous(in NonContiguousList<GameObjectStruct> objects, float deltaTime, float flightJitter, float damping)
    {
        var jitterMulDt = flightJitter * deltaTime;
        var _1minDamping = (1f - damping);
        for (int i = 0; i < objects.Length; i++)
        {
            ref var o = ref objects.AsRef(i);
            o.Velocity += o.Random.NextFloat3Direction() * jitterMulDt;
            o.Velocity *= _1minDamping;
        }
    }
}



public struct MockData1Bytes
{
    public byte Value;
}

public struct MockData2Bytes
{
    public MockData1Bytes Value0;
    public MockData1Bytes Value1;
}

public struct MockData4Bytes
{
    public MockData2Bytes Value0;
    public MockData2Bytes Value1;
}

public struct MockData8Bytes
{
    public MockData4Bytes Value0;
    public MockData4Bytes Value1;
}

public struct MockData16Bytes
{
    public MockData8Bytes Value0;
    public MockData8Bytes Value1;
}

public struct MockData32Bytes
{
    public MockData16Bytes Value0;
    public MockData16Bytes Value1;
}

public struct MockData64Bytes
{
    public MockData32Bytes Value0;
    public MockData32Bytes Value1;
}

public struct MockData128Bytes
{
    public MockData64Bytes Value0;
    public MockData64Bytes Value1;
}

public struct MockData256Bytes
{
    public MockData128Bytes Value0;
    public MockData128Bytes Value1;
}

public struct MockData512Bytes
{
    public MockData256Bytes Value0;
    public MockData256Bytes Value1;
}