using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.GameSystems.Chat;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ChatFilter
{
    [HarmonyPatch]
    public class ChatFilter
    {
        public ChatFilter()
        {
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyChatCommandSystem), MethodType.Constructor)]
        public static void AddCommands(MyChatCommandSystem __instance)
        {
            IMyChatCommand myChatCommand = (IMyChatCommand)Activator.CreateInstance(typeof(ChatCmd_Mute).GetTypeInfo());
            if (myChatCommand != null)
            {
                __instance.ChatCommands.Add(myChatCommand.CommandText, myChatCommand);
            }
            IMyChatCommand myChatCommand2 = (IMyChatCommand)Activator.CreateInstance(typeof(ChatCmd_Unmute).GetTypeInfo());
            if (myChatCommand2 != null)
            {
                __instance.ChatCommands.Add(myChatCommand2.CommandText, myChatCommand2);
            }
            IMyChatCommand myChatCommand3 = (IMyChatCommand)Activator.CreateInstance(typeof(ChatCmd_Config).GetTypeInfo());
            if (myChatCommand3 != null)
            {
                __instance.ChatCommands.Add(myChatCommand3.CommandText, myChatCommand3);
            }
            MySession.Static.OnReady += SessionReady;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyHudChat), "OnMultiplayer_ChatMessageReceived")]
        public static bool ChatRecievedPrefix(ulong steamUserId, string messageText, ChatChannel channel, long targetId, string customAuthorName = null)
        {
            if (MySession.Static.IsUserAdmin(steamUserId))
            {
                return true;
            }
            if (!Settings.HideServer && steamUserId == MyMultiplayer.Static.ServerId)
            {
                return true;
            }
            if (Settings.HideServer && steamUserId == MyMultiplayer.Static.ServerId)
            {
                return false;
            }
            if (Settings.HideGlobal && channel == ChatChannel.Global)
            {
                return false;
            }
            string item = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamUserId);
            return !Settings.BlockedNames.Contains(item) && (!Settings.HideFaction || channel != ChatChannel.Faction) && (!Settings.HidePrivate || channel != ChatChannel.Private);
        }

        public static void SessionReady()
        {
            MySession.Static.OnReady -= SessionReady;
            MySession.OnUnloading += OnUnloading;
            LoadSettings();
        }

        public static void OnUnloading()
        {
            MySession.OnUnloading -= OnUnloading;
            SaveSettings();
        }

        public static void LoadSettings()
        {
            if (!File.Exists(path))
            {
                Settings = null;
                MyLog.Default.Info("[ChatFilter] Settings file not found.");
                SaveSettings();
                return;
            }
            try
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    Settings = MyAPIGateway.Utilities.SerializeFromXML<CFSettings>(streamReader.ReadToEnd());
                }
                if (Settings != null)
                {
                    StringBuilder sb = new StringBuilder("[ChatFilter] Loaded settings. Muted players: ");
                    for (int i = 0; i < Settings.BlockedNames.Count; i++)
                    {
                        string name = Settings.BlockedNames[i];
                        sb.Append(name);
                        if (i != Settings.BlockedNames.Count - 1)
                            sb.Append(", ");
                    }
                    MyLog.Default.Info(sb);
                    return;
                }
                else
                {
                    MyLog.Default.Info("[ChatFilter] Failed to load settings.");
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.Error("[Chat Filter] Error during settings load: ", ex);
            }
            Settings = null;
            SaveSettings();
        }

        public static void SaveSettings()
        {
            if (Settings == null)
            {
                Settings = new CFSettings();
                MyLog.Default.Info("[Chat Filter] Saving default settings.");
            }
            try
            {
                File.WriteAllText(path, MyAPIGateway.Utilities.SerializeToXML(Settings));
            }
            catch (Exception ex)
            {
                MyLog.Default.Info("[Chat Filter] Error during settings save: ", ex);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyHudChat), "multiplayer_ScriptedChatMessageReceived")]
        public static bool ScriptedChatRecievedPrefix(string message, string author, string font, Color color)
        {
            if (color == Color.Purple)
            {
                return true;
            }
            if (Settings.HideGlobal && author != "Server" && author != "Good.bot")
            {
                return false;
            }
            if (Settings.HideServer && (author == "Server" || author == "Good.bot"))
            {
                return false;
            }
            foreach (string value in Settings.BlockedNames)
            {
                if (author.Contains(value))
                {
                    return false;
                }
            }
            return true;
        }

        private static readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers\\ChatFilter.cfg");

        public static CFSettings Settings = new CFSettings();
    }

    [Serializable]
    public class CFSettings
    {
        public CFSettings()
        {
        }

        public List<string> BlockedNames = new List<string>();

        public bool HideFaction;

        public bool HideGlobal;

        public bool HidePrivate;

        public bool HideServer;
    }

    public class ChatCmd_Config : IMyChatCommand
    {
        public ChatCmd_Config()
        {
        }

        public void Handle(string[] args)
        {
            MyGuiSandbox.AddScreen(new CFConfig());
        }

        public string CommandText
        {
            get
            {
                return "/cfconfig";
            }
        }

        public string HelpSimpleText
        {
            get
            {
                return "cfconfig";
            }
        }

        public string HelpText
        {
            get
            {
                return "Open Chat Filter configuration screen.";
            }
        }

        public MyPromoteLevel VisibleTo
        {
            get
            {
                return MyPromoteLevel.None;
            }
        }
    }

    public class ChatCmd_Mute : IMyChatCommand
    {
        public ChatCmd_Mute()
        {
        }

        public void Handle(string[] args)
        {
            if (args != null && args.Length != 0)
            {
                string text = args[0];
                for (int i = 1; i < args.Length; i++)
                {
                    text = text + " " + args[i];
                }

                if (text.Contains('"'))
                    text = text.Replace('"', ':').Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0];

                HashSet<string> names = MyHud.Chat.MessageHistory.Select(m => m.Sender).ToHashSet();
                if (!names.Contains(text) && text.Contains(' '))
                {
                    string[] split = text.Split(' ');
                    bool flag;
                    foreach (string name in names)
                    {
                        flag = false;
                        for (int o = 0; o < split.Length; o++)
                        {
                            flag = name.Contains(split[o]);
                            if (!flag)
                                break;
                        }
                        if (flag)
                        {
                            text = name;
                            break;
                        }
                    }
                }
                if (!ChatFilter.Settings.BlockedNames.Contains(text))
                {
                    ChatFilter.Settings.BlockedNames.Add(text);
                    MyHud.Chat.ShowMessage("ChatFilter", "You will no longer see messages from " + text, Color.SlateGray);
                    ChatFilter.SaveSettings();
                    return;
                }
                else
                {
                    MyHud.Chat.ShowMessage("ChatFilter", text + " is already muted.", Color.SlateGray);
                    return;
                }
            }
            else
            {
                string text2 = "Muted players: ";
                foreach (string str in ChatFilter.Settings.BlockedNames)
                {
                    text2 = text2 + str + ", ";
                }
                MyHud.Chat.ShowMessage("ChatFilter", text2, Color.SlateGray);
            }
        }

        public string CommandText
        {
            get
            {
                return "/mute";
            }
        }

        public string HelpSimpleText
        {
            get
            {
                return "/mute [player name]";
            }
        }

        public string HelpText
        {
            get
            {
                return "/mute [player name] - WHen provided with a player name: mutes that players chat messages. Otherwise shows a list of muted player names.";
            }
        }

        public MyPromoteLevel VisibleTo
        {
            get
            {
                return MyPromoteLevel.None;
            }
        }
    }

    public class ChatCmd_Unmute : IMyChatCommand
    {
        public ChatCmd_Unmute()
        {
        }

        public void Handle(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                string text = "Muted players: ";
                foreach (string str in ChatFilter.Settings.BlockedNames)
                {
                    text = text + str + ", ";
                }
                MyHud.Chat.ShowMessage("ChatFilter", text, Color.SlateGray);
                return;
            }
            string text2 = args[0];
            for (int i = 1; i < args.Length; i++)
            {
                text2 = text2 + " " + args[i];
            }
            if (ChatFilter.Settings.BlockedNames.Remove(text2))
            {
                MyHud.Chat.ShowMessage("ChatFilter", text2 + " has been unmuted.", Color.SlateGray);
                ChatFilter.SaveSettings();
                return;
            }
            MyHud.Chat.ShowMessage("ChatFilter", "That player is not currently muted.", Color.SlateGray);
        }

        public string CommandText
        {
            get
            {
                return "/unmute";
            }
        }

        public string HelpSimpleText
        {
            get
            {
                return "/unmute [player name]";
            }
        }

        public string HelpText
        {
            get
            {
                return "/unmute [player name] - Unmutes a player.";
            }
        }

        public MyPromoteLevel VisibleTo
        {
            get
            {
                return MyPromoteLevel.None;
            }
        }
    }
}
