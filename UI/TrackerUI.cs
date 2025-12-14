using System;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Microsoft.Xna.Framework;
using Terraria;
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
        _rootPanel.Width.Set(0f, 1f);
        _rootPanel.Height.Set(0f, 1f);

        Append(_rootPanel);

        BuildLayout();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshTrackedExpedition();
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

        if (_lastTrackedId == trackedId)
        {
            return;
        }

        if (!registry.TryGetDefinition(trackedId, out ExpeditionDefinition definition))
        {
            _lastTrackedId = string.Empty;
            ShowPlaceholder("Tracking data is unavailable for this expedition.");
            return;
        }

        ExpeditionProgress? progress = player?.ExpeditionProgressEntries.FirstOrDefault(entry => entry.ExpeditionId == trackedId);

        ShowTrackedDetails(definition, progress);
        _lastTrackedId = trackedId;
    }

    private static string? GetTrackedExpeditionId(ExpeditionsPlayer? player)
    {
        if (player is null)
            return null;

        // Future: replace this heuristic with the authoritative tracked expedition once selection wiring exists.
        var active = player.ExpeditionProgressEntries.FirstOrDefault(entry => !entry.IsCompleted);
        if (active != null)
            return active.ExpeditionId;

        return player.ExpeditionProgressEntries.FirstOrDefault()?.ExpeditionId;
    }

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

        _contentList.Add(new UIText("Tracked Expedition", 0.95f, true));
        _contentList.Add(new UIText(definition.DisplayName, 0.9f, true));
        _contentList.Add(new UIText($"Category: {definition.Category}", 0.85f));
        _contentList.Add(new UIText($"Duration: {FormatDuration(definition.DurationTicks)}", 0.85f));

        string statusText = progress switch
        {
            null => "Status: Not started",
            { IsCompleted: true } => "Status: Completed",
            _ => "Status: Active"
        };

        _contentList.Add(new UIText(statusText, 0.85f));

        var startButton = CreateDisabledButton("Start Expedition");
        var completeButton = CreateDisabledButton("Complete Expedition");

        // Future: wire these buttons to the authoritative expedition lifecycle when server-safe actions are available.

        var buttonsRow = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(36f)
        };

        startButton.Left.Set(0f, 0f);
        completeButton.Left.Set(180f, 0f);

        buttonsRow.Append(startButton);
        buttonsRow.Append(completeButton);

        _contentList.Add(buttonsRow);
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

    private UITextPanel<string> CreateDisabledButton(string label)
    {
        return new UITextPanel<string>(label, 0.85f, true)
        {
            Width = StyleDimension.FromPixels(160f),
            Height = StyleDimension.FromPixels(32f),
            BackgroundColor = new Color(60, 60, 60),
            BorderColor = new Color(90, 90, 90),
            TextColor = Color.Gray,
            IgnoreMouseInteraction = true
        };
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
