﻿using System;
using Sparrow.Utils;

namespace Sparrow.LowMemory
{
    public abstract class AbstractLowMemoryMonitor : IDisposable
    {
        public abstract MemoryInfoResult GetMemoryInfoOnce();

        public abstract MemoryInfoResult GetMemoryInfo(bool extended = false);

        public abstract bool IsEarlyOutOfMemory(MemoryInfoResult memInfo, out Size commitChargeThreshold);

        public abstract DirtyMemoryState GetDirtyMemoryState();

        public abstract void AssertNotAboutToRunOutOfMemory();

        internal static long GetManagedMemoryInBytes()
        {
            return GC.GetTotalMemory(false);
        }

        internal static long GetUnmanagedAllocationsInBytes()
        {
            return NativeMemory.TotalAllocatedMemory;
        }

        public abstract void Dispose();
    }
}
