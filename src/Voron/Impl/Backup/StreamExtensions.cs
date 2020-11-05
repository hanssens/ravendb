﻿using System;
using System.IO;
using System.Threading;
using Sparrow.Utils;

namespace Voron.Impl.Backup
{
    public static class StreamExtensions
    {
        private const int DefaultBufferSize = 81920;

        [ThreadStatic]
        private static byte[] _readBuffer;

        static StreamExtensions()
        {
            ThreadLocalCleanup.ReleaseThreadLocalState += () => _readBuffer = null;
        }

        public static void CopyTo(this Stream source, Stream destination, Action<int> onProgress, CancellationToken cancellationToken)
        {
            if (_readBuffer == null)
                _readBuffer = new byte[DefaultBufferSize];

            int count;
            while ((count = source.Read(_readBuffer, 0, _readBuffer.Length)) != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                onProgress?.Invoke(count);
                destination.Write(_readBuffer, 0, count);
            }
        }

    }
}
