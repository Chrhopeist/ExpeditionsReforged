using System;
using System.Collections.Generic;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ID;

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
private readonly List<int> _questGiverNpcIds = new();
private string _selectedExpeditionId = string.Empty;
private string _selectedCategory = "All";
private CompletionFilter _completionFilter = CompletionFilter.All;
private bool _filterRepeatableOnly;
private bool _filterTrackedOnly;
private int? _filterQuestGiverNpcId;
private SortMode _sortMode = SortMode.Name;
private bool _sortAscending = true;
private bool _needsPopulate = true;
private bool _wasPlayerOpen;
private UITextPanel<string> _closeButton = null!;
private float _uiScale = 1f;
private UIElement _detailsListContainer = null!;
private UIElement _detailsFooter = null!;
private UIElement _detailsButtonRow = null!;

private const float BaseScreenHeight = 1080f;
private const float MinUiScale = 0.85f;
private const float MaxUiScale = 1f;
private const float ListPanelWidthPixels = 320f;
private const float DetailsFooterHeightPixels = 42f;
private const float FilterBarHeightPixels = 118f;
private const float FilterRowHeightPixels = 32f;
private const float FilterRowPaddingPixels = 6f;
private const float FilterButtonHeightPixels = 32f;
// Tighten horizontal spacing so the top control strip stays on one line at 1024x768.
private const float FilterButtonPaddingPixels = 4f;

