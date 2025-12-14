using System;
using System.Collections.Generic;
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

public class ExpeditionUI : UIState
{
    private enum SortMode
    {
        Name,
        Availability,
        Category,
        Rarity,
        Duration,
        Difficulty
    }

    private enum CompletionFilter
    {
        All,
        Available,
        Active,
        Completed
    }

    private UIPanel _rootPanel = null!;
    private UIElement _bodyContainer = null!;
    private UITextPanel<string> _categoryButton = null!;
    private UITextPanel<string> _sortButton = null!;
    private UITextPanel<string> _sortDirectionButton = null!;
    private UITextPanel<string> _completionButton = null!;
    private UITextPanel<string> _repeatableButton = null!;
    private UITextPanel<string> _trackedFilterButton = null!;
    private UIImage _npcHeadButton = null!;
    private UIList _expeditionList = null!;
    private UIScrollbar _expeditionScrollbar = null!;
    private RarityScrollbarMarkers _rarityMarkers = null!;
    private UIPanel _detailsPanel = null!;
    private UIList _detailsList = null!;
    private UIText _detailsPlaceholder = null!;

    private readonly List<ExpeditionListEntry> _entries = new();
    private readonly List<string> _categories = new();
    private readonly List<int> _npcHeads = new();
    private string _selectedExpeditionId = string.Empty;
    private string _selectedCategory = "All";
    private CompletionFilter _completionFilter = CompletionFilter.All;
    private bool _filterRepeatableOnly;
    private bool _filterTrackedOnly;
    private int? _filterNpcHeadId;
    private SortMode _sortMode = SortMode.Name;
    private bool _sortAscending = true;
    private bool _needsPopulate = true;

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

