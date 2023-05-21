using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using LibHac;
using LibHac.Loader;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace QPatcher
{
	class Program
	{

		static void Main(string[] args)
		{
            if (args.Length == 0)
            {
                throw new Exception("Usage: qpatcher <path to prod.keys> <path to firmware folder> <save folder>");
            }

		    string SaveFolder = args[2];
            string KeysetPath = args[0];
            Keyset Keys = ExternalKeyReader.ReadKeyFile(KeysetPath);

			LocalStorage ncaData = null;
			{
                var ncas = Directory.GetFiles(args[1], "*.nca");
                foreach (var nca in ncas)
                {
                    try
                    {
                        using var file = new LocalStorage(nca, FileAccess.Read);
                        var ncaFile = new Nca(Keys, file);
                        if (ncaFile.Header.TitleId == 0x0100000000001000 && ncaFile.Header.ContentType == NcaContentType.Program)
                        {
                            ncaData = new LocalStorage(nca, FileAccess.Read);
                        }
                    }
                    catch { }
                }
			}

			if (ncaData == null)
                throw new Exception("Could not find the qlaunch NCA");

			var extractor = new ExefsExtractor(Keys, ncaData);

			var nso = new Nso(extractor.Main());

			Console.WriteLine($"NSO build id is {nso.Reader.Header.ModuleId}");

			IpsPort.IPS32Writer writer = new();
			uint offsetFor(long addr, int count, int patchLen) => 
				(uint)(addr + 4 * count - patchLen + 0x100);

            using (var f = new TargetFinder(nso.GetSegment(NsoReader.SegmentType.Text), nso.GetSegment(NsoReader.SegmentType.Ro))) { 
				var (target, count) = f.FindLockscreenTargets();

                // TODO: What if the register changes ?
				writer.Add(offsetFor(target, count, 4), new byte[] { 0x08, 0x04, 0xA0, 0x52 });
			}

			writer.FinalizePatch();

			while (!SaveFile(writer.ToArray(), Path.Combine(SaveFolder, $"{nso.Reader.Header.ModuleId}.ips"))) ;
		}

		static bool SaveFile(byte[] data, string defaultName) 
		{
			try
			{
				Console.Write("Saved as: " + defaultName);
				File.WriteAllBytes(defaultName, data);

				return true;
			}
			catch (Exception ex) 
			{
				Console.WriteLine(ex.Message);
				return false; 
			}
		}
	}

}
