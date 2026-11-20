namespace CommunityTrainer;

internal enum Context { Battle, Campaign, Any }
internal enum Boss { Unchanged, Azazel, Nancy, GoldAzazel }
internal sealed record Feature(string Id, LocalizedText Label, Context Context);
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
        new("health1", new("Keep player 1 health full", "保持 1P 生命值全满"), Context.Battle),
        new("health2", new("Keep player 2 health full", "保持 2P 生命值全满"), Context.Battle),
        new("invincible1", new("Player 1 invulnerability", "1P 无敌"), Context.Battle),
        new("invincible2", new("Player 2 invulnerability", "2P 无敌"), Context.Battle),
        new("defense1", new("Disable player 1 blocking", "禁用 1P 防御"), Context.Battle),
        new("defense2", new("Disable player 2 blocking", "禁用 2P 防御"), Context.Battle),
        new("clock", new("Hold round clock", "锁定回合倒计时"), Context.Battle),
        new("best", new("Set elapsed time to zero", "将已用时间归零"), Context.Battle),
        new("roulette", new("Enable roulette (Lars)", "启用轮盘（Lars）"), Context.Battle),
        new("hit1", new("Player 1 wins with one hit", "1P 一击制胜"), Context.Battle),
        new("hit2", new("Player 2 wins with one hit", "2P 一击制胜"), Context.Battle),
        new("teamhealth", new("Keep both players at 600 health", "将双方生命值锁定为 600"), Context.Campaign),
        new("campaignclock", new("Hold campaign clock", "锁定剧情战役倒计时"), Context.Campaign),
        new("campaignbest", new("Set campaign elapsed time to zero", "将剧情战役已用时间归零"), Context.Campaign),
        new("rage", new("Keep rage active", "持续保持愤怒状态"), Context.Campaign),
        new("element", new("Elemental resistance", "属性抗性"), Context.Campaign),
        new("drops", new("Maximum item drop rate", "道具掉落率最大化"), Context.Campaign),
        new("electric", new("Electric attack effect", "攻击附带电击效果"), Context.Campaign),
        new("ice", new("Freezing attack effect", "攻击附带冰冻效果"), Context.Campaign),
        new("damage", new("Custom bonus damage", "自定义附加伤害"), Context.Campaign),
        new("money", new("Set fight money to 765999999", "将格斗奖金设为 765999999"), Context.Any),
        new("control2", new("Player 1 controls player 2", "由 1P 操控 2P"), Context.Any),
        new("control1", new("Player 2 controls player 1", "由 2P 操控 1P"), Context.Any),
        new("invisible1", new("Hide player 1 model", "隐藏 1P 角色模型"), Context.Any),
        new("invisible2", new("Hide player 2 model", "隐藏 2P 角色模型"), Context.Any),
        new("speed1", new("Custom player 1 movement speed", "自定义 1P 移动速度"), Context.Any),
        new("speed2", new("Custom player 2 movement speed", "自定义 2P 移动速度"), Context.Any)
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

    public LocalizedText Tick(Settings settings)
    {
        uint state = memory.Resolve(0xB88480, 0xF14, 0);
        uint modeAddress = memory.Resolve(0xB07878, 0x3BC, 0x517);
        if (state == 0 || modeAddress == 0 || !memory.ReadNumber(modeAddress, 1, out uint mode))
            return new("Waiting for game memory. Check BLUS30359 / update 1.03 and the PPU base.",
                "正在等待游戏内存就绪。请检查游戏版本（BLUS30359 / 更新 1.03）和 PPU 基址。");
        if (state != lastState)
        {
            saved.Clear(); // The previous object can already be freed/reused.
            lastState = state;
        }
        if (mode is not (1 or 3)) return new("Waiting for a supported game mode.", "正在等待进入支持的游戏模式。");
        if (!memory.ReadNumber(state + 0x144, 4, out uint elapsed) || !memory.ReadNumber(state + 0x143, 1, out uint stage))
            return new("Game state is unreadable; no patches applied.", "无法读取游戏状态，未应用任何修改。");

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
        if (invalidGold) return new("Gold field rejected: this build's Gold layout is not validated.",
            "无法启用黄金 Azazel。此版本的黄金外观内存字段尚未验证。");
        if (failures > 0) return new($"{failures} memory operation(s) failed. Some options were not applied.",
            $"有 {failures} 次内存操作失败，部分选项未生效。");
        if (gold) return new("Gold active (experimental). One shared skin flag for both players.",
            "黄金 Azazel 已启用（实验功能）。双方共用同一外观设置。");
        return mode == 1 ? new("Connected - battle.", "已连接：对战。")
            : new("Connected - campaign.", "已连接：剧情战役。");
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
