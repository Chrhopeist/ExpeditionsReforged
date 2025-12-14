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

public class ExpeditionUI : UIState
{
    private enum SortMode
    {
        Name,
        Availability,
        Category
    }

    private UIPanel _rootPanel = null!;
    private UIElement _bodyContainer = null!;
    private UITextPanel<string> _categoryButton = null!;
    private UITextPanel<string> _sortButton = null!;
    private UITextPanel<string> _sortDirectionButton = null!;
    private UIList _expeditionList = null!;
    private UIPanel _detailsPanel = null!;
    private UIList _detailsList = null!;
    private UIText _detailsPlaceholder = null!;

    private readonly List<ExpeditionListEntry> _entries = new();
    private readonly List<string> _categories = new();
    private string _selectedExpeditionId = string.Empty;
    private string _selectedCategory = "All";
    private SortMode _sortMode = SortMode.Name;
    private bool _sortAscending = true;

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

        var controlsBar = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(72f)
        };

        BuildControls(controlsBar);

        _bodyContainer = new UIElement
        {
            Top = StyleDimension.FromPixels(76f),
            Width = StyleDimension.FromPercent(1f),
            Height = new StyleDimension(-76f, 1f)
        };

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

        PopulateCategories();
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

        _bodyContainer.Append(listContainer);
        _bodyContainer.Append(_detailsPanel);

        _rootPanel.Append(controlsBar);
        _rootPanel.Append(_bodyContainer);
    }

    private void BuildControls(UIElement controlsBar)
    {
        var categoryLabel = new UIText("Category:", 0.9f)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        categoryLabel.Left.Set(0f, 0f);
        categoryLabel.Top.Set(0f, 0f);
        controlsBar.Append(categoryLabel);

        _categoryButton = new UITextPanel<string>("All", 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _categoryButton.Left.Set(90f, 0f);
        _categoryButton.Top.Set(12f, 0f);
        _categoryButton.OnLeftClick += (_, _) => CycleCategory();
        controlsBar.Append(_categoryButton);

        var sortLabel = new UIText("Sort:", 0.9f)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        sortLabel.Left.Set(250f, 0f);
        sortLabel.Top.Set(0f, 0f);
        controlsBar.Append(sortLabel);

        _sortButton = new UITextPanel<string>(_sortMode.ToString(), 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _sortButton.Left.Set(310f, 0f);
        _sortButton.Top.Set(12f, 0f);
        _sortButton.OnLeftClick += (_, _) => CycleSortMode();
        controlsBar.Append(_sortButton);

        _sortDirectionButton = new UITextPanel<string>(_sortAscending ? "Ascending" : "Descending", 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _sortDirectionButton.Left.Set(430f, 0f);
        _sortDirectionButton.Top.Set(12f, 0f);
        _sortDirectionButton.OnLeftClick += (_, _) => ToggleSortDirection();
        controlsBar.Append(_sortDirectionButton);
    }

    private void PopulateExpeditionList()
    {
        _expeditionList.Clear();
        _entries.Clear();

        var registry = ModContent.GetInstance<ExpeditionRegistry>();
        var definitions = registry.Definitions.AsEnumerable();

        if (!string.Equals(_selectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            definitions = definitions.Where(definition => string.Equals(definition.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        var player = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();

        var orderedDefinitions = ApplySort(definitions, player);

        foreach (var view in orderedDefinitions)
        {
            var entry = new ExpeditionListEntry(this, view);
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

        _detailsList.Clear();

        var player = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
        var progress = player?.ExpeditionProgressEntries.FirstOrDefault(progressEntry => progressEntry.ExpeditionId == definition.Id);

        _detailsList.Add(new UIText(definition.DisplayName, 0.95f, true));
        _detailsList.Add(new UIText($"Category: {definition.Category}", 0.85f));
        _detailsList.Add(new UIText($"Duration: {FormatDuration(definition.DurationTicks)}", 0.85f));
        _detailsList.Add(new UIText($"Difficulty: {definition.Difficulty}", 0.85f));
        _detailsList.Add(new UIText($"Minimum Level: {definition.MinPlayerLevel}", 0.85f));
        _detailsList.Add(new UIText(definition.IsRepeatable ? "Repeatable: Yes" : "Repeatable: No", 0.85f));

        var statusText = progress switch
        {
            null => "Status: Not started",
            { IsCompleted: true } => "Status: Completed",
            _ => "Status: Active"
        };

        _detailsList.Add(new UIText(statusText, 0.85f));

        AddSectionHeading("Prerequisites");
        if (definition.Prerequisites.Count == 0)
        {
            _detailsList.Add(new UIText("• None", 0.8f));
        }
        else
        {
            foreach (var prerequisite in definition.Prerequisites)
            {
                _detailsList.Add(new UIText($"• {FormatCondition(prerequisite)}", 0.8f));
            }
        }

        AddSectionHeading("Deliverables");
        if (definition.Deliverables.Count == 0)
        {
            _detailsList.Add(new UIText("• None", 0.8f));
        }
        else
        {
            foreach (var deliverable in definition.Deliverables)
            {
                _detailsList.Add(new UIText($"• {FormatDeliverable(deliverable)}", 0.8f));
            }
        }

        AddSectionHeading("Rewards");
        if (definition.Rewards.Count == 0)
        {
            _detailsList.Add(new UIText("• None", 0.8f));
        }
        else
        {
            foreach (var reward in definition.Rewards)
            {
                _detailsList.Add(new UIText($"• {FormatReward(reward)}", 0.8f));
            }
        }

        if (definition.DailyRewards.Count > 0)
        {
            AddSectionHeading("Daily Bonus Rewards");
            foreach (var reward in definition.DailyRewards)
            {
                _detailsList.Add(new UIText($"• {FormatReward(reward)}", 0.8f));
            }
        }

        var buttonRow = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(36f)
        };

        var startButton = CreateDisabledButton("Start Expedition");
        startButton.Left.Set(0f, 0f);
        buttonRow.Append(startButton);

        var trackButton = CreateDisabledButton("Track Expedition");
        trackButton.Left.Set(180f, 0f);
        buttonRow.Append(trackButton);

        // Future: wire these buttons to authoritative expedition start/track calls with proper networking safeguards.

        _detailsList.Add(buttonRow);
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

    private static string FormatCondition(ConditionDefinition definition)
    {
        string description = string.IsNullOrWhiteSpace(definition.Description)
            ? definition.Id
            : definition.Description;

        return definition.IsBoolean
            ? description
            : $"{description} (x{definition.RequiredCount})";
    }

    private static string FormatDeliverable(DeliverableDefinition definition)
    {
        string description = string.IsNullOrWhiteSpace(definition.Description)
            ? definition.Id
            : definition.Description;

        string quantity = definition.IsBoolean ? string.Empty : $" x{definition.RequiredCount}";
        string consumption = definition.ConsumesItems ? " (consumed)" : string.Empty;
        return $"{description}{quantity}{consumption}";
    }

    private static string FormatReward(RewardDefinition definition)
    {
        string amount = definition.IsBoolean
            ? string.Empty
            : definition.MinStack == definition.MaxStack
                ? $" x{definition.MinStack}"
                : $" x{definition.MinStack}-{definition.MaxStack}";

        string chance = definition.DropChance < 1f ? $" ({definition.DropChance:P0})" : string.Empty;
        return $"{definition.Id}{amount}{chance}";
    }

    private void PopulateCategories()
    {
        _categories.Clear();
        _categories.Add("All");

        var registry = ModContent.GetInstance<ExpeditionRegistry>();
        foreach (var category in registry.Definitions.Select(definition => definition.Category).Where(category => !string.IsNullOrWhiteSpace(category)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category, StringComparer.OrdinalIgnoreCase))
        {
            _categories.Add(category);
        }

        if (!_categories.Contains(_selectedCategory, StringComparer.OrdinalIgnoreCase))
        {
            _selectedCategory = "All";
        }

        _categoryButton?.SetText(_selectedCategory);
    }

    private void CycleCategory()
    {
        if (_categories.Count == 0)
        {
            PopulateCategories();
        }

        int currentIndex = _categories.FindIndex(category => string.Equals(category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
        int nextIndex = (currentIndex + 1) % _categories.Count;
        _selectedCategory = _categories[nextIndex];
        _categoryButton.SetText(_selectedCategory);
        PopulateExpeditionList();
    }

    private void CycleSortMode()
    {
        _sortMode = _sortMode switch
        {
            SortMode.Name => SortMode.Availability,
            SortMode.Availability => SortMode.Category,
            _ => SortMode.Name
        };

        _sortButton.SetText(_sortMode.ToString());
        PopulateExpeditionList();
    }

    private void ToggleSortDirection()
    {
        _sortAscending = !_sortAscending;
        _sortDirectionButton.SetText(_sortAscending ? "Ascending" : "Descending");
        PopulateExpeditionList();
    }

    private IEnumerable<ExpeditionView> ApplySort(IEnumerable<ExpeditionDefinition> definitions, ExpeditionsPlayer? player)
    {
        var views = definitions.Select(definition => BuildView(definition, player));

        IOrderedEnumerable<ExpeditionView> ordered = _sortMode switch
        {
            SortMode.Availability => views.OrderByDescending(view => view.IsAvailable).ThenBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase),
            SortMode.Category => views.OrderBy(view => view.Category, StringComparer.OrdinalIgnoreCase).ThenBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase),
            _ => views.OrderBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase)
        };

        var sorted = ordered.ToList();

        if (!_sortAscending)
        {
            sorted.Reverse();
        }

        return sorted;
    }

    private ExpeditionView BuildView(ExpeditionDefinition definition, ExpeditionsPlayer? player)
    {
        ExpeditionProgress? progress = player?.ExpeditionProgressEntries.FirstOrDefault(entry => entry.ExpeditionId == definition.Id);

        bool isAvailable = progress == null || (definition.IsRepeatable || !progress.IsCompleted);
        string status = progress switch
        {
            null => "Not started",
            { IsCompleted: true } => "Completed",
            _ => "Active"
        };

        return new ExpeditionView(definition.Id, definition.DisplayName, definition.Category, status, isAvailable);
    }

    private UITextPanel<string> CreateDisabledButton(string label)
    {
        var button = new UITextPanel<string>(label, 0.85f, true)
        {
            Width = StyleDimension.FromPixels(160f),
            Height = StyleDimension.FromPixels(32f),
            BackgroundColor = new Color(60, 60, 60),
            BorderColor = new Color(90, 90, 90),
            TextColor = Color.Gray,
            IgnoreMouseInteraction = true
        };

        return button;
    }

    private void AddSectionHeading(string text)
    {
        _detailsList.Add(new UIText(text, 0.85f, true)
        {
            TextColor = new Color(200, 210, 230)
        });
    }

    private class ExpeditionListEntry : UIPanel
    {
        private readonly ExpeditionUI _owner;
        private readonly UIText _label;
        private readonly UIText _categoryText;
        private readonly UIText _statusText;
        private readonly Color _defaultBackground = new(44, 57, 105);
        private readonly Color _selectedBackground = new(72, 129, 191);

        public string ExpeditionId { get; }

        public ExpeditionListEntry(ExpeditionUI owner, ExpeditionView view)
        {
            _owner = owner;
            ExpeditionId = view.Id;

            Height = StyleDimension.FromPixels(64f);
            Width = StyleDimension.FromPercent(1f);
            PaddingTop = 6f;
            PaddingBottom = 6f;
            BackgroundColor = _defaultBackground;
            BorderColor = new Color(80, 104, 192);

            _label = new UIText(view.DisplayName)
            {
                HAlign = 0f,
                VAlign = 0f
            };

            _categoryText = new UIText($"Category: {view.Category}", 0.75f)
            {
                HAlign = 0f,
                Top = new StyleDimension(22f, 0f)
            };

            _statusText = new UIText(view.IsAvailable ? $"Status: {view.Status}" : "Status: Unavailable", 0.75f)
            {
                HAlign = 0f,
                Top = new StyleDimension(40f, 0f),
                TextColor = view.IsAvailable ? Color.White : Color.LightGray
            };

            Append(_label);
            Append(_categoryText);
            Append(_statusText);

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

    private readonly struct ExpeditionView
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Status { get; }
        public bool IsAvailable { get; }

        public ExpeditionView(string id, string displayName, string category, string status, bool isAvailable)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            Status = status;
            IsAvailable = isAvailable;
        }
    }
}
