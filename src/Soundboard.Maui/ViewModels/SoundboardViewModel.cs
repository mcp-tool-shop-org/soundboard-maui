using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    private string _selectedPreset = "assistant";
    private string _selectedVoice = "default";
    private bool _isSpeaking;
    private string _status = "Ready";
    private IReadOnlyList<string> _presets = [];
    private IReadOnlyList<string> _voices = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public SoundboardViewModel(ISoundboardClient client, IAudioPlayer player)
    {
        _client = client;
        _player = player;
    }

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public string SelectedPreset
    {
        get => _selectedPreset;
        set => SetField(ref _selectedPreset, value);
    }

    public string SelectedVoice
    {
        get => _selectedVoice;
        set => SetField(ref _selectedVoice, value);
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        private set => SetField(ref _isSpeaking, value);
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public IReadOnlyList<string> Presets
    {
        get => _presets;
        private set => SetField(ref _presets, value);
    }

    public IReadOnlyList<string> Voices
    {
        get => _voices;
        private set => SetField(ref _voices, value);
    }

    public bool CanSpeak => !IsSpeaking && !string.IsNullOrWhiteSpace(Text);

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Status = "Connecting...";
        var health = await _client.GetHealthAsync(ct);
        Status = $"Engine v{health.EngineVersion} (API {health.ApiVersion})";

        Presets = await _client.GetPresetsAsync(ct);
        Voices = await _client.GetVoicesAsync(ct);
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

            var progress = new Progress<AudioChunk>(chunk => _player.Feed(chunk));

            await _client.SpeakAsync(
                new SpeakRequest(Text, SelectedPreset, SelectedVoice),
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

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (name == nameof(Text) || name == nameof(IsSpeaking))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSpeak)));
        return true;
    }
}
