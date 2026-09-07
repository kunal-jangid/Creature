using System.Collections.Generic;

namespace Creature.Settings;

public class UserSettings
{
    public double GlobalScale { get; set; } = 1.0;
    public bool IsSoundEnabled { get; set; } = true;
    public bool IsPaused { get; set; } = false;
    public List<string> DisabledPetIds { get; set; } = new();
}
