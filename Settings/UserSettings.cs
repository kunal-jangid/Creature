using System.Collections.Generic;

namespace Creature.Settings;

public class UserSettings
{
    public double GlobalScale { get; set; } = 2.0;
    public bool ShowPetNames { get; set; } = true;
    public bool IsSoundEnabled { get; set; } = true;
    public bool IsPaused { get; set; } = false;
    public List<PetProfile> Pets { get; set; } = new();
    public List<string> DisabledPetIds { get; set; } = new();
}
