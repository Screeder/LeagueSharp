using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = SharpDX.Color;

namespace SAssemblies.Trackers
{
    using Color = System.Drawing.Color;

    class MinimapAwareness
    {
        public static Menu.MenuItemSettings MinimapAwarenessTracker = new Menu.MenuItemSettings(typeof(MinimapAwareness));

        private static Dictionary<Obj_AI_Hero, InternalMinimapTracker> _enemies = new Dictionary<Obj_AI_Hero, InternalMinimapTracker>();

        private int lastGameUpdateTime = 0;

        public MinimapAwareness()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        ~MinimapAwareness()
        {
            Obj_AI_Base.OnTeleport -= Obj_AI_Base_OnTeleport;
            Game.OnUpdate -= Game_OnGameUpdate;
            Drawing.OnEndScene -= Drawing_OnEndScene;
            _enemies = null;
        }

        public bool IsActive()
        {
#if TRACKERS
            return Tracker.Trackers.GetActive() && MinimapAwarenessTracker.GetActive();
#else
            return MinimapAwarenessTracker.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesTrackersUim");
            if (newMenu == null)
            {
                MinimapAwarenessTracker.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TRACKERS_UIM_MAIN"), "SAssembliesTrackersUim"));
                MinimapAwarenessTracker.Menu.AddItem(new MenuItem("SAssembliesTrackersUimScale", Language.GetString("TRACKERS_UIM_SCALE")).SetValue(new Slider(100, 100, 0)));
                MinimapAwarenessTracker.Menu.AddItem(new MenuItem("SAssembliesTrackersUimShowSS", Language.GetString("TRACKERS_UIM_TIME")).SetValue(false));
                MinimapAwarenessTracker.Menu.AddItem(new MenuItem("SAssembliesTrackersUimShowCircleRange", Language.GetString("TRACKERS_UIM_CIRCLE_RANGE")).SetValue(new Slider(2000, 15000, 100)));
                MinimapAwarenessTracker.Menu.AddItem(new MenuItem("SAssembliesTrackersUimShowCircle", Language.GetString("TRACKERS_UIM_CIRCLE")).SetValue(false));
                MinimapAwarenessTracker.CreateActiveMenuItem("SAssembliesTrackersUimActive", () => new MinimapAwareness());
            }
            return MinimapAwarenessTracker;
        }

