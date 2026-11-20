using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace CommunityTrainer;

internal sealed class MainWindow : Form
{
    private sealed record Target(int Pid, string Name)
    {
        public override string ToString() => $"{Name}  (PID {Pid})";
    }
    private sealed record Choice(Boss Value, string Label)
    {
        public override string ToString() => Label;
    }
    private static readonly LocalizedText Unchanged = new("Unchanged", "保持原样");
    private static readonly LocalizedText[] BossLabels =
        [Unchanged, new("Azazel", "Azazel"), new("Nancy-MI847J", "NANCY-MI847J"), new("Gold Azazel", "黄金 Azazel")];
    private static readonly string Version =
        typeof(MainWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

    private readonly Settings settings = new();
    private readonly LanguagePreference languagePreference;
    private readonly CultureInfo systemUiCulture;
    private readonly List<Action<bool>> languageBindings = new();
    private readonly ComboBox languagePicker = new() { Name = "languagePicker", DropDownStyle = ComboBoxStyle.DropDownList, Width = 132, Margin = new(3, 12, 3, 0) };
    private readonly ComboBox processes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 310 };
    private readonly TextBox ppuBase = new() { Text = "300000000", Width = 125 };
    private readonly Label status = new() { Name = "status", AutoSize = false, Dock = DockStyle.Bottom, Height = 64, Padding = new(12) };
    private LocalizedText statusText = new("Choose the emulator process and connect.", "请选择模拟器进程，然后点击连接。");
    private bool chinese;
    private bool updatingLanguage;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
    private readonly List<CheckBox> toggles = new();
    private readonly ComboBox player1;
    private readonly ComboBox player2;
    private readonly ComboBox stages = new() { Name = "stages", DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private ProcessMemory? memory;
    private TrainerEngine? engine;
    internal int FeatureControlCount => toggles.Count;

    public MainWindow(LanguagePreference? languagePreference = null, CultureInfo? systemUiCulture = null)
    {
        this.languagePreference = languagePreference ?? LanguagePreference.ForCurrentUser();
        this.systemUiCulture = systemUiCulture ?? CultureInfo.CurrentUICulture;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new(760, 660);
        MinimumSize = new(680, 600);
        Font = new("Segoe UI", 10);
        BackColor = Color.FromArgb(246, 247, 250);
        ForeColor = Color.FromArgb(30, 35, 44);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new(16) };
        layout.RowStyles.Add(new(SizeType.Absolute, 64));
        layout.RowStyles.Add(new(SizeType.Absolute, 90));
        layout.RowStyles.Add(new(SizeType.Absolute, 105));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.Controls.Add(BindText(new Label { AutoSize = true, Font = new(Font.FontFamily, 13, FontStyle.Bold) },
            new("Community Trainer\nTekken 6 · RPCS3 · BLUS30359 / 1.03", "社区修改器\n铁拳6 · RPCS3 · BLUS30359 / 1.03")), 0, 0);

        var connection = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        connection.Controls.Add(processes);
        connection.Controls.Add(Button(new("Refresh", "刷新"), (_, _) => RefreshTargets()));
        connection.Controls.Add(Button(new("Connect", "连接"), (_, _) => Connect()));
        connection.Controls.Add(Button(new("Disconnect", "断开连接"), (_, _) => Disconnect()));
        connection.SetFlowBreak(connection.Controls[^1], true);
        connection.Controls.Add(BindText(new Label { AutoSize = true, Padding = new(0, 5, 0, 0) }, new("PPU base (hex)", "PPU 基址（十六进制）")));
        BindAccessibleName(ppuBase, new("PPU memory base in hexadecimal", "PPU 内存基址（十六进制）"));
        BindAccessibleName(processes, new("Emulator process", "模拟器进程"));
        connection.Controls.Add(ppuBase);
        layout.Controls.Add(connection, 0, 1);

        var bosses = new FlowLayoutPanel { Dock = DockStyle.Fill };
        player1 = BossPicker("player1", new("Player 1 boss", "1P 首领"), value => settings.Player1 = value);
        player2 = BossPicker("player2", new("Player 2 boss", "2P 首领"), value => settings.Player2 = value);
        bosses.Controls.Add(BindText(new Label { AutoSize = true, Padding = new(0, 6, 0, 0) }, new("P1", "1P")));
        bosses.Controls.Add(player1);
        bosses.Controls.Add(BindText(new Label { AutoSize = true, Padding = new(8, 6, 0, 0) }, new("P2", "2P")));
        bosses.Controls.Add(player2);
        bosses.Controls.Add(Button(new("Stop all options", "关闭所有修改"), (_, _) => ResetOptions()));
        bosses.SetFlowBreak(bosses.Controls[^1], true);
        bosses.Controls.Add(BindText(new Label { AutoSize = true, MaximumSize = new(690, 0), Padding = new(0, 8, 0, 0) },
            new("Select bosses before the fight loads. Gold Azazel is experimental and uses a shared skin flag.\nThe layout targets USA update 1.03; gameplay compatibility has not yet been confirmed.",
                "请在对战加载前选择首领。黄金 Azazel 为实验功能，双方共用同一个外观设置。\n适用版本：《铁拳6》美版，更新 1.03。游戏内兼容性仍待确认。")));
        layout.Controls.Add(bosses, 0, 2);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        foreach (Context context in Enum.GetValues<Context>())
        {
            var page = BindText(new TabPage { Name = context.ToString(), BackColor = BackColor }, context switch
            {
                Context.Battle => new("Battle", "对战"),
                Context.Campaign => new("Campaign", "剧情战役"),
                _ => new("General / stages", "通用 / 场地")
            });
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new(8) };
            foreach (var feature in Catalog.Features.Where(item => item.Context == context))
            {
                var toggle = BindText(new CheckBox { Name = feature.Id, AutoSize = true, Margin = new(3, 4, 3, 4) }, feature.Label);
                toggle.CheckedChanged += (_, _) =>
                {
                    if (toggle.Checked)
                    {
                        settings.Enabled.Add(feature.Id);
                        if (feature.Id is "ice" or "electric")
                        {
                            string opposite = feature.Id == "ice" ? "electric" : "ice";
                            var other = toggles.FirstOrDefault(item => item.Name == opposite);
                            if (other != null) other.Checked = false;
                        }
                    }
                    else settings.Enabled.Remove(feature.Id);
                    Pulse();
                };
                toggles.Add(toggle);
                flow.Controls.Add(toggle);
                if (feature.Id == "damage") flow.Controls.Add(Number(200, feature.Label, value => settings.Damage = value));
                if (feature.Id == "speed1") flow.Controls.Add(Number(4096, feature.Label, value => settings.Speed1 = value));
                if (feature.Id == "speed2") flow.Controls.Add(Number(4096, feature.Label, value => settings.Speed2 = value));
            }
            if (context == Context.Any)
            {
                var stageLabel = new LocalizedText("Force stage before loading", "加载前指定场地");
                flow.Controls.Add(BindText(new Label { AutoSize = true, Margin = new(3, 15, 3, 4) }, stageLabel));
                BindAccessibleName(stages, stageLabel);
                stages.DropDownWidth = 440;
                stages.FormattingEnabled = true;
                stages.Format += (_, e) =>
                {
                    if (e.ListItem is byte id)
                    {
                        var name = StageNames.For(id);
                        e.Value = chinese ? $"{name.English}（{name.Get(true)}）" : name.English;
                    }
                };
                stages.Items.Add(Unchanged.English);
                foreach (byte stage in Catalog.StageIds) stages.Items.Add(stage);
                stages.SelectedIndex = 0;
                stages.SelectedIndexChanged += (_, _) =>
                {
                    if (updatingLanguage) return;
                    settings.Stage = stages.SelectedItem is byte id ? id : null;
                    Pulse();
                };
                languageBindings.Add(useChinese => UpdateChoices(stages, () =>
                {
                    stages.Items.Clear();
                    stages.Items.Add(Unchanged.Get(useChinese));
                    foreach (byte id in Catalog.StageIds) stages.Items.Add(id);
                }));
                flow.Controls.Add(stages);
            }
            page.Controls.Add(flow);
            tabs.TabPages.Add(page);
        }
        var credits = BindText(new TabPage { Name = "Credits", BackColor = BackColor }, new("Credits", "鸣谢"));
        var creditFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(12), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        creditFlow.Controls.Add(BindText(new Label { AutoSize = true, MaximumSize = new(640, 0) }, new(
            "Independent community implementation maintained by danstronics.\nOriginal trainer research: Hyper Cheats.\nOriginal PS3 RTM / Gold research: Ryu_Hayabusa80.\nNo original trainer code, images, icons, music or binaries are included.\n\nTekken belongs to Bandai Namco; RPCS3 belongs to its contributors.\nNo endorsement or affiliation is implied.",
            "本工具由社区独立开发，由 danstronics 维护。\n原版修改器研究：Hyper Cheats。\nPS3 实时内存修改及黄金外观研究：Ryu_Hayabusa80。\n本工具未包含原版修改器的代码、图像、图标、音乐或可执行文件。\n\n《铁拳》归万代南梦宫所有。RPCS3 归其贡献者所有。\n本项目与上述权利方无隶属关系，也未获得其背书。")));
        creditFlow.Controls.Add(Link(new("Hyper Cheats — original channel", "Hyper Cheats 的原始频道"), "https://www.youtube.com/@hypercheats1113"));
        creditFlow.Controls.Add(Link(new("Ryu_Hayabusa80 — original RTM announcement", "Ryu_Hayabusa80 的 RTM 工具发布帖"), "https://nextgenupdate.com/forums/accounts-services/929589-tekken-hack-services-tekken-rtm-tools.html"));
        creditFlow.Controls.Add(Link(new("Project source and releases", "项目源代码和发行版本"), "https://github.com/danstronics/tekken6-trainer-community"));
        languagePicker.Items.AddRange(["Auto / 自动", "English", "简体中文"]);
        languagePicker.SelectedIndex = (int)this.languagePreference.Load();
        BindAccessibleName(languagePicker, new("Interface language", "界面语言"));
        creditFlow.Controls.Add(languagePicker);
        credits.Controls.Add(creditFlow);
        tabs.TabPages.Add(credits);
        layout.Controls.Add(tabs, 0, 3);
        Controls.Add(layout);
        Controls.Add(status);

