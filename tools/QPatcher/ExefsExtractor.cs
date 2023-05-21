using LibHac;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QPatcher
{
	class ExefsExtractor
	{
		public readonly Keyset Keys;
		public readonly IFileSystem Fs;
		public readonly Nca Nca;

		public ExefsExtractor(Keyset keys, IStorage st)
		{
			Keys = keys;
			Nca = new Nca(Keys, st);
			Fs = Nca.OpenFileSystem(NcaSectionType.Code, IntegrityCheckLevel.ErrorOnInvalid);
		}

		public IFile this[string fullPath]
		{
			get
			{
				Fs.OpenFile(out IFile f, (LibHac.Common.U8Span)fullPath, OpenMode.Read).ThrowIfFailure();
				return f;
			}
		}

		public IFile Main() => this["/main"];	

		public static ExefsExtractor FromFile(Keyset keys, string FilePath) =>
			new ExefsExtractor(keys, new LocalStorage(FilePath, FileAccess.Read));

		public static ExefsExtractor FromMemory(Keyset keys, byte[] Nca) =>
			new ExefsExtractor(keys, new MemoryStorage(Nca));
	}

	static class ExtenIfs
	{
		public static byte[] ToArray(this IFile file)
		{
			file.GetSize(out long size).ThrowIfFailure();

			byte[] res = new byte[size];
			file.Read(out long read, 0, res).ThrowIfFailure();

			if (size != read)
				throw new Exception("Couldn't read full file");

			return res;
		}
	}
}
