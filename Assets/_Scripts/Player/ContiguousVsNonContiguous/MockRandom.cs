using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine.Assertions;

public struct MockRandom
{
    public uint mState;
    public MockRandom(uint state)
    {
        Assert.IsTrue(state != 0);
        mState = state;
    }

    public float3 NextFloat3Direction()
    {
        return new float3(mState);
    }
}

