using System;
using System.Collections.Generic;
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

/// <summary>
/// Client-only UI shown from NPC chat to list expeditions offered by the current quest giver.
/// </summary>
public class NpcExpeditionUI : UIState
{
    private UIPanel _rootPanel = null!;
    private UIList _expeditionList = null!;
    private UIScrollbar _scrollbar = null!;
    private UIText _placeholderText = null!;
    private int _questGiverNpcType = -1;
    private bool _needsRefresh;

    /// <summary>
    /// The NPC type currently driving the list. -1 indicates no NPC has been assigned yet.
    /// </summary>
    public int QuestGiverNpcType => _questGiverNpcType;

    public override void OnInitialize()
    {
        _rootPanel = new UIPanel
        {
            BackgroundColor = new Color(34, 40, 52),
            BorderColor = new Color(69, 82, 110)
        };

        _rootPanel.SetPadding(12f);
        _rootPanel.Width.Set(570f, 0f);
        _rootPanel.Height.Set(420f, 0f);
        _rootPanel.HAlign = 0.5f;
        _rootPanel.VAlign = 0.5f;

        Append(_rootPanel);

        var title = new UIText("Available Expeditions", 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0f
        };

        _rootPanel.Append(title);

        var listContainer = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(340f),
            Top = StyleDimension.FromPixels(36f)
        };

