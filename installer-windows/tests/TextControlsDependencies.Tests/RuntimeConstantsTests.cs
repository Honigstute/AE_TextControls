using TextControlsDependencies.Core;
using Xunit;

namespace TextControlsDependencies.Tests;

public sealed class RuntimeConstantsTests
{
    [Fact]
    public void RuntimeRootIsUserScopedLocalAppData()
    {
        Assert.Contains("Text Controls Local", RuntimeConstants.RuntimeRoot);
        Assert.EndsWith(Path.Combine("Text Controls Local", "bin", "tc-whisper-helper.exe"), RuntimeConstants.HelperPath);
        Assert.EndsWith(Path.Combine("Text Controls Local", "bin", "ffmpeg.exe"), RuntimeConstants.FfmpegPath);
        Assert.EndsWith(Path.Combine("Text Controls Local", "models", "ggml-base.bin"), RuntimeConstants.ModelPath);
    }

    [Fact]
    public void VersionsAreExplicit()
    {
        Assert.Equal(1, RuntimeConstants.SchemaVersion);
        Assert.False(string.IsNullOrWhiteSpace(RuntimeConstants.RuntimeVersion));
        Assert.False(string.IsNullOrWhiteSpace(RuntimeConstants.HelperVersion));
    }
}
