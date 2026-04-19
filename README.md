# Text Controls v1.0.0

`Text Controls` is an Adobe After Effects CEP panel for subtitle and text workflows.

## Main Features

- Transcribe the selected comp audio
- Transcribe an external audio or video file
- Use `OpenAI Whisper API` or `Local Whisper`
- Import `.srt` subtitle files
- Create highlight-based subtitle animation
- Decompose selected text into words or letters
- Save and reuse decompose presets

## Compatibility

- Adobe After Effects `CC 2019+`
- CEP panel extension

## Included In This Release

- `TextControls-v1.0.0.zxp`
- `INSTALLATION.md`
- `UPLOAD_COPY.md`
- `RELEASE_NOTES.md`
- `TextControls-icon.png`
- `SHA256SUMS.txt`

## Fastest Setup Recommendation

For the simplest transcription setup:

1. Open `Transcribe Settings`
2. Set `Engine` to `OpenAI Whisper API`
3. Use `Slow Render (compatibility)` for comp transcription
4. Paste your OpenAI API key
5. Run `Check Setup`

That route does not require a local Whisper, Python, or ffmpeg install.

## Local Offline Setup

If you want fully local transcription instead of API usage, install:

- Python 3
- `openai-whisper`
- `ffmpeg`

Then run the built-in `Check Setup` inside the panel.

## Open In After Effects

After installation:

`Window -> Extensions -> Text Controls`

## Notes

- Comp transcription creates a temporary audio export automatically.
- File transcription can use an existing audio or video file directly.
- Focused highlight mode is enabled by default.
