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
    class ParticleLooper
    {
        public static Menu.MenuItemSettings ParticleLooperMisc = new Menu.MenuItemSettings(typeof(ParticleLooper));

        public ParticleLooper()
        {
            Game.OnProcessPacket += Game_OnGameProcessPacket;
        }

        ~ParticleLooper()
        {
            Game.OnProcessPacket -= Game_OnGameProcessPacket;
        }

        public bool IsActive()
        {
#if DETECTORS
            return Misc.Miscs.GetActive() && ParticleLooperMisc.GetActive();
#else
            return ParticleLooperMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsParticleLooper");
            if (newMenu == null)
            {
                ParticleLooperMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_PARTICLELOOPER_MAIN"), "SAssembliesMiscsParticleLooper"));
                ParticleLooperMisc.CreateActiveMenuItem("SAssembliesMiscsParticleLooperActive", () => new ParticleLooper());
            }
            return ParticleLooperMisc;
        }

        private void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (!IsActive()) return;

            try
            {
                var reader = new BinaryReader(new MemoryStream(args.PacketData));
                byte packetId = reader.ReadByte(); //PacketId
                int packet = -1;
                if (Game.Version.Contains("6.12"))
                {
                    packet = 4;
                }
                if (packetId != packet) // Length: 12
                    return;
                args.Process = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ParticleLooper: " + ex);
            }
        }
    }
}