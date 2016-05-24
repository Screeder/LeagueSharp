using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAssemblies.Miscs
{
    using LeagueSharp.SDK.Core.UI.Animations;

    using SharpDX;

    class LanePower // TODO:: Add Moveable
    {
        public static Menu.MenuItemSettings LanePowerMisc = new Menu.MenuItemSettings(typeof(LanePower));

        private Dictionary<String, Double> minionPower = new Dictionary<string, double>
                                                             {
            { "SRU_ChaosMinionMelee", 0.5 },
            { "SRU_ChaosMinionRanged", 0.35 },
            { "SRU_ChaosMinionSiege", 1.5 },
            { "SRU_ChaosMinionSuper", 4.0 },
            { "SRU_OrderMinionMelee", 0.5 },
            { "SRU_OrderMinionRanged", 0.35 },
            { "SRU_OrderMinionSiege", 1.5 },
            { "SRU_OrderMinionSuper", 4.0 },
                                                             };

        private Dictionary<String, Lane> turretBonus = new Dictionary<string, Lane> {
            { "Turret_T2_R_01_A", Lane.Bot },
            { "Turret_T2_R_02_A", Lane.Bot },
            { "Turret_T2_R_03_A", Lane.Bot },
            { "Turret_T2_C_03_A", Lane.Mid },
            { "Turret_T2_C_04_A", Lane.Mid },
            { "Turret_T2_C_05_A", Lane.Mid },
            { "Turret_T2_L_01_A", Lane.Top },
            { "Turret_T2_L_02_A", Lane.Top },
            { "Turret_T2_L_03_A", Lane.Top },
            { "Turret_T1_C_07_A", Lane.Bot },
            { "Turret_T1_R_02_A", Lane.Bot },
            { "Turret_T1_R_03_A", Lane.Bot },
            { "Turret_T1_C_03_A", Lane.Mid },
            { "Turret_T1_C_04_A", Lane.Mid },
            { "Turret_T1_C_05_A", Lane.Mid },
            { "Turret_T1_C_06_A", Lane.Top },
            { "Turret_T1_L_02_A", Lane.Top },
            { "Turret_T1_L_03_A", Lane.Top },
                                                             };

        private Dictionary<Obj_AI_Minion, MinionStruct> minions = new Dictionary<Obj_AI_Minion, MinionStruct>();
        private Dictionary<Obj_AI_Turret, Lane> turrets = new Dictionary<Obj_AI_Turret, Lane>();

        private List<Obj_AI_Hero> heroes = new List<Obj_AI_Hero>();

        private Dictionary<Lane, DrawingsClass> drawings = new Dictionary<Lane, DrawingsClass>();

        private bool moveActive = false;
        private Vector2 lastCursorPos;

        enum Lane
        {
            Unknown,
            Top,
            Mid,
            Bot   
        }

        class DrawingsClass
        {
            public List<Render.RenderObject> Drawings;

            public AnimationSlide AnimArrowRight = new AnimationSlide(AnimationSlide.Mode.Right, 100, 1.5f);
            public AnimationSlide AnimArrowLeft = new AnimationSlide(AnimationSlide.Mode.Left, 100, 1.5f);

            public PowerDiff.Orientation CurrentDirection = PowerDiff.Orientation.None;
        }

        struct MinionStruct
        {
            public Lane Lane;

            public double Power;

            public bool Active;
        }

        struct PowerDiff
        {
            public enum Orientation
            {
                None,
                Ally,
                Enemy
            }

            public double Ally;

            public double Enemy;

            public Orientation Direction;
        }

        public LanePower()
        {
            if (Game.MapId != GameMapId.SummonersRift)
                return;

            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                heroes.Add(hero);
            }

            foreach (Obj_AI_Minion minion in ObjectManager.Get<Obj_AI_Minion>())
            {
                Obj_AI_Minion_OnCreate(minion, null);
            }

            foreach (Obj_AI_Turret turret in ObjectManager.Get<Obj_AI_Turret>())
            {
                if (turretBonus.ContainsKey(turret.Name))
                {
                    turrets.Add(turret, turretBonus[turret.Name]);
                }
            }

            Common.ExecuteInOnGameUpdate(() => Init());

            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += Obj_AI_Minion_OnCreate;
            GameObject.OnDelete += Obj_AI_Minion_OnDelete;
        }

        ~LanePower()
        {
            Game.OnUpdate -= Game_OnUpdate;
            Game.OnWndProc -= Game_OnWndProc;
            Obj_AI_Base.OnProcessSpellCast -= Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate -= Obj_AI_Minion_OnCreate;
            GameObject.OnDelete -= Obj_AI_Minion_OnDelete;
        }

        public bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && LanePowerMisc.GetActive();
#else
            return LanePowerMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsAntiVisualScreenStealth");
            if (newMenu == null)
            {
                LanePowerMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_LANEPOWER_MAIN"), "SAssembliesMiscsLanePower"));
                LanePowerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsLanePowerShowAnimation", Language.GetString("MISCS_LANEPOWER_ANIMATION")).SetValue(true));
                LanePowerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsLanePowerPositionX", Language.GetString("MISCS_LANEPOWER_POSITION_X")).SetValue(new Slider(Drawing.Width - 250, 0, Drawing.Width)));
                LanePowerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsLanePowerPositionY", Language.GetString("MISCS_LANEPOWER_POSITION_Y")).SetValue(new Slider(Drawing.Height - 100, 0, Drawing.Height)));
                LanePowerMisc.CreateActiveMenuItem("SAssembliesMiscsLanePowerActive", () => new LanePower());
            }
            return LanePowerMisc;
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!IsActive())
                return;

            Obj_AI_Minion unit = sender as Obj_AI_Minion;
            if (unit != null)
            {
                foreach (KeyValuePair<Obj_AI_Minion, MinionStruct> minion in minions.ToArray())
                {
                    if (unit.NetworkId == minion.Key.NetworkId)
                    {
                        MinionStruct value = minion.Value;
                        value.Active = true;
                        minions[minion.Key] = value;
                    }
                }
            }
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (KeyValuePair<Obj_AI_Minion, MinionStruct> minion in minions.ToArray())
            {
                if (!minion.Key.IsValid)
                {
                    this.minions.Remove(minion.Key);
                }
            }

            int posX = LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionX").GetValue<Slider>().Value;
            int posY = LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionY").GetValue<Slider>().Value;

            int i = 0;
            foreach (KeyValuePair<Lane, DrawingsClass> drawing in drawings)
            {
                PowerDiff diff = GetPowerDifference(drawing.Key);
                diff.Ally = MaxDiff(diff.Ally);
                diff.Enemy = MaxDiff(diff.Enemy);
                float sumDiff = 10 + (float)(diff.Ally - diff.Enemy);

                Render.Rectangle bgRec = (Render.Rectangle)drawing.Value.Drawings[0];
                bgRec.X = posX;
                bgRec.Y = posY + (i * 50);

                Render.Rectangle greenRec = (Render.Rectangle)drawing.Value.Drawings[1];
                greenRec.X = posX + 1;
                greenRec.Y = posY + 1 + (i * 50);
                greenRec.Width = (int)(10 * sumDiff);

                Render.Rectangle redRec = (Render.Rectangle)drawing.Value.Drawings[2];
                redRec.X = (int)(greenRec.X + 10 * sumDiff);
                redRec.Y = posY + 1 + (i * 50);
                redRec.Width = 200 - (int)(10 * sumDiff);

                Render.Text nameText = (Render.Text)drawing.Value.Drawings[3];
                nameText.X = posX + 100;
                nameText.Y = posY - 10 + (i * 50);

                Render.Text percentText = (Render.Text)drawing.Value.Drawings[4];
                percentText.X = posX + 100;
                percentText.Y = posY + 20 + (i * 50);
                percentText.text = "Power " + (100f / 20f * sumDiff).ToString("0.00") + " %";

                Render.Text directionText = (Render.Text)drawing.Value.Drawings[5];
                directionText.Y = posY + 3 + (i * 50);

                i++;
                if (this.IsActive())
                {
                    directionText.Visible = true;
                }
                if (!LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerShowAnimation").GetValue<bool>())
                {
                    drawing.Value.AnimArrowLeft.Stop();
                    drawing.Value.AnimArrowRight.Stop();
                    drawing.Value.CurrentDirection = PowerDiff.Orientation.None;
                    directionText.X = posX + 100;
                }
                if (diff.Direction == PowerDiff.Orientation.None)
                {
                    drawing.Value.CurrentDirection = PowerDiff.Orientation.None;
                    directionText.X = posX + 100;
                    directionText.Visible = false;
                }
                else if (diff.Direction == PowerDiff.Orientation.Ally)
                {
                    if (drawing.Value.CurrentDirection != PowerDiff.Orientation.Ally)
                    {
                        directionText.X = posX + 100;
                        drawing.Value.AnimArrowRight.Start(new Vector2(directionText.X, directionText.Y));
                    }
                    if (drawing.Value.AnimArrowRight.IsWorking)
                    {
                        directionText.X = (int)drawing.Value.AnimArrowRight.GetCurrentValue().X;
                    }
                    else
                    {
                        directionText.X = posX + 100;
                        drawing.Value.AnimArrowRight.Start(new Vector2(directionText.X, directionText.Y));
                        directionText.X = (int)drawing.Value.AnimArrowRight.GetCurrentValue().X;
                    }
                    drawing.Value.CurrentDirection = PowerDiff.Orientation.Ally;
                    directionText.text = "→";
                    directionText.Visible = true;
                }
                else if (diff.Direction == PowerDiff.Orientation.Enemy)
                {
                    if (drawing.Value.CurrentDirection != PowerDiff.Orientation.Enemy)
                    {
                        directionText.X = posX + 110;
                        drawing.Value.AnimArrowLeft.Start(new Vector2(directionText.X, directionText.Y));
                    }
                    if (drawing.Value.AnimArrowLeft.IsWorking)
                    {
                        directionText.X = (int)drawing.Value.AnimArrowLeft.GetCurrentValue().X;
                    }
                    else
                    {
                        directionText.X = posX + 110;
                        drawing.Value.AnimArrowLeft.Start(new Vector2(directionText.X, directionText.Y));
                        directionText.X = (int)drawing.Value.AnimArrowLeft.GetCurrentValue().X;
                    }
                    drawing.Value.CurrentDirection = PowerDiff.Orientation.Enemy;
                    directionText.text = "←";
                    directionText.Visible = true;
                }
            }
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (!IsActive())
                return;

            HandleInput((WindowsMessages)args.Msg, Utils.GetCursorPos(), args.WParam);
        }

        private void HandleInput(WindowsMessages message, Vector2 cursorPos, uint key)
        {
            if (message != WindowsMessages.WM_LBUTTONDOWN && message != WindowsMessages.WM_MOUSEMOVE &&
                message != WindowsMessages.WM_LBUTTONUP || (!moveActive && message == WindowsMessages.WM_MOUSEMOVE)
                )
            {
                return;
            }
            if (message == WindowsMessages.WM_LBUTTONDOWN)
            {
                lastCursorPos = cursorPos;
            }
            if (message == WindowsMessages.WM_LBUTTONUP)
            {
                lastCursorPos = new Vector2();
                moveActive = false;
                return;
            }
            Render.Rectangle bgRec = (Render.Rectangle)drawings[Lane.Top].Drawings[0];
            if (Common.IsInside(cursorPos, new System.Drawing.Point(bgRec.X, bgRec.Y - 10),
                    bgRec.Width, (bgRec.Height * 3) + (50 * 3) + 20))
            {
                moveActive = true;
                if (message == WindowsMessages.WM_MOUSEMOVE)
                {
                    var curSliderX =
                        LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionX").GetValue<Slider>();
                    var curSliderY =
                        LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionY").GetValue<Slider>();
                    LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionX").SetValue<Slider>(
                        new Slider(curSliderX.Value + (int)cursorPos.X - (int)lastCursorPos.X, curSliderX.MinValue, curSliderX.MaxValue));
                    LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionY").SetValue<Slider>(
                        new Slider(curSliderY.Value + (int)cursorPos.Y - (int)lastCursorPos.Y, curSliderY.MinValue, curSliderY.MaxValue));
                    lastCursorPos = cursorPos;
                }
            }
        }

        private void Obj_AI_Minion_OnCreate(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;

            Obj_AI_Minion minion = sender as Obj_AI_Minion;
            if (minion != null)
            {
                if (minionPower.ContainsKey(minion.BaseSkinName))
                {
                    minions.Add(minion, new MinionStruct() { Lane = GetLane(minion), Power = minionPower[minion.BaseSkinName], Active = false });
                }
            }
        }

        private void Obj_AI_Minion_OnDelete(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (KeyValuePair<Obj_AI_Minion, MinionStruct> minion in minions.ToArray())
            {
                if (minion.Key.IsValid && minion.Key.NetworkId == sender.NetworkId)
                {
                    minions.Remove(minion.Key);
                }
            }
        }

        private void Init()
        {
            int posX = LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionX").GetValue<Slider>().Value;
            int posY = LanePowerMisc.GetMenuItem("SAssembliesMiscsLanePowerPositionY").GetValue<Slider>().Value;
            for (int i = 1; i <= (int)Lane.Bot; i++)
            {
                List<Render.RenderObject> recs = new List<Render.RenderObject>();
                Render.Rectangle bgRec = new Render.Rectangle(posX, posY + (i * 50), 202, 12, Color.Black);
                bgRec.VisibleCondition = delegate
                {
                    return this.IsActive();
                };
                bgRec.Add(1);

                Render.Rectangle greenRec = new Render.Rectangle(posX + 1, posY + 1 + (i * 50), 100, 10, Color.GreenYellow);
                greenRec.VisibleCondition = delegate
                {
                    return this.IsActive();
                };
                greenRec.Add(1);

                Render.Rectangle redRec = new Render.Rectangle(posX + 101, posY + 1 + (i * 50), 100, 10, Color.Red);
                redRec.VisibleCondition = delegate
                {
                    return this.IsActive();
                };
                redRec.Add(1);

                Render.Text nameText = new Render.Text(((Lane)i).ToString(), posX + 100, posY - 10 + (i * 50), 16, Color.Orange);
                nameText.OutLined = true;
                nameText.Centered = true;
                nameText.VisibleCondition = delegate
                {
                    return this.IsActive();
                };
                nameText.Add(1);

                Render.Text directionText = new Render.Text("→", posX + 100, posY + 3 + (i * 50), 20, Color.Peru);
                directionText.OutLined = true;
                directionText.Centered = true;
                directionText.Add(2);

                Render.Text percentText = new Render.Text("Power 50 %", posX + 100, posY + 20 + (i * 50), 16, Color.Orange);
                percentText.OutLined = true;
                percentText.Centered = true;
                percentText.VisibleCondition = delegate
                {
                    return this.IsActive();
                };
                percentText.Add(1);

                recs.Add(bgRec);
                recs.Add(greenRec);
                recs.Add(redRec);
                recs.Add(nameText);
                recs.Add(percentText);
                recs.Add(directionText);

                drawings.Add((Lane)i, new DrawingsClass() { Drawings = recs } );
            }
        }

        private double MaxDiff(double value)
        {
            return value > 10 ? 10 : value < -10 ? -10 : value;
        }

        private PowerDiff GetPowerDifference(Lane lane)
        {
            PowerDiff powerDiff = new PowerDiff();
            powerDiff.Direction = PowerDiff.Orientation.None;

            int allyCount = 0;
            int enemyCount = 0;

            foreach (KeyValuePair<Obj_AI_Minion, MinionStruct> minion in minions)
            {
                if (minion.Key != null && minion.Key.IsValid && minion.Value.Lane == lane && minion.Value.Active)
                {
                    if (ObjectManager.Player.Team == minion.Key.Team)
                    {
                        allyCount++;
                        powerDiff.Ally += minion.Value.Power + (minion.Value.Power * GetTurretBonus(minion));
                    }
                    else
                    {
                        enemyCount++;
                        powerDiff.Enemy += minion.Value.Power + (minion.Value.Power * GetTurretBonus(minion));
                    }
                }
            }

            if (allyCount > 0 && enemyCount > 0)
            {
                int teamDiff = allyCount - enemyCount;
                if (teamDiff > 4)
                {
                    powerDiff.Ally += 2;
                    powerDiff.Direction = PowerDiff.Orientation.Ally;
                }
                else if (teamDiff < -4)
                {
                    powerDiff.Enemy += 2;
                    powerDiff.Direction = PowerDiff.Orientation.Enemy;
                }
            }
            else if (enemyCount == 0 && allyCount > 7)
            {
                powerDiff.Ally += 2;
                powerDiff.Direction = PowerDiff.Orientation.Ally;
            }
            else if (allyCount == 0 && enemyCount > 7)
            {
                powerDiff.Enemy += 2;
                powerDiff.Direction = PowerDiff.Orientation.Enemy;
            }

            return powerDiff;
        }

        private Lane GetLane(Obj_AI_Minion minion)
        {
            if (minion.Name.Contains("L0"))
            {
                return Lane.Bot;
            }
            else if (minion.Name.Contains("L1"))
            {
                return Lane.Mid;
            }
            else if (minion.Name.Contains("L2"))
            {
                return Lane.Top;
            }
            return Lane.Unknown;
        }

        private double GetHeroLevelDiff(GameObjectTeam team)
        {
            int maxAllies = 0;
            int maxEnemies = 0;
            int sumLevelAllies = 0;
            int sumLevelEnemies = 0;

            foreach (Obj_AI_Hero hero in heroes)
            {
                if (team == hero.Team)
                {
                    maxAllies++;
                    sumLevelAllies += hero.Level;
                }
                else
                {
                    maxEnemies++;
                    sumLevelEnemies += hero.Level;
                }
            }

            double leveldiff = (maxAllies > 0 ? sumLevelAllies / maxAllies : 0) - (maxEnemies > 0 ? sumLevelEnemies / maxEnemies : 0);
            return Math.Min(Math.Max(leveldiff, -3), 3);
        }

        private double GetTurretDiff(Lane lane, GameObjectTeam team)
        {
            int turretDiff = 0;

            foreach (KeyValuePair<Obj_AI_Turret, Lane> turret in turrets)
            {
                if (turret.Key.IsValid && lane == turret.Value)
                {
                    if (team == turret.Key.Team)
                    {
                        turretDiff++;
                    }
                    else
                    {
                        turretDiff--;
                    }
                }
            }

            return turretDiff;
        }

        private double GetTurretBonus(KeyValuePair<Obj_AI_Minion, MinionStruct> minion)
        {
            double bonus = 0.0;
            double  level = GetHeroLevelDiff(minion.Key.Team);
            if (level != 0)
            {
                if (level > 0)
                {
                    Lane lane = minion.Value.Lane;
                    if (lane != Lane.Unknown)
                    {
                        double turretDiff = GetTurretDiff(lane, minion.Key.Team);
                        bonus = 0.05 + (0.05 * Math.Max(0, turretDiff));
                    }
                }
                else if (minion.Key.Target != null)
                {
                    Lane lane = minion.Value.Lane;
                    if (lane != Lane.Unknown)
                    {
                        double turretDiff = GetTurretDiff(lane, minion.Key.Target.Team);
                        bonus = -0.05 - (0.05 * Math.Max(0, turretDiff));
                    }
                }
            }
            return bonus;
        }

    }
}
