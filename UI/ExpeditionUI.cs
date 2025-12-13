using System;
using System.Collections.Generic;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Systems;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExpeditionsReforged.UI;

public class ExpeditionUI : UIState
{
    private UIPanel _rootPanel = null!;
    private UIList _expeditionList = null!;
    private UIPanel _detailsPanel = null!;
    private UIList _detailsList = null!;
    private UIText _detailsTitle = null!;
    private UIText _detailsDuration = null!;
    private UIText _detailsDifficulty = null!;
    private UIText _detailsMinLevel = null!;
    private UIText _detailsRepeatable = null!;
    private UIText _detailsPlaceholder = null!;

    private readonly List<ExpeditionListEntry> _entries = new();
    private string _selectedExpeditionId = string.Empty;

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

    private void BuildLayout()
    {
        _entries.Clear();
        _selectedExpeditionId = string.Empty;

        const float listWidthPercent = 0.45f;

        var listContainer = new UIElement
        {
            Width = new StyleDimension(-8f, listWidthPercent),
            Height = StyleDimension.FromPercent(1f)
        };

        _expeditionList = new UIList
        {
            ListPadding = 6f,
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPercent(1f)
        };

        var scrollbar = new UIScrollbar
        {
            HAlign = 1f
        };

        scrollbar.Height = StyleDimension.FromPercent(1f);
        scrollbar.SetView(100f, 1000f);
        _expeditionList.SetScrollbar(scrollbar);

        PopulateExpeditionList();

        listContainer.Append(_expeditionList);
        listContainer.Append(scrollbar);

        _detailsPanel = new UIPanel
        {
            Left = new StyleDimension(8f, listWidthPercent),
            Width = new StyleDimension(-8f, 1f - listWidthPercent),
            Height = StyleDimension.FromPercent(1f),
            BackgroundColor = new Color(40, 46, 60),
            BorderColor = new Color(69, 82, 110)
        };

        _detailsPanel.SetPadding(12f);

        BuildDetailsPanel();

        _rootPanel.Append(listContainer);
        _rootPanel.Append(_detailsPanel);
    }

    private void PopulateExpeditionList()
    {
        _expeditionList.Clear();

        var registry = ModContent.GetInstance<ExpeditionRegistry>();

        // registry.Definitions is an IReadOnlyCollection<ExpeditionDefinition>, so iterate it directly.
        foreach (var definition in registry.Definitions)
        {
            var entry = new ExpeditionListEntry(this, definition.Id, definition.DisplayName);

            _entries.Add(entry);
            _expeditionList.Add(entry);
        }
    }

    private void BuildDetailsPanel()
    {
        _detailsList = new UIList
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPercent(1f),
            ListPadding = 8f
        };

        _detailsTitle = new UIText(string.Empty, 0.9f, true);
        _detailsDuration = new UIText(string.Empty, 0.85f);
        _detailsDifficulty = new UIText(string.Empty, 0.85f);
        _detailsMinLevel = new UIText(string.Empty, 0.85f);
        _detailsRepeatable = new UIText(string.Empty, 0.85f);

        _detailsList.Add(_detailsTitle);
        _detailsList.Add(_detailsDuration);
        _detailsList.Add(_detailsDifficulty);
        _detailsList.Add(_detailsMinLevel);
        _detailsList.Add(_detailsRepeatable);

        _detailsPlaceholder = new UIText("Select an expedition to view its details.")
        {
            HAlign = 0f,
            VAlign = 0f
        };

        ShowPlaceholder();
    }

    private void HandleSelectionChanged(string expeditionId)
    {
        _selectedExpeditionId = expeditionId;

        var registry = ModContent.GetInstance<ExpeditionRegistry>();

        if (registry.TryGetDefinition(_selectedExpeditionId, out ExpeditionDefinition definition))
        {
            ShowDetails(definition);
        }
        else
        {
            ShowPlaceholder();
        }

        foreach (var entry in _entries)
        {
            entry.SetSelected(entry.ExpeditionId == _selectedExpeditionId);
        }
    }

    private void ShowPlaceholder()
    {
        if (_detailsList.Parent != null)
        {
            _detailsPanel.RemoveChild(_detailsList);
        }

        if (_detailsPlaceholder.Parent == null)
        {
            _detailsPanel.Append(_detailsPlaceholder);
        }

        _detailsPlaceholder.SetText("Select an expedition to view its details.");
    }

    private void ShowDetails(ExpeditionDefinition definition)
    {
        if (definition == null)
        {
            ShowPlaceholder();
            return;
        }

        if (_detailsPlaceholder.Parent != null)
        {
            _detailsPanel.RemoveChild(_detailsPlaceholder);
        }

        if (_detailsList.Parent == null)
        {
            _detailsPanel.Append(_detailsList);
        }

        _detailsTitle.SetText(definition.DisplayName);
        _detailsDuration.SetText($"Duration: {FormatDuration(definition.DurationTicks)}");
        _detailsDifficulty.SetText($"Difficulty: {definition.Difficulty}");
        _detailsMinLevel.SetText($"Minimum Level: {definition.MinPlayerLevel}");
        _detailsRepeatable.SetText(definition.IsRepeatable ? "Repeatable: Yes" : "Repeatable: No");
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

    private class ExpeditionListEntry : UIPanel
    {
        private readonly ExpeditionUI _owner;
        private readonly UIText _label;
        private readonly Color _defaultBackground = new(44, 57, 105);
        private readonly Color _selectedBackground = new(72, 129, 191);

        public string ExpeditionId { get; }

        public ExpeditionListEntry(ExpeditionUI owner, string expeditionId, string displayName)
        {
            _owner = owner;
            ExpeditionId = expeditionId;

            Height = StyleDimension.FromPixels(32f);
            Width = StyleDimension.FromPercent(1f);
            PaddingTop = 6f;
            PaddingBottom = 6f;
            BackgroundColor = _defaultBackground;
            BorderColor = new Color(80, 104, 192);

            _label = new UIText(displayName)
            {
                HAlign = 0f,
                VAlign = 0.5f
            };

            Append(_label);

            OnLeftClick += HandleLeftClick;
        }

        public void SetSelected(bool isSelected)
        {
            BackgroundColor = isSelected ? _selectedBackground : _defaultBackground;
        }

        private void HandleLeftClick(UIMouseEvent evt, UIElement listeningElement)
        {
            _owner.HandleSelectionChanged(ExpeditionId);
        }
    }
}
