using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ExpeditionsReforged.Systems
{
    /// <summary>
    /// Client-side configuration that controls presentation of expedition UI elements.
    /// </summary>
    public class ExpeditionsClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        [Label("Auto-show tracker when an expedition is tracked")]
        public bool TrackerAutoShow { get; set; }

        [DefaultValue(1f)]
        [Range(0.6f, 1.6f)]
        [Label("Tracker scale")] 
        public float TrackerScale { get; set; }

        [DefaultValue(0.9f)]
        [Range(0.25f, 1f)]
        [Label("Tracker alpha")]
        public float TrackerAlpha { get; set; }

        [DefaultValue(typeof(Vector2), "32, 120")]
        [Label("Tracker position (pixels)")]
        public Vector2 TrackerPosition { get; set; }

        [DefaultValue(0.2f)]
        [Range(0.05f, 1f)]
        [Label("Fade speed (alpha per second)")]
        public float TrackerFadeSpeed { get; set; }
    }
}
