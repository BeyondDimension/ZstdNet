﻿using System;
using size_t = System.UIntPtr;

namespace ZstdNet
{
	public class Compressor : IDisposable
	{
		public Compressor()
			: this(CompressionOptions.Default)
		{}

		public Compressor(CompressionOptions options)
		{
			Options = options;

			cctx = ExternMethods.ZSTD_createCCtx().EnsureZstdSuccess();
		}

		~Compressor() => Dispose(false);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if(cctx == IntPtr.Zero)
				return;

			ExternMethods.ZSTD_freeCCtx(cctx);

			cctx = IntPtr.Zero;
		}

		public byte[] Wrap(byte[] src)
			=> Wrap(new ArraySegment<byte>(src));

		public byte[] Wrap(ArraySegment<byte> src)
		{
			var dstCapacity = GetCompressBound(src.Count);
			var dst = new byte[dstCapacity];

			var dstSize = Wrap(src, dst, 0);
			if(dstCapacity == dstSize)
				return dst;

			var result = new byte[dstSize];
			Array.Copy(dst, result, dstSize);
			return result;
		}

		public static int GetCompressBound(int size)
			=> (int)ExternMethods.ZSTD_compressBound((size_t)size);

		public int Wrap(byte[] src, byte[] dst, int offset)
			=> Wrap(new ArraySegment<byte>(src), dst, offset);

		public int Wrap(ArraySegment<byte> src, byte[] dst, int offset)
		{
			if(offset < 0 || offset >= dst.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));

			var dstCapacity = dst.Length - offset;

			size_t dstSize;
			using(var srcPtr = new ArraySegmentPtr(src))
			using(var dstPtr = new ArraySegmentPtr(dst, offset, dstCapacity))
			{
				if(Options.Cdict == IntPtr.Zero)
					dstSize = ExternMethods.ZSTD_compressCCtx(cctx, dstPtr, (size_t)dstCapacity, srcPtr, (size_t)src.Count, Options.CompressionLevel);
				else
					dstSize = ExternMethods.ZSTD_compress_usingCDict(cctx, dstPtr, (size_t)dstCapacity, srcPtr, (size_t)src.Count, Options.Cdict);
			}

			dstSize.EnsureZstdSuccess();
			return (int)dstSize;
		}

		public readonly CompressionOptions Options;

		private IntPtr cctx;
	}
}
