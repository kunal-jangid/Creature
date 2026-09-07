using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Creature.Graphics;

public class SpriteManager
{
    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.OrdinalIgnoreCase);

    public AnimationClip LoadAnimation(string name, string jsonPath, string pngPath, bool loop = true)
    {
        if (_clips.TryGetValue(name, out var cached))
            return cached;

        var jsonText = File.ReadAllText(jsonPath);
        var metadata = JsonSerializer.Deserialize<AsepriteMetadata>(jsonText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException($"Failed to deserialize Aseprite JSON: {jsonPath}");

        var masterBitmap = new BitmapImage();
        masterBitmap.BeginInit();
        masterBitmap.UriSource = new Uri(Path.GetFullPath(pngPath), UriKind.Absolute);
        masterBitmap.CacheOption = BitmapCacheOption.OnLoad;
        masterBitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        masterBitmap.EndInit();
        masterBitmap.Freeze();

        var frames = new List<BitmapSource>();
        var frameDuration = 100;

        foreach (var (_, entry) in metadata.Frames)
        {
            frameDuration = entry.Duration;
            var rect = new Int32Rect(entry.Frame.X, entry.Frame.Y, entry.Frame.W, entry.Frame.H);
            var cropped = new CroppedBitmap(masterBitmap, rect);
            cropped.Freeze();
            frames.Add(cropped);
        }

        var clip = new AnimationClip(name, frames.ToArray(), frameDuration, loop);
        _clips[name] = clip;
        return clip;
    }

    public AnimationClip? GetClip(string name)
    {
        _clips.TryGetValue(name, out var clip);
        return clip;
    }
}
