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
    private CancellationTokenSource? _statusResetCts;
    private CancellationTokenSource? _reconnectCts;
    private static readonly int[] ReconnectDelaysMs = [1000, 2000, 5000, 10000];

    private const string WelcomePrefKey = "HasSeenWelcome";
    private const string LastTextKey = "LastText";
    private const string LastStyleKey = "LastStyle";
    private const string LastVoiceKey = "LastVoice";
    private const string WelcomePhrase = "Welcome to the future of voice.";

    private const string FirstRunHint = "Type a line \u2014 anything works.";
    private const string ReturningHint = "Try a different style for a new delivery.";

    private static readonly string[] ExamplePhrases =
    [
        "Welcome to the future of voice.",
        "Make it dramatic.",
        "Say this like you mean it."
    ];

    private string _text = "";
    private string? _selectedPreset;
    private string? _selectedVoice;
    private bool _isSpeaking;
    private bool _isOffline;
    private bool _isConnected;
    private string _status = "Ready";
    private Color _statusColor = Colors.Gray;
    private string _hintText = FirstRunHint;
    private bool _showHint = true;
    private bool _showWelcome;
    private bool _showRetryHint;
    private bool _pickersLoading = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SoundboardViewModel(ISoundboardClient client, IAudioPlayer player)
    {
        _client = client;
        _player = player;

        _showWelcome = !Preferences.Get(WelcomePrefKey, false);

        if (_showWelcome)
        {
            _text = WelcomePhrase;
            _showHint = false;
            _hintText = FirstRunHint;
        }
        else
        {
            _text = Preferences.Get(LastTextKey, "");
            _showHint = string.IsNullOrWhiteSpace(_text);
            _hintText = ReturningHint;
        }

        SpeakCommand = new Command(async () => await SpeakAsync(), () => CanSpeak);
        StopCommand = new Command(() => Stop(), () => IsSpeaking);
        WelcomeSpeakCommand = new Command(async () => await WelcomeSpeakAsync());
        DismissWelcomeCommand = new Command(DismissWelcome);
        RetryConnectCommand = new Command(async () => { _reconnectCts?.Cancel(); await LoadAsync(); }, () => IsOffline);
        SetExampleCommand = new Command<string>(phrase => Text = phrase);
    }

    public ICommand SpeakCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand WelcomeSpeakCommand { get; }
    public ICommand DismissWelcomeCommand { get; }
    public ICommand RetryConnectCommand { get; }
    public ICommand SetExampleCommand { get; }

    public IReadOnlyList<string> Examples => ExamplePhrases;

    public string Text
    {
        get => _text;
        set
        {
            if (SetField(ref _text, value))
            {
                ShowHint = string.IsNullOrWhiteSpace(value);
                Preferences.Set(LastTextKey, value ?? "");
                RefreshCommands();
            }
        }
    }

    public string? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetField(ref _selectedPreset, value) && value is not null)
                Preferences.Set(LastStyleKey, value);
        }
    }

    public string? SelectedVoice
    {
        get => _selectedVoice;
        set
        {
            if (SetField(ref _selectedVoice, value) && value is not null)
                Preferences.Set(LastVoiceKey, value);
        }
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

    public bool PickersLoading
    {
        get => _pickersLoading;
        private set => SetField(ref _pickersLoading, value);
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

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetField(ref _isConnected, value))
                RefreshCommands();
        }
    }

    public bool CanSpeak => !IsSpeaking && !string.IsNullOrWhiteSpace(Text) && IsConnected;
    public bool ShowExamples => !IsSpeaking && string.IsNullOrWhiteSpace(Text);

    public string SpeakHintText =>
        IsOffline ? "Disconnected — tap the status above to reconnect." :
        !IsConnected ? "Connecting to voice engine\u2026" :
        string.IsNullOrWhiteSpace(Text) ? "Type a line to hear it spoken." :
        "";

    public bool ShowSpeakHint => !CanSpeak && !IsSpeaking;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var reconnectToken = _reconnectCts.Token;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                IsOffline = false;
                IsConnected = false;
                PickersLoading = true;
                SetStatus(attempt == 0 ? "Connecting\u2026" : "Reconnecting\u2026", Colors.Gray);

                var health = await _client.GetHealthAsync(ct);
                IsConnected = true;
                SetStatus("Connected", Colors.Green);

                var presets = await _client.GetPresetsAsync(ct);
                Presets.Clear();
                foreach (var p in presets) Presets.Add(p);
                var savedStyle = Preferences.Get(LastStyleKey, "");
                SelectedPreset = (!string.IsNullOrEmpty(savedStyle) && Presets.Contains(savedStyle))
                    ? savedStyle
                    : PickBestDefault(Presets, "expressive", "conversational", "narrator");

                var voices = await _client.GetVoicesAsync(ct);
                Voices.Clear();
                foreach (var v in voices) Voices.Add(v);
                var savedVoice = Preferences.Get(LastVoiceKey, "");
                SelectedVoice = (!string.IsNullOrEmpty(savedVoice) && Voices.Contains(savedVoice))
                    ? savedVoice
                    : PickBestDefault(Voices, "default", "neutral");

                PickersLoading = false;
                return; // connected successfully
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                IsOffline = true;
                IsConnected = false;
                var delayMs = ReconnectDelaysMs[Math.Min(attempt, ReconnectDelaysMs.Length - 1)];
                SetStatus("Disconnected", Colors.Red, retryHint: true);

                try
                {
                    await Task.Delay(delayMs, reconnectToken);
                }
                catch (OperationCanceledException)
                {
                    return; // manual retry or dispose cancelled the backoff
                }
            }
            catch
            {
                IsOffline = true;
                IsConnected = false;
                SetStatus("Disconnected", Colors.Red, retryHint: true);
                return;
            }
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

            SetStatusTransient("Done", Colors.Green, 1500);
        }
        catch (OperationCanceledException)
        {
            SetStatusTransient("Stopped", Colors.Gray, 1000);
        }
        catch (Exception)
        {
            SetStatusTransient("Something didn\u2019t work", Colors.Orange, 3000);
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
        SetStatusTransient("Stopped", Colors.Gray, 1000);
    }

    public void Dispose()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _statusResetCts?.Cancel();
        _statusResetCts?.Dispose();
        _speakCts?.Cancel();
        _speakCts?.Dispose();
        _player.Dispose();
    }

    private void SetStatus(string text, Color color, bool retryHint = false)
    {
        _statusResetCts?.Cancel();
        Status = text;
        StatusColor = color;
        ShowRetryHint = retryHint;
    }

    private void SetStatusTransient(string text, Color color, int delayMs = 1500)
    {
        SetStatus(text, color);
        _statusResetCts?.Cancel();
        _statusResetCts = new CancellationTokenSource();
        var token = _statusResetCts.Token;
        _ = Task.Delay(delayMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested && IsConnected)
                SetStatus("Connected", Colors.Green);
        }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private static string? PickBestDefault(ObservableCollection<string> items, params string[] preferred)
    {
        if (items.Count == 0) return null;
        foreach (var pref in preferred)
        {
            var match = items.FirstOrDefault(i => i.Contains(pref, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }
        return items[0];
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeakHintText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowExamples)));
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
