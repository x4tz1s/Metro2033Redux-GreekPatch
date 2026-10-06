// Metro 2033 Redux - Greek translation installer (Windows builds: Steam, Epic Games Store, GOG).
// The Windows metro.exe cannot load files from a patch.vfx, so the translation goes into a new pack (content00_greek.vfs)
// that is added to the game's own index, content.vfx. This tool edits the index that is installed (it differs per store),
// takes the user's own config.bin / stable_us.lng out of the game packs and only swaps the font tables / merges the text by key.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

static class Program
{
    const string Version = "1.0.0";
    const string VfsName = "content00_greek.vfs";       // must scan as "content%d" (metro.exe reads a pack number from the name)
    const string BackupName = "content.vfx.greek_backup";
    static readonly string[] Fonts = {                   // same order as tools/build.py
        "font_font_regular_us.512", "font_metro_2034_font_2_us.512", "font_metro_for_menu_us.512",
        "font_metro_for_menu_main_strings_us.512", "font_metro_for_map_us.1024", "font_metro_for_map_us.512" };

    class Entry { public bool Dir; public int Pos, Flags, N, First, Vfs; public uint Off, USize, CSize; public string Name; }
    class Vfs { public string Name; public List<string> Levels = new(); public uint Size; }
    class Index
    {
        public byte[] Raw; public byte[] Guid; public List<Vfs> VfsList = new(); public List<Entry> Ents = new();
        public int EntsPos, TailPos, NDup;
    }
    class Fail : Exception { public Fail(string m) : base(m) { } }