        ApplyLanguage();
        languagePicker.SelectedIndexChanged += (_, _) =>
        {
            ApplyLanguage();
            if (!this.languagePreference.Save((UiLanguage)languagePicker.SelectedIndex))
                MessageBox.Show(this, new LocalizedText("Could not save the language preference.", "无法保存语言设置。").Get(chinese), Text);
        };
        timer.Tick += (_, _) => Pulse();
        timer.Start();
        RefreshTargets();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
            Disconnect();
            timer.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ApplyLanguage()
    {
        chinese = LanguagePreference.UsesChinese((UiLanguage)languagePicker.SelectedIndex, systemUiCulture);
        updatingLanguage = true;
        SuspendLayout();
        try
        {
            Text = chinese ? $"铁拳6 社区修改器 — v{Version}" : $"Community Trainer for Tekken 6 — v{Version}";
            foreach (var update in languageBindings) update(chinese);
            status.Text = statusText.Get(chinese);
        }
        finally
        {
            updatingLanguage = false;
            ResumeLayout();
        }
    }

    private T BindText<T>(T control, LocalizedText text) where T : Control
    {
        control.Text = text.English;
        languageBindings.Add(useChinese => control.Text = text.Get(useChinese));
        return control;
    }

    private void BindAccessibleName(Control control, LocalizedText name) =>
        languageBindings.Add(useChinese => control.AccessibleName = name.Get(useChinese));

