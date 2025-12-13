using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ExpeditionsReforged.UI
{
    public class TrackerUI : UIState
    {
        private UIPanel _rootPanel;

        public override void OnInitialize()
        {
            _rootPanel = new UIPanel();
            _rootPanel.Left.Set(0f, 0f);
            _rootPanel.Top.Set(0f, 0f);
            _rootPanel.Width.Set(0f, 1f);
            _rootPanel.Height.Set(0f, 1f);
            _rootPanel.SetPadding(16f);
            _rootPanel.BackgroundColor = new Color(30, 35, 52, 200);

            Append(_rootPanel);
        }
    }
}
