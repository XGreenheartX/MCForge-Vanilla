﻿/*
 * Created by SharpDevelop.
 * User: Eddie
 * Date: 6/24/2012
 * Time: 4:07 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Net;
using System.Reflection;
using System.Threading;
using MCForge.Utils.Settings;
using MCForge.Utils;
using MCForge.API.Events;
using MCForge.Entity;
using MCForge.Groups;
using MCForge.Interface;
using MCForge.Interface.Plugin;
using MCForge.Interface.Command;

namespace MCForge.Core
{
	/// <summary>
	/// The Updater
	/// </summary>
	public static class Updater
	{
		private static bool cmdupdate = false;
		
		private static bool plugupdate = false;
		
		private static bool coreupdate = false;
		
		private static bool coreupdated = false;
		
		/// <summary>
		/// If enabled, will check for core updates
		/// </summary>
		public static bool checkcore {
		    get {
		        return bool.Parse(ServerSettings.GetSetting("Check-Core-Updates"));
		    }
		}
		
		/// <summary>
		/// If enabled, will check for plugin and command updates
		/// </summary>
		public static bool checkmisc {
		    get {
		        return bool.Parse(ServerSettings.GetSetting("Check-Misc-Updates"));
		    }
		}
		/// <summary>
		/// If true, commands, plugins, and the core will automatically update WITH notification
		/// </summary>
		public static bool autoupdate {
			get {
				return bool.Parse(ServerSettings.GetSetting("Auto-Update"));
			}
		}
		
		/// <summary>
		/// If true, commands and plugins will be updated without notification
		/// </summary>
		public static bool silentupdate {
			get {
				return bool.Parse(ServerSettings.GetSetting("Silent-Update"));
			}
		}
		
		/// <summary>
		/// If true, the server will ask before updating the core (If GUI is enabled)
		/// If no gui is enabled, then the user must update using /update
		/// If silentupdate or autoupdate is enabled, then this setting is ignored
		/// </summary>
		public static bool askbefore {
			get {
		        return bool.Parse(ServerSettings.GetSetting("Ask-Before-Core"));
			}
		}
		
		/// <summary>
		/// If true, the server will ask before updating plugins and commands (If GUI is enabled)
		/// If no gui is enabled, then the user must update using /update
		/// If silentupdate or autoupdate is enabled, then this setting is ignored
		/// </summary>
		public static bool askbeforemisc {
			get {
		        return bool.Parse(ServerSettings.GetSetting("Ask-Before-Misc"));
			}
		}
		
		/// <summary>
		/// If enabled, the server will attempt to udpate when server activity is low
		/// </summary>
		public static bool silentcoreupdate {
		    get {
		        return bool.Parse(ServerSettings.GetSetting("Silent-Core-Update"));
		    }
		}
		
		/// <summary>
		/// How often to check for updates (in minutes)
		/// </summary>
		public static int checkinterval {
		    get {
		        int time = 10;
		        int.TryParse(ServerSettings.GetSetting("Updatecheck-Interval"), out time);
		        if (time == null)
		            return 10;
		        return time;
		    }
		}
		
		/// <summary>
        /// The server version
        /// </summary>
        public static Version Version {
        	get {
        		return Assembly.GetEntryAssembly().GetName().Version;
            }
        }
        
        internal static void InIt() {
            Thread check = new Thread(new ThreadStart(delegate
                                                      {
                                                          while(true)
                                                          {
                                                            Tick();
                                                            Thread.Sleep(checkinterval * 60000);
                                                          }
                                                      }));
            check.Start();
            Player.OnAllPlayersCommand.SystemLvl += new Event<Player, CommandEventArgs>.EventHandler(OnAllPlayersCommand_SystemLvl);
        }

        static void OnAllPlayersCommand_SystemLvl(Player sender, CommandEventArgs args)
        {
            if (sender.Group.Permission >= (byte)PermissionLevel.Operator) {
                if (args.Command.ToLower() == "update") {
                    if (cmdupdate || plugupdate || coreupdate)
                        ManualUpdate(sender);
                    else
                        sender.SendMessage("No updates are ready for install..");
                    args.Cancel();
                }
            }
        }
        private static void ReloadCommands() {
            Logger.Log("Reloading Plugins and Commands", LogType.Debug);
            Plugin.unloadAll();
			Command.Commands.Clear();
			LoadAllDlls.InitCommandsAndPlugins();
			new MCForge.Interface.Command.CmdReloadCmds().Initialize();
        }
        private static void ManualUpdate(Player p) {
            using (WebClient wc = new WebClient()) {
                if (cmdupdate) {
                    p.SendMessage("Updating Commands..");
                    wc.DownloadFile("http://update.mcforge.net/DLL/Commands.dll", "Commands.dll");
                }
                
                if (plugupdate) {
                    p.SendMessage("Downloading Plugins..");
                    wc.DownloadFile("http://update.mcforge.net/DLL/Commands.dll", "Plugins.dll");
                }
                
                if (!coreupdate && (plugupdate || cmdupdate)) {
                    p.SendMessage("Reloading Commands and Plugins");
                    ReloadCommands();
                }
                
                else if (coreupdate) {
                    if (Server.OnMono) {
                        wc.DownloadFile("http://update.mcforge.net/DLL/Core.dll", "MCForge.dll");
                        Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "MCForge has been updated!");
                        Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "Updates will be applied next restart.");
                        Logger.Log("MCForge has been updated!", LogType.Critical);
                        Logger.Log("Updates will be applied next restart.", LogType.Critical);
                        coreupdated = true;
                    }
                    else {
                        //TODO Windows you suck
                    }
                }
            }
            cmdupdate = false;
            plugupdate = false;
            coreupdate = false;
        }
        
        //UNDONE Windows needs to update
        private static object Tick() {
            using (WebClient wc = new WebClient()) {
                if (checkmisc) {
                    bool updated = false;
                    if (!cmdupdate) {
                        //Check commands first
                        Logger.Log("Checking Commands for updates", LogType.Debug);
                        Version cmdv = LoadAllDlls.LoadFile("Plugins.dll").GetName().Version;
                        Version clastest = new Version(wc.DownloadString("http://update.mcforge.net/cmdv.txt"));
                        if (clastest > cmdv) {
                            updated = true;
                            Logger.Log("Updates found, updating", LogType.Debug);
                            if (silentupdate) {
                                wc.DownloadFile("http://update.mcforge.net/DLL/Commands.dll", "Commands.dll");
                            }
                            else if (autoupdate) {
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "An update for the Core Commands are available!");
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "Downloading update..");
                                wc.DownloadFile("http://update.mcforge.net/DLL/Commands.dll", "Commands.dll");
                            }
                            else if (askbeforemisc) {
                                cmdupdate = true;
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "An update for the Core Commands are available!");
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "To update, type /update");
                            }
                        }
                    }
                    if (!plugupdate) {
                        //Check plugin system
                        Logger.Log("Checking Plugins for updates", LogType.Debug);
                        Version plugv = LoadAllDlls.LoadFile("Plugins.dll").GetName().Version;
                        Version plastest = new Version(wc.DownloadString("http://update.mcforge.net/plugv.txt"));
                        if (plastest > plugv) {
                            updated = true;
                            Logger.Log("Updates found, updating", LogType.Debug);
                            if (silentupdate) {
                                wc.DownloadFile("http://update.mcforge.net/DLL/Plugins.dll", "Plugins.dll");
                            }
                            else if (autoupdate) {
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "An update for the Core Plugins are available!");
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "Downloading update..");
                                wc.DownloadFile("http://update.mcforge.net/DLL/Commands.dll", "Plugins.dll");
                            }
                            else if (askbeforemisc) {
                                plugupdate = true;
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "An update for the Core Plugins are available!");
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "To update, type /update");
                            }
                        }
                    }
                    
                    //Reload the system if new updates were installed
                    if ((silentupdate || autoupdate) && updated)
                        ReloadCommands();
                }
                
                if (checkcore && !coreupdated) {
                    Logger.Log("Checking Core for updates", LogType.Debug);
                    //Check core
                    if (coreupdate && silentcoreupdate) {
                        if (Server.PlayerCount < (int)((double)(ServerSettings.GetSettingInt("MaxPlayers") / 4))) {
                            if (Server.OnMono) {
                                wc.DownloadFile("http://update.mcforge.net/DLL/Core.dll", "MCForge.dll");
                            }
                            else {
                                //TODO Windows you suck
                            }
                        }
                    }
                    Version corel = new Version(wc.DownloadString("http://update.mcforge.net/corev.txt"));
                    if (corel > Version) {
                        Logger.Log("Updates found, updating", LogType.Debug);
                        coreupdate = true;
                        if (silentcoreupdate) {
                            if (Server.PlayerCount < (int)((double)(ServerSettings.GetSettingInt("MaxPlayers") / 4))) {
                                if (Server.OnMono) {
                                    wc.DownloadFile("http://update.mcforge.net/DLL/Core.dll", "MCForge.dll");
                                }
                                else {
                                    //TODO Windows you suck
                                }
                            }
                        }
                        else if (autoupdate) {
                            Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "An update for the Core is available!");
                            Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "Downloading update..");
                            if (Server.OnMono) {
                                wc.DownloadFile("http://update.mcforge.net/DLL/Core.dll", "MCForge.dll");
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "MCForge has been updated!");
                                Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "Updates will be applied next restart.");
                                Logger.Log("MCForge has been updated!", LogType.Critical);
                                Logger.Log("Updates will be applied next restart.", LogType.Critical);
                                coreupdated = true;
                            }
                            else {
                                //TODO Windows you suck
                            }
                        }
                        else {
                            Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "An update for the Core is available!");
                            Player.UniversalChatOps("&2[Updater] " + Server.DefaultColor + "To update, type /update");
                        }
                    }
                }
            }
            return null;
        }
	}
}
