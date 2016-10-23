using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;

namespace SAssemblies.Miscs
{
    using Color = System.Drawing.Color;

    internal class SmartPingImprovement //https://www.youtube.com/watch?v=HBvZZWSrmng
    {
        public static Menu.MenuItemSettings SmartPingImprovementMisc = new Menu.MenuItemSettings(typeof(SmartPingImprovement));

        private List<PingInfo> pingInfo = new List<PingInfo>();

        private Dictionary<Obj_AI_Hero, ChampionInfo> allies = new Dictionary<Obj_AI_Hero, ChampionInfo>();

        public SmartPingImprovement()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Game.OnPing += Game_OnPing;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        ~SmartPingImprovement()
        {
            Game.OnPing -= Game_OnPing;
            Drawing.OnDraw -= Drawing_OnDraw;
            Drawing.OnEndScene -= Drawing_OnEndScene;
            pingInfo = null;
        }

        public bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && SmartPingImproveMisc.GetActive();
#else
            return SmartPingImprovementMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            SmartPingImprovementMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_SMARTPINGIMPROVE_MAIN"), "SAssembliesMiscsSmartPingImprovement"));
            SmartPingImprovementMisc.CreateActiveMenuItem("SAssembliesMiscsSmartPingImproveActive", () => new SmartPingImprovement());
            return SmartPingImprovementMisc;
        }

