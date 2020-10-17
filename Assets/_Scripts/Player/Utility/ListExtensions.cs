using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class ListExtensions
{
    public struct ListNativeArrayViewHandle : IDisposable
    {
        private ulong _gcHandle;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle _safety;
        private DisposeSentinel _sentinel;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public ListNativeArrayViewHandle(ulong gcHandle, AtomicSafetyHandle safety, DisposeSentinel sentinel)
        {
            _safety = safety;
            _sentinel = sentinel;
            _gcHandle = gcHandle;
        }
#else
        public ListNativeArrayViewHandle(ulong gcHandle)
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
    }

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
    /// The array itself does not need to be disposed, but you need to dispose the <see cref="ListNativeArrayViewHandle"/> you get back, Unity's Memory Leak Detection will tell you if you forget.
    /// Do not use the array after calling <see cref="ListNativeArrayViewHandle.Dispose"/> on the <see cref="ListNativeArrayViewHandle"/> returned from this function, 
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
    public unsafe static ListNativeArrayViewHandle ViewAsNativeArray<T>(this List<T> list, out NativeArray<T> array) where T : unmanaged
    {
        var lArray = NoAllocHelpers.ExtractArrayFromListT(list);
        var ptr = UnsafeUtility.PinGCArrayAndGetDataAddress(lArray, out var handle);
        array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, list.Count, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out var safety, out var sentinel, 0, Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);
        return new ListNativeArrayViewHandle(handle, safety, sentinel);
#else
        return new ListNativeArrayViewHandle(handle);
#endif
    }
}
