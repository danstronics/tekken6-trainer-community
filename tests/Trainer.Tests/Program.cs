using CommunityTrainer;
using System.Runtime.InteropServices;

internal sealed class TestMemory : IGuestMemory
{
    private readonly Dictionary<uint, byte> data = new();
    public readonly List<(uint Address, byte[] Data)> Writes = new();
    public uint? FailRead;
    public uint? FailWrite;
    public void Map(uint start, uint length) { for (uint i = 0; i < length; i++) data[start + i] = 0; }
    public void Put(uint address, uint value, int width = 4)
    { foreach (var (b, index) in Guest.Encode(value, width).Select((b, i) => (b, i))) data[address + (uint)index] = b; }
    public uint At(uint address, int width = 4) => this.ReadNumber(address, width, out uint value) ? value : throw new Exception("Unmapped test read");
    public bool Read(uint address, int length, out byte[] bytes)
    {
        bytes = new byte[length];
        if (address == FailRead) return false;
        for (uint i = 0; i < length; i++) if (!data.TryGetValue(address + i, out bytes[i])) return false;
        return true;
    }
    public bool Write(uint address, byte[] bytes)
    {
        if (address == FailWrite || !Enumerable.Range(0, bytes.Length).All(i => data.ContainsKey(address + (uint)i))) return false;
        for (uint i = 0; i < bytes.Length; i++) data[address + i] = bytes[i];
        Writes.Add((address, (byte[])bytes.Clone()));
        return true;
    }
}

