using System.Text.Json.Serialization;

namespace TextControlsDependencies.Core;

public sealed class RuntimeManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = RuntimeConstants.SchemaVersion;

    [JsonPropertyName("runtimeVersion")]
    public string RuntimeVersion { get; set; } = RuntimeConstants.RuntimeVersion;

    [JsonPropertyName("helperVersion")]
    public string HelperVersion { get; set; } = RuntimeConstants.HelperVersion;

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = RuntimeConstants.ModelId;

    [JsonPropertyName("modelPath")]
    public string ModelPath { get; set; } = RuntimeConstants.ModelPath;

    [JsonPropertyName("modelSHA256")]
    public string ModelSha256 { get; set; } = RuntimeConstants.ModelSha256;

    [JsonPropertyName("helperPath")]
    public string HelperPath { get; set; } = RuntimeConstants.HelperPath;

    [JsonPropertyName("whisperCliPath")]
    public string WhisperCliPath { get; set; } = RuntimeConstants.WhisperCliPath;

    [JsonPropertyName("ffmpegPath")]
    public string FfmpegPath { get; set; } = RuntimeConstants.FfmpegPath;

    [JsonPropertyName("ffmpegSHA256")]
    public string FfmpegSha256 { get; set; } = RuntimeConstants.FfmpegZipSha256;

    [JsonPropertyName("ffmpegSourceURL")]
    public string FfmpegSourceUrl { get; set; } = RuntimeConstants.FfmpegDownloadUrl;

    [JsonPropertyName("installedAt")]
    public string InstalledAt { get; set; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("sourceURL")]
    public string SourceUrl { get; set; } = RuntimeConstants.ModelDownloadUrl;

    public static RuntimeManifest CreateInstalled() => new();
}
