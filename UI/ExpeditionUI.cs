using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ExpeditionsReforged.UI;

public class ExpeditionUI : UIState
{
    private UIPanel _rootPanel = null!;

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
    }
}
