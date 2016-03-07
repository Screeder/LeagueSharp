﻿using System;
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
    class HealthRelic
    {
        public static Menu.MenuItemSettings HealthRelicTimer = new Menu.MenuItemSettings(typeof(HealthRelic));

        private static readonly Utility.Map GMap = Utility.Map.GetMap();
        private static List<HealthObject> Healths = new List<HealthObject>();
        private int lastGameUpdateTime = 0;

        public HealthRelic()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Game.OnUpdate += Game_OnGameUpdate;
        }

        ~HealthRelic()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            Healths = null;
        }

        public static bool IsActive()
        {
#if TIMERS
            return Timer.Timers.GetActive() && HealthTimer.GetActive();
#else
            return HealthRelicTimer.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesTimersHealth");
            if (newMenu == null)
            {
                HealthRelicTimer.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TIMERS_HEALTH_MAIN"), "SAssembliesTimersHealth"));
                HealthRelicTimer.CreateActiveMenuItem("SAssembliesTimersHealthActive", () => new HealthRelic());
            }
            return HealthRelicTimer;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            HealthObject healthDestroyed = null;
            foreach (HealthObject health in Healths)
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
                Healths.Remove(healthDestroyed);
            }
            foreach (Obj_AI_Minion health in ObjectManager.Get<Obj_AI_Minion>())
            {
                HealthObject nHealth = null;
                if (health.Name.Contains("HealthRelic") || health.Name.Contains("OdinShieldRelic"))
                {
                    HealthObject health1 = Healths.Find(jm => jm.Obj.NetworkId == health.NetworkId);
                    if (health1 == null)
                        nHealth = new HealthObject(health);
                }

                if (nHealth != null)
                    Healths.Add(nHealth);
            }

            /////

            foreach (HealthObject health in Healths)
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
                        Timer.PingAndCall("Heal unlocks in " + time + " seconds!", health.Position);
                    }
                }
            }
        }

        public void Init()
        {
            foreach (Obj_AI_Minion objectType in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (objectType.Name.Contains("HealthRelic") || objectType.Name.Contains("OdinShieldRelic"))
                    Healths.Add(new HealthObject(objectType));
            }
        }

        public class HealthObject
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

            public HealthObject()
            {

            }

            public HealthObject(Obj_AI_Minion obj)
            {
                Obj = obj;
                if (obj != null && obj.IsValid)
                    Position = obj.Position;
                else
                    Position = new Vector3();
                SpawnTime = (int)Game.ClockTime;
                if (GMap.Type == Utility.Map.MapType.HowlingAbyss)
                {
                    MapType = Utility.Map.MapType.HowlingAbyss;
                    RespawnTime = 40;
                }
                else if (GMap.Type == 0)
                {
                    MapType = 0;
                    RespawnTime = 40;
                }
                else if (GMap.Type == Utility.Map.MapType.CrystalScar)
                {
                    MapType = Utility.Map.MapType.CrystalScar;
                    RespawnTime = 32;
                }
                else if (GMap.Type == Utility.Map.MapType.TwistedTreeline)
                {
                    MapType = Utility.Map.MapType.TwistedTreeline;
                    RespawnTime = 90;
                }
                NextRespawnTime = 0;
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
                    Height = e.GetNewValue<Slider>().Value,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                };
                TextMap.Add();
            }
        }
    }
}
