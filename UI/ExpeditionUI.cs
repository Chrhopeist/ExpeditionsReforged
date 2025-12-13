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
            var entry = new UIText(definition.DisplayName)
            {
                Width = { Percent = 1f }
            };

            _expeditionList.Add(entry);
        }

        _rootPanel.Append(_expeditionList);
        _rootPanel.Append(scrollbar);
    }
}
