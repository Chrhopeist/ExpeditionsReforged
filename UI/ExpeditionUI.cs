using ExpeditionsReforged.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExpeditionsReforged.UI;

public class ExpeditionUI : UIState
{
    private UIPanel _rootPanel = null!;
    private UIList _expeditionList = null!;
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

        BuildExpeditionList();
    }

    private void BuildExpeditionList()
    {
        _entries.Clear();
        _selectedExpeditionId = string.Empty;

        _expeditionList = new UIList
        {
            ListPadding = 6f
        };

        var listWidth = StyleDimension.FromPercent(1f);
        listWidth.Pixels = -20f;
        _expeditionList.Width = listWidth;
        _expeditionList.Height = StyleDimension.FromPercent(1f);

        var scrollbar = new UIScrollbar
        {
            HAlign = 1f
        };

        scrollbar.Height = StyleDimension.FromPercent(1f);

        scrollbar.SetView(100f, 1000f);
        _expeditionList.SetScrollbar(scrollbar);

        var registry = ModContent.GetInstance<ExpeditionRegistry>();

        foreach (var definition in registry.Definitions.Values)
        {
            var entry = new ExpeditionListEntry(this, definition.Id, definition.DisplayName);

            _entries.Add(entry);
            _expeditionList.Add(entry);
        }

        _rootPanel.Append(_expeditionList);
        _rootPanel.Append(scrollbar);
    }

    private void HandleSelectionChanged(string expeditionId)
    {
        _selectedExpeditionId = expeditionId;

        foreach (var entry in _entries)
        {
            entry.SetSelected(entry.ExpeditionId == _selectedExpeditionId);
        }
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
