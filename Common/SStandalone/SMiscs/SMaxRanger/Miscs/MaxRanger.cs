using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAssemblies.Miscs
{
    class MaxRanger
    {
        public static Menu.MenuItemSettings MaxRangerMisc = new Menu.MenuItemSettings(typeof(MaxRanger));

        public MaxRanger()
        {
            Game.OnUpdate += Game_OnUpdate;
        }

        ~MaxRanger()
        {
            Game.OnUpdate -= Game_OnUpdate;
        }

        public bool IsActive()
        {
#if DETECTORS
            return Misc.Miscs.GetActive() && MaxRangerMisc.GetActive();
#else
            return MaxRangerMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsMaxRanger");
            if (newMenu == null)
            {
                MaxRangerMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_MAXRANGER_MAIN"), "SAssembliesMiscsMaxRanger"));
                MaxRangerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsMaxRangerDisable", Language.GetString("MISCS_MAXRANGER_DISABLE_ALL")).SetValue(new KeyBind('J', KeyBindType.Toggle)));
                MaxRangerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsMaxRangerDisableInCombo", Language.GetString("MISCS_MAXRANGER_DISABLE_COMBO")).SetValue(false));
                MaxRangerMisc.CreateActiveMenuItem("SAssembliesMiscsMaxRangerActive", () => new MaxRanger());
            }
            return MaxRangerMisc;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!IsActive()) return;

            if (MaxRangerMisc.GetMenuItem("SAssembliesMiscsMaxRangerDisable").GetValue<KeyBind>().Active
                || (MaxRangerMisc.GetMenuItem("SAssembliesMiscsMaxRangerDisableInCombo").GetValue<bool>()
                    && Orbwalking.Orbwalker.Instances.Any(x => x.ActiveMode == Orbwalking.OrbwalkingMode.Combo)))
            {
                return;
            }

            var enemyList = ObjectManager.Get<Obj_AI_Hero>().Where(x =>
                        x.IsEnemy && x.IsValidTarget(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
                        && x.IsFacing(ObjectManager.Player));

            foreach (Obj_AI_Hero hero in enemyList)
            {
                if (Orbwalking.GetAttackRange(ObjectManager.Player) < Orbwalking.GetAttackRange(hero))
                {
                    continue;
                }

                var distance = hero.ServerPosition.Distance(ObjectManager.Player.ServerPosition);
                var neededAdditionalDistance = Orbwalking.GetAttackRange(hero) + ObjectManager.Player.BoundingRadius;

                if (distance < neededAdditionalDistance + 45)
                {
                    var maxRangePosition = ObjectManager.Player.ServerPosition.Extend(ObjectManager.Player.ServerPosition - hero.ServerPosition + ObjectManager.Player.ServerPosition, 
                                                                                        neededAdditionalDistance - distance + 95);
                    //ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, maxRangePosition);
                    Orbwalking.MoveTo(maxRangePosition, 0, false, false, false);
                }
            }
        }
    }
}