    private void SetStatus(LocalizedText text)
    {
        statusText = text;
        status.Text = text.Get(chinese);
    }

    private static void UpdateChoices(ComboBox picker, Action update)
    {
        int selected = picker.SelectedIndex;
        picker.BeginUpdate();
        try
        {
            update();
            picker.SelectedIndex = selected;
        }
        finally { picker.EndUpdate(); }
    }

    private Button Button(LocalizedText text, EventHandler click)
    {
        var button = BindText(new Button { AutoSize = true, FlatStyle = FlatStyle.System, Padding = new(4, 0, 4, 0) }, text);
        button.Click += click;
        return button;
    }

    private LinkLabel Link(LocalizedText label, string url)
    {
        var link = BindText(new LinkLabel { AutoSize = true, Margin = new(3, 12, 3, 0) }, label);
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return link;
    }

    private ComboBox BossPicker(string name, LocalizedText accessibleName, Action<Boss> setter)
    {
        var box = new ComboBox { Name = name, DropDownStyle = ComboBoxStyle.DropDownList, Width = 165 };
        BindAccessibleName(box, accessibleName);
        box.Items.AddRange(Enum.GetValues<Boss>().Select(boss => new Choice(boss, BossLabels[(int)boss].English)).ToArray());
        box.SelectedIndex = 0;
        box.SelectedIndexChanged += (_, _) =>
        {
            if (updatingLanguage) return;
            if (box.SelectedItem is Choice choice) setter(choice.Value);
            Pulse();
        };
        languageBindings.Add(useChinese => UpdateChoices(box, () =>
        {
            for (int i = 0; i < box.Items.Count; i++)
            {
                var choice = (Choice)box.Items[i]!;
                box.Items[i] = choice with { Label = BossLabels[(int)choice.Value].Get(useChinese) };
            }
        }));
        return box;
    }