        // Defer population until a player exists; OnInitialize runs during mod load when no LocalPlayer is present.
        _needsPopulate = true;
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);
        if (_expeditionScrollbar != null)
        {
            _expeditionScrollbar.ViewPosition -= evt.ScrollWheelValue * 0.2f;
        }
    }

    private void BuildLayout()
    {
        _entries.Clear();
        _selectedExpeditionId = string.Empty;

        const float listWidthPercent = 0.45f;

        var controlsBar = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(86f)
        };

        BuildControls(controlsBar);

        _bodyContainer = new UIElement
        {
            Top = StyleDimension.FromPixels(90f),
            Width = StyleDimension.FromPercent(1f),
            Height = new StyleDimension(-90f, 1f)
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

        _expeditionScrollbar = new UIScrollbar
        {
            HAlign = 1f
        };

        _expeditionScrollbar.Height = StyleDimension.FromPercent(1f);
        _expeditionScrollbar.SetView(100f, 1000f);
        _expeditionList.SetScrollbar(_expeditionScrollbar);

        _rarityMarkers = new RarityScrollbarMarkers();
        _rarityMarkers.Left.Set(-6f, 1f);
        _rarityMarkers.Width.Set(6f, 0f);
        _rarityMarkers.Height.Set(0f, 1f);

        PopulateCategories();
        PopulateNpcHeads();

        listContainer.Append(_expeditionList);
        listContainer.Append(_expeditionScrollbar);
        listContainer.Append(_rarityMarkers);

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
        float x = 0f;

        var categoryLabel = new UIText("Category:", 0.9f)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        categoryLabel.Left.Set(x, 0f);
        categoryLabel.Top.Set(0f, 0f);
        controlsBar.Append(categoryLabel);

        x += 80f;

        _categoryButton = new UITextPanel<string>(_selectedCategory, 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _categoryButton.Left.Set(x, 0f);
        _categoryButton.Top.Set(12f, 0f);
        _categoryButton.OnLeftClick += (_, _) => CycleCategory();
        AddTooltip(_categoryButton, "Cycle expedition categories");
        controlsBar.Append(_categoryButton);

        x += 140f;

        _completionButton = new UITextPanel<string>(_completionFilter.ToString(), 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _completionButton.Left.Set(x, 0f);
        _completionButton.Top.Set(12f, 0f);
        _completionButton.OnLeftClick += (_, _) => CycleCompletionFilter();
        AddTooltip(_completionButton, "Filter by availability/active/completed");
        controlsBar.Append(_completionButton);

        x += 160f;

        _repeatableButton = new UITextPanel<string>("Repeatable: Any", 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _repeatableButton.Left.Set(x, 0f);
        _repeatableButton.Top.Set(12f, 0f);
        _repeatableButton.OnLeftClick += (_, _) => ToggleRepeatable();
        AddTooltip(_repeatableButton, "Toggle showing only repeatable expeditions");
        controlsBar.Append(_repeatableButton);

        x += 170f;

        _trackedFilterButton = new UITextPanel<string>("Tracked: Any", 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _trackedFilterButton.Left.Set(x, 0f);
        _trackedFilterButton.Top.Set(12f, 0f);
        _trackedFilterButton.OnLeftClick += (_, _) => ToggleTrackedFilter();
        AddTooltip(_trackedFilterButton, "Toggle showing only tracked expedition");
        controlsBar.Append(_trackedFilterButton);

        x += 170f;

        _npcHeadButton = new UIImage(TextureAssets.MagicPixel)
        {
            HAlign = 0f,
            VAlign = 0.5f,
            Color = Color.Gray
        };

        _npcHeadButton.Left.Set(x, 0f);
        _npcHeadButton.Top.Set(10f, 0f);
        _npcHeadButton.Width.Set(48f, 0f);
        _npcHeadButton.Height.Set(48f, 0f);
        _npcHeadButton.OnLeftClick += (_, _) => CycleNpcHead();
        AddTooltip(_npcHeadButton, "Cycle NPC head filter");
        controlsBar.Append(_npcHeadButton);

        x += 80f;

        var sortLabel = new UIText("Sort:", 0.9f)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        sortLabel.Left.Set(x, 0f);
        sortLabel.Top.Set(0f, 0f);
        controlsBar.Append(sortLabel);

        x += 50f;

        _sortButton = new UITextPanel<string>(_sortMode.ToString(), 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _sortButton.Left.Set(x, 0f);
        _sortButton.Top.Set(12f, 0f);
        _sortButton.OnLeftClick += (_, _) => CycleSortMode();
        AddTooltip(_sortButton, "Cycle sorting mode");
        controlsBar.Append(_sortButton);

        x += 150f;

        _sortDirectionButton = new UITextPanel<string>(_sortAscending ? "Ascending" : "Descending", 0.9f, true)
        {
            HAlign = 0f,
            VAlign = 0.5f
        };

        _sortDirectionButton.Left.Set(x, 0f);
        _sortDirectionButton.Top.Set(12f, 0f);
        _sortDirectionButton.OnLeftClick += (_, _) => ToggleSortDirection();
        AddTooltip(_sortDirectionButton, "Toggle ascending/descending");
        controlsBar.Append(_sortDirectionButton);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        _needsPopulate = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_needsPopulate)
        {
            TryPopulateExpeditionList();
        }
    }

    private void TryPopulateExpeditionList()
    {
        if (PopulateExpeditionList())
        {
            _needsPopulate = false;
        }
    }

    private void RequestExpeditionListRefresh()
    {
        _needsPopulate = true;
        TryPopulateExpeditionList();
    }

    private bool PopulateExpeditionList()
    {
        // Player data is unavailable while in the main menu or during mod loading, so defer
        // any ModPlayer access until a world and LocalPlayer have been created.
        if (Main.gameMenu)
        {
            return false;
        }

        if (Main.LocalPlayer == null)
        {
            return false;
        }

        _expeditionList.Clear();
        _entries.Clear();

        var registry = ModContent.GetInstance<ExpeditionRegistry>();
        var definitions = registry.Definitions.AsEnumerable();

        if (!string.Equals(_selectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            definitions = definitions.Where(definition => string.Equals(definition.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        var player = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();

        if (_filterRepeatableOnly)
        {
            definitions = definitions.Where(definition => definition.IsRepeatable);
        }

        if (_filterNpcHeadId.HasValue)
        {
            definitions = definitions.Where(definition => definition.NpcHeadId == _filterNpcHeadId.Value);
        }

        var orderedDefinitions = ApplySort(definitions, player);

        string trackedId = player?.TrackedExpeditionId ?? string.Empty;
        if (_filterTrackedOnly)
        {
            orderedDefinitions = orderedDefinitions.Where(view => view.IsTracked);
        }

        foreach (var view in orderedDefinitions)
        {
            if (_completionFilter == CompletionFilter.Available && !view.IsAvailable)
            {
                continue;
            }

            if (_completionFilter == CompletionFilter.Active && !view.IsActive)
            {
                continue;
            }

            if (_completionFilter == CompletionFilter.Completed && !view.IsCompleted)
            {
                continue;
            }

            var entry = new ExpeditionListEntry(this, view);
            _entries.Add(entry);
            _expeditionList.Add(entry);
        }

        _rarityMarkers.SetEntries(_entries.Select(entry => (entry.View, entry.Height.Pixels + _expeditionList.ListPadding)).ToList());
        return true;
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
            { IsActive: true } => "Status: Active",
            _ => "Status: Available"
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
                var row = new UIText($"• {FormatCondition(prerequisite)}", 0.8f);
                AddTooltip(row, prerequisite.Description);
                _detailsList.Add(row);
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
                int value = progress?.ConditionProgress.TryGetValue(deliverable.Id, out int current) == true ? current : 0;
                _detailsList.Add(CreateProgressRow(FormatDeliverable(deliverable), value, deliverable.RequiredCount));
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

        bool canStart = progress is null || (!progress.IsCompleted || definition.IsRepeatable);
        bool canTurnIn = progress is { IsCompleted: true } && !progress.RewardsClaimed;
        bool canTrack = player != null;

        var startButton = CreateActionButton("Start", canStart, () => player?.TryStartExpedition(definition.Id));
        startButton.Left.Set(0f, 0f);
        buttonRow.Append(startButton);

        var turnInButton = CreateActionButton("Turn In", canTurnIn, () =>
        {
            if (player == null)
                return;

            if (player.TryCompleteExpedition(definition.Id))
            {
                player.TryClaimRewards(definition.Id);
            }
        });
        turnInButton.Left.Set(140f, 0f);
        buttonRow.Append(turnInButton);

        bool isTracked = string.Equals(player?.TrackedExpeditionId, definition.Id, StringComparison.OrdinalIgnoreCase);
        var trackButton = CreateActionButton(isTracked ? "Untrack" : "Track", canTrack, () =>
        {
            player?.TryTrackExpedition(isTracked ? string.Empty : definition.Id);
            RequestExpeditionListRefresh();
        });
        trackButton.Left.Set(280f, 0f);
        buttonRow.Append(trackButton);

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

    internal static string FormatDeliverable(DeliverableDefinition definition)
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

    private void PopulateNpcHeads()
    {
        _npcHeads.Clear();
        var registry = ModContent.GetInstance<ExpeditionRegistry>();
        foreach (int head in registry.Definitions.Select(definition => definition.NpcHeadId).Distinct())
        {
            if (head >= 0)
            {
                _npcHeads.Add(head);
            }
        }

        UpdateNpcHeadTexture();
    }

    private void UpdateNpcHeadTexture()
    {
        if (_npcHeadButton == null)
            return;

        if (!_filterNpcHeadId.HasValue)
        {
            _npcHeadButton.SetImage(TextureAssets.NpcHead[0]);
            _npcHeadButton.Color = Color.Gray * 0.7f;
            return;
        }

        int headId = _filterNpcHeadId.Value;
        headId = Math.Clamp(headId, 0, TextureAssets.NpcHead.Length - 1);
        _npcHeadButton.SetImage(TextureAssets.NpcHead[headId]);
        _npcHeadButton.Color = Color.White;
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
        RequestExpeditionListRefresh();
    }

    private void CycleCompletionFilter()
    {
        _completionFilter = _completionFilter switch
        {
            CompletionFilter.All => CompletionFilter.Available,
            CompletionFilter.Available => CompletionFilter.Active,
            CompletionFilter.Active => CompletionFilter.Completed,
            _ => CompletionFilter.All
        };

        _completionButton.SetText(_completionFilter.ToString());
        RequestExpeditionListRefresh();
    }

    private void ToggleRepeatable()
    {
        _filterRepeatableOnly = !_filterRepeatableOnly;
        _repeatableButton.SetText(_filterRepeatableOnly ? "Repeatable: Yes" : "Repeatable: Any");
        RequestExpeditionListRefresh();
    }

    private void ToggleTrackedFilter()
    {
        _filterTrackedOnly = !_filterTrackedOnly;
        _trackedFilterButton.SetText(_filterTrackedOnly ? "Tracked: Only" : "Tracked: Any");
        RequestExpeditionListRefresh();
    }

    private void CycleNpcHead()
    {
        if (_npcHeads.Count == 0)
        {
            _filterNpcHeadId = null;
            UpdateNpcHeadTexture();
            RequestExpeditionListRefresh();
            return;
        }

        if (!_filterNpcHeadId.HasValue)
        {
            _filterNpcHeadId = _npcHeads.First();
        }
        else
        {
            int currentIndex = _npcHeads.IndexOf(_filterNpcHeadId.Value);
            int nextIndex = (currentIndex + 1) % (_npcHeads.Count + 1);
            _filterNpcHeadId = nextIndex >= _npcHeads.Count ? null : _npcHeads[nextIndex];
        }

        UpdateNpcHeadTexture();
        RequestExpeditionListRefresh();
    }

    private void CycleSortMode()
    {
        _sortMode = _sortMode switch
        {
            SortMode.Name => SortMode.Availability,
            SortMode.Availability => SortMode.Category,
            SortMode.Category => SortMode.Rarity,
            SortMode.Rarity => SortMode.Duration,
            SortMode.Duration => SortMode.Difficulty,
            _ => SortMode.Name
        };

        _sortButton.SetText(_sortMode.ToString());
        RequestExpeditionListRefresh();
    }

    private void ToggleSortDirection()
    {
        _sortAscending = !_sortAscending;
        _sortDirectionButton.SetText(_sortAscending ? "Ascending" : "Descending");
        RequestExpeditionListRefresh();
    }

    private IEnumerable<ExpeditionView> ApplySort(IEnumerable<ExpeditionDefinition> definitions, ExpeditionsPlayer? player)
    {
        var views = definitions.Select(definition => BuildView(definition, player));

        IOrderedEnumerable<ExpeditionView> ordered = _sortMode switch
        {
            SortMode.Availability => views.OrderByDescending(view => view.IsAvailable).ThenBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase),
            SortMode.Category => views.OrderBy(view => view.Category, StringComparer.OrdinalIgnoreCase).ThenBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase),
            SortMode.Rarity => views.OrderByDescending(view => view.Rarity).ThenBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase),
            SortMode.Duration => views.OrderBy(view => view.DurationTicks).ThenBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase),
            SortMode.Difficulty => views.OrderBy(view => view.Difficulty).ThenBy(view => view.DisplayName, StringComparer.OrdinalIgnoreCase),
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
        bool isCompleted = progress?.IsCompleted == true;
        bool isActive = progress?.IsActive == true && !isCompleted;
        string status = progress switch
        {
            null => "Not started",
            { IsCompleted: true } => "Completed",
            { IsActive: true } => "Active",
            _ => "Available"
        };

        float progressFraction = 0f;
        if (progress != null && definition.Deliverables.Count > 0)
        {
            int totalRequired = definition.Deliverables.Sum(d => d.RequiredCount);
            int totalProgress = definition.Deliverables.Sum(d => Math.Min(progress.ConditionProgress.TryGetValue(d.Id, out int value) ? value : 0, d.RequiredCount));
            progressFraction = totalRequired > 0 ? Math.Clamp(totalProgress / (float)totalRequired, 0f, 1f) : 0f;
        }

        bool isTracked = player != null && string.Equals(player.TrackedExpeditionId, definition.Id, StringComparison.OrdinalIgnoreCase);

        return new ExpeditionView(definition.Id, definition.DisplayName, definition.Category, status, isAvailable, isCompleted, isActive, definition.Rarity, definition.DurationTicks, definition.Difficulty, definition.NpcHeadId, definition.IsRepeatable, progressFraction, isTracked);
    }

    private UITextPanel<string> CreateActionButton(string label, bool enabled, Action? onClick)
    {
        var button = new UITextPanel<string>(label, 0.85f, true)
        {
            Width = StyleDimension.FromPixels(130f),
            Height = StyleDimension.FromPixels(32f),
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

    private UIElement CreateProgressRow(string label, int current, int required)
    {
        var row = new UIElement
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(40f)
        };

        var text = new UIText(label, 0.8f)
        {
            HAlign = 0f,
            VAlign = 0f
        };

        row.Append(text);

        var progressText = new UIText($"{current}/{required}", 0.8f)
        {
            HAlign = 1f,
            VAlign = 0f
        };

        row.Append(progressText);

        float fraction = required > 0 ? Math.Clamp(current / (float)required, 0f, 1f) : 0f;
        var bar = new SegmentedProgressBar
        {
            Top = StyleDimension.FromPixels(22f),
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(12f)
        };

        bar.SetProgress(fraction);
        row.Append(bar);

        return row;
    }

    private void AddSectionHeading(string text)
    {
        _detailsList.Add(new UIText(text, 0.85f, true)
        {
            TextColor = new Color(200, 210, 230)
        });
    }

    private static void AddTooltip(UIElement element, string tooltip)
    {
        element.OnMouseOver += (_, _) => Main.instance.MouseText(tooltip);
    }

    private static Color GetRarityColor(int rarity)
    {
        return rarity switch
        {
            >= 7 => new Color(255, 50, 255),
            6 => new Color(255, 128, 0),
            5 => new Color(180, 180, 255),
            4 => new Color(80, 200, 255),
            3 => new Color(80, 255, 120),
            2 => new Color(255, 215, 0),
            _ => new Color(200, 200, 200)
        };
    }

    private class RarityScrollbarMarkers : UIElement
    {
        private readonly List<(float position, Color color)> _markers = new();

        public void SetEntries(List<(ExpeditionView view, float height)> entries)
        {
            _markers.Clear();
            float totalHeight = entries.Sum(entry => entry.height);
            float running = 0f;

            foreach (var (view, height) in entries)
            {
                float normalized = totalHeight <= 0f ? 0f : running / totalHeight;
                _markers.Add((normalized, GetRarityColor(view.Rarity)));
                running += height;
            }
        }

        protected override void DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (_markers.Count == 0)
            {
                return;
            }

            var dim = GetDimensions();
            foreach (var (position, color) in _markers)
            {
                float y = dim.Y + position * dim.Height;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dim.X, (int)y, (int)dim.Width, 3), color * 0.9f);
            }
        }
    }

    private class ExpeditionListEntry : UIPanel
    {
        private readonly ExpeditionUI _owner;
        private readonly UIText _label;
        private readonly UIText _categoryText;
        private readonly UIText _statusText;
        private readonly SegmentedProgressBar _progressBar;
        private readonly UIImage _npcHead;
        private readonly Color _defaultBackground = new(44, 57, 105);
        private readonly Color _selectedBackground = new(72, 129, 191);

        public string ExpeditionId { get; }
        public ExpeditionView View { get; }

        public ExpeditionListEntry(ExpeditionUI owner, ExpeditionView view)
        {
            _owner = owner;
            View = view;
            ExpeditionId = view.Id;

            Height = StyleDimension.FromPixels(72f);
            Width = StyleDimension.FromPercent(1f);
            PaddingTop = 6f;
            PaddingBottom = 6f;
            BackgroundColor = _defaultBackground;
            BorderColor = new Color(80, 104, 192);

            int headIndex = Math.Clamp(view.NpcHeadId, 0, TextureAssets.NpcHead.Length - 1);
            _npcHead = new UIImage(TextureAssets.NpcHead[headIndex]);
            _npcHead.Left.Set(-54f, 1f);
            _npcHead.Top.Set(4f, 0f);
            _npcHead.Width.Set(44f, 0f);
            _npcHead.Height.Set(44f, 0f);
            Append(_npcHead);

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

            string statusLabel = view.IsAvailable ? $"Status: {view.Status}" : "Status: Unavailable";
            _statusText = new UIText(statusLabel, 0.75f)
            {
                HAlign = 0f,
                Top = new StyleDimension(40f, 0f),
                TextColor = view.IsAvailable ? Color.White : Color.LightGray
            };

            _progressBar = new SegmentedProgressBar
            {
                Top = StyleDimension.FromPixels(48f),
                Left = StyleDimension.FromPixels(0f),
                Width = new StyleDimension(-60f, 1f),
                Height = StyleDimension.FromPixels(10f)
            };

            _progressBar.SetProgress(view.ProgressFraction);

            Append(_label);
            Append(_categoryText);
            Append(_statusText);
            Append(_progressBar);

            AddTooltip(this, view.IsTracked ? "Currently tracked" : "Click to view details");
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

    internal class SegmentedProgressBar : UIElement
    {
        private float _progress;

        public void SetProgress(float value)
        {
            _progress = Math.Clamp(value, 0f, 1f);
        }

        protected override void DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            var dim = GetDimensions();
            int segments = 4;
            float segmentWidth = dim.Width / segments;

            for (int i = 0; i < segments; i++)
            {
                float left = dim.X + i * segmentWidth + 1f;
                var rect = new Rectangle((int)left, (int)dim.Y, (int)(segmentWidth - 2f), (int)dim.Height);
                float threshold = (i + 1) / (float)segments;
                Color color = _progress >= threshold ? new Color(120, 220, 120) : new Color(70, 80, 90);
                if (_progress > i / (float)segments && _progress < threshold)
                {
                    float partial = (_progress - i / (float)segments) * segments;
                    rect.Width = (int)(rect.Width * partial);
                    color = new Color(170, 230, 140);
                }

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, color);
            }
        }
    }

    private readonly struct ExpeditionView
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Status { get; }
        public bool IsAvailable { get; }
        public bool IsCompleted { get; }
        public bool IsActive { get; }
        public int Rarity { get; }
        public int DurationTicks { get; }
        public int Difficulty { get; }
        public int NpcHeadId { get; }
        public bool IsRepeatable { get; }
        public float ProgressFraction { get; }
        public bool IsTracked { get; }

        public ExpeditionView(string id, string displayName, string category, string status, bool isAvailable, bool isCompleted, bool isActive, int rarity, int durationTicks, int difficulty, int npcHeadId, bool isRepeatable, float progressFraction, bool isTracked)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            Status = status;
            IsAvailable = isAvailable;
            IsCompleted = isCompleted;
            IsActive = isActive;
            Rarity = rarity;
            DurationTicks = durationTicks;
            Difficulty = difficulty;
            NpcHeadId = npcHeadId;
            IsRepeatable = isRepeatable;
            ProgressFraction = progressFraction;
            IsTracked = isTracked;
        }
    }
}
