﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace SAssemblies.Healths
{
    using System.Drawing;

    using Color = SharpDX.Color;

    class Inhibitor
    {
        public static Menu.MenuItemSettings InhibitorHealth = new Menu.MenuItemSettings(typeof(Inhibitor));

        List<Health.HealthConf> healthConf = new List<Health.HealthConf>();
        private int lastGameUpdateTime = 0;

        public Inhibitor()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Game.OnUpdate += Game_OnGameUpdate;
            Health.Healths.GetMenuItem("SAssembliesHealthsTextScale").ValueChanged += Inhibitor_ValueChanged;
        }

        ~Inhibitor()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            healthConf = null;
        }

        public bool IsActive()
        {
#if HEALTHS
            return Health.Healths.GetActive() && InhibitorHealth.GetActive();
#else
            return InhibitorHealth.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesHealthsInhibitor");
            if (newMenu == null)
            {
                InhibitorHealth.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("HEALTHS_INHIBITOR_MAIN"), "SAssembliesHealthsInhibitor"));
                InhibitorHealth.CreateActiveMenuItem("SAssembliesHealthsInhibitorActive", () => new Inhibitor());
            }
            return InhibitorHealth;
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            foreach (Health.HealthConf health in healthConf.ToArray())
            {
                Obj_BarracksDampener objBarracks = health.Obj as Obj_BarracksDampener;
                if (objBarracks != null)
                {
                    if (objBarracks.IsValid)
                    {
                        if (((objBarracks.Health / objBarracks.MaxHealth) * 100) > 75)
                            health.Text.Color = Color.LightGreen;
                        else if (((objBarracks.Health / objBarracks.MaxHealth) * 100) <= 75)
                            health.Text.Color = Color.LightYellow;
                        else if (((objBarracks.Health / objBarracks.MaxHealth) * 100) <= 50)
                            health.Text.Color = Color.Orange;
                        else if (((objBarracks.Health / objBarracks.MaxHealth) * 100) <= 25)
                            health.Text.Color = Color.IndianRed;
                    }
                    else
                    {
                        healthConf.Remove(health);
                    }
                }
            }
        }

        private void Init()
        {
            if (!IsActive())
                return;

            foreach (Obj_BarracksDampener inhibitor in ObjectManager.Get<Obj_BarracksDampener>())
            {
                int health = 0;
                Render.Text Text = new Render.Text(0, 0, "", 14, new ColorBGRA(Color4.White));
                Text.TextUpdate = delegate
                {
                    if (!inhibitor.IsValid)
                        return "";
                    var mode =
                    Health.Healths.GetMenuItem("SAssembliesHealthsMode")
                        .GetValue<StringList>();
                    switch (mode.SelectedIndex)
                    {
                        case 0:
                            health = (int)((inhibitor.Health / inhibitor.MaxHealth) * 100);
                            break;

                        case 1:
                            health = (int)inhibitor.Health;
                            break;
                    }
                    return health.ToString();
                };
                Text.PositionUpdate = delegate
                {
                    if (!inhibitor.IsValid)
                        return new Vector2(0, 0);
                    Vector2 pos = Drawing.WorldToMinimap(inhibitor.Position);
                    return new Vector2(pos.X, pos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    if (!inhibitor.IsValid)
                        return false;
                    return IsActive() && inhibitor.IsValid && !inhibitor.IsDead && inhibitor.IsValid && inhibitor.Health > 0.1f &&
                    ((inhibitor.Health / inhibitor.MaxHealth) * 100) != 100;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add();

                healthConf.Add(new Health.HealthConf(inhibitor, Text));
            }
        }

        void Inhibitor_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            foreach (var conf in healthConf)
            {
                conf.Text.Remove();
                conf.Text.TextFontDescription = new FontDescription
                {
                    FaceName = "Calibri",
                    Height = e.GetNewValue<Slider>().Value,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                };
                conf.Text.Add();
            }
        }
    }
}