internal static class Program
{
    private const uint State = 0x1100000;
    private const uint Mode = 0x1200517;
    private static int checks;
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception("FAIL: " + label);
        checks++;
        Console.WriteLine("PASS: " + label);
    }
    private static (TestMemory M, TrainerEngine E, Settings S) New()
    {
        var memory = new TestMemory();
        memory.Map(State, 0x400);
        memory.Map(0xB2C000, 0x2000);
        memory.Map(0xA76000, 0x1000);
        memory.Map(0x216A0000, 0x10000);
        memory.Map(0xB7F000, 0x1000);
        memory.Put(0xB88480, 0x1000000);
        memory.Put(0x1000F14, State);
        memory.Put(0xB07878, 0x1000000);
        memory.Put(0x10003BC, 0x1200000);
        memory.Put(Mode, 1, 1);
        memory.Put(State + 0x144, 120);
        memory.Put(State + 0x143, 6, 1);
        memory.Put(State + 0x174, 39);
        memory.Put(State + 0x178, 43);
        memory.Put(0xB2C36F, 1, 1);
        memory.Put(0xB2CFAF, 1, 1);
        memory.Put(0xB2C35F, 1, 1);
        memory.Put(0xB2CF9F, 1, 1);
        memory.Put(0xB2CD6F, 1, 1);
        memory.Put(0xB2C1F6, 4096, 2);
        memory.Put(0xB2CE36, 4096, 2);
        memory.Put(0xB2C144, 180, 2);
        memory.Put(0xB2CD84, 180, 2);
        memory.Put(0xA667CC, 0x1300000);
        memory.Put(0x1300A54, 0x1400000);
        memory.Map(0x1400000, 0x1000);
        memory.Map(0x98E000, 0x1000);
        return (memory, new(memory), new());
    }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Check(Guest.Encode(0x12345678, 4).SequenceEqual(new byte[] { 0x12, 0x34, 0x56, 0x78 }), "32-bit writes use big endian");
            Check(Guest.Encode(0x1234, 2).SequenceEqual(new byte[] { 0x12, 0x34 }), "16-bit writes use big endian");
            Check(Catalog.StageIds.Length == 89 && !Catalog.StageIds.Contains((byte)15), "stage picker contains only the 89 recovered IDs");
            var (m, e, s) = New();
            Check(m.Resolve(0xB88480, 0xF14, 0) == State, "32-bit guest pointer chain");
            e.Tick(s);
            Check(m.Writes.Count == 0, "default UI settings perform no writes");
            s.Player1 = Boss.GoldAzazel;
            Check(e.Tick(s).Contains("Gold active"), "Gold reports experimental activation");
            Check(m.At(State + 0x120) == 1 && m.At(State + 0x174) == 68, "Gold flag and P1 ID are separate 32-bit words");
            Check(m.At(State + 0x178) == 43, "Gold P1 preserves the P2 character");
            s.Player2 = Boss.GoldAzazel;
            e.Tick(s);
            Check(m.At(State + 0x178) == 68, "P2 Gold uses Azazel ID");
            s.Player1 = Boss.Unchanged;
            e.Tick(s);
            Check(m.At(State + 0x120) == 1, "P2 keeps the shared Gold flag active");
            s.Player2 = Boss.Unchanged;
            e.Tick(s);
            Check(m.At(State + 0x120) == 0, "last Gold selection restores original flag");
            s.Player1 = Boss.Nancy;
            e.Tick(s);
            Check(m.At(State + 0x174) == 82, "Nancy ID is 82");
            s.Player1 = Boss.Azazel;
            e.Tick(s);
            Check(m.At(State + 0x174) == 68, "normal Azazel ID is 68");

            (m, e, s) = New();
            m.Put(State + 0x120, 1);
            s.Player1 = Boss.GoldAzazel;
            e.Tick(s);
            e.Restore();
            Check(m.At(State + 0x120) == 1, "existing Gold value survives restoration");
            (m, e, s) = New();
            m.Put(State + 0x120, 0x12345678);
            s.Player1 = Boss.GoldAzazel;
            Check(e.Tick(s).Contains("rejected") && m.At(State + 0x120) == 0x12345678, "non-boolean Gold field is rejected");
            (m, e, s) = New();
            s.Player1 = Boss.GoldAzazel;
            m.FailRead = State + 0x120;
            Check(e.Tick(s).Contains("rejected") && !m.Writes.Any(w => w.Address == State + 0x120), "failed Gold read never becomes a write");
            (m, e, s) = New();
            s.Player1 = Boss.GoldAzazel;
            m.FailWrite = State + 0x120;
            Check(e.Tick(s).Contains("failed"), "failed native write is visible in status");

            (m, e, s) = New();
            s.Player1 = Boss.GoldAzazel;
            e.Tick(s);
            m.Put(State + 0x120, 2);
            s.Player1 = Boss.Unchanged;
            e.Tick(s);
            Check(m.At(State + 0x120) == 2, "restoration preserves a later game-side change");
            (m, e, s) = New();
            s.Player1 = Boss.GoldAzazel;
            e.Tick(s);
            m.Map(0x1500000, 0x400);
            m.Put(0x1000F14, 0x1500000);
            m.Put(0x1500144, 100);
            m.Writes.Clear();
            s.Player1 = Boss.Unchanged;
            e.Tick(s);
            Check(!m.Writes.Any(w => w.Address == State + 0x120), "object replacement never restores into stale memory");

            (m, e, s) = New();
            m.Put(0xB88480, 0);
            s.Player1 = Boss.GoldAzazel;
            Check(e.Tick(s).Contains("Waiting") && m.Writes.Count == 0, "null root blocks every write");
            m.Put(0xB88480, 0x1000000);
            m.Put(0x1000F14, 0);
            Check(m.Resolve(0xB88480, 0xF14, 0x174) == 0, "null intermediate pointer is rejected");
            m.Put(0xB88480, 0x7FFFFFFF);
            Check(m.Resolve(0xB88480, 1) == 0, "pointer arithmetic cannot cross guest range");
            (m, e, s) = New();
            m.Put(Mode, 8, 1);
            s.Player1 = Boss.Azazel;
            Check(e.Tick(s).Contains("supported") && m.Writes.Count == 0, "unrecognized mode blocks patches");

            (m, e, s) = New();
            s.Enabled.UnionWith(["defense1", "control2", "speed1", "invisible1"]);
            s.Speed1 = 5000;
            e.Tick(s);
            Check(m.At(0xB2C36F, 1) == 0 && m.At(0xB2CD6F, 1) == 0, "blocking and controller overrides apply");
            Check(m.At(0xB2C1F6, 2) == 5000 && m.At(0xB2C35F, 1) == 0, "speed and visibility overrides apply");
            s.Enabled.Clear();
            e.Tick(s);
            Check(m.At(0xB2C36F, 1) == 1 && m.At(0xB2CD6F, 1) == 1, "blocking and controller state restore");
            Check(m.At(0xB2C1F6, 2) == 4096 && m.At(0xB2C35F, 1) == 1, "speed and model visibility restore");
            s.Enabled.Add("health1");
            e.Tick(s);
            Check(m.At(0xB2C140, 2) == 180, "health uses current maximum");
            s.Enabled.Clear();
            s.Enabled.Add("hit1");
            m.Put(0xB7FAA3, 1, 1);
            e.Tick(s);
            Check(m.At(0xB2CD80, 2) == 0, "one-hit defeat requires a hit indicator");
            s.Enabled.Add("money");
            e.Tick(s);
            Check(m.At(0x1400154) == 765999999, "fight money resolves its independent pointer chain");
            s.Stage = 42;
            e.Tick(s);
            Check(m.At(State + 0x173, 1) == 42, "stage byte does not overlap character ID");

            (m, e, s) = New();
            m.Put(Mode, 3, 1);
            s.Enabled.UnionWith(["teamhealth", "rage", "damage", "ice", "electric"]);
            s.Damage = 333;
            s.Player1 = Boss.Nancy;
            s.Player2 = Boss.GoldAzazel;
            e.Tick(s);
            Check(m.At(0x98E9F2, 2) == 82 && m.At(State + 0x178) == 43, "campaign replaces only its supported player slot");
            Check(m.At(State + 0x120) == 0, "unsupported P2 campaign Gold does not set a flag");
            Check(m.At(0xB2C140, 2) == 600 && m.At(0xB2CD80, 2) == 600, "campaign health applies to both players");
            Check(m.At(0xA76C0A, 2) == 333 && m.At(0xA76C3E, 2) == 200, "campaign damage values are independently written");
            Check(m.At(0xA76C1E, 2) == 1000 && m.At(0xA76C1A, 2) == 0, "electric and freeze effects cannot conflict");
            Check(e.Restore() && m.At(0xA76C0A, 2) == 0, "campaign stat overrides restore on detach");

            // Every catalog option gets a complete pass with an appropriate mode.
            foreach (var feature in Catalog.Features)
            {
                (m, e, s) = New();
                m.Put(Mode, feature.Context == Context.Campaign ? 3u : 1u, 1);
                m.Put(0xB7FAA3, 1, 1);
                m.Put(0xB7FB1F, 1, 1);
                s.Enabled.Add(feature.Id);
                Check(!e.Tick(s).Contains("failed") && m.Writes.Count > 0, "catalog option: " + feature.Id);
            }

            using (var window = new MainWindow())
            {
                Check(window.FeatureControlCount == Catalog.Features.Length, "UI exposes every catalog option");
                Check(window.Text.Contains("3.0.0"), "independent UI constructs with correct version");
                if (args.Length > 0)
                {
                    window.ShowInTaskbar = false;
                    window.Opacity = 0;
                    window.Show();
                    Application.DoEvents();
                    using var bitmap = new System.Drawing.Bitmap(window.Width, window.Height);
                    window.DrawToBitmap(bitmap, new(0, 0, window.Width, window.Height));
                    bitmap.Save(args[0]);
                    window.Close();
                }
            }
            NativeMemoryTest();
            Console.WriteLine($"RESULT: {checks} checks passed");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void NativeMemoryTest()
    {
        nint allocation = Marshal.AllocHGlobal(16);
        try
        {
            uint guest = 0x10000;
            ulong baseAddress = (ulong)allocation.ToInt64() - guest;
            Marshal.WriteInt32(allocation, 0);
            using var process = new ProcessMemory(Environment.ProcessId, baseAddress);
            Check(process.IsAlive, "native process adapter attaches the requested PID");
            Check(process.Write(guest, Guest.Encode(0x12345678, 4)) && process.ReadNumber(guest, 4, out uint value) && value == 0x12345678, "native Windows read/write adapter transfers exact big endian bytes");
            Check(!process.Read(0, 4, out _) && !process.Write(0, new byte[4]), "native adapter rejects null guest addresses");
        }
        finally { Marshal.FreeHGlobal(allocation); }
    }
}
