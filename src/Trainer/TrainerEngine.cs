namespace CommunityTrainer;

internal enum Context { Battle, Campaign, Any }
internal enum Boss { Unchanged, Azazel, Nancy, GoldAzazel }
internal sealed record Feature(string Id, string Label, Context Context);
internal sealed record Patch(uint Address, int Width, uint Value, bool Restore = false);
internal sealed class Settings
{
    public HashSet<string> Enabled { get; } = new();
    public Boss Player1 { get; set; }
    public Boss Player2 { get; set; }
    public ushort Speed1 { get; set; } = 4096;
    public ushort Speed2 { get; set; } = 4096;
    public ushort Damage { get; set; } = 200;
    public byte? Stage { get; set; }
}

// Addresses, integer widths and values are interoperability observations.
// See docs/PROVENANCE.md for their origin and the unverified Gold relocation.
internal static class Catalog
{
    public static readonly Feature[] Features =
    [
        new("health1", "Keep player 1 health full", Context.Battle),
        new("health2", "Keep player 2 health full", Context.Battle),
        new("invincible1", "Player 1 invulnerability", Context.Battle),
        new("invincible2", "Player 2 invulnerability", Context.Battle),
        new("defense1", "Disable player 1 blocking", Context.Battle),
        new("defense2", "Disable player 2 blocking", Context.Battle),
        new("clock", "Hold round clock", Context.Battle),
        new("best", "Set elapsed time to zero", Context.Battle),
        new("roulette", "Enable roulette (Lars)", Context.Battle),
        new("hit1", "Player 1 wins with one hit", Context.Battle),
        new("hit2", "Player 2 wins with one hit", Context.Battle),
        new("teamhealth", "Keep both players at 600 health", Context.Campaign),
        new("campaignclock", "Hold campaign clock", Context.Campaign),
        new("campaignbest", "Set campaign elapsed time to zero", Context.Campaign),
        new("rage", "Keep rage active", Context.Campaign),
        new("element", "Elemental resistance", Context.Campaign),
        new("drops", "Maximum item drop rate", Context.Campaign),
        new("electric", "Electric attack effect", Context.Campaign),
        new("ice", "Freezing attack effect", Context.Campaign),
        new("damage", "Custom bonus damage", Context.Campaign),
        new("money", "Set fight money to 765999999", Context.Any),
        new("control2", "Player 1 controls player 2", Context.Any),
        new("control1", "Player 2 controls player 1", Context.Any),
        new("invisible1", "Hide player 1 model", Context.Any),
        new("invisible2", "Hide player 2 model", Context.Any),
        new("speed1", "Custom player 1 movement speed", Context.Any),
        new("speed2", "Custom player 2 movement speed", Context.Any)
    ];

    public static readonly byte[] StageIds = Enumerable.Range(0, 137)
        .Where(i => i <= 20 && i != 15 || i is >= 40 and <= 82 or >= 100 and <= 106 or 109 or 111 or >= 112 and <= 117 or 119 or 120 or 121 or 122 or 127 or 129 or >= 132 and <= 136)
        .Select(i => (byte)i).ToArray();

    public static Patch[] Rules(string feature, Settings settings, uint state) => feature switch
    {
        "invincible1" => [new(0xB2C1EC, 4, 0), new(0xB2C1F0, 4, 0), new(0xB2CED0, 4, 0)],
        "invincible2" => [new(0xB2CE2C, 4, 0), new(0xB2CE30, 4, 0), new(0xB2C290, 4, 0)],
        "defense1" => [new(0xB2C36F, 1, 0, true)],
        "defense2" => [new(0xB2CFAF, 1, 0, true)],
        "clock" => [new(state + 0x15A, 2, 6000)],
        "best" or "campaignbest" => [new(state + 0x144, 4, 0)],
        "roulette" => [new(0x216A32BA, 2, 9969), new(0x216A3366, 2, 0)],
        "teamhealth" => [new(0xB2C140, 2, 600), new(0xB2C144, 2, 600), new(0xB2CD80, 2, 600), new(0xB2CD84, 2, 600)],
        "campaignclock" => [new(state + 0x15A, 2, 59960)],
        "rage" => [new(0xA76C13, 1, 100, true), new(0xA76C47, 1, 100, true)],
        "element" => [new(0xA76C2A, 2, 1000, true), new(0xA76C5E, 2, 1000, true)],
        "drops" => [new(0xA76C22, 2, 1000, true), new(0xA76C56, 2, 1000, true)],
        "electric" => [new(0xA76C1E, 2, 1000, true), new(0xA76C4E, 2, 1000, true)],
        "ice" => [new(0xA76C1A, 2, 1000, true), new(0xA76C52, 2, 1000, true)],
        "damage" => [new(0xA76C0A, 2, settings.Damage, true), new(0xA76C3E, 2, 200, true)],
        "control2" => [new(0xB2CD6F, 1, 0, true)],
        "control1" => [new(0xB2C12F, 1, 1, true)],
        "invisible1" => [new(0xB2C35F, 1, 0, true)],
        "invisible2" => [new(0xB2CF9F, 1, 0, true)],
        "speed1" => [new(0xB2C1F6, 2, settings.Speed1, true)],
        "speed2" => [new(0xB2CE36, 2, settings.Speed2, true)],
        _ => []
    };
}