        private void Init()
        {
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    InternalMinimapTracker champ = new InternalMinimapTracker(hero);
                    champ.RecallInfo = new Packet.S2C.Teleport.Struct(hero.NetworkId, Packet.S2C.Teleport.Status.Unknown, Packet.S2C.Teleport.Type.Unknown, 0, 0);
                    champ = LoadTexts(champ);
                    _enemies.Add(hero, champ);
                }
            }
            new System.Threading.Thread(LoadSprites).Start();
        }

        InternalMinimapTracker LoadTexts(InternalMinimapTracker champ)
        {
            champ.Text = new Render.Text(0, 0, "", 14, SharpDX.Color.Orange);
            champ.Text.TextUpdate = delegate
            {
                if (champ.RecallInfo.Start != 0)
                {
                    float time = Environment.TickCount + champ.RecallInfo.Duration - champ.RecallInfo.Start;
                    if (time > 0.0f &&
                        (champ.RecallInfo.Status == Packet.S2C.Teleport.Status.Start))
                    {
                        return "Recalling";
                    }
                }
                if (champ.Timer.InvisibleTime != 0 && MinimapAwarenessTracker.GetMenuItem("SAssembliesTrackersUimShowSS").GetValue<bool>())
                {
                    return champ.Timer.InvisibleTime.ToString();
                }
                return "";
            };
            champ.Text.PositionUpdate = delegate
            {
                return Drawing.WorldToMinimap(champ.LastPosition);
            };
            champ.Text.VisibleCondition = sender =>
            {
                bool recall = false;

                if (champ.RecallInfo.Start != 0)
                {
                    float time = Environment.TickCount + champ.RecallInfo.Duration - champ.RecallInfo.Start;
                    if (time > 0.0f &&
                        (champ.RecallInfo.Status == Packet.S2C.Teleport.Status.Start))
                    {
                        recall = true;
                    }
                }
                return IsActive() && (recall || champ.Timer.InvisibleTime != 0);
            };
            champ.Text.OutLined = true;
            champ.Text.Centered = true;
            champ.Text.Add(4);
            return champ;
        }

        void Drawing_OnEndScene(EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (var enemy in _enemies)
            {
                Obj_AI_Hero hero = enemy.Key;
                if (!hero.IsVisible && !hero.IsDead && enemy.Value.LastPosition != Vector3.Zero && MinimapAwarenessTracker.GetMenuItem("SAssembliesTrackersUimShowCircle").GetValue<bool>())
                {
                    float radius = Math.Abs(enemy.Value.LastPosition.X - enemy.Value.PredictedPosition.X);
                    if (radius < MinimapAwarenessTracker.GetMenuItem("SAssembliesTrackersUimShowCircleRange").GetValue<Slider>().Value)
                    {
                        Utility.DrawCircle(enemy.Value.LastPosition, radius, System.Drawing.Color.Goldenrod, 1, 30, true);
                        if (enemy.Value.LastPosition.IsOnScreen())
                        {
                            Utility.DrawCircle(enemy.Value.LastPosition, radius, System.Drawing.Color.Goldenrod);
                        }
                    }
                    else if (radius >= MinimapAwarenessTracker.GetMenuItem("SAssembliesTrackersUimShowCircleRange").GetValue<Slider>().Value)
                    {
                        radius = MinimapAwarenessTracker.GetMenuItem("SAssembliesTrackersUimShowCircleRange").GetValue<Slider>().Value;
                        Utility.DrawCircle(enemy.Value.LastPosition, radius, System.Drawing.Color.Goldenrod, 1, 30, true);
                        if (enemy.Value.LastPosition.IsOnScreen())
                        {
                            Utility.DrawCircle(enemy.Value.LastPosition, radius, System.Drawing.Color.Goldenrod);
                        }
                    }
                }
            }
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;

            float percentScale = (float)MinimapAwarenessTracker.GetMenuItem("SAssembliesTrackersUimScale").GetValue<Slider>().Value / 100;
            foreach (var enemy in _enemies)
            {
                //if (enemy.Key.IsVisible)
                //{
                //    enemy.Value.LastPosition = enemy.Key.ServerPosition;
                //}
                enemy.Value.LastPosition = enemy.Key.ServerPosition;
                if (enemy.Value.SpriteInfo == null || enemy.Value.SpriteInfo.Sprite == null)
                {
                    SpriteHelper.LoadTexture(enemy.Value.Name, ref enemy.Value.SpriteInfo, "UIM");
                }
                if (enemy.Value.SpriteInfo != null && enemy.Value.SpriteInfo.DownloadFinished && !enemy.Value.SpriteInfo.LoadingFinished)
                {
                    enemy.Value.SpriteInfo.Sprite.GrayScale();
                    enemy.Value.SpriteInfo.Sprite.UpdateTextureBitmap(CropImage(enemy.Value.SpriteInfo.Sprite.Bitmap, enemy.Value.SpriteInfo.Sprite.Width));
                    enemy.Value.SpriteInfo.Sprite.Scale = new Vector2(((float)24 / enemy.Value.SpriteInfo.Sprite.Width) * percentScale, ((float)24 / enemy.Value.SpriteInfo.Sprite.Height) * percentScale);
                    enemy.Value.SpriteInfo.Sprite.PositionUpdate = delegate
                    {
                        Vector2 serverPos = Drawing.WorldToMinimap(enemy.Value.LastPosition);
                        var mPos = new Vector2((int)(serverPos[0] - 32 * 0.3f), (int)(serverPos[1] - 32 * 0.3f));
                        return new Vector2(mPos.X, mPos.Y);
                    };
                    enemy.Value.SpriteInfo.Sprite.VisibleCondition = delegate
                    {
                        return IsActive() && !enemy.Key.IsVisible && enemy.Value.LastPosition != Vector3.Zero;
                    };
                    enemy.Value.SpriteInfo.Sprite.Add(0);
                    enemy.Value.SpriteInfo.LoadingFinished = true;
                }
            }
        }

        void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            Packet.S2C.Teleport.Struct decoded = Packet.S2C.Teleport.Decoded(sender, args);
            foreach (var enemy in _enemies)
            {
                if (enemy.Value.RecallInfo.UnitNetworkId == decoded.UnitNetworkId)
                {
                    enemy.Value.RecallInfo = decoded;
                    if (decoded.Status == Packet.S2C.Teleport.Status.Finish)
                    {
                        Vector3 spawnPos = ObjectManager.Get<GameObject>().First(spawnPoint => spawnPoint is Obj_SpawnPoint &&
                                spawnPoint.Team != ObjectManager.Player.Team).Position;
                        enemy.Value.LastPosition = spawnPos;
                    }
                }
            }
        }

        void LoadSprites()
        {
            foreach (var enemy in _enemies)
            {
                enemy.Value.Name = SpriteHelper.DownloadImageRiot(enemy.Key.ChampionName, SpriteHelper.ChampionType.Champion, SpriteHelper.DownloadType.Champion, "UIM");
            }
        }

        public static Bitmap CropImage(Bitmap srcBitmap, int imageWidth)
        {
            Bitmap finalImage = new Bitmap(imageWidth, imageWidth);
            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(0, 0,
                imageWidth, imageWidth);

            using (Bitmap sourceImage = srcBitmap)
            using (Bitmap croppedImage = sourceImage.Clone(cropRect, sourceImage.PixelFormat))
            using (TextureBrush tb = new TextureBrush(croppedImage))
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                g.FillEllipse(tb, 0, 0, imageWidth, imageWidth);
                Pen p = new Pen(System.Drawing.Color.Black, 10) { Alignment = PenAlignment.Inset };
                g.DrawEllipse(p, 0, 0, imageWidth, imageWidth);
            }
            return finalImage;
        }

        class InternalMinimapTracker
        {
            public Obj_AI_Hero Hero;
            public SpriteHelper.SpriteInfo SpriteInfo;
            public Render.Text Text;
            public Packet.S2C.Teleport.Struct RecallInfo;
            public SsTimer Timer;
            public Vector3 LastPosition;
            public Vector3 PredictedPosition;
            public String Name;

            public InternalMinimapTracker(Obj_AI_Hero hero)
            {
                Hero = hero;
                Timer = new SsTimer(Hero);
                Game.OnUpdate += Game_OnGameUpdate;
            }

            ~InternalMinimapTracker()
            {
                Game.OnUpdate -= Game_OnGameUpdate;
            }

            private void Game_OnGameUpdate(EventArgs args)
            {
                PredictedPosition = new Vector3(LastPosition.X + ((Game.ClockTime - Timer.VisibleTime) * Hero.MoveSpeed), LastPosition.Y, LastPosition.Z);
                if (Hero.IsVisible)
                {
                    Timer.Active = true;
                }
            }
        }

        class SsTimer
        {
            public int InvisibleTime;
            public int VisibleTime;
            public Obj_AI_Hero Hero;
            public bool Active = false;

            public SsTimer(Obj_AI_Hero hero)
            {
                Hero = hero;
                Game.OnUpdate += Game_OnGameUpdate;
            }

            ~SsTimer()
            {
                Game.OnUpdate -= Game_OnGameUpdate;
                Hero = null;
            }

            private void Game_OnGameUpdate(EventArgs args)
            {
                if (!Active)
                {
                    return;
                }
                if (Hero.IsVisible)
                {
                    InvisibleTime = 0;
                    VisibleTime = (int)Game.Time;
                }
                else
                {
                    if (VisibleTime != 0)
                    {
                        InvisibleTime = (int)(Game.Time - VisibleTime);
                    }
                    else
                    {
                        InvisibleTime = 0;
                    }
                }
            }
        }
    }
}
