using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.GameSystems.Chat;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.GameServices;
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
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyHudChat), "OnMultiplayer_ChatMessageReceived")]
        public static bool ChatRecievedPrefix(ulong steamUserId, string messageText, ChatChannel channel, long targetId, ChatMessageCustomData? customData)
        {
            if (MySession.Static.IsUserAdmin(steamUserId))
            {
                return true;
            }
            if (!ChatFilterPlugin.Config.HideServer && steamUserId == MyMultiplayer.Static.ServerId)
            {
                return true;
            }
            if (ChatFilterPlugin.Config.HideServer && steamUserId == MyMultiplayer.Static.ServerId)
            {
                return false;
            }
            if (ChatFilterPlugin.Config.HideGlobal && channel == ChatChannel.Global)
            {
                return false;
            }
            string item = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamUserId);
            return !ChatFilterPlugin.Config.BlockedNames.Contains(item) && (!ChatFilterPlugin.Config.HideFaction || channel != ChatChannel.Faction) && (!ChatFilterPlugin.Config.HidePrivate || channel != ChatChannel.Private);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyHudChat), "multiplayer_ScriptedChatMessageReceived")]
        public static bool ScriptedChatRecievedPrefix(string message, string author, string font, Color color)
        {
            if (color == Color.Purple)
            {
                return true;
            }
            if (ChatFilterPlugin.Config.HideGlobal && author != "Server" && author != "Good.bot")
            {
                return false;
            }
            if (ChatFilterPlugin.Config.HideServer && (author == "Server" || author == "Good.bot"))
            {
                return false;
            }
            foreach (string value in ChatFilterPlugin.Config.BlockedNames)
            {
                if (author.Contains(value))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class ChatCmd_Config : IMyChatCommand
    {
        public ChatCmd_Config()
        {
        }

        public void Handle(string[] args)
        {
            MyGuiSandbox.AddScreen(new CFConfigGui());
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

                HashSet<string> names = MyHud.Chat.Messages.Select(m => m.Sender).ToHashSet();
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
                if (!ChatFilterPlugin.Config.BlockedNames.Contains(text))
                {
					ChatFilterPlugin.Config.BlockedNames.Add(text);
                    MyHud.Chat.ShowMessage("ChatFilter", "You will no longer see messages from " + text, Color.SlateGray);
					ChatFilterPlugin.SaveConfig();
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
                foreach (string str in ChatFilterPlugin.Config.BlockedNames)
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
                foreach (string str in ChatFilterPlugin.Config.BlockedNames)
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
            if (ChatFilterPlugin.Config.BlockedNames.Remove(text2))
            {
                MyHud.Chat.ShowMessage("ChatFilter", text2 + " has been unmuted.", Color.SlateGray);
                ChatFilterPlugin.SaveConfig();
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
