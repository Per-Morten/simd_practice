using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public unsafe struct ContiguousList<T> : IDisposable
    where T : unmanaged
{
    public T* mData;
    public int mSize;
    public int mCapacity;
    public Allocator mAllocator;

    public int Length => mSize;

    public ContiguousList(Allocator allocator)
    {
        mSize = 0;
        mCapacity = 2;
        mAllocator = allocator;
        mData = (T*)UnsafeUtility.Malloc(mCapacity * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), mAllocator);
    }

    public void Dispose()
    {
        UnsafeUtility.Free(mData, mAllocator);
        mData = null;
    }

    public void Add(T data)
    {
        if (mSize >= mCapacity)
        {
            mCapacity *= 2;
            var newData = (T*)UnsafeUtility.Malloc(mCapacity * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), mAllocator);
            UnsafeUtility.MemCpy(newData, mData, mSize * UnsafeUtility.SizeOf<T>());
            UnsafeUtility.Free(mData, mAllocator);
            mData = newData;
        }
        mData[mSize++] = data;
    }

    public T this[int key]
    {
        get => mData[key];
        set => mData[key] = value;
    }

    public ref T AsRef(int key)
    {
        return ref mData[key];
    }
}

