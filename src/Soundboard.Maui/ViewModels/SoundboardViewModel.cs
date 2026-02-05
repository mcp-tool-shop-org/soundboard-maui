using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Soundboard.Client;
using Soundboard.Client.Models;
using Soundboard.Maui.Audio;

namespace Soundboard.Maui.ViewModels;

public sealed class SoundboardViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISoundboardClient _client;
    private readonly IAudioPlayer _player;
    private CancellationTokenSource? _speakCts;

    private const string WelcomePrefKey = "HasSeenWelcome";
    private const string WelcomePhrase = "Welcome to the future of voice.";

    private const string FirstRunHint = "Type a line \u2014 anything works.";
    private const string ReturningHint = "Try a different style for a new delivery.";

    private string _text = "";
    private string? _selectedPreset;
    private string? _selectedVoice;
    private bool _isSpeaking;
    private bool _isOffline;
    private string _status = "Ready";
    private Color _statusColor = Colors.Gray;
    private string _hintText = FirstRunHint;
    private bool _showHint = true;
    private bool _showWelcome;
    private bool _showRetryHint;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SoundboardViewModel(ISoundboardClient client, IAudioPlayer player)
    {
        _client = client;
        _player = player;

        _showWelcome = !Preferences.Get(WelcomePrefKey, false);

        if (_showWelcome)
        {
            _text = WelcomePhrase;   // pre-fill so Speak is enabled immediately
            _showHint = false;       // don't show hint over pre-filled text
            _hintText = FirstRunHint;
        }
        else
        {
            _hintText = ReturningHint;
        }

        SpeakCommand = new Command(async () => await SpeakAsync(), () => CanSpeak);
        StopCommand = new Command(() => Stop(), () => IsSpeaking);
        WelcomeSpeakCommand = new Command(async () => await WelcomeSpeakAsync());
        DismissWelcomeCommand = new Command(DismissWelcome);
        RetryConnectCommand = new Command(async () => await LoadAsync(), () => IsOffline);
    }

    public ICommand SpeakCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand WelcomeSpeakCommand { get; }
    public ICommand DismissWelcomeCommand { get; }
    public ICommand RetryConnectCommand { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (SetField(ref _text, value))
            {
                ShowHint = string.IsNullOrWhiteSpace(value);
                RefreshCommands();
            }
        }
    }

    public string? SelectedPreset
    {
        get => _selectedPreset;
        set => SetField(ref _selectedPreset, value);
    }

    public string? SelectedVoice
    {
        get => _selectedVoice;
        set => SetField(ref _selectedVoice, value);
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        private set
        {
            if (SetField(ref _isSpeaking, value))
                RefreshCommands();
        }
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public Color StatusColor
    {
        get => _statusColor;
        private set => SetField(ref _statusColor, value);
    }

    public bool ShowRetryHint
    {
        get => _showRetryHint;
        private set => SetField(ref _showRetryHint, value);
    }

    public ObservableCollection<string> Presets { get; } = [];
    public ObservableCollection<string> Voices { get; } = [];

    public string HintText
    {
        get => _hintText;
        private set => SetField(ref _hintText, value);
    }

    public bool ShowHint
    {
        get => _showHint;
        private set => SetField(ref _showHint, value);
    }

    public bool ShowWelcome
    {
        get => _showWelcome;
        private set => SetField(ref _showWelcome, value);
    }

    public bool IsOffline
    {
        get => _isOffline;
        private set
        {
            if (SetField(ref _isOffline, value))
                (RetryConnectCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool CanSpeak => !IsSpeaking && !string.IsNullOrWhiteSpace(Text);
    public bool ShowSpeakHint => !CanSpeak && !IsSpeaking;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            IsOffline = false;
            SetStatus("Connecting\u2026", Colors.Gray);
            var health = await _client.GetHealthAsync(ct);
            SetStatus("Connected", Colors.Green);

            var presets = await _client.GetPresetsAsync(ct);
            Presets.Clear();
            foreach (var p in presets) Presets.Add(p);
            if (Presets.Count > 0) SelectedPreset = Presets[0];

            var voices = await _client.GetVoicesAsync(ct);
            Voices.Clear();
            foreach (var v in voices) Voices.Add(v);
            if (Voices.Count > 0) SelectedVoice = Voices[0];
        }
        catch (Exception)
        {
            IsOffline = true;
            SetStatus("Offline", Colors.Red, retryHint: true);
        }
    }

    public async Task SpeakAsync()
    {
        if (!CanSpeak) return;

        _speakCts?.Cancel();
        _speakCts = new CancellationTokenSource();
        var ct = _speakCts.Token;

        IsSpeaking = true;
        SetStatus("Streaming live\u2026", Color.FromArgb("#2196F3"));

        try
        {
            _player.Start(sampleRate: 24000);

            var firstChunk = true;
            var progress = new Progress<AudioChunk>(chunk =>
            {
                _player.Feed(chunk);
                if (firstChunk)
                {
                    firstChunk = false;
                    SetStatus("Streaming live\u2026", Color.FromArgb("#2196F3"));
                }
            });

            await _client.SpeakAsync(
                new SpeakRequest(Text, SelectedPreset ?? "narrator", SelectedVoice ?? "default"),
                progress,
                ct);

            SetStatus("Done", Colors.Green);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Stopped", Colors.Gray);
        }
        catch (Exception)
        {
            SetStatus("Something didn\u2019t work", Colors.Orange);
        }
        finally
        {
            IsSpeaking = false;
        }
    }

    public async Task WelcomeSpeakAsync()
    {
        DismissWelcome();
        await SpeakAsync();
    }

    public void Stop()
    {
        _speakCts?.Cancel();
        _player.Stop();
        IsSpeaking = false;
        SetStatus("Stopped", Colors.Gray);
    }

    public void Dispose()
    {
        _speakCts?.Cancel();
        _speakCts?.Dispose();
        _player.Dispose();
    }

    private void SetStatus(string text, Color color, bool retryHint = false)
    {
        Status = text;
        StatusColor = color;
        ShowRetryHint = retryHint;
    }

    private void DismissWelcome()
    {
        ShowWelcome = false;
        Preferences.Set(WelcomePrefKey, true);
    }

    private void RefreshCommands()
    {
        (SpeakCommand as Command)?.ChangeCanExecute();
        (StopCommand as Command)?.ChangeCanExecute();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSpeak)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowSpeakHint)));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
