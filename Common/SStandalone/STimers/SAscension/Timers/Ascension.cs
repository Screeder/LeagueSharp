using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace SAssemblies.Timers
{
    class Ascension
    {
        public static Menu.MenuItemSettings AscensionTimer = new Menu.MenuItemSettings(typeof(Ascension));

        private static readonly Utility.Map GMap = Utility.Map.GetMap();
        private static List<AscensionObject> Ascensions = new List<AscensionObject>();
        private int lastGameUpdateTime = 0;

        public Ascension()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Game.OnUpdate += Game_OnGameUpdate;
        }

        ~Ascension()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            Ascensions = null;
        }

        public static bool IsActive()
        {
#if TIMERS
            return Timer.Timers.GetActive() && HealthTimer.GetActive();
#else
            return AscensionTimer.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesTimersAscension");
            if (newMenu == null)
            {
                AscensionTimer.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TIMERS_ASCENSION_MAIN"), "SAssembliesTimersAscension"));
                AscensionTimer.CreateActiveMenuItem("SAssembliesTimersAscensionActive", () => new Ascension());
            }
            return AscensionTimer;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            AscensionObject healthDestroyed = null;
            foreach (AscensionObject health in Ascensions)
            {
                if (health.Obj.IsValid)
                {
                    if (health.Obj.Health > 0)
                    {
                        health.Locked = false;
                        health.NextRespawnTime = 0;
                        health.Called = false;
                    }
                    else if (health.Obj.Health < 1 && health.Locked == false)
                    {
                        health.Locked = true;
                        health.NextRespawnTime = health.RespawnTime + (int)Game.ClockTime;
                    }
                }
                if (health.NextRespawnTime < (int)Game.ClockTime && health.Locked)
                {
                    healthDestroyed = health;
                }
            }
            if (healthDestroyed != null)
            {
                healthDestroyed.TextMinimap.Dispose();
                healthDestroyed.TextMinimap.Remove();
                healthDestroyed.TextMap.Dispose();
                healthDestroyed.TextMap.Remove();
                Ascensions.Remove(healthDestroyed);
            }
            foreach (Obj_AI_Minion health in ObjectManager.Get<Obj_AI_Minion>())
            {
                AscensionObject nHealth = null;
                if (health.Name.Contains("AscRelic") || health.Name.Contains("OdinShieldRelic"))
                {
                    AscensionObject health1 = Ascensions.Find(jm => jm.Obj.NetworkId == health.NetworkId);
                    if (health1 == null)
                        nHealth = new AscensionObject(health);
                }

                if (nHealth != null)
                    Ascensions.Add(nHealth);
            }

            /////

            foreach (AscensionObject health in Ascensions)
            {
                if (health.Locked)
                {
                    if (health.NextRespawnTime - (int)Game.ClockTime <= 0 || health.MapType != GMap.Type)
                        continue;
                    int time = Timer.Timers.GetMenuItem("SAssembliesTimersRemindTime").GetValue<Slider>().Value;
                    if (!health.Called && health.NextRespawnTime - (int)Game.ClockTime <= time &&
                        health.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                    {
                        health.Called = true;
                        Timer.PingAndCall("Relic unlocks in " + time + " seconds!", health.Position);
                    }
                }
            }
        }

        public void Init()
        {
            foreach (Obj_AI_Minion objectType in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (objectType.Name.Contains("AscRelic") || objectType.Name.Contains("OdinShieldRelic"))
                    Ascensions.Add(new AscensionObject(objectType));
            }
        }

        public class AscensionObject
        {
            public bool Called;
            public bool Locked;
            public Utility.Map.MapType MapType;
            public int NextRespawnTime;
            public Obj_AI_Minion Obj;
            public Vector3 Position;
            public int RespawnTime;
            public int SpawnTime;
            public Render.Text TextMinimap;
            public Render.Text TextMap;

            public AscensionObject()
            {

            }

            public AscensionObject(Obj_AI_Minion obj)
            {
                Obj = obj;
                if (obj != null && obj.IsValid)
                    Position = obj.Position;
                else
                    Position = new Vector3();
                SpawnTime = (int)Game.ClockTime;
                MapType = Utility.Map.MapType.CrystalScar;
                if (obj.Name.Contains("AscRelic"))
                {
                    RespawnTime = 30;
                }
                else if (obj.Name.Contains("OdinShieldRelic"))
                {
                    RespawnTime = 32;
                } 
                Locked = false;
                Called = false;
                TextMinimap = new Render.Text(0, 0, "", Timer.Timers.GetMenuItem("SAssembliesTimersTextScale").GetValue<Slider>().Value, new ColorBGRA(Color4.White));
                Timer.Timers.GetMenuItem("SAssembliesTimersTextScale").ValueChanged += HealthObject_ValueChanged;
                TextMinimap.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                TextMinimap.PositionUpdate = delegate
                {
                    Vector2 sPos = Drawing.WorldToMinimap(Position);
                    return new Vector2(sPos.X, sPos.Y);
                };
                TextMinimap.VisibleCondition = sender =>
                {
                    return IsActive() && (NextRespawnTime - (int)Game.ClockTime) > 0 && MapType == GMap.Type;
                };
                TextMinimap.OutLined = true;
                TextMinimap.Centered = true;
                TextMinimap.Add();
                TextMap = new Render.Text(0, 0, "", (int)(Timer.Timers.GetMenuItem("SAssembliesTimersTextScale").GetValue<Slider>().Value * 3.5), new ColorBGRA(Color4.White));
                TextMap.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                TextMap.PositionUpdate = delegate
                {
                    Vector2 sPos = Drawing.WorldToScreen(Position);
                    return new Vector2(sPos.X, sPos.Y);
                };
                TextMap.VisibleCondition = sender =>
                {
                    return IsActive() && (NextRespawnTime - (int)Game.ClockTime) > 0 && MapType == GMap.Type;
                };
                TextMap.OutLined = true;
                TextMap.Centered = true;
                TextMap.Add();
            }

            void HealthObject_ValueChanged(object sender, OnValueChangeEventArgs e)
            {
                TextMinimap.Remove();
                TextMinimap.TextFontDescription = new FontDescription
                {
                    FaceName = "Calibri",
                    Height = e.GetNewValue<Slider>().Value,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                };
                TextMinimap.Add();
                TextMap.Remove();
                TextMap.TextFontDescription = new FontDescription
                {
                    FaceName = "Calibri",
                    Height = (int)(e.GetNewValue<Slider>().Value * 3.5),
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                };
                TextMap.Add();
            }
        }
    }
}
