using System;

namespace Creature.SystemIntegration;

public record UpdateCheckResult(
    bool IsUpdateAvailable,
    Version CurrentVersion,
    Version? LatestVersion,
    string ReleaseNotes,
    string ReleaseUrl,
    string? ErrorMessage = null
);
