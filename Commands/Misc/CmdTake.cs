﻿/*
Copyright 2012 MCForge
Dual-licensed under the Educational Community License, Version 2.0 and
the GNU General Public License, Version 3 (the "Licenses"); you may
not use this file except in compliance with the Licenses. You may
obtain a copy of the Licenses at
http://www.opensource.org/licenses/ecl2.php
http://www.gnu.org/licenses/gpl-3.0.html
Unless required by applicable law or agreed to in writing,
software distributed under the Licenses are distributed on an "AS IS"
BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
or implied. See the Licenses for the specific language governing
permissions and limitations under the Licenses.
*/
using System;
using MCForge.Interface.Command;
using MCForge.Entity;
using MCForge.Core;

namespace CommandDll.Misc
{
    class CmdTake : ICommand
    {
        public string Name { get { return "Take"; } }
        public CommandTypes Type { get { return CommandTypes.Misc; } }
        public string Author { get { return "Sinjai"; } }
        public Version Version { get { return new Version(1,0); } }
        public string CUD { get { return ""; } }
        public byte Permission { get { return 0; } }
        public void Use(Player p, string[] args)
        {
            if (args.Length != 2) { Help(p); return; }
            Player who = Player.Find(args[0]);
            if (who == null) { p.SendMessage("Could not find \"" + args[0] + "\"!"); return; }
            if (who == p && !p.isOwner) { p.SendMessage("You cannot take money from yourself!"); return; }
            int amt;
            try { amt = int.Parse(args[1]); }
            catch { p.SendMessage("Invalid amount!"); return; }
            if (who.money - amt < 0) { p.SendMessage("Players cannot have less than 0 " + Server.moneys + "."); return; }
            if (amt < 0) { p.SendMessage("Cannot take negative amounts of " + Server.moneys + "."); return; }
            who.money += amt;
            Player.UniversalChat(who.color + who.Username + Server.DefaultColor + " was rattled down for &3" + amt + Server.DefaultColor + " " + Server.moneys + ".");
            //TODO: DB save
        }
        public void Help(Player p)
        {
            p.SendMessage("/take <player> <amount> - Take <amount> of " + Server.moneys + " from <player>.");
        }
        public void Initialize()
        {
            Command.AddReference(this, new string[1] { "take" });
        }
    }
}