        void Game_OnPing(GamePingEventArgs args)
        {
            if (!IsActive())
                return;

            Obj_AI_Hero hero = args.Source as Obj_AI_Hero;
            if (hero != null && hero.IsValid)
            {
                foreach (var info in pingInfo)
                {
                    if (info.NetworkId == hero.NetworkId && info.Time - 2 < Game.Time && info.Type == args.PingType)
                    {
                        return;
                    }
                }
                PingInfo pingInfoN = new PingInfo(hero.NetworkId, args.Position, Game.Time + 4, args.PingType);
                pingInfo.Add(pingInfoN);
                switch (args.PingType)
                {
                    case PingCategory.AssistMe:
                        CreateSprites(pingInfoN);
                        break;

                    case PingCategory.Danger:
                        CreateSprites(pingInfoN);
                        break;

                    case PingCategory.OnMyWay:
                        CreateSprites(pingInfoN);
                        break;
                }
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (var info in pingInfo.ToList())
            {
                if (info.Time < Game.Time)
                {
                    DeleteSprites(info);
                    if (info.Text != null)
                    {
                        info.Text.Dispose();
                        info.Text.Remove();
                    }
                    pingInfo.Remove(info);
                    continue;
                }

                Obj_AI_Hero hero = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(info.NetworkId);
                Vector2 screenPos = Drawing.WorldToScreen(new Vector3(info.Pos, NavMesh.GetHeightForPosition(info.Pos.X, info.Pos.Y)));
                Drawing.DrawText(screenPos.X - 25, screenPos.Y, System.Drawing.Color.DeepSkyBlue, hero.ChampionName);
                switch (info.Type)
                {
                    case PingCategory.OnMyWay:
                        if (!hero.Position.IsOnScreen())
                        {
                            //DrawWaypoint(hero, info.Pos.To3D2());
                        }
                        break;
                }
            }
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (var info in pingInfo.ToList())
            {
                if (!Common.IsOnScreen(DirectXDrawer.GetScreenPosition(info.Pos.To3D2(), null, false).Position) && 
                    (info.Type == PingCategory.AssistMe || info.Type == PingCategory.Danger || info.Type == PingCategory.OnMyWay))
                {
                    DrawCircle(info.IconBackground.Sprite.Position.X + info.IconBackground.Sprite.Width / 2f,
                        info.IconBackground.Sprite.Position.Y + info.IconBackground.Sprite.Height / 2f,
                        25, 0, true, 32, new ColorBGRA(255, 255, 0, 255));
                }
                //else if (IsActive() &&
                //         !Common.IsOnScreen(
                //             Drawing.WorldToScreen(ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(info.NetworkId).Position)) &&
                //         info.Type == PingCategory.OnMyWay)
                //{
                //    DrawCircle(info.IconBackground.Sprite.Position.X + info.IconBackground.Sprite.Width / 2f,
                //        info.IconBackground.Sprite.Position.Y + info.IconBackground.Sprite.Height / 2f,
                //        25, 0, true, 32, new ColorBGRA(255, 255, 0, 255));
                //}
            }
        }

        private void Init()
        {
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsAlly)
                {
                    allies.Add(hero, new ChampionInfo());
                }
            }
            new System.Threading.Thread(LoadSprites).Start();
        }

        private void LoadSprites()
        {
            foreach (var ally in allies)
            {
                ally.Value.SpriteName = SpriteHelper.DownloadImageRiot(ally.Key.ChampionName, SpriteHelper.ChampionType.Champion, SpriteHelper.DownloadType.Champion, "SPI");
            }
        }

        private void DrawCircle(
            float x,
            float y,
            float radius,
            int rotate,
            bool smoothing,
            int resolution,
            SharpDX.ColorBGRA color)
        {
            var vertices = new VertexBuffer(
                Drawing.Direct3DDevice,
                SharpDX.Utilities.SizeOf<Vector4>() * 2 * (resolution + 4),
                Usage.WriteOnly,
                VertexFormat.Diffuse | VertexFormat.PositionRhw,
                Pool.Default);

            var angle = rotate * (float)Math.PI / 180f;
            var pi = (float)Math.PI;

            var data = new List<Vector4>();

            for (var i = 0; i < resolution + 4; i++)
            {
                var x1 = x - radius * (float)Math.Cos(i * (2f * pi / resolution));
                var y1 = y - radius * (float)Math.Sin(i * (2f * pi / resolution));
                data.AddRange(new[] { new Vector4(x1, y1, 0f, 1.0f), color.ToVector4() });
            }

            // Rotate matrix
            var res = 2 * resolution + 4;
            for (var i = 0; i < res; i = i + 2)
            {
                data[i] = new Vector4(
                    (float)(x + Math.Cos(angle) * (data[i].X - x) - Math.Sin(angle) * (data[i].Y - y)),
                    (float)(y + Math.Sin(angle) * (data[i].X - x) + Math.Cos(angle) * (data[i].Y - y)),
                    data[i].Z,
                    data[i].W);
            }

            vertices.Lock(0, 0, LockFlags.None).WriteRange(data.ToArray());
            vertices.Unlock();

            VertexElement[] vertexElements =
                {
                    new VertexElement(
                        0,
                        0,
                        DeclarationType.Float4,
                        DeclarationMethod.Default,
                        DeclarationUsage.Position,
                        0),
                    new VertexElement(
                        0,
                        16,
                        DeclarationType.Float4,
                        DeclarationMethod.Default,
                        DeclarationUsage.Color,
                        0),
                    VertexElement.VertexDeclarationEnd
                };

            var vertexDeclaration = new VertexDeclaration(Drawing.Direct3DDevice, vertexElements);

            if (smoothing)
            {
                Drawing.Direct3DDevice.SetRenderState(RenderState.MultisampleAntialias, true);
                Drawing.Direct3DDevice.SetRenderState(RenderState.AntialiasedLineEnable, true);
            }
            else
            {
                Drawing.Direct3DDevice.SetRenderState(RenderState.MultisampleAntialias, false);
                Drawing.Direct3DDevice.SetRenderState(RenderState.AntialiasedLineEnable, false);
            }

            var olddec = Drawing.Direct3DDevice.VertexDeclaration;
            Drawing.Direct3DDevice.SetStreamSource(0, vertices, 0, SharpDX.Utilities.SizeOf<Vector4>() * 2);
            Drawing.Direct3DDevice.VertexDeclaration = vertexDeclaration;
            Drawing.Direct3DDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, resolution);
            Drawing.Direct3DDevice.VertexDeclaration = olddec;

            vertexDeclaration.Dispose();
            vertices.Dispose();
        }

        private void DeleteSprites(PingInfo info)
        {
            if (info.Direction != null)
            {
                info.Direction.Dispose();
            }
            if (info.Icon != null)
            {
                info.Icon.Dispose();
            }
            if (info.IconBackground != null)
            {
                info.IconBackground.Dispose();
            }
            if (info.ChampionDirection != null)
            {
                info.ChampionDirection.Dispose();
            }
            if (info.Champion != null)
            {
                info.Champion.Dispose();
            }
        }

        private static float value = 0.0f;

        private void CreateSprites(PingInfo info)
        {
            String iconName = null;
            String iconBackgroundName = null;
            String directionName = null;
            String typeName = "";
            ColorBGRA directionColor = SharpDX.Color.White;

            switch (info.Type)
            {
                case PingCategory.AssistMe:
                    iconName = "pingcomehere";
                    iconBackgroundName = "pingmarker";
                    directionName = "directionindicator";
                    typeName = "Assist";
                    directionColor = SharpDX.Color.DeepSkyBlue;
                    break;

                case PingCategory.Danger:
                    iconName = "pinggetback";
                    iconBackgroundName = "pingmarker_red";
                    directionName = "directionindicator";
                    typeName = "Danger";
                    directionColor = SharpDX.Color.Red;
                    break;

                case PingCategory.OnMyWay:
                    iconName = "pingomw";
                    iconBackgroundName = "pingmarker";
                    directionName = "directionindicator";
                    typeName = "OnMyWay";
                    directionColor = SharpDX.Color.MediumSeaGreen;
                    break;
            }

            //info.Text = new Render.Text(typeName, 0, 0, 20, Color.Aqua);
            //info.Text.PositionUpdate = delegate
            //    {
            //        return GetScreenPosition(info.Pos, new Size(50, 50));
            //    };
            //info.Text.Add(1);

            if (iconName == null)
                return;

            SpriteHelper.LoadTexture(iconName, ref info.Icon, SpriteHelper.TextureType.Default);
            info.Icon.Sprite.Scale = new Vector2(0.3f);
            SpriteHelper.LoadTexture(iconBackgroundName, ref info.IconBackground, SpriteHelper.TextureType.Default);
            info.IconBackground.Sprite.Scale = new Vector2(1.5f);
            SpriteHelper.LoadTexture(directionName, ref info.Direction, SpriteHelper.TextureType.Default);
            info.Direction.Sprite.Scale = new Vector2(0.6f);
            info.Direction.Sprite.Color = directionColor;

            info.Direction.Sprite.PositionUpdate = delegate
            {
                var posInfo = DirectXDrawer.GetScreenPosition(info.Pos.To3D2(), new Size(info.Direction.Sprite.Width, info.Direction.Sprite.Height));
                Vector2 screenPos = posInfo.Position;
                int apparentX = (int)Math.Max(1 + info.Direction.Sprite.Width, Math.Min(screenPos.X, Drawing.Width - info.Direction.Sprite.Width));
                int apparentY = (int)Math.Max(1 + info.Direction.Sprite.Height, Math.Min(screenPos.Y, Drawing.Height - info.Direction.Sprite.Height));
                float angle = posInfo.Angle;
                info.Direction.Sprite.Rotation = angle;
                return new Vector2(apparentX, apparentY);
            };
            info.Direction.Sprite.VisibleCondition = delegate
            {
                return IsActive() && !Common.IsOnScreen(Drawing.WorldToScreen(info.Pos.To3D2()));
            };
            info.Direction.Sprite.Add(2);

            info.Icon.Sprite.PositionUpdate = delegate
            {
                var posInfo = DirectXDrawer.GetScreenPosition(info.Pos.To3D2(), new Size(info.Icon.Sprite.Width + info.Direction.Sprite.Width, info.Icon.Sprite.Height + info.Direction.Sprite.Height));
                Vector2 position = posInfo.Position;
                for (int i = 0; i < 200; i = i + 10)
                {
                    var aabbBox = DirectXDrawer.GetAABBBox(info.Direction.Sprite);
                    Vector2 positionNew = position.Extend(position + posInfo.Direction, -i);
                    if (!((Math.Abs(positionNew.X - aabbBox.X) * 2 <
                         (info.Icon.Sprite.Width + aabbBox.Width)) &&
                        (Math.Abs(positionNew.Y - aabbBox.Y) * 2 <
                         (info.Icon.Sprite.Height + aabbBox.Height))) &&
                            positionNew.X > 50 && positionNew.X < Drawing.Width - 100 &&
                            positionNew.Y > 50 && positionNew.Y < Drawing.Height - 100)
                    {
                        if (Common.IsOnScreen(positionNew))
                        {
                            return positionNew;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                for (int i = 0; i < 200; i = i + 10)
                {
                    var aabbBox = DirectXDrawer.GetAABBBox(info.Direction.Sprite);
                    Vector2 positionNew = position.Extend(position + posInfo.Direction, i);
                    if (!((Math.Abs(positionNew.X - aabbBox.X) * 2 <
                         (info.Icon.Sprite.Width + aabbBox.Width)) &&
                        (Math.Abs(positionNew.Y - aabbBox.Y) * 2 <
                         (info.Icon.Sprite.Height + aabbBox.Height))) &&
                            positionNew.X > 50 && positionNew.X < Drawing.Width - 100 &&
                            positionNew.Y > 50 && positionNew.Y < Drawing.Height - 100)
                    {
                        if (Common.IsOnScreen(positionNew))
                        {
                            return positionNew;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                return new Vector2(1, 1);
            };
            info.Icon.Sprite.VisibleCondition = delegate
            {
                return IsActive() && !Common.IsOnScreen(Drawing.WorldToScreen(info.Pos.To3D2()));
            };
            info.Icon.Sprite.Add(1);

            info.IconBackground.Sprite.PositionUpdate = delegate
            {
                var backgroundPos = info.Icon.Sprite.Position;
                return new Vector2(backgroundPos.X - 7, backgroundPos.Y - 7);
            };
            info.IconBackground.Sprite.VisibleCondition = delegate
            {
                return IsActive() && !Common.IsOnScreen(Drawing.WorldToScreen(info.Pos.To3D2()));
            };
            info.IconBackground.Sprite.Add(0);

            if (info.Type == PingCategory.OnMyWay)
            {
                SpriteHelper.LoadTexture(ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(info.NetworkId).ChampionName + ".png", ref info.Champion, "SPI");
                if (info.Champion == null)
                {
                    return;
                }

                SpriteHelper.LoadTexture(directionName, ref info.ChampionDirection, SpriteHelper.TextureType.Default);
                info.ChampionDirection.Sprite.Scale = new Vector2(0.6f);
                info.ChampionDirection.Sprite.Color = directionColor;

                info.ChampionDirection.Sprite.PositionUpdate = delegate
                {
                    var posInfo = DirectXDrawer.GetScreenPosition(ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(info.NetworkId).Position, 
                        new Size(info.ChampionDirection.Sprite.Width, info.ChampionDirection.Sprite.Height));
                    Vector2 screenPos = posInfo.Position;
                    int apparentX = (int)Math.Max(1 + info.ChampionDirection.Sprite.Width, Math.Min(screenPos.X, Drawing.Width - info.ChampionDirection.Sprite.Width));
                    int apparentY = (int)Math.Max(1 + info.ChampionDirection.Sprite.Height, Math.Min(screenPos.Y, Drawing.Height - info.ChampionDirection.Sprite.Height));
                    float angle = posInfo.Angle;
                    info.ChampionDirection.Sprite.Rotation = angle;
                    return new Vector2(apparentX, apparentY);
                };
                info.ChampionDirection.Sprite.VisibleCondition = delegate
                {
                    return IsActive() && !Common.IsOnScreen(Drawing.WorldToScreen(ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(info.NetworkId).Position));
                };
                info.ChampionDirection.Sprite.Add(2);

                info.Champion.Sprite.UpdateTextureBitmap(SpriteHelper.CropImage(info.Champion.Sprite.Bitmap, info.Champion.Sprite.Width));
                info.Champion.Sprite.Scale = new Vector2(0.5f);

                info.Champion.Sprite.PositionUpdate = delegate
                {
                    var posInfo = DirectXDrawer.GetScreenPosition(ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(info.NetworkId).Position, 
                        new Size(info.Champion.Sprite.Width + info.ChampionDirection.Sprite.Width, info.Champion.Sprite.Height + info.ChampionDirection.Sprite.Height));
                    Vector2 position = posInfo.Position;
                    for (int i = 0; i < 200; i = i + 10)
                    {
                        var aabbBox = DirectXDrawer.GetAABBBox(info.ChampionDirection.Sprite);
                        Vector2 positionNew = position.Extend(position + posInfo.Direction, -i);
                        if (!((Math.Abs(positionNew.X - aabbBox.X) * 2 <
                             (info.Champion.Sprite.Width + aabbBox.Width)) &&
                            (Math.Abs(positionNew.Y - aabbBox.Y) * 2 <
                             (info.Champion.Sprite.Height + aabbBox.Height))) &&
                                positionNew.X > 50 && positionNew.X < Drawing.Width - 100 &&
                                positionNew.Y > 50 && positionNew.Y < Drawing.Height - 100)
                        {
                            if (Common.IsOnScreen(positionNew))
                            {
                                return positionNew;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    for (int i = 0; i < 200; i = i + 10)
                    {
                        var aabbBox = DirectXDrawer.GetAABBBox(info.ChampionDirection.Sprite);
                        Vector2 positionNew = position.Extend(position + posInfo.Direction, i);
                        if (!((Math.Abs(positionNew.X - aabbBox.X) * 2 <
                             (info.Champion.Sprite.Width + aabbBox.Width)) &&
                            (Math.Abs(positionNew.Y - aabbBox.Y) * 2 <
                             (info.Champion.Sprite.Height + aabbBox.Height))) &&
                                positionNew.X > 50 && positionNew.X < Drawing.Width - 100 &&
                                positionNew.Y > 50 && positionNew.Y < Drawing.Height - 100)
                        {
                            if (Common.IsOnScreen(positionNew))
                            {
                                return positionNew;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    return new Vector2(1, 1);
                };
                info.Champion.Sprite.VisibleCondition = delegate
                {
                    return IsActive() && !Common.IsOnScreen(Drawing.WorldToScreen(ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(info.NetworkId).Position));
                };
                info.Champion.Sprite.Add(1);
            }
        }

        private class ChampionInfo
        {
            public String SpriteName;
        }

        private class PingInfo
        {
            public Vector2 Pos;
            public int NetworkId;
            public float Time;
            public PingCategory Type;
            public SpriteHelper.SpriteInfo Icon;
            public SpriteHelper.SpriteInfo IconBackground;
            public SpriteHelper.SpriteInfo Direction;
            public SpriteHelper.SpriteInfo ChampionDirection;
            public SpriteHelper.SpriteInfo Champion;

            public Render.Circle Circle;

            public Render.Text Text;

            public PingInfo(int networkId, Vector2 pos, float time, PingCategory type)
            {
                NetworkId = networkId;
                Pos = pos;
                Time = time;
                Type = type;
            }
        }
    }
}
