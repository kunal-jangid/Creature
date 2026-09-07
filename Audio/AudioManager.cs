using System.Threading;
using System.Threading.Tasks;

namespace Creature.Audio;

public class AudioManager
{
    public bool IsSoundEnabled { get; set; } = true;
    public int MaxVoices { get; set; } = 2;
    private int _activeVoices;

    public int ActiveVoiceCount => _activeVoices;

    public bool PlaySound(string soundName)
    {
        if (!IsSoundEnabled || _activeVoices >= MaxVoices)
            return false;

        Interlocked.Increment(ref _activeVoices);
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            Interlocked.Decrement(ref _activeVoices);
        });

        return true;
    }
}
