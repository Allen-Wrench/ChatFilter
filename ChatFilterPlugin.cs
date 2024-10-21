using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SpaceEngineers.Game.GUI;
using VRage.FileSystem;
using VRage.Plugins;
using VRage.Utils;

namespace ChatFilter
{
	public class ChatFilterPlugin : IDisposable, IPlugin
	{
		public ChatFilterPlugin()
		{

			try
			{
				if (!File.Exists(configFilePath))
				{
					SaveConfig();
					NewUpdate = true;
				}
				else
				{
					if (LoadConfig() && latestPatchNotes != Config.LatestPatchNotes)
					{
						NewUpdate = true;
					}
				}
			}
			catch { }


		}
		public void Dispose()
		{
		}

		public void Init(object gameInstance)
		{
			new Harmony("ChatFilter").PatchAll(Assembly.GetExecutingAssembly());
			if (NewUpdate)
			{
				MyGuiScreenMainMenu.OnOpened += ShowNewUpdatePopup;
			}
		}

		public void Update()
		{
		}

		public void OpenConfigDialog()
		{
			MyScreenManager.AddScreen(new CFConfigGui());
		}

		public static bool LoadConfig()
		{
			try
			{
				if (File.Exists(configFilePath))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(CFSettings));
					Config = (CFSettings)serializer.Deserialize(XmlReader.Create(configFilePath));
					return true;
				}
				return false;
			}
			catch { return false; }
		}

		public static void SaveConfig()
		{
			try
			{
				Directory.CreateDirectory(configFileDirectory);
				XmlSerializer serializer = new XmlSerializer(typeof(CFSettings));
				serializer.Serialize(XmlWriter.Create(configFilePath), Config);
			}
			catch { }
		}

		private static void ShowNewUpdatePopup()
		{
			MyGuiScreenMainMenu.OnOpened -= ShowNewUpdatePopup;
			NewUpdate = false;
			Config.LatestPatchNotes = latestPatchNotes;
			MyGuiSandbox.Show(new StringBuilder(latestPatchNotes), MyStringId.GetOrCompute("Chat Filter plugin has updated!"), MyMessageBoxStyleEnum.Info);
			SaveConfig();
		}

		public static CFSettings Config = new CFSettings();
		public static bool NewUpdate { get; private set; }

		private static readonly string configFileDirectory = Path.Combine(MyFileSystem.UserDataPath, "Plugins");
		private static readonly string configFilePath = Path.Combine(configFileDirectory, "ChatFilter.config.xml");
		private static readonly string latestPatchNotes =
			"- Configuration UI accessible through the plugin menu." + "\n" +
			"- Ability to mute/unmute players from within the config gui." + "\n" +
			"- These fancy new update notifications.";
	}
}