public override void OnInitialize()
{
// UIState instances are constructed during mod loading when no players exist; keep any
// LocalPlayer/ModPlayer access out of OnInitialize and wire only visual structure here.
UpdateUiScale();
InitializeRootPanel();
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

private void InitializeRootPanel()
{
_rootPanel = new UIPanel
{
BackgroundColor = new Color(34, 40, 52),
BorderColor = new Color(69, 82, 110)
};

// Slightly tighter padding keeps the layout readable at 1024x768 without cramped panels.
_rootPanel.SetPadding(Scale(8f));
// Match Terraria's inventory footprint: centered, fixed pixel size with safe max bounds.
_rootPanel.HAlign = 0.5f;
_rootPanel.VAlign = 0.5f;
_rootPanel.Width = StyleDimension.FromPixels(Scale(900f));
_rootPanel.Height = StyleDimension.FromPixels(Scale(600f));
_rootPanel.MaxWidth = StyleDimension.FromPixels(Scale(1100f));
_rootPanel.MaxHeight = StyleDimension.FromPixels(Scale(720f));

// Client-only close control anchored to the top-right of the root panel.
_closeButton = new UITextPanel<string>("X", 0.9f * _uiScale, true)
{
HAlign = 1f,
VAlign = 0f,
Width = StyleDimension.FromPixels(Scale(32f)),
Height = StyleDimension.FromPixels(Scale(32f)),
BackgroundColor = new Color(60, 60, 60),
BorderColor = new Color(110, 140, 220),
TextColor = Color.White
};

_closeButton.Left.Set(-Scale(42f), 1f);
_closeButton.Top.Set(Scale(10f), 0f);
_closeButton.OnLeftClick += (_, _) =>
{
// UI is client-only; closing it should not mutate gameplay state or send packets.
ExpeditionsPlayer? player = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
if (player != null)
{
player.ExpeditionUIOpen = false;
}
};

AddTooltip(_closeButton, "Close");
}

private bool UpdateUiScale()
{
// Scale the UI based on resolution while clamping to avoid oversized layouts on high-res displays.
float scale = MathHelper.Clamp(Main.screenHeight / BaseScreenHeight, MinUiScale, MaxUiScale);
if (Math.Abs(scale - _uiScale) < 0.001f)
{
return false;
}

_uiScale = scale;
return true;
}

private void RebuildLayoutForScale()
{
if (_rootPanel.Parent != null)
{
RemoveChild(_rootPanel);
}

InitializeRootPanel();
Append(_rootPanel);
BuildLayout();
RequestExpeditionListRefresh();
}

private float Scale(float value) => value * _uiScale;

private void BuildLayout()
{
_entries.Clear();

float listWidthPixels = Scale(ListPanelWidthPixels);
float controlsHeightPixels = Scale(FilterBarHeightPixels);
var controlsBar = new UIElement
{
Width = StyleDimension.FromPercent(1f),
Height = StyleDimension.FromPixels(controlsHeightPixels)
};

BuildControls(controlsBar);
// Keep the close button aligned with the controls bar to avoid covering the list or details panels.
controlsBar.Append(_closeButton);

_bodyContainer = new UIElement
{
Top = StyleDimension.FromPixels(controlsHeightPixels + Scale(2f)),
Width = StyleDimension.FromPercent(1f),
Height = new StyleDimension(-(controlsHeightPixels + Scale(2f)), 1f)
};

var listContainer = new UIElement
{
Width = StyleDimension.FromPixels(listWidthPixels),
Height = StyleDimension.FromPercent(1f)
};

_expeditionList = new UIList
{
// Match padding with other scrollable lists for consistent spacing.
ListPadding = Scale(4f),
Width = StyleDimension.FromPercent(1f),
Height = StyleDimension.FromPercent(1f)
};

_expeditionScrollbar = new UIScrollbar
{
HAlign = 1f
};

_expeditionScrollbar.Height = StyleDimension.FromPercent(1f);
_expeditionScrollbar.SetView(Scale(100f), Scale(1000f));
_expeditionList.SetScrollbar(_expeditionScrollbar);

_rarityMarkers = new RarityScrollbarMarkers(_uiScale);
_rarityMarkers.Left.Set(-Scale(6f), 1f);
_rarityMarkers.Width.Set(Scale(6f), 0f);
_rarityMarkers.Height.Set(0f, 1f);

PopulateCategories();
PopulateNpcHeads();

listContainer.Append(_expeditionList);
listContainer.Append(_expeditionScrollbar);
listContainer.Append(_rarityMarkers);

_detailsPanel = new UIPanel
{
Left = StyleDimension.FromPixels(listWidthPixels + Scale(8f)),
Width = new StyleDimension(-listWidthPixels - Scale(8f), 1f),
Height = StyleDimension.FromPercent(1f),
BackgroundColor = new Color(40, 46, 60),
BorderColor = new Color(69, 82, 110)
};

// Reduce padding to keep the details panel readable on low-resolution displays.
_detailsPanel.SetPadding(Scale(8f));

BuildDetailsPanel();

_bodyContainer.Append(listContainer);
_bodyContainer.Append(_detailsPanel);

_rootPanel.Append(controlsBar);
_rootPanel.Append(_bodyContainer);
}

private void BuildControls(UIElement controlsBar)
{
// Build a single-row control strip to keep every filter in one horizontal band.
var topControlsRow = new UIElement
{
Width = StyleDimension.FromPercent(1f),
Height = StyleDimension.FromPixels(Scale(FilterRowHeightPixels)),
VAlign = 0.5f
};

topControlsRow.SetPadding(0f);
controlsBar.Append(topControlsRow);

var categoryLabel = CreateFilterLabel("Category:", 65f);
topControlsRow.Append(categoryLabel);

_categoryButton = CreateFilterButton(_selectedCategory, 110f);
_categoryButton.OnLeftClick += (_, _) => CycleCategory();
AddTooltip(_categoryButton, "Cycle expedition categories");
topControlsRow.Append(_categoryButton);

var npcLabel = CreateFilterLabel("NPC:", 35f);
topControlsRow.Append(npcLabel);

_npcHeadButton = new UIImage(TextureAssets.MagicPixel)
{
HAlign = 0f,
VAlign = 0.5f,
Color = Color.Gray
};

_npcHeadButton.Width.Set(Scale(36f), 0f);
_npcHeadButton.Height.Set(Scale(36f), 0f);
_npcHeadButton.OnLeftClick += (_, _) => CycleNpcHead();
AddTooltip(_npcHeadButton, "Cycle NPC head filter");
topControlsRow.Append(_npcHeadButton);

_completionButton = CreateFilterButton(_completionFilter.ToString(), 110f);
_completionButton.OnLeftClick += (_, _) => CycleCompletionFilter();
AddTooltip(_completionButton, "Filter by availability/active/completed");
topControlsRow.Append(_completionButton);

_repeatableButton = CreateFilterButton("Repeatable: Any", 110f);
_repeatableButton.OnLeftClick += (_, _) => ToggleRepeatable();
AddTooltip(_repeatableButton, "Toggle showing only repeatable expeditions");
topControlsRow.Append(_repeatableButton);

_trackedFilterButton = CreateFilterButton("Tracked: Any", 110f);
_trackedFilterButton.OnLeftClick += (_, _) => ToggleTrackedFilter();
AddTooltip(_trackedFilterButton, "Toggle showing only tracked expedition");
topControlsRow.Append(_trackedFilterButton);

var sortLabel = CreateFilterLabel("Sort:", 35f);
topControlsRow.Append(sortLabel);

_sortButton = CreateFilterButton(_sortMode.ToString(), 100f);
_sortButton.OnLeftClick += (_, _) => CycleSortMode();
AddTooltip(_sortButton, "Cycle sorting mode");
topControlsRow.Append(_sortButton);

_sortDirectionButton = CreateFilterButton(_sortAscending ? "Ascending" : "Descending", 100f);
_sortDirectionButton.OnLeftClick += (_, _) => ToggleSortDirection();
AddTooltip(_sortDirectionButton, "Toggle ascending/descending");
topControlsRow.Append(_sortDirectionButton);

LayoutControlsRow(
topControlsRow,
categoryLabel,
_categoryButton,
npcLabel,
_npcHeadButton,
_completionButton,
_repeatableButton,
_trackedFilterButton,
sortLabel,
_sortButton,
_sortDirectionButton);
}

private void LayoutControlsRow(UIElement row, params UIElement[] children)
{
// Manually lay out controls so they stay on one row and shrink buttons as needed.
float spacing = Scale(FilterButtonPaddingPixels);
row.Recalculate();
float rowWidth = row.GetInnerDimensions().Width;

float minButtonWidth = Scale(80f);
float npcHeadWidth = _npcHeadButton?.Width.Pixels ?? Scale(36f);
float npcHeadHeight = _npcHeadButton?.Height.Pixels ?? Scale(36f);

var preferredWidths = new Dictionary<UIElement, float>
{
{ _categoryButton, Scale(110f) },
{ _completionButton, Scale(110f) },
{ _repeatableButton, Scale(110f) },
{ _trackedFilterButton, Scale(110f) },
{ _sortButton, Scale(100f) },
{ _sortDirectionButton, Scale(100f) }
};

float fixedWidth = 0f;
float totalPreferredButtonWidth = 0f;

foreach (UIElement element in children)
{
element.Recalculate();
if (element == _npcHeadButton)
{
fixedWidth += npcHeadWidth;
continue;
}

if (preferredWidths.TryGetValue(element, out float preferredWidth))
{
totalPreferredButtonWidth += preferredWidth;
continue;
}

fixedWidth += element.GetOuterDimensions().Width;
}

float totalSpacing = spacing * Math.Max(0, children.Length - 1);
float availableForButtons = Math.Max(0f, rowWidth - fixedWidth - totalSpacing);
float scale = totalPreferredButtonWidth > 0f ? Math.Min(1f, availableForButtons / totalPreferredButtonWidth) : 1f;

var resolvedButtonWidths = new Dictionary<UIElement, float>();
float remainingWidth = availableForButtons;
int remainingButtons = 0;

foreach (var pair in preferredWidths)
{
float scaledWidth = Math.Max(minButtonWidth, pair.Value * scale);
resolvedButtonWidths[pair.Key] = scaledWidth;
remainingWidth -= scaledWidth;
remainingButtons++;
}

// If the minimum width forces overflow, compress proportionally while honoring the minimum.
if (remainingWidth < 0f && remainingButtons > 0)
{
float excess = -remainingWidth;
float adjustableTotal = 0f;

foreach (var pair in preferredWidths)
{
float currentWidth = resolvedButtonWidths[pair.Key];
adjustableTotal += Math.Max(0f, currentWidth - minButtonWidth);
}

if (adjustableTotal > 0f)
{
foreach (var pair in preferredWidths)
{
float currentWidth = resolvedButtonWidths[pair.Key];
float adjustable = Math.Max(0f, currentWidth - minButtonWidth);
float reduction = adjustable / adjustableTotal * excess;
resolvedButtonWidths[pair.Key] = Math.Max(minButtonWidth, currentWidth - reduction);
}
}
}

float x = 0f;
foreach (UIElement element in children)
{
float width = element.GetOuterDimensions().Width;
float height = element.GetOuterDimensions().Height;

if (element == _npcHeadButton)
{
width = npcHeadWidth;
height = npcHeadHeight;
element.Width.Set(width, 0f);
element.Height.Set(height, 0f);
}
else if (resolvedButtonWidths.TryGetValue(element, out float buttonWidth))
{
width = buttonWidth;
height = Scale(FilterButtonHeightPixels);
element.Width.Set(width, 0f);
element.Height.Set(height, 0f);
}

element.Left.Set(x, 0f);
element.Top.Set((row.GetInnerDimensions().Height - height) * 0.5f, 0f);
element.Recalculate();
x += width + spacing;
}
}

private UIText CreateFilterLabel(string text, float widthPixels)
{
var label = new UIText(text, 0.9f * _uiScale)
{
HAlign = 0f,
VAlign = 0.5f,
Width = StyleDimension.FromPixels(Scale(widthPixels))
};

return label;
}

private UITextPanel<string> CreateFilterButton(string text, float widthPixels)
{
var button = new UITextPanel<string>(text, 0.9f * _uiScale, true)
{
HAlign = 0f,
VAlign = 0.5f,
Width = StyleDimension.FromPixels(Scale(widthPixels)),
Height = StyleDimension.FromPixels(Scale(FilterButtonHeightPixels))
};

return button;
}

public override void OnActivate()
{
base.OnActivate();
if (UpdateUiScale())
{
RebuildLayoutForScale();
}
_needsPopulate = true;
}

public override void Update(GameTime gameTime)
{
base.Update(gameTime);

// UIState.Update can run even while the player is not ready; only rebuild when a world is
// active and the expedition window is actually open to avoid race conditions during load.
if (Main.gameMenu)
{
_wasPlayerOpen = false;
return;
}

if (!TryGetActivePlayer(out ExpeditionsPlayer? player))
{
_wasPlayerOpen = false;
return;
}

bool isOpen = player.ExpeditionUIOpen;
if (isOpen && !_wasPlayerOpen)
{
// Refresh the list when the UI is opened so we only populate after the player exists.
_needsPopulate = true;
}

_wasPlayerOpen = isOpen;

if (!_needsPopulate || !isOpen)
{
return;
}

if (PopulateExpeditionList(player))
{
_needsPopulate = false;
}
}

private void RequestExpeditionListRefresh()
{
// Mark the list dirty; Update will rebuild once a player is available and the UI is open to
// avoid redundant work while filters are toggled rapidly.
_needsPopulate = true;
}

private bool TryGetActivePlayer(out ExpeditionsPlayer? player)
{
// Safely gate LocalPlayer/ModPlayer access; Update can run during load before a player exists.
player = null;
if (Main.gameMenu || Main.LocalPlayer == null)
{
return false;
}

player = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
return player != null;
}

private bool PopulateExpeditionList(ExpeditionsPlayer player)
{
// At this point the UI is open and a LocalPlayer exists; keep population client-only and read-only.

_expeditionList.Clear();
_entries.Clear();

var registry = ModContent.GetInstance<ExpeditionRegistry>();
var definitions = registry.Definitions.AsEnumerable();

if (!string.Equals(_selectedCategory, "All", StringComparison.OrdinalIgnoreCase))
{
definitions = definitions.Where(definition => string.Equals(definition.CategoryName, _selectedCategory, StringComparison.OrdinalIgnoreCase));
}

if (_filterRepeatableOnly)
{
definitions = definitions.Where(definition => definition.IsRepeatable);
}

if (_filterQuestGiverNpcId.HasValue)
{
definitions = definitions.Where(definition => definition.QuestGiverNpcId == _filterQuestGiverNpcId.Value);
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
// The details panel is split into a scrollable content region and a fixed footer for action buttons.
_detailsListContainer = new UIElement
{
Width = StyleDimension.FromPercent(1f),
Height = new StyleDimension(-Scale(DetailsFooterHeightPixels), 1f)
};

_detailsList = new UIList
{
Width = StyleDimension.FromPercent(1f),
Height = StyleDimension.FromPercent(1f),
// Match padding with the expedition list for consistency.
ListPadding = Scale(4f)
};

_detailsPlaceholder = new UIText("Select an expedition to view its details.", 0.9f * _uiScale)
{
HAlign = 0f,
VAlign = 0f
};

_detailsFooter = new UIElement
{
Width = StyleDimension.FromPercent(1f),
Height = StyleDimension.FromPixels(Scale(DetailsFooterHeightPixels)),
VAlign = 1f
};

_detailsButtonRow = new UIElement
{
Width = StyleDimension.FromPercent(1f),
Height = StyleDimension.FromPixels(Scale(36f)),
VAlign = 0.5f
};

_detailsFooter.Append(_detailsButtonRow);
_detailsPanel.Append(_detailsListContainer);
_detailsPanel.Append(_detailsFooter);

ShowPlaceholder();
}

private void HandleSelectionChanged(string expeditionId)
{
_selectedExpeditionId = expeditionId;

var registry = ModContent.GetInstance<ExpeditionRegistry>();

if (registry.TryGetExpedition(_selectedExpeditionId, out ExpeditionDefinition definition))
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
_detailsListContainer.RemoveChild(_detailsList);
}

if (_detailsPlaceholder.Parent == null)
{
_detailsPlaceholder.Top.Set(Scale(4f), 0f);
_detailsListContainer.Append(_detailsPlaceholder);
}

_detailsPlaceholder.SetText("Select an expedition to view its details.");
_detailsButtonRow.RemoveAllChildren();
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
_detailsListContainer.RemoveChild(_detailsPlaceholder);
}

if (_detailsList.Parent == null)
{
_detailsListContainer.Append(_detailsList);
}

_detailsList.Clear();

var player = TryGetActivePlayer(out ExpeditionsPlayer? activePlayer) ? activePlayer : null;
ExpeditionProgress? progress = player?.ExpeditionProgressEntries.FirstOrDefault(progressEntry => progressEntry.ExpeditionId == definition.Id);

_detailsList.Add(new UIText(definition.DisplayName, 0.95f * _uiScale, true));
_detailsList.Add(new UIText(definition.Description, 0.85f * _uiScale));
_detailsList.Add(new UIText($"Category: {definition.CategoryName}", 0.85f * _uiScale));
_detailsList.Add(new UIText($"Duration: {FormatDuration(definition.DurationTicks)}", 0.85f * _uiScale));
_detailsList.Add(new UIText($"Difficulty: {definition.Difficulty}", 0.85f * _uiScale));
_detailsList.Add(new UIText($"Minimum Level: {definition.MinPlayerLevel}", 0.85f * _uiScale));
_detailsList.Add(new UIText(definition.IsRepeatable ? "Repeatable: Yes" : "Repeatable: No", 0.85f * _uiScale));

var statusText = progress switch
{
null => "Status: Not started",
{ IsCompleted: true } => "Status: Completed",
{ IsActive: true } => "Status: Active",
_ => "Status: Available"
};

_detailsList.Add(new UIText(statusText, 0.85f * _uiScale));

AddSectionHeading("Prerequisites");
if (definition.Prerequisites.Count == 0)
{
_detailsList.Add(new UIText("• None", 0.8f * _uiScale));
}
else
{
foreach (var prerequisite in definition.Prerequisites)
{
var row = new UIText($"• {FormatCondition(prerequisite)}", 0.8f * _uiScale);
AddTooltip(row, prerequisite.Description);
_detailsList.Add(row);
}
}

AddSectionHeading("Deliverables");
if (definition.Deliverables.Count == 0)
{
_detailsList.Add(new UIText("• None", 0.8f * _uiScale));
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
_detailsList.Add(new UIText("• None", 0.8f * _uiScale));
}
else
{
foreach (var reward in definition.Rewards)
{
_detailsList.Add(new UIText($"• {FormatReward(reward)}", 0.8f * _uiScale));
}
}

if (definition.DailyRewards.Count > 0)
{
AddSectionHeading("Daily Bonus Rewards");
foreach (var reward in definition.DailyRewards)
{
_detailsList.Add(new UIText($"• {FormatReward(reward)}", 0.8f * _uiScale));
}
}

_detailsButtonRow.RemoveAllChildren();
bool canClaim = progress != null && progress.IsCompleted && !progress.RewardsClaimed;
bool isTracked = activePlayer != null &&
    string.Equals(activePlayer.TrackedExpeditionId, definition.Id, StringComparison.OrdinalIgnoreCase);

// Expedition acceptance should happen through NPC interactions, not the log UI.
var startButton = CreateActionButton("Start", false, null);
AddTooltip(startButton, "Accept expeditions by speaking with the quest giver.");
startButton.Left.Set(0f, 0f);
_detailsButtonRow.Append(startButton);

var claimButton = CreateActionButton("Claim", canClaim, () =>
{
    activePlayer?.TryClaimRewards(definition.Id);
    RequestExpeditionListRefresh();
});
claimButton.Left.Set(Scale(132f), 0f);
_detailsButtonRow.Append(claimButton);

bool canTrack = activePlayer != null && (isTracked || progress != null);
var trackButton = CreateActionButton(isTracked ? "Untrack" : "Track", canTrack, () =>
{
    activePlayer?.TryTrackExpedition(isTracked ? string.Empty : definition.Id);
    RequestExpeditionListRefresh();
});
trackButton.Left.Set(Scale(264f), 0f);
_detailsButtonRow.Append(trackButton);
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
foreach (var category in registry.Definitions.Select(definition => definition.CategoryName).Where(category => !string.IsNullOrWhiteSpace(category)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category, StringComparer.OrdinalIgnoreCase))
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
_questGiverNpcIds.Clear();
var registry = ModContent.GetInstance<ExpeditionRegistry>();
foreach (int head in registry.Definitions.Select(definition => definition.QuestGiverNpcId).Distinct())
{
if (head >= 0)
{
_questGiverNpcIds.Add(head);
}
}

UpdateNpcHeadTexture();
}

