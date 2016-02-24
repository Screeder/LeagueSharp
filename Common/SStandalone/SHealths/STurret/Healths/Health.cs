using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace SAssemblies.Healths
{
    internal class Health
    {
        public static Menu.MenuItemSettings Healths = new Menu.MenuItemSettings();

        public Health()
        {

        }

        ~Health()
        {

        }

        private static void SetupMainMenu()
        {
            var menu = new LeagueSharp.Common.Menu("SAssemblies", "SAssemblies", true);
            SetupMenu(menu);
            menu.AddToMainMenu();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu, bool useExisitingMenu = false)
        {
            Language.SetLanguage();
            if (!useExisitingMenu)
            {
                bool loaded = Menu.GetSubMenu(menu, "SAssembliesHealths") != null;
                Healths.Menu = Menu.GetSubMenu(menu, "SAssembliesHealths") ?? menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("HEALTHS_HEALTH_MAIN"), "SAssembliesHealths"));
                if (!loaded)
                {
                    Healths.Menu.AddItem(new MenuItem("SAssembliesHealthsMode", Language.GetString("GLOBAL_MODE")).SetValue(new StringList(new[]
                    {
                        Language.GetString("GLOBAL_MODE_PERCENT"),
                        Language.GetString("GLOBAL_MODE_VALUE")
                    })));
                    Healths.Menu.AddItem(new MenuItem("SAssembliesHealthsTextScale", Language.GetString("GLOBAL_SCALE")).SetValue(new Slider(14, 8, 20)));
                }
            }
            else
            {
                Healths.Menu = menu;
            }
            if (!useExisitingMenu)
            {
                Healths.CreateActiveMenuItem("SAssembliesHealthsActive");
            }
            return Healths;
        }

        public class HealthConf
        {
            public Object Obj;
            public int Health;
            public Render.Text Text;

            public HealthConf(Object obj, Render.Text text)
            {
                Obj = obj;
                Text = text;
            }
        }
    }
}
