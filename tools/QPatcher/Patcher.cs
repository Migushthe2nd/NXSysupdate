using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace QPatcher
{
	class TargetFinder : IDisposable
	{
		private readonly byte[] Code, Data;
		private readonly CapstoneArm64Disassembler disassembler = CapstoneDisassembler.CreateArm64Disassembler(Arm64DisassembleMode.LittleEndian);
		Arm64Instruction[] _disasm = null;

		const long IdaOffset = 0x7100000000;
		Arm64Instruction[] Disasm => _disasm ??= disassembler.Disassemble(Code, IdaOffset);

		public TargetFinder(byte[] code, byte[] data) 
		{
			Code = code;
			Data = data;
			disassembler.EnableSkipDataMode = true;
			disassembler.EnableInstructionDetails = false;
			disassembler.DisassembleSyntax = DisassembleSyntax.Intel;
		}

		public class PatchTargets
		{
			public List<Arm64Instruction[]> Blocks = new List<Arm64Instruction[]>();

			public void Add(params Arm64Instruction[] args)
			{
				foreach (var b in Blocks)
					foreach (var i in b)
						if (args.Any(x => x.Address == i.Address))
							return;

				Blocks.Add(args);
			}

			public int Count => Blocks.Count;
		}

		private static int[] IndexesOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
		{
			List<int> result = new List<int>();
			int offset = 0;
			while (haystack.Length > 0)
			{
				int matchIndex = haystack.IndexOf(needle);
				if (matchIndex >= 0)
					result.Add(matchIndex + offset);
				else break;
				offset += matchIndex + 1;
				haystack = haystack.Slice(offset);
			}
			return result.ToArray();
		}


		int[] FindStringOffs(string str, int offset = 0)
		{
			byte[] strData = Encoding.ASCII.GetBytes(str);
			return IndexesOf(Data, strData).Select(x => x + offset).ToArray();
		}

		long[] FindPossilbeReferences(int[] stringOffsets)
		{
			long index = 0;
			List<long> instrIndex = new List<long>();

			string[] strings = new string[stringOffsets.Length];
			foreach (var off in stringOffsets)
				strings[index++] = "#0x" + (off & 0xFFF).ToString("x");

			for (index = 100; index < Disasm.LongLength; index++)
			{                
				Arm64Instruction instr = Disasm[index];

				/*
				 *	We're looking for the code loading the SceneEntrance string, it's in the form of
				 *		ADDRP	xx, #immpage
				 *		ADD		xx, xx, #(off & 0xFFF)
				*/

				if (instr.Id != Arm64InstructionId.ARM64_INS_ADD)
					continue;

				Arm64Instruction addrp = null;

				if (Disasm[index - 1].Id == Arm64InstructionId.ARM64_INS_ADRP)
					addrp = Disasm[index - 1];

				if (addrp == null)
					continue;

				var addrpops = addrp.Operands();
				var operands = instr.Operands();

				if (operands.Length < 3) // Add should have at least three operands
					Debugger.Break();

				if (operands.Length != 3) // We're looking for three operands
					continue;

				if (operands[1] != addrpops[0]) // Add should be adding to the same register adrp loaded the page
					continue;

				if (operands[0] != operands[1]) // Add works on the same register but this may not always be the case, TODO: check
					continue;

				if (!strings.Contains(operands[2])) // And make sure the last 12 bits match
					continue;

				instrIndex.Add(index);
			}

			return instrIndex.ToArray();
		}

		PatchTargets FindConstantsNearRefs(long[] refs, int rangeMin, int rangeMax)
		{
			/*
			 *	As of 10.0 the value to patch is 0x145000 assigned with
			 *		MOV		W8, #0x5000
			 *		MOVK	W8, #0x14,LSL#16
			 */
			PatchTargets res = new PatchTargets();

			foreach (int possibleref in refs)
			{
				// Look around possible references
				var instrArr = Disasm.Skip(possibleref - 20).Take(40).ToArray();
				for (int i = 0; i < instrArr.Length - 1; i++)
				{
					var instr = instrArr[i];

					//if (instr.Address == 0x071004D7590) Debugger.Break();

					if (!instr.IsMov())
						continue;

					if (!instr.Operand.Contains("#")) // It must be an immediate
						continue;

					bool IsRangeValid(Arm64Instruction ins)
					{
						var op = ins.Operands();
						int v = Convert.ToInt32(op[op.Length - 2].Replace("#0x", ""), 16);
						return !(v < rangeMin || v > rangeMax);
					}

					if (instr.Operand.Contains("lsl #16")) // if it's shifting by 16 bits check if we're in range
					{
						if (IsRangeValid(instr))
						{
							res.Add(instr);
							continue; // Print single instruciton and continue
						}
						else continue;
					}

					var next = instrArr[i + 1];

					if (!next.IsMov()) // Constant is > 16-bit so it must be loaded in two steps
						continue;

					if (next.Operands()[0] != instr.Operands()[0]) // So they must target the same register
						continue;

					if (!next.Operand.Contains("lsl #16") || !IsRangeValid(next)) // Constant is too small
						continue;

					res.Add(instr, next);
				}
			}

			return res;
		}

		(long, int) SelectPatch(PatchTargets targets) 
		{
			long PatchTarget = -1;
			int PatchCount = 0;

            if (targets.Count == 0)
				Console.WriteLine("No targets found :(");
			else if (targets.Count == 1)
			{
				PatchTarget = targets.Blocks[0][0].Address;
				PatchCount = targets.Blocks[0].Length;
				Console.WriteLine($"Target found: {PatchTarget:x}");
				foreach (var i in targets.Blocks[0])
					Console.WriteLine($"  {i.Mnemonic}\t{i.Operand}");
			}
			else
			{
				Console.WriteLine("Found multiple targets:");
				int index = 0;
				foreach (var t in targets.Blocks)
				{
					Console.WriteLine($"{index++} at {t[0].Address:x}");
					foreach (var i in t)
						Console.WriteLine($"  {i.Mnemonic}\t{i.Operand}");
				}

				Console.Write("Which one to use ? ");
				index = int.Parse(Console.ReadLine());
				if (index < 0)
					Console.WriteLine("Aborting...");
				else
				{
					PatchTarget = targets.Blocks[index][0].Address;
					PatchCount = targets.Blocks[index].Length;
				}
			}

			return (PatchTarget - IdaOffset, PatchCount);
		}

		public (long, int) FindLockscreenTargets()
		{
			Console.WriteLine("Scanning for SceneEntrance...");
			var strOffs = FindStringOffs("\0SceneEntrance\0", 1);
			var targets = FindConstantsNearRefs(FindPossilbeReferences(strOffs), 0x10, 0x25);
			return SelectPatch(targets);
		}

		//public (long, int) FindAllAppsTargets()
		//{
		//	Console.WriteLine("Scanning for SceneGrpList...");
		//	var strOffs = FindStringOffs("\0SceneGrpList\0", 1);
		//	var targets = FindConstantsNearRefs(FindPossilbeReferences(strOffs), 0x100, 0x400);
		//	return SelectPatch(targets);
		//}

		public void Dispose()
		{
			disassembler.Dispose();
		}
	}

	static class ExtenArm
	{
		public static string[] Operands(this Arm64Instruction instr) =>
			instr.Operand.Split(',').Select(x => x.ToLower().Trim()).ToArray();

		public static bool IsMov(this Arm64Instruction instr) =>
					instr.Id == Arm64InstructionId.ARM64_INS_MOVK || instr.Id == Arm64InstructionId.ARM64_INS_MOV ||
					instr.Id == Arm64InstructionId.ARM64_INS_MOVZ || instr.Id == Arm64InstructionId.ARM64_INS_MOVI ||
					instr.Id == Arm64InstructionId.ARM64_INS_MOVN;
	}
}