private void UpdateNpcHeadTexture()
{
if (_npcHeadButton == null)
return;

if (!_filterQuestGiverNpcId.HasValue)
{
_npcHeadButton.SetImage(TextureAssets.NpcHead[0]);
_npcHeadButton.Color = Color.Gray * 0.7f;
return;
}

if (TryGetQuestGiverHeadTexture(_filterQuestGiverNpcId.Value, out Asset<Texture2D> headTexture))
{
_npcHeadButton.SetImage(headTexture);
_npcHeadButton.Color = Color.White;
return;
}

// Fallback to a generic head icon if the NPC head lookup is invalid.
_npcHeadButton.SetImage(TextureAssets.NpcHead[0]);
_npcHeadButton.Color = Color.Gray * 0.7f;
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
if (_questGiverNpcIds.Count == 0)
{
_filterQuestGiverNpcId = null;
UpdateNpcHeadTexture();
RequestExpeditionListRefresh();
return;
}

if (!_filterQuestGiverNpcId.HasValue)
{
_filterQuestGiverNpcId = _questGiverNpcIds.First();
}
else
{
int currentIndex = _questGiverNpcIds.IndexOf(_filterQuestGiverNpcId.Value);
int nextIndex = (currentIndex + 1) % (_questGiverNpcIds.Count + 1);
_filterQuestGiverNpcId = nextIndex >= _questGiverNpcIds.Count ? null : _questGiverNpcIds[nextIndex];
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

return new ExpeditionView(definition.Id, definition.DisplayName, definition.CategoryName, status, isAvailable, isCompleted, isActive, definition.Rarity, definition.DurationTicks, definition.Difficulty, definition.QuestGiverNpcId, definition.IsRepeatable, progressFraction, isTracked);
}

private UITextPanel<string> CreateActionButton(string label, bool enabled, Action? onClick)
{
var button = new UITextPanel<string>(label, 0.85f * _uiScale, true)
{
Width = StyleDimension.FromPixels(Scale(130f)),
Height = StyleDimension.FromPixels(Scale(32f)),
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
Height = StyleDimension.FromPixels(Scale(40f))
};

var text = new UIText(label, 0.8f * _uiScale)
{
HAlign = 0f,
VAlign = 0f
};

row.Append(text);

var progressText = new UIText($"{current}/{required}", 0.8f * _uiScale)
{
HAlign = 1f,
VAlign = 0f
};

row.Append(progressText);

float fraction = required > 0 ? Math.Clamp(current / (float)required, 0f, 1f) : 0f;
var bar = new SegmentedProgressBar
{
Top = StyleDimension.FromPixels(Scale(22f)),
Width = StyleDimension.FromPercent(1f),
Height = StyleDimension.FromPixels(Scale(12f))
};

bar.SetProgress(fraction);
row.Append(bar);

return row;
}

private void AddSectionHeading(string text)
{
_detailsList.Add(new UIText(text, 0.85f * _uiScale, true)
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

public static bool TryGetQuestGiverHeadTexture(int questGiverNpcId, out Asset<Texture2D> headTex)
{
headTex = null!;
// Only attempt to resolve head assets using the ContentSamples cache so we never instantiate new NPCs.
if (!ContentSamples.NpcsByNetId.TryGetValue(questGiverNpcId, out NPC sampleNpc))
{
return false;
}

int derivedHeadIndex = Terraria.GameContent.TownNPCProfiles.GetHeadIndexSafe(sampleNpc);
if (derivedHeadIndex < 0 || derivedHeadIndex >= Terraria.GameContent.TextureAssets.NpcHead.Length)
{
return false;
}

headTex = Terraria.GameContent.TextureAssets.NpcHead[derivedHeadIndex];
return headTex != null;
}

private class RarityScrollbarMarkers : UIElement
{
private readonly List<(float position, Color color)> _markers = new();
private readonly float _markerScale;

public RarityScrollbarMarkers(float uiScale)
{
_markerScale = uiScale;
}

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
int markerHeight = Math.Max(1, (int)MathF.Round(3f * _markerScale));
spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dim.X, (int)y, (int)dim.Width, markerHeight), color * 0.9f);
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

Height = StyleDimension.FromPixels(owner.Scale(72f));
Width = StyleDimension.FromPercent(1f);
PaddingTop = owner.Scale(6f);
PaddingBottom = owner.Scale(6f);
BackgroundColor = _defaultBackground;
BorderColor = new Color(80, 104, 192);

Asset<Texture2D> headTexture = TextureAssets.NpcHead[0];
Color headColor = Color.Gray * 0.7f;
if (TryGetQuestGiverHeadTexture(view.QuestGiverNpcId, out Asset<Texture2D> resolvedHead))
{
headTexture = resolvedHead;
headColor = Color.White;
}

_npcHead = new UIImage(headTexture)
{
Color = headColor
};
_npcHead.Left.Set(-owner.Scale(54f), 1f);
_npcHead.Top.Set(owner.Scale(4f), 0f);
_npcHead.Width.Set(owner.Scale(44f), 0f);
_npcHead.Height.Set(owner.Scale(44f), 0f);
Append(_npcHead);

_label = new UIText(view.DisplayName, 0.9f * owner._uiScale)
{
HAlign = 0f,
VAlign = 0f
};

_categoryText = new UIText($"Category: {view.Category}", 0.75f * owner._uiScale)
{
HAlign = 0f,
Top = new StyleDimension(owner.Scale(22f), 0f)
};

string statusLabel = view.IsAvailable ? $"Status: {view.Status}" : "Status: Unavailable";
_statusText = new UIText(statusLabel, 0.75f * owner._uiScale)
{
HAlign = 0f,
Top = new StyleDimension(owner.Scale(40f), 0f),
TextColor = view.IsAvailable ? Color.White : Color.LightGray
};

_progressBar = new SegmentedProgressBar
{
Top = StyleDimension.FromPixels(owner.Scale(48f)),
Left = StyleDimension.FromPixels(0f),
Width = new StyleDimension(-owner.Scale(60f), 1f),
Height = StyleDimension.FromPixels(owner.Scale(10f))
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
public int QuestGiverNpcId { get; }
public bool IsRepeatable { get; }
public float ProgressFraction { get; }
public bool IsTracked { get; }

public ExpeditionView(string id, string displayName, string category, string status, bool isAvailable, bool isCompleted, bool isActive, int rarity, int durationTicks, int difficulty, int questGiverNpcId, bool isRepeatable, float progressFraction, bool isTracked)
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
QuestGiverNpcId = questGiverNpcId;
IsRepeatable = isRepeatable;
ProgressFraction = progressFraction;
IsTracked = isTracked;
}
}
}
