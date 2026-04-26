namespace TextControlsDependencies.Core;

public static class RuntimeConstants
{
    public const int SchemaVersion = 1;
    public const string RuntimeVersion = "1.0.0";
    public const string HelperVersion = "1.0.0";
    public const string RuntimeFolderName = "Text Controls Local";
    public const string HelperExecutableName = "tc-whisper-helper.exe";
    public const string WhisperCliExecutableName = "whisper-cli.exe";
    public const string FfmpegExecutableName = "ffmpeg.exe";
    public const string ModelId = "multilingual-base";
    public const string ModelFileName = "ggml-base.bin";
    public const string WhisperCppVersion = "v1.7.5";

    public const string ModelDownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin";
    public const string ModelSha256 = "60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe";

    public const string FfmpegDownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-04-25-14-00/ffmpeg-N-124094-g45fe315cf0-win64-lgpl.zip";
    public const string FfmpegZipSha256 = "d068411a6738490bbc87c90cf54fd0e2aa7fe03ceef7f8da548f5080b0ae8d79";

    // This URL is replaced by the GitHub workflow before publishing the public
    // bootstrapper. It points to a small zip that contains tc-whisper-helper.exe,
    // whisper-cli.exe, and whisper.cpp DLLs built by the same workflow.
    public const string LocalEngineDownloadUrl = "__WINDOWS_LOCAL_ENGINE_URL__";
    public const string LocalEngineZipSha256 = "__WINDOWS_LOCAL_ENGINE_SHA256__";

    public static string RuntimeRoot =>
        Path.Combine(GetLocalAppDataRoot(), RuntimeFolderName);

    public static string BinDirectory =>
        Path.Combine(RuntimeRoot, "bin");

    public static string ModelsDirectory =>
        Path.Combine(RuntimeRoot, "models");

    public static string NoticesDirectory =>
        Path.Combine(RuntimeRoot, "THIRD_PARTY_NOTICES");

    public static string ManifestPath =>
        Path.Combine(RuntimeRoot, "install-manifest.json");

    public static string HelperPath =>
        Path.Combine(BinDirectory, HelperExecutableName);

    public static string WhisperCliPath =>
        Path.Combine(BinDirectory, WhisperCliExecutableName);

    public static string FfmpegPath =>
        Path.Combine(BinDirectory, FfmpegExecutableName);

    public static string ModelPath =>
        Path.Combine(ModelsDirectory, ModelFileName);

    public static bool HasLocalEngineDownload =>
        !LocalEngineDownloadUrl.StartsWith("__", StringComparison.Ordinal) &&
        !LocalEngineZipSha256.StartsWith("__", StringComparison.Ordinal);

    private static string GetLocalAppDataRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            return localAppData;
        }

        var env = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData",
            "Local"
        );
    }
}
