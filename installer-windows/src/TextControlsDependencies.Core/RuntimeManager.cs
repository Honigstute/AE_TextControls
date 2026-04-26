using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace TextControlsDependencies.Core;

public sealed class RuntimeManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly HttpClient httpClient;
    private readonly Assembly resourceAssembly;

    public RuntimeManager(HttpClient? httpClient = null, Assembly? resourceAssembly = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
        if (!this.httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TextControlsDependencies/1.0");
        }

        this.resourceAssembly = resourceAssembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
    }

    public async Task<RuntimeInspection> InspectAsync()
    {
        var manifest = ReadManifest();
        var helperPath = manifest?.HelperPath ?? RuntimeConstants.HelperPath;
        var whisperCliPath = manifest?.WhisperCliPath ?? RuntimeConstants.WhisperCliPath;
        var ffmpegPath = manifest?.FfmpegPath ?? RuntimeConstants.FfmpegPath;
        var modelPath = manifest?.ModelPath ?? RuntimeConstants.ModelPath;

        var manifestReadable = manifest is not null &&
            manifest.SchemaVersion == RuntimeConstants.SchemaVersion &&
            manifest.RuntimeVersion == RuntimeConstants.RuntimeVersion &&
            manifest.HelperVersion == RuntimeConstants.HelperVersion;
        var helperHealthy = manifestReadable && await IsHelperHealthyAsync(helperPath).ConfigureAwait(false);
        var whisperCliExists = File.Exists(whisperCliPath);
        var ffmpegHealthy = await IsFfmpegHealthyAsync(ffmpegPath).ConfigureAwait(false);
        var modelChecksumMatches = await HasExpectedChecksumAsync(modelPath, RuntimeConstants.ModelSha256).ConfigureAwait(false);

        var anyRuntimeFileExists = Directory.Exists(RuntimeConstants.RuntimeRoot) ||
            File.Exists(RuntimeConstants.ManifestPath) ||
            File.Exists(helperPath) ||
            File.Exists(whisperCliPath) ||
            File.Exists(ffmpegPath) ||
            File.Exists(modelPath);

        var state = manifestReadable && helperHealthy && whisperCliExists && ffmpegHealthy && modelChecksumMatches
            ? RuntimeInstallState.Ready
            : anyRuntimeFileExists
                ? RuntimeInstallState.Repair
                : RuntimeInstallState.Missing;

        return new RuntimeInspection
        {
            RuntimeRoot = RuntimeConstants.RuntimeRoot,
            ManifestPath = RuntimeConstants.ManifestPath,
            HelperPath = helperPath,
            WhisperCliPath = whisperCliPath,
            FfmpegPath = ffmpegPath,
            ModelPath = modelPath,
            InstallState = state,
            ManifestReadable = manifestReadable,
            HelperHealthy = helperHealthy,
            WhisperCliExists = whisperCliExists,
            FfmpegHealthy = ffmpegHealthy,
            ModelChecksumMatches = modelChecksumMatches
        };
    }

    public async Task<RuntimeInspection> InstallOrRepairAsync(Action<RuntimeProgress>? progress = null, Action<string>? log = null)
    {
        progress ??= _ => { };
        log ??= _ => { };

        progress(new RuntimeProgress(0.05, "Preparing runtime folders"));
        Directory.CreateDirectory(RuntimeConstants.BinDirectory);
        Directory.CreateDirectory(RuntimeConstants.ModelsDirectory);
        Directory.CreateDirectory(RuntimeConstants.NoticesDirectory);

        progress(new RuntimeProgress(0.15, "Installing local engine"));
        await DownloadAndExtractLocalEngineAsync(RuntimeConstants.BinDirectory).ConfigureAwait(false);
        log("Local engine files installed");

        progress(new RuntimeProgress(0.35, "Checking Whisper model"));
        if (!await HasExpectedChecksumAsync(RuntimeConstants.ModelPath, RuntimeConstants.ModelSha256).ConfigureAwait(false))
        {
            progress(new RuntimeProgress(0.45, "Downloading Whisper model"));
            await DownloadVerifiedFileAsync(
                RuntimeConstants.ModelDownloadUrl,
                RuntimeConstants.ModelPath,
                RuntimeConstants.ModelSha256
            ).ConfigureAwait(false);
            log("Whisper model downloaded");
        }
        else
        {
            log("Whisper model already verified");
        }

        progress(new RuntimeProgress(0.70, "Checking FFmpeg"));
        if (!await IsFfmpegHealthyAsync(RuntimeConstants.FfmpegPath).ConfigureAwait(false))
        {
            progress(new RuntimeProgress(0.78, "Downloading FFmpeg"));
            await DownloadAndExtractFfmpegAsync(RuntimeConstants.FfmpegPath).ConfigureAwait(false);
            log("FFmpeg downloaded");
        }
        else
        {
            log("FFmpeg already verified");
        }

        progress(new RuntimeProgress(0.90, "Writing notices"));
        await ExtractOptionalPayloadNoticesAsync().ConfigureAwait(false);

        progress(new RuntimeProgress(0.96, "Writing manifest"));
        await File.WriteAllTextAsync(
            RuntimeConstants.ManifestPath,
            JsonSerializer.Serialize(RuntimeManifest.CreateInstalled(), JsonOptions)
        ).ConfigureAwait(false);

        var inspection = await InspectAsync().ConfigureAwait(false);
        progress(new RuntimeProgress(inspection.InstallState == RuntimeInstallState.Ready ? 1.0 : 0.98, inspection.Summary));
        return inspection;
    }

    public void Uninstall(Action<string>? log = null)
    {
        log ??= _ => { };

        if (!Directory.Exists(RuntimeConstants.RuntimeRoot))
        {
            log("Runtime folder already removed");
            return;
        }

        Directory.Delete(RuntimeConstants.RuntimeRoot, recursive: true);
        log("Runtime folder removed");
    }

    private RuntimeManifest? ReadManifest()
    {
        try
        {
            if (!File.Exists(RuntimeConstants.ManifestPath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<RuntimeManifest>(File.ReadAllText(RuntimeConstants.ManifestPath));
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> IsHelperHealthyAsync(string helperPath)
    {
        if (!File.Exists(helperPath))
        {
            return false;
        }

        var result = await ProcessRunner.RunAsync(helperPath, ["check", "--json"], TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        return result.Succeeded &&
            result.StandardOutput.Contains("\"ok\"", StringComparison.OrdinalIgnoreCase) &&
            result.StandardOutput.Contains(RuntimeConstants.HelperVersion, StringComparison.Ordinal);
    }

    private static async Task<bool> IsFfmpegHealthyAsync(string ffmpegPath)
    {
        if (!File.Exists(ffmpegPath))
        {
            return false;
        }

        var result = await ProcessRunner.RunAsync(ffmpegPath, ["-version"], TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        var output = result.StandardOutput + result.StandardError;
        return result.Succeeded && output.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> HasExpectedChecksumAsync(string path, string expectedSha256)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            return string.Equals(await FileHash.Sha256Async(path).ConfigureAwait(false), expectedSha256, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task DownloadVerifiedFileAsync(string url, string destinationPath, string expectedSha256)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var tempPath = destinationPath + ".download";

        await using (var input = await httpClient.GetStreamAsync(url).ConfigureAwait(false))
        await using (var output = File.Create(tempPath))
        {
            await input.CopyToAsync(output).ConfigureAwait(false);
        }

        var actualSha = await FileHash.Sha256Async(tempPath).ConfigureAwait(false);
        if (!string.Equals(actualSha, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempPath);
            throw new InvalidOperationException($"Checksum mismatch for {Path.GetFileName(destinationPath)}. Expected {expectedSha256}, got {actualSha}.");
        }

        MoveReplacing(tempPath, destinationPath);
    }

    private async Task DownloadAndExtractFfmpegAsync(string destinationPath)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), "text-controls-ffmpeg-" + Guid.NewGuid().ToString("N") + ".zip");
        var extractRoot = Path.Combine(Path.GetTempPath(), "text-controls-ffmpeg-" + Guid.NewGuid().ToString("N"));

        try
        {
            await DownloadVerifiedFileAsync(RuntimeConstants.FfmpegDownloadUrl, zipPath, RuntimeConstants.FfmpegZipSha256).ConfigureAwait(false);
            ZipFile.ExtractToDirectory(zipPath, extractRoot);
            var ffmpegSource = Directory
                .EnumerateFiles(extractRoot, RuntimeConstants.FfmpegExecutableName, SearchOption.AllDirectories)
                .FirstOrDefault(path => path.Replace('\\', '/').Contains("/bin/", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(ffmpegSource))
            {
                throw new InvalidOperationException("FFmpeg executable was not found inside the downloaded archive.");
            }

            MoveReplacing(ffmpegSource, destinationPath);
        }
        finally
        {
            TryDeleteFile(zipPath);
            TryDeleteDirectory(extractRoot);
        }
    }

    private async Task DownloadAndExtractLocalEngineAsync(string destinationDirectory)
    {
        if (!RuntimeConstants.HasLocalEngineDownload)
        {
            throw new InvalidOperationException("Windows local engine download URL was not configured in this build.");
        }

        var zipPath = Path.Combine(Path.GetTempPath(), "text-controls-local-engine-" + Guid.NewGuid().ToString("N") + ".zip");
        var extractRoot = Path.Combine(Path.GetTempPath(), "text-controls-local-engine-" + Guid.NewGuid().ToString("N"));

        try
        {
            await DownloadVerifiedFileAsync(RuntimeConstants.LocalEngineDownloadUrl, zipPath, RuntimeConstants.LocalEngineZipSha256).ConfigureAwait(false);
            ZipFile.ExtractToDirectory(zipPath, extractRoot);

            Directory.CreateDirectory(destinationDirectory);
            foreach (var filePath in Directory.EnumerateFiles(extractRoot, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(extractRoot, filePath);
                MoveReplacing(filePath, Path.Combine(destinationDirectory, relativePath));
            }
        }
        finally
        {
            TryDeleteFile(zipPath);
            TryDeleteDirectory(extractRoot);
        }
    }

    private async Task ExtractPayloadResourceAsync(string resourceName, string destinationPath)
    {
        await using var stream = resourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Installer payload is missing {resourceName}.");

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var tempPath = destinationPath + ".tmp";
        await using (var output = File.Create(tempPath))
        {
            await stream.CopyToAsync(output).ConfigureAwait(false);
        }

        MoveReplacing(tempPath, destinationPath);
    }

    private async Task ExtractPayloadDirectoryAsync(string resourcePrefix, string destinationDirectory)
    {
        var resourceNames = resourceAssembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal))
            .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new InvalidOperationException($"Installer payload is missing {resourcePrefix}.");
        }

        foreach (var resourceName in resourceNames)
        {
            var relative = resourceName[resourcePrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(relative))
            {
                continue;
            }

            await ExtractPayloadResourceAsync(resourceName, Path.Combine(destinationDirectory, relative)).ConfigureAwait(false);
        }
    }

    private async Task ExtractOptionalPayloadNoticesAsync()
    {
        foreach (var resourceName in resourceAssembly.GetManifestResourceNames().Where(name => name.StartsWith("payload/THIRD_PARTY_NOTICES/", StringComparison.Ordinal)))
        {
            var relative = resourceName["payload/THIRD_PARTY_NOTICES/".Length..].Replace('/', Path.DirectorySeparatorChar);
            await ExtractPayloadResourceAsync(resourceName, Path.Combine(RuntimeConstants.NoticesDirectory, relative)).ConfigureAwait(false);
        }
    }

    private static void MoveReplacing(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(sourcePath, destinationPath);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch { }
    }
}
