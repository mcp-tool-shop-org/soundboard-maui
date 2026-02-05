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

    private string _text = "";
    private string? _selectedPreset;
    private string? _selectedVoice;
    private bool _isSpeaking;
    private string _status = "Ready";
    private string _hintText = "Type anything above, pick a style, and hit Speak.";
    private bool _showHint = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SoundboardViewModel(ISoundboardClient client, IAudioPlayer player)
    {
        _client = client;
        _player = player;

        SpeakCommand = new Command(async () => await SpeakAsync(), () => CanSpeak);
        StopCommand = new Command(() => Stop(), () => IsSpeaking);
    }

    public ICommand SpeakCommand { get; }
    public ICommand StopCommand { get; }

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

    public bool CanSpeak => !IsSpeaking && !string.IsNullOrWhiteSpace(Text);

    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            Status = "Connecting...";
            var health = await _client.GetHealthAsync(ct);
            Status = $"\u25cf Engine v{health.EngineVersion} (API {health.ApiVersion})";

            var presets = await _client.GetPresetsAsync(ct);
            Presets.Clear();
            foreach (var p in presets) Presets.Add(p);
            if (Presets.Count > 0) SelectedPreset = Presets[0];

            var voices = await _client.GetVoicesAsync(ct);
            Voices.Clear();
            foreach (var v in voices) Voices.Add(v);
            if (Voices.Count > 0) SelectedVoice = Voices[0];
        }
        catch (Exception ex)
        {
            Status = $"\u25cb Offline: {ex.Message}";
        }
    }

    public async Task SpeakAsync()
    {
        if (!CanSpeak) return;

        _speakCts?.Cancel();
        _speakCts = new CancellationTokenSource();
        var ct = _speakCts.Token;

        IsSpeaking = true;
        Status = "Speaking...";

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
                    Status = "Playing...";
                }
            });

            await _client.SpeakAsync(
                new SpeakRequest(Text, SelectedPreset ?? "assistant", SelectedVoice ?? "default"),
                progress,
                ct);

            Status = $"Done ({_player.BufferedChunks} chunks)";
        }
        catch (OperationCanceledException)
        {
            Status = "Stopped";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsSpeaking = false;
        }
    }

    public void Stop()
    {
        _speakCts?.Cancel();
        _player.Stop();
        IsSpeaking = false;
        Status = "Stopped";
    }

    public void Dispose()
    {
        _speakCts?.Cancel();
        _speakCts?.Dispose();
        _player.Dispose();
    }

    private void RefreshCommands()
    {
        (SpeakCommand as Command)?.ChangeCanExecute();
        (StopCommand as Command)?.ChangeCanExecute();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSpeak)));
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
