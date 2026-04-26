namespace TextControlsDependencies.Core;

public enum RuntimeInstallState
{
    Missing,
    Repair,
    Ready
}

public sealed class RuntimeInspection
{
    public string RuntimeRoot { get; init; } = RuntimeConstants.RuntimeRoot;
    public string ManifestPath { get; init; } = RuntimeConstants.ManifestPath;
    public string HelperPath { get; init; } = RuntimeConstants.HelperPath;
    public string WhisperCliPath { get; init; } = RuntimeConstants.WhisperCliPath;
    public string FfmpegPath { get; init; } = RuntimeConstants.FfmpegPath;
    public string ModelPath { get; init; } = RuntimeConstants.ModelPath;
    public RuntimeInstallState InstallState { get; init; } = RuntimeInstallState.Missing;
    public bool ManifestReadable { get; init; }
    public bool HelperHealthy { get; init; }
    public bool WhisperCliExists { get; init; }
    public bool FfmpegHealthy { get; init; }
    public bool ModelChecksumMatches { get; init; }
    public string Summary => InstallState == RuntimeInstallState.Ready ? "Ready" : "Missing";
}
