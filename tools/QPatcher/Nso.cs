using LibHac;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Loader;
using System;
using System.Collections.Generic;
using System.Text;

namespace QPatcher
{
	class Nso
	{
		public readonly IFile Main;
		public readonly NsoReader Reader = new NsoReader();

		public Nso(IFile file)
		{
			Main = file;
			Reader.Initialize(file).ThrowIfFailure();
		}

		public static Nso FromBuffer(byte[] Data) =>
			new Nso(new MemoryFile(Data));

		public byte[] GetSegment(NsoReader.SegmentType type)
		{
			Reader.GetSegmentSize(type, out uint dataSize).ThrowIfFailure();
			byte[] data = new byte[dataSize];
			Reader.ReadSegment(type, data).ThrowIfFailure();
			return data;
		}
	}

	class MemoryFile : IFile
	{
		readonly byte[] Data;

		public MemoryFile(byte[] Data)
		{
			this.Data = Data;
		}

		protected override Result DoFlush()
		{
			return Result.Success;
		}

		protected override Result DoGetSize(out long size)
		{
			size = Data.LongLength;
			return Result.Success;
		}

		protected override Result DoOperateRange(Span<byte> outBuffer, OperationId operationId, long offset, long size, ReadOnlySpan<byte> inBuffer)
		{
			return ResultFs.InvalidOpenMode.Value;
		}

		protected override Result DoRead(out long bytesRead, long offset, Span<byte> destination, in ReadOption option)
		{
			long len = Math.Min(Data.LongLength - offset, destination.Length);

			Data.AsSpan().Slice((int)offset, (int)len).CopyTo(destination);
			bytesRead = Math.Min(Data.LongLength - offset, destination.Length);
			return Result.Success;
		}

		protected override Result DoSetSize(long size)
		{
			return ResultFs.InvalidOpenMode.Value;
		}

		protected override Result DoWrite(long offset, ReadOnlySpan<byte> source, in WriteOption option)
		{
			return ResultFs.InvalidOpenMode.Value;
		}
	}
}
