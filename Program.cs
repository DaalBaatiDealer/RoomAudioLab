using NAudio.CoreAudioApi;
using AudioTwin.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace RoomAudioLab;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly ComboBox _frontDevice = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _rearDevice = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TrackBar _frontDelay = Slider(0, 500, 0);
    private readonly TrackBar _rearDelay = Slider(0, 500, 50);
    private readonly TrackBar _frontLevel = Slider(0, 100, 100);
    private readonly TrackBar _rearLevel = Slider(0, 100, 100);
    private readonly TrackBar _sourcePosition = Slider(0, 100, 50);
    private readonly Button _play = new() { Text = "Play test tone", AutoSize = true };
    private readonly Button _load = new() { Text = "Open audio file…", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, Text = "Stopped" };
    private readonly RoomCanvas _room = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private Playback? _playback;
    private string? _file;

    public MainForm()
    {
        Text = "Room Audio Lab — prototype";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(790, 620);
        ClientSize = new Size(900, 690);
        Font = new Font("Segoe UI", 9F);
        BuildUi();
        RefreshDevices();
        _play.Click += (_, _) => TogglePlayback();
        _load.Click += (_, _) => LoadFile();
        _frontDevice.SelectedIndexChanged += (_, _) => RestartIfPlaying();
        _rearDevice.SelectedIndexChanged += (_, _) => RestartIfPlaying();
        foreach (var slider in new[] { _frontDelay, _rearDelay, _frontLevel, _rearLevel, _sourcePosition })
            slider.ValueChanged += (_, _) => ApplyControls();
        _room.PositionChanged += (_, value) => _sourcePosition.Value = value;
        _timer.Tick += (_, _) => { if (_playback?.Finished == true) StopPlayback(); };
        FormClosed += (_, _) => { StopPlayback(); _enumerator.Dispose(); };
    }

    private void BuildUi()
    {
        var header = new Label { Text = "Place two outputs in the room and move the sound source", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(18, 14) };
        Controls.Add(header);
        var layout = new TableLayoutPanel { Location = new Point(18, 46), Size = new Size(850, 550), ColumnCount = 2, RowCount = 1, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        _room.Dock = DockStyle.Fill;
        layout.Controls.Add(_room, 0, 0);
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(14, 8, 6, 6) };
        layout.Controls.Add(panel, 1, 0);
        Controls.Add(layout);

        int y = 5;
        AddLabel(panel, "FRONT SPEAKER", y, true); y += 28;
        AddControl(panel, "Device", _frontDevice, ref y);
        AddTrack(panel, "Level", _frontLevel, ref y);
        AddTrack(panel, "Sync delay (ms)", _frontDelay, ref y);
        y += 6;
        AddLabel(panel, "REAR SPEAKER", y, true); y += 28;
        AddControl(panel, "Device", _rearDevice, ref y);
        AddTrack(panel, "Level", _rearLevel, ref y);
        AddTrack(panel, "Sync delay (ms)", _rearDelay, ref y);
        y += 4;
        AddLabel(panel, "SOURCE POSITION", y, true); y += 25;
        AddTrack(panel, "Front ↔ Rear", _sourcePosition, ref y);
        _load.Location = new Point(8, y + 4); panel.Controls.Add(_load); y += 42;
        _play.Location = new Point(8, y); panel.Controls.Add(_play); y += 38;
        _status.Location = new Point(8, y); panel.Controls.Add(_status); y += 28;
        var info = new Label { Location = new Point(8, y), Size = new Size(350, 100), ForeColor = Color.DimGray, Text = "The movable dot controls relative front/rear loudness for this app's test tone or opened file. Ordinary stereo files do not contain object positions, so the dot is manual. This prototype does not reroute audio from other apps." };
        panel.Controls.Add(info);
        var refresh = new Button { Text = "Refresh devices", AutoSize = true, Location = new Point(18, 610), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        refresh.Click += (_, _) => RefreshDevices();
        Controls.Add(refresh);
    }

    private static TrackBar Slider(int min, int max, int value) => new() { Minimum = min, Maximum = max, Value = value, TickFrequency = max <= 100 ? 10 : 50, SmallChange = 1, LargeChange = 10, AutoSize = false, Height = 34, Width = 240 };
    private static void AddLabel(Control parent, string text, int y, bool bold = false) => parent.Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(8, y), Font = new Font("Segoe UI", 9F, bold ? FontStyle.Bold : FontStyle.Regular) });
    private static void AddControl(Control parent, string caption, Control control, ref int y)
    {
        parent.Controls.Add(new Label { Text = caption, AutoSize = true, Location = new Point(8, y + 5) });
        control.Location = new Point(82, y); control.Width = 255; parent.Controls.Add(control); y += 36;
    }
    private static void AddTrack(Control parent, string caption, TrackBar slider, ref int y)
    {
        parent.Controls.Add(new Label { Text = caption, AutoSize = true, Location = new Point(8, y + 7) });
        slider.Location = new Point(105, y); parent.Controls.Add(slider);
        var value = new Label { AutoSize = true, Location = new Point(350, y + 7) };
        value.Text = slider.Maximum == 100 ? slider.Value + "%" : slider.Value + " ms";
        slider.ValueChanged += (_, _) => value.Text = slider.Maximum == 100 ? slider.Value + "%" : slider.Value + " ms";
        parent.Controls.Add(value); y += 42;
    }

    private void RefreshDevices()
    {
        var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToArray();
        var defaultId = "";
        try { defaultId = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID; } catch { }
        Fill(_frontDevice, devices, defaultId, 0);
        Fill(_rearDevice, devices, defaultId, devices.Length > 1 ? 1 : 0);
        if (devices.Length > 1 && (_frontDevice.SelectedItem as DeviceChoice)?.Id == (_rearDevice.SelectedItem as DeviceChoice)?.Id)
            _rearDevice.SelectedIndex = Array.FindIndex(devices, d => d.ID != (_frontDevice.SelectedItem as DeviceChoice)?.Id);
        _status.Text = devices.Length < 2 ? "Connect two active playback devices, then refresh." : $"Found {devices.Length} playback devices.";
    }

    private static void Fill(ComboBox box, MMDevice[] devices, string defaultId, int preferred)
    {
        var old = (box.SelectedItem as DeviceChoice)?.Id;
        box.Items.Clear();
        foreach (var d in devices) box.Items.Add(new DeviceChoice(d.ID, d.FriendlyName));
        var ix = Array.FindIndex(devices, d => d.ID == old);
        if (ix < 0) ix = Array.FindIndex(devices, d => d.ID == defaultId);
        if (ix < 0 && devices.Length > 0) ix = Math.Clamp(preferred, 0, devices.Length - 1);
        box.SelectedIndex = ix;
    }

    private void TogglePlayback()
    {
        if (_playback != null) { StopPlayback(); return; }
        try
        {
            var f = _frontDevice.SelectedItem as DeviceChoice ?? throw new InvalidOperationException("Choose a front speaker.");
            var r = _rearDevice.SelectedItem as DeviceChoice ?? throw new InvalidOperationException("Choose a rear speaker.");
            if (f.Id == r.Id) throw new InvalidOperationException("Choose two different output devices.");
            _playback = new Playback(f.Id, _file);
            _playback.Start(r.Id);
            _play.Text = "Stop playback";
            _status.Text = _file == null ? "Playing 440 Hz test tone on both outputs." : "Playing " + Path.GetFileName(_file);
            ApplyControls(); _timer.Start();
        }
        catch (Exception ex) { StopPlayback(); MessageBox.Show(this, ex.Message, "Could not start playback", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadFile()
    {
        using var dialog = new OpenFileDialog { Filter = "Audio files|*.wav;*.mp3;*.aiff;*.wma;*.aac;*.m4a|All files|*.*", Title = "Choose audio to play on both speakers" };
        if (dialog.ShowDialog(this) == DialogResult.OK) { _file = dialog.FileName; _play.Text = "Play selected file"; _status.Text = Path.GetFileName(_file); }
    }

    private void ApplyControls()
    {
        _room.SourcePosition = _sourcePosition.Value;
        _playback?.SetGains(Gain(_sourcePosition.Value, true) * _frontLevel.Value / 100f, Gain(_sourcePosition.Value, false) * _rearLevel.Value / 100f);
        _playback?.SetDelays(_frontDelay.Value, _rearDelay.Value);
    }
    private static float Gain(int position, bool front) { double a = position / 100.0 * Math.PI / 2; return (float)(front ? Math.Cos(a) : Math.Sin(a)); }
    private void RestartIfPlaying() { if (_playback != null) { StopPlayback(); TogglePlayback(); } }
    private void StopPlayback() { _timer.Stop(); _playback?.Dispose(); _playback = null; _play.Text = _file == null ? "Play test tone" : "Play selected file"; }

    private sealed record DeviceChoice(string Id, string Name) { public override string ToString() => Name; }

    private sealed class Playback : IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator = new();
        private readonly string _frontId;
        private readonly string? _file;

        private WasapiOut? _frontOut, _rearOut;
        private VolumeSampleProvider? _frontVolume, _rearVolume;
        private DelaySampleProvider? _frontDelay, _rearDelay;
        private IDisposable? _frontReader, _rearReader;
        public bool Finished => _file != null && ((_frontOut?.PlaybackState ?? PlaybackState.Stopped) == PlaybackState.Stopped || (_rearOut?.PlaybackState ?? PlaybackState.Stopped) == PlaybackState.Stopped);
        public Playback(string frontId, string? file) { _frontId = frontId; _file = file; }
        public void Start(string rearId)
        {
            var frontSource = CreateSource(out _frontReader); var rearSource = CreateSource(out _rearReader);
            _frontDelay = new DelaySampleProvider(frontSource, 500);
            _rearDelay = new DelaySampleProvider(rearSource, 500);
            _frontVolume = new VolumeSampleProvider(_frontDelay);
            _rearVolume = new VolumeSampleProvider(_rearDelay);
            var frontDevice = _enumerator.GetDevice(_frontId); var rearDevice = _enumerator.GetDevice(rearId);
            _frontOut = new WasapiOut(frontDevice, AudioClientShareMode.Shared, true, 100);
            _rearOut = new WasapiOut(rearDevice, AudioClientShareMode.Shared, true, 100);
            _frontOut.Init(_frontVolume); _rearOut.Init(_rearVolume);
            _frontOut.Play(); _rearOut.Play();
        }
        private ISampleProvider CreateSource(out IDisposable? owner)
        {
            if (_file != null) { var reader = new AudioFileReader(_file); owner = reader; return reader; }
            owner = null;
            return new SignalGenerator(44100, 2) { Frequency = 440, Gain = 0.12, Type = SignalGeneratorType.Sin };
        }
        public void SetGains(float front, float rear) { if (_frontVolume != null) _frontVolume.Volume = front; if (_rearVolume != null) _rearVolume.Volume = rear; }
        public void SetDelays(int front, int rear) { if (_frontDelay != null) _frontDelay.DelayMilliseconds = front; if (_rearDelay != null) _rearDelay.DelayMilliseconds = rear; }
        public void Dispose() { try { _frontOut?.Stop(); } catch { } try { _rearOut?.Stop(); } catch { } _frontOut?.Dispose(); _rearOut?.Dispose(); _frontReader?.Dispose(); _rearReader?.Dispose(); _enumerator.Dispose(); }
    }
}

internal sealed class RoomCanvas : Control
{
    private bool _dragging;
    private int _position = 50;
    public event EventHandler<int>? PositionChanged;
    public int SourcePosition { get => _position; set { _position = Math.Clamp(value, 0, 100); Invalidate(); } }
    public RoomCanvas() { DoubleBuffered = true; BackColor = Color.FromArgb(245, 247, 250); Cursor = Cursors.Hand; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var room = new Rectangle(30, 42, Math.Max(100, Width - 60), Math.Max(180, Height - 85));
        using var pen = new Pen(Color.FromArgb(80, 94, 110), 2); g.DrawRectangle(pen, room);
        using var frontBrush = new SolidBrush(Color.FromArgb(46, 125, 220)); using var rearBrush = new SolidBrush(Color.FromArgb(135, 93, 190));
        using var textBrush = new SolidBrush(Color.FromArgb(45, 55, 70));
        g.DrawString("FRONT", Font, textBrush, room.Left + 8, room.Top + 8); g.DrawString("REAR", Font, textBrush, room.Left + 8, room.Bottom - 25);
        DrawSpeaker(g, room.Left + room.Width / 2, room.Top + 28, frontBrush); DrawSpeaker(g, room.Left + room.Width / 2, room.Bottom - 28, rearBrush);
        int sy = room.Top + 50 + (room.Height - 100) * _position / 100; int sx = room.Left + room.Width / 2;
        using var line = new Pen(Color.FromArgb(180, 68, 80, 95), 1) { DashPattern = new float[] { 4, 3 } }; g.DrawLine(line, sx, room.Top + 42, sx, room.Bottom - 42);
        using var source = new SolidBrush(Color.FromArgb(230, 240, 120, 45)); g.FillEllipse(source, sx - 12, sy - 12, 24, 24); g.DrawEllipse(Pens.White, sx - 12, sy - 12, 24, 24);
        g.DrawString("Drag the dot to move the sound", Font, textBrush, 15, room.Bottom + 22);
    }
    private static void DrawSpeaker(Graphics g, int x, int y, Brush b) { g.FillRectangle(b, x - 18, y - 10, 36, 20); g.FillEllipse(Brushes.White, x - 8, y - 7, 14, 14); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _dragging = true; SetFromY(e.Y); } }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (_dragging) SetFromY(e.Y); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _dragging = false; }
    private void SetFromY(int y) { int top = 42 + 50, bottom = Height - 43 - 50; SourcePosition = (int)Math.Round(Math.Clamp((y - top) / (double)Math.Max(1, bottom - top), 0, 1) * 100); PositionChanged?.Invoke(this, _position); }
}







