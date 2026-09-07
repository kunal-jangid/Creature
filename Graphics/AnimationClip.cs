using System.Windows.Media.Imaging;

namespace Creature.Graphics;

public class AnimationClip
{
    public string Name { get; }
    public BitmapSource[] Frames { get; }
    public int FrameDurationMs { get; }
    public bool Loop { get; }

    public AnimationClip(string name, BitmapSource[] frames, int frameDurationMs, bool loop = true)
    {
        Name = name;
        Frames = frames;
        FrameDurationMs = frameDurationMs > 0 ? frameDurationMs : 100;
        Loop = loop;
    }
}
