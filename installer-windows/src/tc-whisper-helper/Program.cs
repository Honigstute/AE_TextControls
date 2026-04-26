using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TextControlsDependencies.Core;

namespace TcWhisperHelper;

internal static partial class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                return WriteFailure("unknown", UsageText);
            }

            return args[0] switch
            {
                "check" => WriteSuccess("check", null, null),
                "version" => WriteSuccess("version", null, null),
                "transcribe" => await TranscribeAsync(ParseTranscribeArgs(args.Skip(1).ToArray())).ConfigureAwait(false),
                _ => WriteFailure(args[0], "Unknown command.\n\n" + UsageText)
            };
        }
        catch (Exception error)
        {
            return WriteFailure("unknown", error.Message);
        }
    }

    private static async Task<int> TranscribeAsync(TranscribeOptions options)
    {
        ValidateTranscribeOptions(options);
        Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);

        var tempRoot = Path.Combine(Path.GetTempPath(), "tc-whisper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var wavPath = Path.Combine(tempRoot, "input.wav");
            await RunRequiredAsync(options.FfmpegPath, [
                "-y",
                "-i", options.InputPath,
                "-ar", "16000",
                "-ac", "1",
                "-c:a", "pcm_s16le",
                wavPath
            ], TimeSpan.FromMinutes(5), "FFmpeg conversion failed").ConfigureAwait(false);

            var srtPath = await RunWhisperToSrtAsync(options, wavPath, tempRoot).ConfigureAwait(false);
            if (options.OutputFormat == "srt")
            {
                File.Copy(srtPath, options.OutputPath, overwrite: true);
            }
            else
            {
                var transcript = Transcript.FromSrt(File.ReadAllText(srtPath));
                await File.WriteAllTextAsync(
                    options.OutputPath,
                    JsonSerializer.Serialize(transcript, JsonOptions)
                ).ConfigureAwait(false);
            }

            return WriteSuccess("transcribe", options.OutputPath, null);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static async Task<string> RunWhisperToSrtAsync(TranscribeOptions options, string wavPath, string tempRoot)
    {
        var outputBase = Path.Combine(tempRoot, "transcript");
        var wordModeArgs = options.OutputFormat == "json"
            ? new[] { "-ml", "1", "-sow" }
            : [];

        var args = new List<string>
        {
            "-m", options.ModelPath,
            "-f", wavPath,
            "-osrt",
            "-of", outputBase
        };
        args.AddRange(wordModeArgs);

        var result = await ProcessRunner.RunAsync(options.WhisperCliPath, args, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        if (!result.Succeeded && wordModeArgs.Length > 0)
        {
            args.RemoveRange(args.Count - wordModeArgs.Length, wordModeArgs.Length);
            result = await ProcessRunner.RunAsync(options.WhisperCliPath, args, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        }

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("whisper.cpp failed: " + FirstUsefulLine(result.StandardError, result.StandardOutput));
        }

        var srtPath = outputBase + ".srt";
        if (!File.Exists(srtPath))
        {
            throw new InvalidOperationException("whisper.cpp did not create an SRT output file.");
        }

        return srtPath;
    }

    private static void ValidateTranscribeOptions(TranscribeOptions options)
    {
        if (!File.Exists(options.InputPath)) throw new FileNotFoundException("Input file was not found.", options.InputPath);
        if (!File.Exists(options.ModelPath)) throw new FileNotFoundException("Model file was not found.", options.ModelPath);
        if (!File.Exists(options.FfmpegPath)) throw new FileNotFoundException("FFmpeg was not found.", options.FfmpegPath);
        if (!File.Exists(options.WhisperCliPath)) throw new FileNotFoundException("whisper-cli was not found.", options.WhisperCliPath);
        if (options.OutputFormat is not ("srt" or "json")) throw new InvalidOperationException("Output format must be srt or json.");
    }

    private static async Task RunRequiredAsync(string executable, IEnumerable<string> args, TimeSpan timeout, string message)
    {
        var result = await ProcessRunner.RunAsync(executable, args, timeout).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(message + ": " + FirstUsefulLine(result.StandardError, result.StandardOutput));
        }
    }

    private static TranscribeOptions ParseTranscribeArgs(string[] args)
    {
        var options = new TranscribeOptions();
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length) throw new InvalidOperationException("Missing value for " + args[index]);
            var value = args[index + 1];
            switch (args[index])
            {
                case "--input": options.InputPath = value; break;
                case "--model": options.ModelPath = value; break;
                case "--ffmpeg": options.FfmpegPath = value; break;
                case "--whisper-cli": options.WhisperCliPath = value; break;
                case "--output-format": options.OutputFormat = value.ToLowerInvariant(); break;
                case "--output": options.OutputPath = value; break;
                default: throw new InvalidOperationException("Unknown argument " + args[index]);
            }
        }

        if (string.IsNullOrWhiteSpace(options.WhisperCliPath))
        {
            options.WhisperCliPath = Path.Combine(AppContext.BaseDirectory, RuntimeConstants.WhisperCliExecutableName);
        }

        return options;
    }

    private static int WriteSuccess(string command, string? outputPath, string? message)
    {
        Console.WriteLine(JsonSerializer.Serialize(new HelperResponse(
            true,
            command,
            RuntimeConstants.HelperVersion,
            RuntimeConstants.RuntimeVersion,
            message,
            outputPath,
            false,
            true,
            "Windows"
        ), JsonOptions));
        return 0;
    }

    private static int WriteFailure(string command, string message)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new HelperResponse(
            false,
            command,
            RuntimeConstants.HelperVersion,
            RuntimeConstants.RuntimeVersion,
            message,
            null,
            false,
            true,
            "Windows"
        ), JsonOptions));
        return 1;
    }

    private static string FirstUsefulLine(params string[] values)
    {
        foreach (var value in values)
        {
            var line = (value ?? "").Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(line)) return line.Trim();
        }

        return "Unknown error";
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch { }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private const string UsageText = """
        tc-whisper-helper check --json
        tc-whisper-helper version --json
        tc-whisper-helper transcribe --input <file> --model <model-path> --ffmpeg <ffmpeg-path> --output-format srt|json --output <file>
        """;
}

