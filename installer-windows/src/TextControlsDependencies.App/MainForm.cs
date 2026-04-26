using TextControlsDependencies.Core;

namespace TextControlsDependencies.App;

public sealed class MainForm : Form
{
    private readonly RuntimeManager runtimeManager = new();
    private readonly Label engineValue = new();
    private readonly Label ffmpegValue = new();
    private readonly Label modelValue = new();
    private readonly ProgressBar progressBar = new();
    private readonly Label activityLabel = new();
    private readonly Button checkButton = new();
    private readonly Button installButton = new();
    private readonly Button uninstallButton = new();

    public MainForm()
    {
        Text = "Text Controls Dependencies";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 430);
        ClientSize = new Size(520, 430);
        BackColor = Color.FromArgb(27, 27, 27);
        ForeColor = Color.FromArgb(200, 200, 200);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        Controls.Add(BuildLayout());
        Shown += async (_, _) => await RefreshStatusAsync().ConfigureAwait(true);
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 7,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Text Controls dependencies installer.",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(235, 235, 235),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        root.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 52,
            Text = "For local auto-transcription, it installs Whisper, an AI-based speech-to-text model, and FFmpeg for audio conversion and preprocessing.",
            ForeColor = Color.FromArgb(185, 185, 185),
            Margin = new Padding(0)
        }, 0, 1);

        root.Controls.Add(BuildStatusPanel(), 0, 3);
        root.Controls.Add(BuildActionsPanel(), 0, 5);
        root.Controls.Add(BuildActivityPanel(), 0, 6);
        return root;
    }

    private Control BuildStatusPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            BackColor = BackColor
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        AddStatusRow(panel, 0, "Local Engine", engineValue);
        AddStatusRow(panel, 1, "FFmpeg", ffmpegValue);
        AddStatusRow(panel, 2, "Model", modelValue);
        return panel;
    }

    private void AddStatusRow(TableLayoutPanel panel, int row, string label, Label valueLabel)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.Controls.Add(BuildStatusCell(label, ContentAlignment.MiddleLeft), 0, row);
        valueLabel.Text = "Missing";
        valueLabel.TextAlign = ContentAlignment.MiddleRight;
        panel.Controls.Add(BuildStatusCell(valueLabel), 1, row);
    }

    private Label BuildStatusCell(string text, ContentAlignment alignment) => BuildStatusCell(new Label
    {
        Text = text,
        TextAlign = alignment
    });

    private Label BuildStatusCell(Label label)
    {
        label.Dock = DockStyle.Fill;
        label.Padding = new Padding(10, 0, 10, 0);
        label.Margin = new Padding(0, 0, 0, 6);
        label.BackColor = Color.FromArgb(16, 16, 16);
        label.ForeColor = Color.FromArgb(200, 200, 200);
        return label;
    }

    private Control BuildActionsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 1,
            AutoSize = true,
            BackColor = BackColor
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

        ConfigureButton(checkButton, "Check", async () => await RefreshStatusAsync().ConfigureAwait(true));
        ConfigureButton(installButton, "Install / Repair", async () => await InstallAsync().ConfigureAwait(true));
        ConfigureButton(uninstallButton, "Uninstall", async () => await UninstallAsync().ConfigureAwait(true));

        panel.Controls.Add(checkButton, 0, 0);
        panel.Controls.Add(installButton, 1, 0);
        panel.Controls.Add(uninstallButton, 2, 0);
        return panel;
    }

    private void ConfigureButton(Button button, string text, Func<Task> action)
    {
        button.Text = text;
        button.Height = 30;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 8, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(46, 46, 46);
        button.BackColor = Color.FromArgb(16, 16, 16);
        button.ForeColor = Color.FromArgb(200, 200, 200);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
    }

    private Control BuildActivityPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            BackColor = BackColor
        };
        progressBar.Dock = DockStyle.Top;
        progressBar.Height = 16;
        progressBar.Minimum = 0;
        progressBar.Maximum = 100;
        progressBar.Margin = new Padding(0, 0, 0, 8);

        activityLabel.AutoSize = false;
        activityLabel.Dock = DockStyle.Top;
        activityLabel.Height = 44;
        activityLabel.Text = "Ready to check.";
        activityLabel.ForeColor = Color.FromArgb(180, 180, 180);

        panel.Controls.Add(progressBar, 0, 0);
        panel.Controls.Add(activityLabel, 0, 1);
        return panel;
    }

    private async Task RefreshStatusAsync()
    {
        SetBusy(true, "Checking...");
        try
        {
            var inspection = await runtimeManager.InspectAsync().ConfigureAwait(true);
            RenderInspection(inspection);
            activityLabel.Text = inspection.InstallState == RuntimeInstallState.Ready ? "Ready." : "Missing.";
        }
        catch (Exception error)
        {
            activityLabel.Text = error.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task InstallAsync()
    {
        SetBusy(true, "Installing...");
        try
        {
            var inspection = await runtimeManager.InstallOrRepairAsync(
                progress =>
                {
                    UpdateProgress(progress);
                }
            ).ConfigureAwait(true);
            RenderInspection(inspection);
            activityLabel.Text = inspection.InstallState == RuntimeInstallState.Ready ? "Ready." : "Install finished, but a dependency is still missing.";
        }
        catch (Exception error)
        {
            activityLabel.Text = error.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task UninstallAsync()
    {
        SetBusy(true, "Uninstalling...");
        try
        {
            runtimeManager.Uninstall();
            RenderInspection(await runtimeManager.InspectAsync().ConfigureAwait(true));
            activityLabel.Text = "Uninstalled.";
        }
        catch (Exception error)
        {
            activityLabel.Text = error.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RenderInspection(RuntimeInspection inspection)
    {
        engineValue.Text = inspection.HelperHealthy && inspection.WhisperCliExists ? "Ready" : "Missing";
        ffmpegValue.Text = inspection.FfmpegHealthy ? "Ready" : "Missing";
        modelValue.Text = inspection.ModelChecksumMatches ? "Ready" : "Missing";
        progressBar.Value = inspection.InstallState == RuntimeInstallState.Ready ? 100 : 0;
    }

    private void UpdateProgress(RuntimeProgress progress)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateProgress(progress)));
            return;
        }

        progressBar.Value = Math.Max(0, Math.Min(100, (int)Math.Round(progress.Fraction * 100)));
        activityLabel.Text = progress.Message;
    }

    private void SetBusy(bool isBusy, string? message = null)
    {
        checkButton.Enabled = !isBusy;
        installButton.Enabled = !isBusy;
        uninstallButton.Enabled = !isBusy;
        if (message is not null)
        {
            activityLabel.Text = message;
        }
    }
}