        _expeditionList = new UIList
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPercent(1f),
            ListPadding = 8f
        };

        _scrollbar = new UIScrollbar
        {
            Height = StyleDimension.FromPercent(1f),
            HAlign = 1f
        };

        _expeditionList.SetScrollbar(_scrollbar);
        listContainer.Append(_expeditionList);
        listContainer.Append(_scrollbar);
        _rootPanel.Append(listContainer);

        _placeholderText = new UIText("No expeditions are currently available.", 0.85f)
        {
            HAlign = 0f,
            VAlign = 0f
        };

        _needsRefresh = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_needsRefresh)
        {
            RefreshExpeditionList();
            _needsRefresh = false;
        }
    }

    /// <summary>
    /// Updates the list to reflect the provided quest giver NPC type.
    /// </summary>
    public void ShowForNpc(int questGiverNpcType)
    {
        _questGiverNpcType = questGiverNpcType;
        _needsRefresh = true;
    }

    /// <summary>
    /// Clears the quest giver context so the UI can be hidden safely.
    /// </summary>
    public void ClearQuestGiver()
    {
        _questGiverNpcType = -1;
        _needsRefresh = true;
    }

    private void RefreshExpeditionList()
    {
        _expeditionList.Clear();

        if (_questGiverNpcType < 0)
        {
            ShowPlaceholder("No quest giver selected.");
            return;
        }

        var player = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
        if (player == null)
        {
            ShowPlaceholder("Player expedition data is unavailable.");
            return;
        }

        var registry = ModContent.GetInstance<ExpeditionRegistry>();

        List<ExpeditionDefinition> available = registry.Definitions
            // Availability is based solely on player state; quest giver usage is handled elsewhere (turn-in/icons).
            .Where(definition => IsAvailableForPlayer(definition, player))
            .OrderBy(definition => definition.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (available.Count == 0)
        {
            ShowPlaceholder("No expeditions are currently available.");
            return;
        }

        foreach (ExpeditionDefinition definition in available)
        {
            _expeditionList.Add(new ExpeditionEntry(definition, player, RequestRefresh));
        }
    }

    private void ShowPlaceholder(string message)
    {
        _placeholderText.SetText(message);

        if (_placeholderText.Parent != null)
        {
            _placeholderText.Remove();
        }

        _expeditionList.Add(_placeholderText);
    }

    private bool IsAvailableForPlayer(ExpeditionDefinition definition, ExpeditionsPlayer player)
    {
        // Only show expeditions the player can actually accept based on current progression.
        if (player.IsExpeditionActive(definition.Id))
        {
            return false;
        }

        if (!definition.IsRepeatable && player.IsExpeditionCompleted(definition.Id))
        {
            return false;
        }

        if (!ExpeditionService.MeetsProgressionRequirement(player.Player, definition))
        {
            return false;
        }

        return ExpeditionService.MeetsPrerequisites(player.Player, definition);
    }

    private void RequestRefresh()
    {
        _needsRefresh = true;
    }

    private sealed class ExpeditionEntry : UIPanel
    {
        private readonly ExpeditionDefinition _definition;
        private readonly ExpeditionsPlayer _player;
        private readonly Action _requestRefresh;

        public ExpeditionEntry(ExpeditionDefinition definition, ExpeditionsPlayer player, Action requestRefresh)
        {
            _definition = definition;
            _player = player;
            _requestRefresh = requestRefresh;

            BackgroundColor = new Color(45, 54, 72);
            BorderColor = new Color(86, 96, 124);
            Width = StyleDimension.FromPercent(1f);
            Height = StyleDimension.FromPixels(150f);
            SetPadding(10f);

            BuildEntryContents();
        }

        private void BuildEntryContents()
        {
            var nameText = new UIText(_definition.DisplayName, 0.85f, true)
            {
                HAlign = 0f,
                VAlign = 0f
            };

            Append(nameText);

            var descriptionText = new UIText(_definition.Description, 0.8f)
            {
                Top = StyleDimension.FromPixels(26f),
                MaxWidth = StyleDimension.FromPixels(360f)
            };

            Append(descriptionText);

            var detailRow = new UIText($"Category: {_definition.CategoryName} | Duration: {FormatDuration(_definition.DurationTicks)} | Difficulty: {_definition.Difficulty}", 0.75f)
            {
                Top = StyleDimension.FromPixels(64f)
            };

            Append(detailRow);

            var repeatableText = new UIText(_definition.IsRepeatable ? "Repeatable: Yes" : "Repeatable: No", 0.75f)
            {
                Top = StyleDimension.FromPixels(86f)
            };

            Append(repeatableText);

            if (_definition.Deliverables.Count > 0)
            {
                string firstDeliverable = ExpeditionUI.FormatDeliverable(_definition.Deliverables[0]);
                string summary = _definition.Deliverables.Count > 1
                    ? $"{firstDeliverable} +{_definition.Deliverables.Count - 1} more"
                    : firstDeliverable;

                var deliverableText = new UIText($"Objectives: {summary}", 0.75f)
                {
                    Top = StyleDimension.FromPixels(108f)
                };

                Append(deliverableText);
            }

            var acceptButton = CreateActionButton("Accept", true, () =>
            {
                _player.TryStartExpedition(_definition.Id);
                _requestRefresh();
            });

            acceptButton.Left.Set(380f, 0f);
            acceptButton.Top.Set(40f, 0f);
            Append(acceptButton);

            var rejectButton = CreateActionButton("Reject", true, () =>
            {
                // Closing the NPC expedition UI is client-only and does not mutate server state.
                _player.NpcExpeditionUIOpen = false;
            });

            rejectButton.Left.Set(380f, 0f);
            rejectButton.Top.Set(80f, 0f);
            Append(rejectButton);
        }

        private static UITextPanel<string> CreateActionButton(string label, bool enabled, Action? onClick)
        {
            var button = new UITextPanel<string>(label, 0.7f, true)
            {
                Width = StyleDimension.FromPixels(95f),
                Height = StyleDimension.FromPixels(28f),
                BackgroundColor = enabled ? new Color(80, 104, 192) : new Color(60, 60, 60),
                BorderColor = enabled ? new Color(110, 140, 220) : new Color(90, 90, 90),
                TextColor = enabled ? Color.White : Color.Gray
            };

            if (enabled && onClick != null)
            {
                button.OnLeftClick += (_, _) => onClick();
            }

            return button;
        }

        private static string FormatDuration(int durationTicks)
        {
            // Keep formatting consistent with the expedition log UI.
            TimeSpan time = TimeSpan.FromSeconds(durationTicks / 60d);

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
}