internal sealed class TrainerEngine(IGuestMemory memory)
{
    private sealed record Saved(byte[] Before, byte[] Written);
    private readonly Dictionary<uint, Saved> saved = new();
    private readonly HashSet<uint> active = new();
    private uint lastState;
    private int failures;

    private bool Write(Patch patch)
    {
        if (patch.Address == 0) return false;
        byte[] next = Guest.Encode(patch.Value, patch.Width);
        if (patch.Restore)
        {
            if (!saved.ContainsKey(patch.Address))
            {
                if (!memory.Read(patch.Address, patch.Width, out var before)) { failures++; return false; }
                saved[patch.Address] = new(before, next);
            }
            active.Add(patch.Address);
            // Remember the last value to avoid restoring over a game's later write.
            saved[patch.Address] = saved[patch.Address] with { Written = next };
        }
        if (memory.Write(patch.Address, next)) return true;
        failures++;
        return false;
    }

    private void RestoreInactive()
    {
        foreach (var item in saved.ToArray())
        {
            if (active.Contains(item.Key)) continue;
            if (!memory.Read(item.Key, item.Value.Written.Length, out var current))
            {
                failures++;
                continue;
            }
            if (current.SequenceEqual(item.Value.Written) && !memory.Write(item.Key, item.Value.Before))
            {
                failures++;
                continue;
            }
            saved.Remove(item.Key);
        }
    }

    public string Tick(Settings settings)
    {
        uint state = memory.Resolve(0xB88480, 0xF14, 0);
        uint modeAddress = memory.Resolve(0xB07878, 0x3BC, 0x517);
        if (state == 0 || modeAddress == 0 || !memory.ReadNumber(modeAddress, 1, out uint mode))
            return "Waiting for game memory. Check BLUS30359 / update 1.03 and the PPU base.";
        if (state != lastState)
        {
            saved.Clear(); // The previous object can already be freed/reused.
            lastState = state;
        }
        if (mode is not (1 or 3)) return "Waiting for a supported game mode.";
        if (!memory.ReadNumber(state + 0x144, 4, out uint elapsed) || !memory.ReadNumber(state + 0x143, 1, out uint stage))
            return "Game state is unreadable; no patches applied.";

        failures = 0;
        active.Clear();
        bool battle = mode == 1 && (elapsed > 0 || stage == 6);
        bool campaign = mode == 3 && elapsed > 0;
        foreach (var feature in Catalog.Features)
        {
            if (!settings.Enabled.Contains(feature.Id)) continue;
            if (feature.Context == Context.Battle && !battle || feature.Context == Context.Campaign && !campaign) continue;
            if (feature.Id == "ice" && settings.Enabled.Contains("electric")) continue;
            foreach (var patch in Catalog.Rules(feature.Id, settings, state)) Write(patch);
        }

        void Health(string option, uint maximum, uint current)
        {
            if (battle && settings.Enabled.Contains(option) && memory.ReadNumber(maximum, 2, out uint hp) && hp > 0)
                Write(new(current, 2, hp));
        }
        Health("health1", 0xB2C144, 0xB2C140);
        Health("health2", 0xB2CD84, 0xB2CD80);
        if (battle && settings.Enabled.Contains("hit1") && memory.ReadNumber(0xB7FAA3, 1, out uint hit1) && hit1 > 0) Write(new(0xB2CD80, 2, 0));
        if (battle && settings.Enabled.Contains("hit2") && memory.ReadNumber(0xB7FB1F, 1, out uint hit2) && hit2 > 0) Write(new(0xB2C140, 2, 0));
        if (settings.Enabled.Contains("money"))
        {
            uint money = memory.Resolve(0xA667CC, 0xA54, 0x154);
            if (money != 0) Write(new(money, 4, 765999999)); else failures++;
        }

        static uint Id(Boss boss) => boss == Boss.Nancy ? 82u : 68u;
        if (mode == 1)
        {
            if (settings.Player1 != Boss.Unchanged) Write(new(state + 0x174, 4, Id(settings.Player1)));
            if (settings.Player2 != Boss.Unchanged) Write(new(state + 0x178, 4, Id(settings.Player2)));
        }
        else if (campaign && settings.Player1 != Boss.Unchanged) Write(new(0x98E9F2, 2, Id(settings.Player1)));

        bool gold = (mode == 1 || campaign) &&
            (settings.Player1 == Boss.GoldAzazel || mode == 1 && settings.Player2 == Boss.GoldAzazel);
        bool invalidGold = false;
        if (gold)
        {
            if (memory.ReadNumber(state + 0x120, 4, out uint flag) && flag <= 1)
                Write(new(state + 0x120, 4, 1, true));
            else invalidGold = true;
        }
        if (mode == 1 && settings.Stage is byte selected) Write(new(state + 0x173, 1, selected));
        RestoreInactive();
        if (invalidGold) return "Gold field rejected: this build's Gold layout is not validated.";
        if (failures > 0) return $"{failures} memory operation(s) failed. Some options were not applied.";
        return gold ? "Gold active (experimental). One shared skin flag for both players." : $"Connected — {(mode == 1 ? "battle" : "campaign")}.";
    }

    public bool Restore()
    {
        uint state = memory.Resolve(0xB88480, 0xF14, 0);
        if (state == 0 || state != lastState) { saved.Clear(); return true; }
        failures = 0;
        active.Clear();
        RestoreInactive();
        return failures == 0;
    }
}
