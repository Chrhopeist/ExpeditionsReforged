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
    private string? _selectedExpeditionId;

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
        _selectedExpeditionId = null;

        _expeditionList = new UIList
        {
            Width = { Pixels = -20f, Percent = 1f },
            Height = { Percent = 1f },
            ListPadding = 6f
        };

        var scrollbar = new UIScrollbar
        {
            HAlign = 1f,
            Height = { Percent = 1f }
        };

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
        private readonly Color _defaultBackground = new(63, 82, 151) * 0.7f;
        private readonly Color _selectedBackground = new(96, 172, 255) * 0.75f;

        public string ExpeditionId { get; }

        public ExpeditionListEntry(ExpeditionUI owner, string expeditionId, string displayName)
        {
            _owner = owner;
            ExpeditionId = expeditionId;

            Height = { Pixels = 32f };
            Width = { Percent = 1f };
            PaddingTop = 6f;
            PaddingBottom = 6f;
            BackgroundColor = _defaultBackground;
            BorderColor = new Color(89, 116, 213) * 0.9f;

            _label = new UIText(displayName)
            {
                HAlign = 0f,
                VAlign = 0.5f
            };

            Append(_label);
        }

        public override void OnLeftClick(UIMouseEvent evt)
        {
            base.OnLeftClick(evt);
            _owner.HandleSelectionChanged(ExpeditionId);
        }

        public void SetSelected(bool isSelected)
        {
            BackgroundColor = isSelected ? _selectedBackground : _defaultBackground;
        }
    }
}
