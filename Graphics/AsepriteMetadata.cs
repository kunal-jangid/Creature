using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Creature.Graphics;

public record AsepriteMetadata(
    [property: JsonPropertyName("frames")] Dictionary<string, AsepriteFrameEntry> Frames,
    [property: JsonPropertyName("meta")] AsepriteMeta? Meta
);

public record AsepriteFrameEntry(
    [property: JsonPropertyName("frame")] AsepriteRect Frame,
    [property: JsonPropertyName("duration")] int Duration
);

public record AsepriteRect(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H
);

public record AsepriteMeta(
    [property: JsonPropertyName("size")] AsepriteSize? Size
);

public record AsepriteSize(
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H
);