    private NumericUpDown Number(ushort initial, LocalizedText name, Action<ushort> setter)
    {
        var number = new NumericUpDown { Minimum = 0, Maximum = ushort.MaxValue, Value = initial, Width = 110, ThousandsSeparator = true };
        BindAccessibleName(number, name);
        number.ValueChanged += (_, _) => { setter((ushort)number.Value); Pulse(); };
        return number;
    }

    private void RefreshTargets()
    {
        int? selected = (processes.SelectedItem as Target)?.Pid;
        var targets = new List<Target>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
                try { targets.Add(new(process.Id, process.ProcessName)); }
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
        }
        processes.Items.Clear();
        foreach (var target in targets.OrderByDescending(item => item.Name.Contains("rpcs3", StringComparison.OrdinalIgnoreCase)).ThenBy(item => item.Name)) processes.Items.Add(target);
        int index = processes.Items.Count > 0 && ((Target)processes.Items[0]!).Name.Contains("rpcs3", StringComparison.OrdinalIgnoreCase) ? 0 : -1;
        for (int i = 0; i < processes.Items.Count; i++) if (((Target)processes.Items[i]!).Pid == selected) index = i;
        processes.SelectedIndex = index;
    }

    private void Connect()
    {
        if (processes.SelectedItem is not Target target) return;
        if (!ulong.TryParse(ppuBase.Text.Replace("0x", "", StringComparison.OrdinalIgnoreCase), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong address))
        {
            SetStatus(new("Enter a hexadecimal PPU base, for example 300000000.", "请输入十六进制 PPU 基址，例如 300000000。"));
            return;
        }
        Disconnect();
        try
        {
            memory = new(target.Pid, address);
            engine = new(memory);
            Pulse();
        }
        catch (Win32Exception error)
        {
            SetStatus(new("Could not connect: " + error.Message,
                $"无法连接到模拟器（Windows 错误代码 {error.NativeErrorCode}）。"));
        }
        catch (ArgumentOutOfRangeException error)
        {
            SetStatus(new("Could not connect: " + error.Message, "PPU 基址超出有效范围。"));
        }
    }

    private void Disconnect()
    {
        bool restored = engine?.Restore() ?? true;
        engine = null;
        memory?.Dispose();
        memory = null;
        SetStatus(restored ? new("Disconnected.", "连接已断开。")
            : new("Disconnected; some previous values could not be restored.", "连接已断开，部分原始数值未能恢复。"));
    }

    private void ResetOptions()
    {
        foreach (var toggle in toggles) toggle.Checked = false;
        player1.SelectedIndex = 0;
        player2.SelectedIndex = 0;
        stages.SelectedIndex = 0;
        Pulse();
    }

    private void Pulse()
    {
        if (memory == null || engine == null) return;
        if (!memory.IsAlive)
        {
            Disconnect();
            SetStatus(new("The emulator exited. Select its new process to reconnect.", "模拟器已退出。请选中新进程并重新连接。"));
            return;
        }
        SetStatus(engine.Tick(settings));
    }
}
