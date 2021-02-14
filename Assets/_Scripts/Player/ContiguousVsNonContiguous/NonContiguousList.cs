using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public unsafe struct NonContiguousList<T> : IDisposable
    where T : unmanaged
{
    public T** mData;
    public int mSize;
    public int mCapacity;
    public Allocator mAllocator;
    
    public int Length => mSize;

    public NonContiguousList(Allocator allocator)
    {
        mSize = 0;
        mCapacity = 2;
        mAllocator = allocator;
        mData = (T**)UnsafeUtility.Malloc(mCapacity * UnsafeUtility.SizeOf<IntPtr>(), UnsafeUtility.AlignOf<IntPtr>(), mAllocator);
    }

    public void Dispose()
    {
        for (int i = 0; i < mSize; i++)
            UnsafeUtility.Free(mData[i], mAllocator);
        UnsafeUtility.Free(mData, mAllocator);
        mData = null;
    }

    public void Add(T data)
    {
        if (mSize >= mCapacity)
        {
            mCapacity *= 2;
            var newData = (T**)UnsafeUtility.Malloc(mCapacity * UnsafeUtility.SizeOf<IntPtr>(), UnsafeUtility.AlignOf<IntPtr>(), mAllocator);
            UnsafeUtility.MemCpy(newData, mData, mSize * UnsafeUtility.SizeOf<IntPtr>());
            UnsafeUtility.Free(mData, mAllocator);
            mData = newData;
        }
        var n = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), mAllocator);
        *n = data;
        mData[mSize++] = n;
    }

    public T this[int key]
    {
        get => *mData[key];
        set => *mData[key] = value;
    }

    public ref T AsRef(int key)
    {
        return ref *mData[key];
    }
}