    static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        bool uninstall = args.Any(a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase));
        bool noPause = args.Any(a => a.Equals("--no-pause", StringComparison.OrdinalIgnoreCase));
        string dirArg = args.FirstOrDefault(a => !a.StartsWith("--"));
        Console.WriteLine($"Metro 2033 Redux - Ελληνική μετάφραση / Greek translation - installer v{Version}");
        Console.WriteLine();
        int rc = 0;
        try
        {
            string game = FindGame(dirArg, !noPause);
            Console.WriteLine("Φάκελος παιχνιδιού / Game folder: " + game);
            if (uninstall) Uninstall(game); else Install(game);
        }
        catch (Fail e) { Console.WriteLine(); Console.WriteLine("ΣΦΑΛΜΑ / ERROR: " + e.Message); rc = 1; }
        catch (Exception e) { Console.WriteLine(); Console.WriteLine("ΑΠΡΟΣΔΟΚΗΤΟ ΣΦΑΛΜΑ / UNEXPECTED ERROR: " + e); rc = 2; }
        if (!noPause) { Console.WriteLine(); Console.WriteLine("Πάτησε Enter για έξοδο / Press Enter to exit."); Console.ReadLine(); }
        return rc;
    }

    static bool IsGame(string d) => d != null && File.Exists(Path.Combine(d, "content.vfx")) && File.Exists(Path.Combine(d, "content00.vfs0"));

    static string FindGame(string arg, bool interactive)
    {
        var cands = new List<string>();
        if (arg != null) cands.Add(arg.Trim('"'));
        cands.Add(Directory.GetCurrentDirectory()); cands.Add(AppContext.BaseDirectory);
        cands.Add(Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\', '/')));
        foreach (var c in cands) if (IsGame(c)) return Path.GetFullPath(c);
        if (arg != null) throw new Fail($"Δεν βρέθηκε το content.vfx στο «{arg}». / content.vfx not found in that folder.");
        while (interactive)
        {
            Console.WriteLine("Δεν βρέθηκε το παιχνίδι δίπλα στο πρόγραμμα. Γράψε (ή σύρε εδώ) τον φάκελο του Metro 2033 Redux και πάτησε Enter:");
            Console.WriteLine("Game not found next to this program. Type (or drag here) the Metro 2033 Redux folder and press Enter:");
            string s = Console.ReadLine(); if (s == null) break;
            s = s.Trim().Trim('"'); if (s.Length == 0) break;
            if (IsGame(s)) return Path.GetFullPath(s);
            Console.WriteLine("  -> εκεί δεν υπάρχουν content.vfx + content00.vfs0 / no content.vfx + content00.vfs0 there."); Console.WriteLine();
        }
        throw new Fail("Δεν βρέθηκε ο φάκελος του παιχνιδιού (content.vfx + content00.vfs0). / Game folder not found.");
    }

    // ---------------------------------------------------------------- content.vfx
    static uint U32(byte[] b, int p) => BitConverter.ToUInt32(b, p);
    static int U16(byte[] b, int p) => BitConverter.ToUInt16(b, p);
    static string CStr(byte[] b, ref int p) { int e = Array.IndexOf(b, (byte)0, p); string s = Encoding.Latin1.GetString(b, p, e - p); p = e + 1; return s; }

    static Index ReadIndex(byte[] b)
    {
        var x = new Index { Raw = b };
        if (b.Length < 40 || U32(b, 0) != 1) throw new Fail("Άγνωστη μορφή content.vfx. / Unknown content.vfx format.");
        x.Guid = b.Skip(8).Take(16).ToArray();
        int nvfs = (int)U32(b, 24), nent = (int)U32(b, 28); x.NDup = (int)U32(b, 32); int p = 36;
        for (int i = 0; i < nvfs; i++)
        {
            var v = new Vfs { Name = CStr(b, ref p) }; int nl = (int)U32(b, p); p += 4;
            for (int k = 0; k < nl; k++) v.Levels.Add(CStr(b, ref p));
            v.Size = U32(b, p); p += 4; x.VfsList.Add(v);
        }
        x.EntsPos = p;
        for (int i = 0; i < nent; i++)
        {
            var e = new Entry { Pos = p, Flags = U16(b, p) }; int np;
            if ((e.Flags & 8) != 0) { e.Dir = true; e.N = U16(b, p + 2); e.First = (int)U32(b, p + 4); np = p + 8; }
            else { e.Vfs = U16(b, p + 2); e.Off = U32(b, p + 4); e.USize = U32(b, p + 8); e.CSize = U32(b, p + 12); np = p + 16; }
            int len = b[np], key = b[np + 1]; var nm = new char[len - 1];
            for (int k = 0; k < len - 1; k++) nm[k] = (char)(b[np + 2 + k] ^ key);
            e.Name = new string(nm); p = np + 2 + len; x.Ents.Add(e);
        }
        x.TailPos = p;
        if (p + x.NDup * 20 > b.Length) throw new Fail("Κατεστραμμένο content.vfx. / Corrupt content.vfx.");
        return x;
    }

    static Entry Find(Index x, string path)
    {
        var d = x.Ents[0];
        foreach (var part in path.Split('\\'))
        {
            Entry nx = null;
            for (int j = d.First; j < d.First + d.N; j++) if (string.Equals(x.Ents[j].Name, part, StringComparison.OrdinalIgnoreCase)) { nx = x.Ents[j]; break; }
            if (nx == null) throw new Fail($"Δεν υπάρχει στο παιχνίδι: {path} / not found in the game index.");
            d = nx;
        }
        return d;
    }

    // ---------------------------------------------------------------- reading a file out of the game packs
    static byte[] Extract(string game, Index x, Entry e)
    {
        string f = Path.Combine(game, x.VfsList[e.Vfs].Name);
        if (!File.Exists(f)) throw new Fail($"Λείπει το αρχείο {x.VfsList[e.Vfs].Name}. / Pack file missing.");
        var raw = new byte[e.CSize];
        using (var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read)) { fs.Seek(e.Off, SeekOrigin.Begin); fs.ReadExactly(raw); }
        if (e.CSize == e.USize) return raw;
        // blocks [u32 csize incl. this 8-byte header][u32 usize][LZ4 block]; matches may reach back into earlier blocks
        var o = new byte[e.USize]; int op = 0, p = 0;
        while (p < raw.Length)
        {
            int cs = (int)U32(raw, p), us = (int)U32(raw, p + 4), end = p + cs, ip = p + 8, oend = op + us;
            while (ip < end)
            {
                int tok = raw[ip++], lit = tok >> 4;
                if (lit == 15) { int s; do { s = raw[ip++]; lit += s; } while (s == 255); }
                Buffer.BlockCopy(raw, ip, o, op, lit); ip += lit; op += lit;
                if (ip >= end) break;
                int off = raw[ip] | raw[ip + 1] << 8; ip += 2; int ml = (tok & 15) + 4;
                if ((tok & 15) == 15) { int s; do { s = raw[ip++]; ml += s; } while (s == 255); }
                if (off == 0 || off > op) throw new Fail("Σφάλμα αποσυμπίεσης. / Decompression error.");
                for (int k = 0; k < ml; k++, op++) o[op] = o[op - off];
            }
            if (op != oend) throw new Fail("Σφάλμα αποσυμπίεσης (μέγεθος). / Decompression size mismatch.");
            p = end;
        }
        if (op != o.Length) throw new Fail("Σφάλμα αποσυμπίεσης (σύνολο). / Decompression total mismatch.");
        return o;
    }

    static byte[] Res(string name)
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new Fail("missing payload " + name);
        using var m = new MemoryStream(); s.CopyTo(m); return m.ToArray();
    }

    // ---------------------------------------------------------------- config.bin: swap the fonts_us section
    static uint Crc32(byte[] d)
    {
        uint c = 0xffffffff;
        foreach (byte b in d) { c ^= b; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320u : c >> 1; }
        return ~c;
    }

    static byte[] PatchConfig(byte[] cfg)
    {
        uint want = Crc32(Encoding.ASCII.GetBytes("content\\scripts\\fonts_us.bin")); int p = 0;
        while (p + 8 <= cfg.Length)
        {
            uint c = U32(cfg, p); int size = (int)U32(cfg, p + 4);
            if (size < 0 || p + 8 + size > cfg.Length) break;
            if (c == want)
            {
                byte[] sec = Res("fonts_us.sec"); int origLen = int.Parse(Encoding.ASCII.GetString(Res("fonts_us.orig_len")));
                if (size != origLen && size != sec.Length)
                    Console.WriteLine($"  ! οι πίνακες γραμματοσειρών του παιχνιδιού σου έχουν άλλο μέγεθος ({size} αντί {origLen}) - συνεχίζω. / font tables differ in size - continuing.");
                using var m = new MemoryStream();
                m.Write(cfg, 0, p); m.Write(BitConverter.GetBytes(want)); m.Write(BitConverter.GetBytes(sec.Length)); m.Write(sec);
                m.Write(cfg, p + 8 + size, cfg.Length - (p + 8 + size)); return m.ToArray();
            }
            p += 8 + size;
        }
        throw new Fail("Δεν βρέθηκαν οι πίνακες γραμματοσειρών (fonts_us) στο config.bin. / fonts_us section not found in config.bin.");
    }

    // ---------------------------------------------------------------- .lng: merge the Greek text by key into the user's file
    static (string table, List<(string k, byte[] v)> ents, Dictionary<uint, byte[]> ch) ReadLng(byte[] b)
    {
        var ch = new Dictionary<uint, byte[]>(); int p = 0;
        while (p + 8 <= b.Length) { uint id = U32(b, p); int size = (int)U32(b, p + 4); ch[id] = b.Skip(p + 8).Take(size).ToArray(); p += 8 + size; }
        if (!ch.ContainsKey(1) || !ch.ContainsKey(2)) throw new Fail("Άγνωστη μορφή stable_us.lng. / Unknown .lng format.");
        string table = Encoding.Unicode.GetString(ch[1]); var d = ch[2]; var ents = new List<(string, byte[])>(); p = 0;
        while (p < d.Length)
        {
            string k = CStr(d, ref p); int e = Array.IndexOf(d, (byte)0, p);
            ents.Add((k, d.Skip(p).Take(e - p).ToArray())); p = e + 1;
        }
        return (table, ents, ch);
    }

    static byte[] MergeLng(byte[] user, out int greek, out int kept)
    {
        var mine = ReadLng(Res("stable_us.lng")); var usr = ReadLng(user);
        var byKey = new Dictionary<string, byte[]>(); var seen = new Dictionary<string, int>();
        foreach (var (k, v) in mine.ents) { seen[k] = seen.GetValueOrDefault(k) + 1; byKey[k + "\u0001" + seen[k]] = v; }
        var idx = new Dictionary<char, byte>(); for (int i = 0; i < mine.table.Length; i++) idx[mine.table[i]] = (byte)i;
        const string cyr = "СсАаЕеОоРрХх", lat = "CcAaEeOoPpXx";      // look-alike Cyrillic letters in a few English strings
        byte q = idx['?']; seen.Clear(); greek = 0; kept = 0;
        using var body = new MemoryStream();
        foreach (var (k, v) in usr.ents)
        {
            seen[k] = seen.GetValueOrDefault(k) + 1; byte[] o;
            if (byKey.TryGetValue(k + "\u0001" + seen[k], out o)) greek++;
            else
            {
                kept++; o = new byte[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    char c = v[i] < usr.table.Length ? usr.table[v[i]] : '?'; int ci = cyr.IndexOf(c); if (ci >= 0) c = lat[ci];
                    o[i] = idx.TryGetValue(c, out byte bi) && bi > 1 ? bi : q;
                }
            }
            body.Write(Encoding.Latin1.GetBytes(k)); body.WriteByte(0); body.Write(o); body.WriteByte(0);
        }
        using var m = new MemoryStream();
        void Chunk(uint id, byte[] d) { m.Write(BitConverter.GetBytes(id)); m.Write(BitConverter.GetBytes(d.Length)); m.Write(d); }
        Chunk(0, mine.ch[0]); Chunk(1, mine.ch[1]); Chunk(2, body.ToArray());
        return m.ToArray();
    }

    // ---------------------------------------------------------------- install / uninstall
    static void Replace(string path, byte[] data)
    {
        string tmp = path + ".tmp"; File.WriteAllBytes(tmp, data);
        if (File.Exists(path)) { File.SetAttributes(path, FileAttributes.Normal); File.Delete(path); }
        File.Move(tmp, path);
    }

    static bool IsPatched(Index x) => x.VfsList.Any(v => v.Name.Equals(VfsName, StringComparison.OrdinalIgnoreCase));

    static void Install(string game)
    {
        if (File.Exists(Path.Combine(game, "metro")) && !File.Exists(Path.Combine(game, "metro.exe")))
            throw new Fail("Αυτή είναι η native Linux έκδοση: εκεί βάλε τα patch.vfx1 + patch.vfs1 από το Nexus (φάκελος Linux), όχι αυτό το πρόγραμμα. / "
                         + "This is the native Linux build: use the Linux files from the Nexus page instead of this tool.");
        string vfxPath = Path.Combine(game, "content.vfx"), bak = Path.Combine(game, BackupName), vfsPath = Path.Combine(game, VfsName);
        byte[] cur = File.ReadAllBytes(vfxPath); var x = ReadIndex(cur);
        if (IsPatched(x))
        {
            if (!File.Exists(bak)) throw new Fail("Το content.vfx είναι ήδη τροποποιημένο και δεν βρέθηκε αντίγραφο του αρχικού. Κάνε «Επαλήθευση αρχείων» από το launcher "
                + "(Steam/Epic/GOG) και ξανατρέξε το πρόγραμμα. / content.vfx is already modded and there is no backup of the original: verify the game files in your launcher, then run this again.");
            Console.WriteLine("Η μετάφραση είναι ήδη εγκατεστημένη - ξαναφτιάχνεται από το αρχικό content.vfx. / Already installed - rebuilding from the original index.");
            cur = File.ReadAllBytes(bak); x = ReadIndex(cur);
            if (IsPatched(x)) throw new Fail("Το αντίγραφο ασφαλείας δεν είναι το αρχικό content.vfx. Κάνε «Επαλήθευση αρχείων» και ξανατρέξε. / The backup is not a pristine content.vfx: verify the game files and run again.");
        }
        else { File.WriteAllBytes(bak, cur); Console.WriteLine("Αντίγραφο του αρχικού / Backup of the original: " + BackupName); }
        if (x.VfsList.Count >= 0x7e) throw new Fail("Το content.vfx έχει πάρα πολλά πακέτα. / Too many packs in content.vfx.");

        var files = new List<(string path, byte[] data)>();
        Console.WriteLine("Διαβάζω το config.bin του παιχνιδιού... / Reading the game's config.bin...");
        files.Add(("content\\config.bin", PatchConfig(Extract(game, x, Find(x, "content\\config.bin")))));
        Console.WriteLine("Διαβάζω τα κείμενα του παιχνιδιού... / Reading the game's text file...");
        files.Add(("content\\localization\\stable_us.lng", MergeLng(Extract(game, x, Find(x, "content\\localization\\stable_us.lng")), out int greek, out int kept)));
        Console.WriteLine($"  {greek} εγγραφές από τη μετάφραση, {kept} άγνωστες στη μετάφραση έμειναν ως έχουν. / {greek} entries from the translation, {kept} unknown ones kept.");
        foreach (var f in Fonts) files.Add(("content\\textures\\font\\" + f, Res(f)));

        var dup = new HashSet<int>(); for (int k = 0; k < x.NDup; k++) dup.Add((int)U32(cur, x.TailPos + k * 20 + 16));
        byte[] nb = (byte[])cur.Clone(); using var data = new MemoryStream();
        foreach (var (path, d) in files)
        {
            var e = Find(x, path);
            if (e.Dir || dup.Contains(x.Ents.IndexOf(e))) throw new Fail($"Απρόσμενη εγγραφή για {path}. / Unexpected index record.");
            BitConverter.GetBytes((ushort)x.VfsList.Count).CopyTo(nb, e.Pos + 2);
            BitConverter.GetBytes((uint)(8 + data.Length)).CopyTo(nb, e.Pos + 4);
            BitConverter.GetBytes((uint)d.Length).CopyTo(nb, e.Pos + 8); BitConverter.GetBytes((uint)d.Length).CopyTo(nb, e.Pos + 12);
            data.Write(d);
        }
        using var vfs = new MemoryStream();
        vfs.Write(BitConverter.GetBytes(0u)); vfs.Write(BitConverter.GetBytes((uint)data.Length)); data.WriteTo(vfs);
        vfs.Write(BitConverter.GetBytes(3u)); vfs.Write(BitConverter.GetBytes(16u)); vfs.Write(x.Guid);
        using var rec = new MemoryStream();                        // new pack record: same level list as the first (always loaded) pack
        rec.Write(Encoding.ASCII.GetBytes(VfsName)); rec.WriteByte(0); rec.Write(BitConverter.GetBytes((uint)x.VfsList[0].Levels.Count));
        foreach (var l in x.VfsList[0].Levels) { rec.Write(Encoding.Latin1.GetBytes(l)); rec.WriteByte(0); }
        rec.Write(BitConverter.GetBytes((uint)vfs.Length));
        BitConverter.GetBytes((uint)(x.VfsList.Count + 1)).CopyTo(nb, 24);
        byte[] outIdx = nb.Take(x.EntsPos).Concat(rec.ToArray()).Concat(nb.Skip(x.EntsPos)).ToArray();

        var chk = ReadIndex(outIdx);                                // re-read what will be written
        if (chk.Ents.Count != x.Ents.Count || !IsPatched(chk) || chk.VfsList.Last().Size != vfs.Length) throw new Fail("Εσωτερικός έλεγχος απέτυχε. / Internal check failed.");
        Replace(vfsPath, vfs.ToArray()); Replace(vfxPath, outIdx);
        Console.WriteLine();
        Console.WriteLine("ΕΤΟΙΜΟ. Γλώσσα κειμένου στο παιχνίδι: ENGLISH. Άνοιξε και τους υπότιτλους.");
        Console.WriteLine("DONE. Keep the in-game text language on ENGLISH and turn subtitles on.");
        Console.WriteLine("Αν το launcher κάνει «επαλήθευση αρχείων» ή ενημέρωση, ξανατρέξε το πρόγραμμα. / Run this again after a file verify or update.");
    }

    static void Uninstall(string game)
    {
        string vfxPath = Path.Combine(game, "content.vfx"), bak = Path.Combine(game, BackupName), vfsPath = Path.Combine(game, VfsName);
        bool patched = IsPatched(ReadIndex(File.ReadAllBytes(vfxPath)));
        if (patched)
        {
            if (!File.Exists(bak)) throw new Fail("Δεν βρέθηκε το αντίγραφο του αρχικού content.vfx. Κάνε «Επαλήθευση αρχείων» από το launcher και μετά σβήσε το "
                + VfsName + ". / Backup not found: verify the game files in your launcher, then delete " + VfsName + ".");
            Replace(vfxPath, File.ReadAllBytes(bak)); Console.WriteLine("Επαναφέρθηκε το αρχικό content.vfx. / Original content.vfx restored.");
        }
        else Console.WriteLine("Το content.vfx είναι ήδη το αρχικό. / content.vfx is already the original.");
        if (File.Exists(bak)) File.Delete(bak);
        if (File.Exists(vfsPath)) { File.Delete(vfsPath); Console.WriteLine("Σβήστηκε το " + VfsName + ". / Deleted."); }
        Console.WriteLine(); Console.WriteLine("Η μετάφραση αφαιρέθηκε. / Translation removed.");
    }
}