internal sealed class TranscribeOptions
{
    public string InputPath { get; set; } = "";
    public string ModelPath { get; set; } = "";
    public string FfmpegPath { get; set; } = "";
    public string WhisperCliPath { get; set; } = "";
    public string OutputFormat { get; set; } = "";
    public string OutputPath { get; set; } = "";
}

internal sealed record HelperResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("helperVersion")] string HelperVersion,
    [property: JsonPropertyName("runtimeVersion")] string RuntimeVersion,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("outputPath")] string? OutputPath,
    [property: JsonPropertyName("wavOnly")] bool WavOnly,
    [property: JsonPropertyName("supportsWordTimestamps")] bool SupportsWordTimestamps,
    [property: JsonPropertyName("platform")] string Platform
);

internal sealed record Transcript(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("segments")] IReadOnlyList<TranscriptSegment> Segments
)
{
    public static Transcript FromSrt(string srt)
    {
        var segments = SrtParser.Parse(srt);
        return new Transcript(string.Join(" ", segments.Select(segment => segment.Text)).Trim(), segments);
    }
}

internal sealed record TranscriptSegment(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("start")] double Start,
    [property: JsonPropertyName("end")] double End,
    [property: JsonPropertyName("words")] IReadOnlyList<TranscriptWord> Words
);

internal sealed record TranscriptWord(
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("start")] double Start,
    [property: JsonPropertyName("end")] double End
);

internal static partial class SrtParser
{
    public static IReadOnlyList<TranscriptSegment> Parse(string srt)
    {
        var normalized = srt.Replace("\r\n", "\n").Trim();
        var blocks = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var segments = new List<TranscriptSegment>();

        foreach (var block in blocks)
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
            if (lines.Length < 2) continue;

            var timeLine = lines.FirstOrDefault(line => line.Contains("-->", StringComparison.Ordinal));
            if (timeLine is null) continue;

            var parts = timeLine.Split("-->", StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;

            var text = string.Join(" ", lines.SkipWhile(line => line != timeLine).Skip(1)).Trim();
            var start = ParseSrtTime(parts[0]);
            var end = ParseSrtTime(parts[1]);
            if (string.IsNullOrWhiteSpace(text) || end <= start) continue;

            segments.Add(new TranscriptSegment(text, start, end, BuildWords(text, start, end)));
        }

        return segments;
    }

    private static IReadOnlyList<TranscriptWord> BuildWords(string text, double start, double end)
    {
        var words = WordRegex().Matches(text).Select(match => match.Value).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (words.Length == 0) return [];

        var duration = Math.Max(0.01, end - start);
        var step = duration / words.Length;
        return words.Select((word, index) => new TranscriptWord(
            word,
            start + (step * index),
            index == words.Length - 1 ? end : start + (step * (index + 1))
        )).ToArray();
    }

    private static double ParseSrtTime(string value)
    {
        var match = SrtTimeRegex().Match(value);
        if (!match.Success) return 0;

        return (int.Parse(match.Groups["h"].Value) * 3600) +
            (int.Parse(match.Groups["m"].Value) * 60) +
            int.Parse(match.Groups["s"].Value) +
            (int.Parse(match.Groups["ms"].Value) / 1000.0);
    }

    [GeneratedRegex(@"(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2}),(?<ms>\d{3})")]
    private static partial Regex SrtTimeRegex();

    [GeneratedRegex(@"\S+")]
    private static partial Regex WordRegex();
}
