///
/// MIT License
/// 
/// Copyright (c) 2021 Per-Morten Straume
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.
/// 

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

public static class ViewAsNativeArrayExtensions
{
    /// <summary>
    /// View the list as a <see cref="NativeArray{T}"/> without having to copy it or doing all the boilerplate for getting the pointer out of a list.
    /// Useful for allowing a job to work on a list.
    ///
    /// <para>
    /// Put this thing in a disposable scope unless you can guarantee that the list will never change size or reallocate (in that case consider using a <see cref="NativeArray{T}"/> instead),
    /// as Unity will <b>not</b> tell you if you're out of bounds, accessing invalid data, or accessing stale data because you have a stale/invalid view of the list.
    /// The following changes to the list will turn the view invalid/stale:
    /// <list type="number">
    /// <item>The contents of the array will be stale (not reflect any changes to the values in the list) in case of a reallocation (changes to, or adding more items than, <see cref="List{T}.Capacity"/> or using <see cref="List{T}.TrimExcess"/>)</item>
    /// <item>The length of the array will be wrong if you add/remove elements from the list</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// The array itself does not need to be disposed, but you need to dispose the <see cref="ViewAsNativeArrayHandle"/> you get back, Unity's Memory Leak Detection will tell you if you forget.
    /// Do not use the array after calling <see cref="ViewAsNativeArrayHandle.Dispose"/> on the <see cref="ViewAsNativeArrayHandle"/> returned from this function,
    /// as you can risk the garbage collector removing the array from down under you, Unity's Collections Safety Checks will tell you if you do this.
    /// There is <b>no</b> race detection for accessing multiple different views of the same list in different jobs concurrently, or modifying the list while a job is working on a view.
    /// </para>
    ///
    /// Usage:
    /// <code>
    /// List&lt;int&gt; l;
    /// using (list.AsNativeArray(out var array))
    /// {
    ///     // work on array
    /// }
    /// </code>
    /// </summary>
    public unsafe static ViewAsNativeArrayHandle ViewAsNativeArray<T>(this List<T> list, out NativeArray<T> nativeArray) where T : unmanaged
    {
        var lArray = list.GetUnderlyingArray();
        return lArray.ViewAsNativeArray(list.Count, out nativeArray);
    }

    /// <summary>
    /// <inheritdoc cref="ViewAsNativeArray{T}(List{T}, out NativeArray{T})"/>
    /// </summary>
    public unsafe static ViewAsNativeArrayHandle ViewAsNativeArray<T>(this T[] array, out NativeArray<T> nativeArray) where T : unmanaged
    {
        return array.ViewAsNativeArray(array.Length, out nativeArray);
    }

    private unsafe static ViewAsNativeArrayHandle ViewAsNativeArray<T>(this T[] array, int length, out NativeArray<T> nativeArray) where T : unmanaged
    {
        var ptr = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out var handle);
        nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out var safety, out var sentinel, 0, Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, safety);
        return new ViewAsNativeArrayHandle(handle, safety, sentinel);
#else
        return new ViewAsNativeArrayHandle(handle);
#endif
    }

    // Adapted from: https://forum.unity.com/threads/collectionmarshal-asspan-support.1235407/#post-8616789
    // Modified to also give access to the _size variable via the UnderlyingListStructure struct
    [StructLayout(LayoutKind.Explicit)]
    private struct CastHelper
    {
        [FieldOffset(0)]
        public StrongBox<UnderlyingListStructure> Underlying;

        [FieldOffset(0)]
        public object List;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnderlyingListStructure
    {
        public Array _items;
        public int _size;
    }

    /// <summary>
    /// Gets the array backing the list.
    /// <para/>
    /// Note: The returned array has length equal to <paramref name="self"/>.Capacity, not list.Count!
    /// </summary>
    public static T[] GetUnderlyingArray<T>(this List<T> self)
    {
        return (T[])new CastHelper { List = self }.Underlying.Value._items;
    }

    public static ref T AsRef<T>(this List<T> self, int idx)
    {
        return ref GetUnderlyingArray(self)[idx];
    }

    /// <summary>
    /// Directly manipulates the _size variable of the list. Does not change
    /// the capacity of the list, nor does it initialize any uninitialized
    /// elements as a result of the resizing.
    ///
    /// Note: It's the callers responsibility to ensure that <paramref
    /// name="self"/>.Capacity >= <paramref name="size"/>
    /// </summary>
    public static void ResizeNoAlloc<T>(this List<T> self, int size)
    {
        //UnityEngine.Assertions.Assert.IsTrue(self.Capacity >= size);
        new CastHelper { List = self }.Underlying.Value._size = size;
    }

    public unsafe static ref T AsRef<T>(this in NativeArray<T> list, int idx) where T : unmanaged
    {
        //UnityEngine.Assertions.Assert.IsTrue(idx >= 0);
        //UnityEngine.Assertions.Assert.IsTrue(idx < list.Length);
        return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafePtr(), idx);
    }

    public struct ViewAsNativeArrayHandle : IDisposable
    {
        private ulong _gcHandle;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle _safety;
        private DisposeSentinel _sentinel;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public ViewAsNativeArrayHandle(ulong gcHandle, AtomicSafetyHandle safety, DisposeSentinel sentinel)
        {
            _safety = safety;
            _sentinel = sentinel;
            _gcHandle = gcHandle;
        }
#else
            public ViewAsNativeArrayHandle(ulong gcHandle)
            {
                _gcHandle = gcHandle;
            }
#endif

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref _safety, ref _sentinel);
#endif
            UnsafeUtility.ReleaseGCObject(_gcHandle);
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref _sentinel);

            var jobHandle = new DisposeJob
            {
                Data = new DisposeData
                {
                    GcHandle = _gcHandle,
                    m_Safety = _safety
                }
            }
            .Schedule(dependsOn);

            AtomicSafetyHandle.Release(_safety);
            return jobHandle;
#else
            return new DisposeJob
            {
                Data = new DisposeData
                {
                    GcHandle = _gcHandle
                }
            }
            .Schedule(dependsOn);
#endif
        }

        [NativeContainer]
        private struct DisposeData
        {
            public ulong GcHandle;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Breaking naming convention required by Unity's safety system.
            public AtomicSafetyHandle m_Safety;
#endif
            public void Dispose()
            {
                UnsafeUtility.ReleaseGCObject(GcHandle);
            }
        }

        [BurstCompile]
        private struct DisposeJob : IJob
        {
            public DisposeData Data;

            public void Execute()
            {
                Data.Dispose();
            }
        }
    }
}
