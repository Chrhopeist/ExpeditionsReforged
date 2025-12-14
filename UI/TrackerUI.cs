using System;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExpeditionsReforged.UI;

public class TrackerUI : UIState
{
    private UIPanel _rootPanel = null!;
    private UIList _contentList = null!;
    private UIText _placeholderText = null!;
    private string _lastTrackedId = string.Empty;
    private float _currentAlpha = 0f;
    private float _targetAlpha = 0f;
    private float _scale = 1f;

    public override void OnInitialize()
    {
        _rootPanel = new UIPanel
        {
            BackgroundColor = new Color(34, 40, 52),
            BorderColor = new Color(69, 82, 110)
        };

        _rootPanel.SetPadding(12f);
        _rootPanel.Left.Set(0f, 0f);
        _rootPanel.Top.Set(0f, 0f);
        _rootPanel.Width.Set(360f, 0f);
        _rootPanel.Height.Set(260f, 0f);

        Append(_rootPanel);

        BuildLayout();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        ApplyConfig(gameTime);
        RefreshTrackedExpedition();
    }

    private void ApplyConfig(GameTime gameTime)
    {
        var config = ModContent.GetInstance<ExpeditionsClientConfig>();
        _scale = config.TrackerScale;

        _rootPanel.Left.Set(config.TrackerPosition.X, 0f);
        _rootPanel.Top.Set(config.TrackerPosition.Y, 0f);
        _rootPanel.Width.Set(360f * _scale, 0f);
        _rootPanel.Height.Set(260f * _scale, 0f);
        _rootPanel.SetPadding(12f * _scale);

        var player = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
        bool hasTracked = !string.IsNullOrWhiteSpace(player?.TrackedExpeditionId);
        _targetAlpha = hasTracked ? config.TrackerAlpha : 0f;

        float fadeSpeed = MathHelper.Clamp(config.TrackerFadeSpeed, 0.05f, 2f);
        float t = MathHelper.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds * fadeSpeed * 5f, 0f, 1f);
        _currentAlpha = MathHelper.Lerp(_currentAlpha, _targetAlpha, t);

        Color baseBg = new(34, 40, 52);
        Color baseBorder = new(69, 82, 110);
        _rootPanel.BackgroundColor = baseBg * _currentAlpha;
        _rootPanel.BorderColor = baseBorder * _currentAlpha;

