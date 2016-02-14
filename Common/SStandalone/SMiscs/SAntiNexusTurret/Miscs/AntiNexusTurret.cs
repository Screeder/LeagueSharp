using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SAssemblies.Miscs
{
    class AntiNexusTurret
    {
        public static Menu.MenuItemSettings AntiNexusTurretMisc = new Menu.MenuItemSettings(typeof(AntiNexusTurret));

        private List<Ability> abilities = new List<Ability>();

        public AntiNexusTurret()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Obj_AI_Base.OnNewPath += Obj_AI_Hero_OnNewPath;
            Game.OnUpdate += Game_OnGameUpdate;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        ~AntiNexusTurret()
        {
            Obj_AI_Base.OnNewPath -= Obj_AI_Hero_OnNewPath;
            Game.OnUpdate -= Game_OnGameUpdate;
            Spellbook.OnCastSpell -= Spellbook_OnCastSpell;
        }

        public bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && AntiNexusTurretMisc.GetActive();
#else
            return AntiNexusTurretMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsAntiNexusTurret");
            if (newMenu == null)
            {
                AntiNexusTurretMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_ANTINEXUSTURRET_MAIN"), "SAssembliesMiscsAntiNexusTurret"));
                AntiNexusTurretMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsAntiNexusTurretFlash", Language.GetString("GLOBAL_FLASH")).SetValue(false));
                AntiNexusTurretMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsAntiNexusTurretAbilities", Language.GetString("MISCS_ANTINEXUSTURRET_ABILITIES")).SetValue(false));
                AntiNexusTurretMisc.CreateActiveMenuItem("SAssembliesMiscsAntiNexusTurretActive", () => new AntiNexusTurret());
            }
            return AntiNexusTurretMisc;
        }

        private void Init()
        {
            foreach (SpellDataInst spell in ObjectManager.Player.Spellbook.Spells)
            {
                if (spell.Name.ToLower().Contains("flash"))
                {
                    abilities.Add(new Ability("SummonerFlash", spell.Slot, 425));
                }
            }
            switch (ObjectManager.Player.ChampionName)
            {
                case "Aatrox":
                    abilities.Add(new Ability("AatroxQ", SpellSlot.Q, 650));
                    break;

                case "Ahri":
                    abilities.Add(new Ability("AhriTumble", SpellSlot.R, 450));
                    break;

                case "Akali":
                    abilities.Add(new Ability("AkaliShadowDance", SpellSlot.R, 700));
                    break;

                case "Alistar":
                    abilities.Add(new Ability("FerociousHowl", SpellSlot.W, 650));
                    break;

                case "Amumu":
                    abilities.Add(new Ability("BandageToss", SpellSlot.Q, 1100));
                    break;

                case "Azir":
                    abilities.Add(new Ability("AzirE", SpellSlot.E, 1100));
                    break;

                case "Braum":
                    abilities.Add(new Ability("BraumW", SpellSlot.W, 650));
                    break;

                case "Caitlyn":
                    abilities.Add(new Ability("CaitlynEntrapment", SpellSlot.E, 1000));
                    break;

                case "Corki":
                    abilities.Add(new Ability("CarpetBomb", SpellSlot.W, 600));
                    break;

                case "Diana":
                    abilities.Add(new Ability("DianaTeleport", SpellSlot.R, 895));
                    break;

                case "Ekko":
                    abilities.Add(new Ability("EkkoE", SpellSlot.E, 325));
                    break;

                case "Ezreal":
                    abilities.Add(new Ability("EzrealArcaneShift", SpellSlot.E, 475));
                    break;

                case "Fiora":
                    abilities.Add(new Ability("FioraQ", SpellSlot.Q, 400));
                    break;

                case "Fizz":
                    abilities.Add(new Ability("FizzPiercingStrike", SpellSlot.Q, 550));
                    abilities.Add(new Ability("FizzJump", SpellSlot.E, 400));
                    break;

                case "Gnar":
                    abilities.Add(new Ability("GnarE", SpellSlot.E, 473));
                    abilities.Add(new Ability("GnarBigE", SpellSlot.E, 475));
                    break;

                case "Gragas":
                    abilities.Add(new Ability("GragasE", SpellSlot.E, 950));
                    break;

                case "Graves":
                    abilities.Add(new Ability("GravesMove", SpellSlot.E, 425));
                    break;

                case "Illaoi":
                    abilities.Add(new Ability("IllaoiW", SpellSlot.W, 350));
                    break;

                case "Irelia":
                    abilities.Add(new Ability("IreliaGatotsu", SpellSlot.Q, 650));
                    break;

                case "Jax":
                    abilities.Add(new Ability("JaxLeapStrike", SpellSlot.Q, 700));
                    break;

                case "Kassadin":
                    abilities.Add(new Ability("RiftWalk", SpellSlot.R, 450));
                    break;

                case "Katarina":
                    abilities.Add(new Ability("KatarinaE", SpellSlot.E, 700));
                    break;

                case "Khazix":
                    abilities.Add(new Ability("KhazixE", SpellSlot.E, 600));
                    break;

                case "Leblanc":
                    abilities.Add(new Ability("LeblancSlide", SpellSlot.W, 600));
                    abilities.Add(new Ability("LeblancSlideM", SpellSlot.R, 600));
                    break;

                case "Leona":
                    abilities.Add(new Ability("LeonaZenithBlade", SpellSlot.E, 905));
                    break;

                case "Lissandra":
                    abilities.Add(new Ability("LissandraE", SpellSlot.E, 1025));
                    break;

                case "Lucian":
                    abilities.Add(new Ability("LucianE", SpellSlot.E, 425));
                    break;

                case "Malphite":
                    abilities.Add(new Ability("UFSlash", SpellSlot.R, 1000));
                    break;

                case "Maokai":
                    abilities.Add(new Ability("MaokaiUnstableGrowth", SpellSlot.W, 525));
                    break;

                case "MasterYi":
                    abilities.Add(new Ability("AlphaStrike", SpellSlot.Q, 600));
                    break;

                case "MonkeyKing":
                    abilities.Add(new Ability("MonkeyKingNimbus", SpellSlot.E, 650));
                    break;

                case "Nautilus":
                    abilities.Add(new Ability("NautilusAnchorDrag", SpellSlot.Q, 1250));
                    break;

                case "Nocturne":
                    abilities.Add(new Ability("NocturneParanoia", SpellSlot.R, 2500));
                    break;

                case "Pantheon":
                    abilities.Add(new Ability("PantheonW", SpellSlot.W, 600));
                    break;

                case "Poppy":
                    abilities.Add(new Ability("PoppyE", SpellSlot.E, 375));
                    break;

                case "Renekton":
                    abilities.Add(new Ability("RenektonSliceAndDice", SpellSlot.E, 450));
                    break;

                case "Riven":
                    abilities.Add(new Ability("RivenTriCleave", SpellSlot.Q, 260));
                    abilities.Add(new Ability("RivenFeint", SpellSlot.E, 325));
                    break;

                case "Sejuani":
                    abilities.Add(new Ability("SejuaniArcticAssault", SpellSlot.Q, 900));
                    break;

                case "Shaco":
                    abilities.Add(new Ability("Deceive", SpellSlot.Q, 400));
                    break;

                case "Shen":
                    abilities.Add(new Ability("ShenShadowDash", SpellSlot.E, 650));
                    break;

                case "Shyvana":
                    abilities.Add(new Ability("ShyvanaTransformCast", SpellSlot.R, 1000));
                    break;

                case "TahmKench":
                    abilities.Add(new Ability("TahmKenchNewR", SpellSlot.R, 4000));
                    break;

                case "Talon":
                    abilities.Add(new Ability("TalonCutthroat", SpellSlot.E, 700));
                    break;

                case "Thresh":
                    abilities.Add(new Ability("ThreshQ", SpellSlot.Q, 1100));
                    break;

                case "Tristana":
                    abilities.Add(new Ability("TristanaW", SpellSlot.W, 900));
                    break;

                case "Tryndamere":
                    abilities.Add(new Ability("slashCast", SpellSlot.E, 660));
                    break;

                case "TwistedFate":
                    abilities.Add(new Ability("Destiny", SpellSlot.R, 5500));
                    break;

                case "Vayne":
                    abilities.Add(new Ability("VayneTumble", SpellSlot.Q, 300));
                    break;

                case "Vi":
                    abilities.Add(new Ability("ViQ", SpellSlot.Q, 1000));
                    break;

                case "Warwick": 
                    abilities.Add(new Ability("InfiniteDuress", SpellSlot.R, 700));
                    break;

                case "XinZhao":
                    abilities.Add(new Ability("", SpellSlot.E, 600));
                    break;

                case "Yasuo":
                    abilities.Add(new Ability("YasuoR", SpellSlot.R, 1200));
                    break;

                case "Zac":
                    abilities.Add(new Ability("ZacE", SpellSlot.E, 1200));
                    break;

                case "Zed":
                    abilities.Add(new Ability("ZedW", SpellSlot.W, 700));
                    abilities.Add(new Ability("ZedR", SpellSlot.R, 625));
                    break;
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;

            Obj_AI_Turret baseTurret = ObjectManager.Get<Obj_AI_Turret>().Find(turret => IsBaseTurret(turret, 1410, true, ObjectManager.Player.ServerPosition));
            if (baseTurret != null)
            {
                Obj_AI_Turret baseAllyTurret = ObjectManager.Get<Obj_AI_Turret>().Find(turret => IsBaseTurret(turret, 999999999, false, ObjectManager.Player.ServerPosition));
                Vector3 newPos = baseTurret.ServerPosition.Extend(baseAllyTurret.ServerPosition, 1425);
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, newPos);
            }
        }

        private void Obj_AI_Hero_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if(!IsActive() || !sender.IsMe)
                return;

            for (int i = 0; i < args.Path.Length; i++)
            {
                var point = args.Path[i];
                Obj_AI_Turret baseTurret = ObjectManager.Get<Obj_AI_Turret>().Find(turret => IsBaseTurret(turret, 1425, true, point));
                if (baseTurret != null)
                {
                    Vector3 newPos;
                    if (i == 0)
                    {
                        Obj_AI_Turret baseAllyTurret = ObjectManager.Get<Obj_AI_Turret>().Find(turret => IsBaseTurret(turret, 999999999, false, ObjectManager.Player.ServerPosition));
                        newPos = baseTurret.ServerPosition.Extend(baseAllyTurret.ServerPosition, 1425);
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, newPos);
                        return;
                    }
                    else
                    {
                        float dist =
                        args.Path[i - 1].Distance(
                            baseTurret.ServerPosition) - 1425f - 70f;
                        newPos = args.Path[i - 1].Extend(point, dist);
                    }
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, newPos);
                }
            }
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!IsActive() || !sender.Owner.IsMe)
                return;

            Obj_AI_Turret baseTurret = ObjectManager.Get<Obj_AI_Turret>().Find(turret => IsBaseTurret(turret, 2000, true, ObjectManager.Player.ServerPosition));
            if (baseTurret != null)
            {
                Ability ability = abilities.Find(x => x.SpellSlot == args.Slot);
                if (!AntiNexusTurretMisc.GetMenuItem("SAssembliesMiscsAntiNexusTurretFlash").GetValue<bool>()
                    && (ability.SpellSlot == SpellSlot.Summoner1 || ability.SpellSlot == SpellSlot.Summoner2))
                {
                    return;
                }
                if (!AntiNexusTurretMisc.GetMenuItem("SAssembliesMiscsAntiNexusTurretAbilities").GetValue<bool>()
                    && (ability.SpellSlot != SpellSlot.Summoner1 && ability.SpellSlot != SpellSlot.Summoner2))
                {
                    return;
                }
                if (ability != null)
                {
                    if (args.EndPosition.IsValid() && args.EndPosition.Distance(baseTurret.ServerPosition) < 1425)
                    {
                        args.Process = false;
                    }
                    else if (args.Target != null && args.Target.Position.Distance(baseTurret.ServerPosition) < 1425)
                    {
                        args.Process = false;
                    }
                    else if (!args.EndPosition.IsValid() && args.Target == null &&
                        args.StartPosition.Distance(baseTurret.ServerPosition) < 1425 + ability.Range)
                    {
                        args.Process = false;
                    }
                }
            }
        }

        private static bool IsBaseTurret(Obj_AI_Turret unit,
            float range,
            bool enemyTeam,
            Vector3 from)
        {
            if (unit == null || !unit.IsValid)
            {
                return false;
            }

            if (enemyTeam && unit.Team == ObjectManager.Player.Team)
            {
                return false;
            }
            else if (!enemyTeam && unit.Team != ObjectManager.Player.Team)
            {
                return false;
            }

            if (!unit.Name.Contains("TurretShrine_A"))
            {
                return false;
            }

            var @base = unit as Obj_AI_Base;
            var unitPosition = @base != null ? @base.ServerPosition : unit.Position;

            return !(range < float.MaxValue) ||
                   !(Vector2.DistanceSquared(
                       (@from.To2D().IsValid() ? @from : ObjectManager.Player.ServerPosition).To2D(),
                       unitPosition.To2D()) > range * range);
        }

        public class Ability
        {
            public String SpellName;

            public SpellSlot SpellSlot;

            public float Range;

            public Ability(string spellName, SpellSlot spellSlot, float range)
            {
                SpellName = spellName;
                SpellSlot = spellSlot;
                Range = range;
            }
        }
    }
}
