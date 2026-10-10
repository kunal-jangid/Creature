using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Creature.SystemIntegration;

public class UpdateManager : IDisposable
{
    public const string GitHubRepoOwner = "kunal-jangid";
    public const string GitHubRepoName = "Creature";
    public const string WingetPackageId = "KunalJangid.DesktopPets";
    public const string DefaultReleaseUrl = "https://github.com/kunal-jangid/Creature/releases";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private System.Threading.Timer? _periodicTimer;

    public UpdateManager(HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DesktopPets-App", "1.0"));
            _ownsHttpClient = true;
        }
    }

    public virtual Version GetCurrentVersion()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        return ver ?? new Version(1, 0, 0);
    }

    public static bool TryParseVersion(string? tag, out Version? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var clean = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(clean, out version);
    }

    public static bool IsNewerVersion(Version current, Version latest)
    {
        return latest > current;
    }

    public static (string Tag, string Url, string Notes) ParseGitHubReleaseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? "" : "";
        var url = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? DefaultReleaseUrl : DefaultReleaseUrl;
        var notes = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";

        return (tag, url, notes);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        var currentVersion = GetCurrentVersion();
        var apiUrl = $"https://api.github.com/repos/{GitHubRepoOwner}/{GitHubRepoName}/releases/latest";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DesktopPets-App", "1.0"));
            }

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(
                    IsUpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseNotes: string.Empty,
                    ReleaseUrl: DefaultReleaseUrl,
                    ErrorMessage: $"GitHub API returned status {(int)response.StatusCode}: {response.ReasonPhrase}"
                );
            }

            var json = await response.Content.ReadAsStringAsync();
            var (tag, url, notes) = ParseGitHubReleaseJson(json);

            if (!TryParseVersion(tag, out var latestVersion) || latestVersion == null)
            {
                return new UpdateCheckResult(
                    IsUpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseNotes: notes,
                    ReleaseUrl: url,
                    ErrorMessage: $"Could not parse release tag: {tag}"
                );
            }

            var isNewer = IsNewerVersion(currentVersion, latestVersion);
            return new UpdateCheckResult(
                IsUpdateAvailable: isNewer,
                CurrentVersion: currentVersion,
                LatestVersion: latestVersion,
                ReleaseNotes: notes,
                ReleaseUrl: url
            );
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                IsUpdateAvailable: false,
                CurrentVersion: currentVersion,
                LatestVersion: null,
                ReleaseNotes: string.Empty,
                ReleaseUrl: DefaultReleaseUrl,
                ErrorMessage: ex.Message
            );
        }
    }

    public static bool IsWingetInstalled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process == null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ApplyUpdateAsync(string releaseUrl)
    {
        if (IsWingetInstalled())
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"upgrade --id {WingetPackageId} --accept-source-agreements --accept-package-agreements",
                    UseShellExecute = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await Task.Delay(1000);
                    return true;
                }
            }
            catch
            {
                // Fallback to browser
            }
        }

        OpenBrowser(releaseUrl);
        return true;
    }

    public static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    public void StartPeriodicChecks(TimeSpan initialDelay, TimeSpan interval, Action<UpdateCheckResult> onUpdateFound)
    {
        _periodicTimer?.Dispose();
        _periodicTimer = new System.Threading.Timer(async _ =>
        {
            try
            {
                var result = await CheckForUpdatesAsync();
                if (result.IsUpdateAvailable)
                {
                    onUpdateFound(result);
                }
            }
            catch { }
        }, null, initialDelay, interval);
    }

    public void StopPeriodicChecks()
    {
        _periodicTimer?.Dispose();
        _periodicTimer = null;
    }

    public void Dispose()
    {
        StopPeriodicChecks();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
