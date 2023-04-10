// exelix11
using LibHac;
using LibHac.Boot;
using LibHac.Common;
using LibHac.Common.FixedArrays;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSrv;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

if (args.Length == 0)
{
    Console.WriteLine("Usage: gibkeys <path to firmware folder>");
    Console.WriteLine("Keys are printed in hactool format, exit code is 0 on sucess.");
    return 1;
}

KeySet keyset = KeySet.CreateDefaultKeySet();
keyset.SetMode(KeySet.Mode.Prod);

// Muh illegal numbers
keyset.MarikoBek = ParseKey("6A5D168B14E64CADD70DA934A06CC222");
keyset.MarikoKek = ParseKey("4130B8B842DD7CD2EA8FD50D3D48B77C");

keyset.DeriveKeys();

var ncas = Directory.GetFiles(args[0], "*.nca");
string? package1Nca = null;
string? qlaunchNca = null;
int KeyGeneration = 0;
foreach (var nca in ncas)
{
    try
    {
        using var file = new LocalStorage(nca, FileAccess.Read);
        var ncaFile = new Nca(keyset, file);
        if (ncaFile.Header.TitleId == 0x0100000000000819 && ncaFile.Header.ContentType == NcaContentType.Data)
            package1Nca = nca;
        // Use qlaunch as an oracle to find the current keygeneration value
        // No real reason to use qlaunch in particular, we just need a program nca that uses latest key generation
        else if (ncaFile.Header.TitleId == 0x0100000000001000 && ncaFile.Header.ContentType == NcaContentType.Program)
        {
            qlaunchNca = nca;
            KeyGeneration = ncaFile.Header.KeyGeneration;
        }
    }
    catch { }
}

if (package1Nca is null)
{
    Console.WriteLine("Couldn't find package1data nca");
    return 1;
}

byte[] package1data;
{
    using var file = new LocalStorage(package1Nca, FileAccess.Read);
    var nca = new Nca(keyset, file);
    var index = Nca.GetSectionIndexFromType(NcaSectionType.Data, nca.Header.ContentType);
    var fs = new UniqueRef<IFileSystem>(nca.OpenFileSystem(index, LibHac.Tools.FsSystem.IntegrityCheckLevel.None));

    var horizon = new Horizon(new HorizonConfiguration());
    using var client = horizon.CreatePrivilegedHorizonClient();
    using var fscli = client.Fs;

    fscli.Register("romfs"u8, ref fs).ThrowIfFailure();
    fscli.OpenFile(out var handle, "romfs:/a/package1"u8, OpenMode.Read).ThrowIfFailure();

    fscli.GetFileSize(out var size, handle).ThrowIfFailure();

    package1data = new byte[(int)size];
    fscli.ReadFile(handle, 0, package1data).ThrowIfFailure();

    fscli.CloseFile(handle);
}

byte[] secmon;
{
    using var file = new SharedRef<IStorage>(new MemoryStorage(package1data));
    var package1 = new Package1();

    package1.Initialize(keyset, in file).ThrowIfFailure();
    using var mon = package1.OpenSecureMonitorStorage();

    mon.GetSize(out var length).ThrowIfFailure();

    secmon = new byte[(int)length];
    mon.Read(0, secmon).ThrowIfFailure();
}

{
    // weeb shit
    var ohayo = new byte[] { 0x4F, 0x48, 0x41, 0x59, 0x4F };
    var index = Search(secmon, ohayo);

    if (index == -1)
    {
        Console.WriteLine("Couldn't find OHAYO string");
        return 1;
    }

    index += 0x3B + ohayo.Length;
    var keysource = secmon.AsSpan(index, 16);
    var k = new LibHac.Crypto.AesKey();
    keysource.CopyTo(k.Data);

    KeyGeneration -= 1;
    Console.WriteLine($"mariko_master_kek_source_{KeyGeneration:x2} = {k}");

    keyset.MarikoMasterKekSources[KeyGeneration] = k;
    keyset.DeriveKeys();

    Console.WriteLine($"master_key_{KeyGeneration:x2} = {keyset.MasterKeys[KeyGeneration]}");
}

// Attempt to decrypt qlaunch to make sure the key is correct 
{
    using var file = new LocalStorage(package1Nca, FileAccess.Read);
    var nca = new Nca(keyset, file);
    
    if (!nca.CanOpenSection(NcaSectionType.Data))
    {
        Console.WriteLine("Couldn't open qlaunch nca");
        return 1;
    }
}

return 0;

LibHac.Crypto.AesKey ParseKey(string hex) 
{
    var key = new byte[16];

    for (int i = 0; i < 16; i++)
    {
        var b = hex.Substring(i * 2, 2);
        key[i] = byte.Parse(b, NumberStyles.HexNumber);
    }

    var k = new LibHac.Crypto.AesKey();
    key.CopyTo(k.Data);

    return k;
}

int Search(byte[] src, byte[] pattern)
{
    int maxFirstCharSlot = src.Length - pattern.Length + 1;
    for (int i = 0; i < maxFirstCharSlot; i++)
    {
        if (src[i] != pattern[0]) // compare only first byte
            continue;

        // found a match on first byte, now try to match rest of the pattern
        for (int j = pattern.Length - 1; j >= 1; j--)
        {
            if (src[i + j] != pattern[j]) break;
            if (j == 1) return i;
        }
    }
    return -1;
}