        Recalculate();
    }

    private void BuildLayout()
    {
        _contentList = new UIList
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPercent(1f),
            ListPadding = 6f
        };

        _placeholderText = new UIText("No expedition is currently being tracked.")
        {
            HAlign = 0f,
            VAlign = 0f
        };

        ShowPlaceholder("No expedition is currently being tracked.");
    }

    private void RefreshTrackedExpedition()
    {
        var player = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
        var registry = ModContent.GetInstance<ExpeditionRegistry>();

        string? trackedId = GetTrackedExpeditionId(player);

        if (string.IsNullOrWhiteSpace(trackedId))
        {
            _lastTrackedId = string.Empty;
            ShowPlaceholder("No expedition is currently being tracked.");
            return;
        }

        if (_lastTrackedId == trackedId && _contentList.Parent != null)
        {
            return;
        }

        if (!registry.TryGetExpedition(trackedId, out ExpeditionDefinition definition))
        {
            _lastTrackedId = string.Empty;
            ShowPlaceholder("Tracking data is unavailable for this expedition.");
            return;
        }

        ExpeditionProgress? progress = player?.ExpeditionProgressEntries.FirstOrDefault(entry => entry.ExpeditionId == trackedId);

        ShowTrackedDetails(definition, progress);
        _lastTrackedId = trackedId;
    }

    private static string? GetTrackedExpeditionId(ExpeditionsPlayer? player) => player?.TrackedExpeditionId;

    private void ShowTrackedDetails(ExpeditionDefinition definition, ExpeditionProgress? progress)
    {
        if (_placeholderText.Parent != null)
        {
            _rootPanel.RemoveChild(_placeholderText);
        }

        if (_contentList.Parent == null)
        {
            _rootPanel.Append(_contentList);
        }

        _contentList.Clear();

        _contentList.Add(new UIText("Tracked Expedition", 0.95f * _scale, true));
        _contentList.Add(new UIText(definition.DisplayName, 0.9f * _scale, true));
        _contentList.Add(new UIText($"Category: {definition.CategoryName}", 0.85f * _scale));
        _contentList.Add(new UIText($"Duration: {FormatDuration(definition.DurationTicks)}", 0.85f * _scale));

        string statusText = progress switch
        {
            null => "Status: Not started",
            { IsCompleted: true } => "Status: Completed",
            _ => "Status: Active"
        };

        _contentList.Add(new UIText(statusText, 0.85f * _scale));

        if (definition.Deliverables.Count > 0)
        {
            _contentList.Add(new UIText("Objectives", 0.85f * _scale, true));
            foreach (var deliverable in definition.Deliverables)
            {
                int value = progress?.ConditionProgress.TryGetValue(deliverable.Id, out int current) == true ? current : 0;
                _contentList.Add(CreateDeliverableRow(deliverable, value));
            }
        }

        var buttonRow = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(36f * _scale)
        };

        var startButton = CreateActionButton("Start", progress is null || (definition.IsRepeatable || !progress.IsCompleted), () => Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>().TryStartExpedition(definition.Id));
        startButton.Left.Set(0f, 0f);
        buttonRow.Append(startButton);

        var completeButton = CreateActionButton("Turn In", progress is { IsCompleted: true } && !progress.RewardsClaimed, () =>
        {
            var expeditionsPlayer = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer == null)
                return;

            if (expeditionsPlayer.TryCompleteExpedition(definition.Id))
            {
                expeditionsPlayer.TryClaimRewards(definition.Id);
            }
        });
        completeButton.Left.Set(150f * _scale, 0f);
        buttonRow.Append(completeButton);

        _contentList.Add(buttonRow);
    }

    private void ShowPlaceholder(string message)
    {
        if (_contentList.Parent != null)
        {
            _rootPanel.RemoveChild(_contentList);
        }

        if (_placeholderText.Parent == null)
        {
            _rootPanel.Append(_placeholderText);
        }

        _placeholderText.SetText(message);
    }

    private UITextPanel<string> CreateActionButton(string label, bool enabled, Action? onClick)
    {
        var button = new UITextPanel<string>(label, 0.85f * _scale, true)
        {
            Width = StyleDimension.FromPixels(140f * _scale),
            Height = StyleDimension.FromPixels(32f * _scale),
            BackgroundColor = enabled ? new Color(80, 104, 192) * _currentAlpha : new Color(60, 60, 60) * _currentAlpha,
            BorderColor = enabled ? new Color(110, 140, 220) * _currentAlpha : new Color(90, 90, 90) * _currentAlpha,
            TextColor = enabled ? Color.White : Color.Gray
        };

        if (enabled && onClick != null)
        {
            button.OnLeftClick += (_, _) => onClick();
        }

        return button;
    }

    private UIElement CreateDeliverableRow(DeliverableDefinition deliverable, int current)
    {
        var row = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(40f * _scale)
        };

        bool complete = current >= deliverable.RequiredCount;

        var checkbox = new UIImage(TextureAssets.MagicPixel)
        {
            Left = StyleDimension.FromPixels(0f),
            Top = StyleDimension.FromPixels(2f),
            Width = StyleDimension.FromPixels(18f * _scale),
            Height = StyleDimension.FromPixels(18f * _scale),
            Color = complete ? new Color(100, 220, 120) : new Color(80, 80, 80)
        };

        row.Append(checkbox);

        var label = new UIText(ExpeditionUI.FormatDeliverable(deliverable), 0.8f * _scale)
        {
            Left = StyleDimension.FromPixels(24f * _scale),
            Top = StyleDimension.FromPixels(0f)
        };

        row.Append(label);

        if (!deliverable.IsBoolean)
        {
            float fraction = Math.Clamp(current / (float)deliverable.RequiredCount, 0f, 1f);
            var bar = new ExpeditionUI.SegmentedProgressBar
            {
                Top = StyleDimension.FromPixels(20f * _scale),
                Left = StyleDimension.FromPixels(24f * _scale),
                Width = new StyleDimension(-24f * _scale, 1f),
                Height = StyleDimension.FromPixels(10f * _scale)
            };

            bar.SetProgress(fraction);
            row.Append(bar);

            var counter = new UIText($"{current}/{deliverable.RequiredCount}", 0.75f * _scale)
            {
                HAlign = 1f,
                VAlign = 1f
            };

            row.Append(counter);
        }

        return row;
    }

    private static string FormatDuration(int durationTicks)
    {
        var time = TimeSpan.FromSeconds(durationTicks / 60d);

        if (time.TotalHours >= 1d)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        }

        if (time.TotalMinutes >= 1d)
        {
            return $"{(int)time.TotalMinutes}m {time.Seconds}s";
        }

        return $"{time.Seconds}s";
    }
}
