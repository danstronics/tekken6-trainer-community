using System.Diagnostics;
using System.Globalization;

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
    private readonly Settings settings = new();
    private readonly ComboBox processes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 310 };
    private readonly TextBox ppuBase = new() { Text = "300000000", Width = 125, AccessibleName = "PPU memory base in hexadecimal" };
    private readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 64, Padding = new(12), Text = "Choose the emulator process and connect." };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
    private readonly List<CheckBox> toggles = new();
    private readonly ComboBox player1;
    private readonly ComboBox player2;
    private readonly ComboBox stages = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private ProcessMemory? memory;
    private TrainerEngine? engine;
    internal int FeatureControlCount => toggles.Count;

    public MainWindow()
    {
        Text = "Community Trainer for Tekken 6 — v3.0.0";
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
        layout.Controls.Add(new Label { Text = "Community Trainer\nTekken 6 · RPCS3 · BLUS30359 / 1.03", AutoSize = true, Font = new(Font.FontFamily, 13, FontStyle.Bold) }, 0, 0);

        var connection = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        connection.Controls.Add(processes);
        connection.Controls.Add(Button("Refresh", (_, _) => RefreshTargets()));
        connection.Controls.Add(Button("Connect", (_, _) => Connect()));
        connection.Controls.Add(Button("Disconnect", (_, _) => Disconnect()));
        connection.SetFlowBreak(connection.Controls[^1], true);
        connection.Controls.Add(new Label { Text = "PPU base (hex)", AutoSize = true, Padding = new(0, 5, 0, 0) });
        connection.Controls.Add(ppuBase);
        layout.Controls.Add(connection, 0, 1);

        var bosses = new FlowLayoutPanel { Dock = DockStyle.Fill };
        player1 = BossPicker("Player 1", value => settings.Player1 = value);
        player2 = BossPicker("Player 2", value => settings.Player2 = value);
        bosses.Controls.Add(new Label { Text = "P1", AutoSize = true, Padding = new(0, 6, 0, 0) });
        bosses.Controls.Add(player1);
        bosses.Controls.Add(new Label { Text = "P2", AutoSize = true, Padding = new(8, 6, 0, 0) });
        bosses.Controls.Add(player2);
        bosses.Controls.Add(Button("Stop all options", (_, _) => ResetOptions()));
        bosses.SetFlowBreak(bosses.Controls[^1], true);
        bosses.Controls.Add(new Label
        {
            Text = "Select bosses before the fight loads. Gold Azazel is experimental and uses a shared skin flag.\nThe layout targets USA update 1.03; gameplay compatibility has not yet been confirmed.",
            AutoSize = true, MaximumSize = new(690, 0), Padding = new(0, 8, 0, 0)
        });
        layout.Controls.Add(bosses, 0, 2);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        foreach (Context context in Enum.GetValues<Context>())
        {
            var page = new TabPage(context switch { Context.Battle => "Battle", Context.Campaign => "Campaign", _ => "General / stages" }) { BackColor = BackColor };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new(8) };
            foreach (var feature in Catalog.Features.Where(item => item.Context == context))
            {
                var toggle = new CheckBox { Text = feature.Label, Name = feature.Id, AutoSize = true, Margin = new(3, 4, 3, 4) };
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
                if (feature.Id == "damage") flow.Controls.Add(Number(200, value => settings.Damage = value));
                if (feature.Id == "speed1") flow.Controls.Add(Number(4096, value => settings.Speed1 = value));
                if (feature.Id == "speed2") flow.Controls.Add(Number(4096, value => settings.Speed2 = value));
            }
            if (context == Context.Any)
            {
                flow.Controls.Add(new Label { Text = "Force stage before loading (numeric game ID)", AutoSize = true, Margin = new(3, 15, 3, 4) });
                stages.Items.Add("Unchanged");
                foreach (byte stage in Catalog.StageIds) stages.Items.Add(stage);
                stages.SelectedIndex = 0;
                stages.SelectedIndexChanged += (_, _) => { settings.Stage = stages.SelectedItem is byte id ? id : null; Pulse(); };
                flow.Controls.Add(stages);
            }
            page.Controls.Add(flow);
            tabs.TabPages.Add(page);
        }
        var credits = new TabPage("Credits") { BackColor = BackColor };
        var creditFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(12), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        creditFlow.Controls.Add(new Label { AutoSize = true, MaximumSize = new(640, 0), Text = "Independent community implementation maintained by danstronics.\nOriginal trainer research: Hyper Cheats.\nOriginal PS3 RTM / Gold research: Ryu_Hayabusa80.\nNo original trainer code, images, icons, music or binaries are included.\n\nTekken belongs to Bandai Namco; RPCS3 belongs to its contributors.\nNo endorsement or affiliation is implied." });
        creditFlow.Controls.Add(Link("Hyper Cheats — original channel", "https://www.youtube.com/@hypercheats1113"));
        creditFlow.Controls.Add(Link("Ryu_Hayabusa80 — original RTM announcement", "https://nextgenupdate.com/forums/accounts-services/929589-tekken-hack-services-tekken-rtm-tools.html"));
        creditFlow.Controls.Add(Link("Project source and releases", "https://github.com/danstronics/tekken6-trainer-community"));
        credits.Controls.Add(creditFlow);
        tabs.TabPages.Add(credits);
        layout.Controls.Add(tabs, 0, 3);
        Controls.Add(layout);
        Controls.Add(status);

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

    private static Button Button(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.System, Padding = new(4, 0, 4, 0) };
        button.Click += click;
        return button;
    }

    private static LinkLabel Link(string label, string url)
    {
        var link = new LinkLabel { Text = label, AutoSize = true, Margin = new(3, 12, 3, 0) };
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return link;
    }

    private ComboBox BossPicker(string accessibleName, Action<Boss> setter)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 165, AccessibleName = accessibleName + " boss" };
        box.Items.AddRange(new Choice[] { new(Boss.Unchanged, "Unchanged"), new(Boss.Azazel, "Azazel"), new(Boss.Nancy, "Nancy-MI847J"), new(Boss.GoldAzazel, "Gold Azazel") });
        box.SelectedIndex = 0;
        box.SelectedIndexChanged += (_, _) => { if (box.SelectedItem is Choice choice) setter(choice.Value); Pulse(); };
        return box;
    }

    private NumericUpDown Number(ushort initial, Action<ushort> setter)
    {
        var number = new NumericUpDown { Minimum = 0, Maximum = ushort.MaxValue, Value = initial, Width = 110, ThousandsSeparator = true };
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
                catch (System.ComponentModel.Win32Exception) { }
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
        { status.Text = "Enter a hexadecimal PPU base, for example 300000000."; return; }
        Disconnect();
        try
        {
            memory = new(target.Pid, address);
            engine = new(memory);
            Pulse();
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or ArgumentOutOfRangeException)
        { status.Text = "Could not connect: " + error.Message; }
    }

    private void Disconnect()
    {
        bool restored = engine?.Restore() ?? true;
        engine = null;
        memory?.Dispose();
        memory = null;
        status.Text = restored ? "Disconnected." : "Disconnected; some previous values could not be restored.";
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
        if (!memory.IsAlive) { Disconnect(); status.Text = "The emulator exited. Select its new process to reconnect."; return; }
        status.Text = engine.Tick(settings);
    }
}
