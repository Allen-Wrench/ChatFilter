using Sandbox.Game.Gui;
using Sandbox;
using System;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using Sandbox.Graphics.GUI;
using Sandbox.Game.World;
using System.Text;

namespace ChatFilter
{
	public class CFConfigGui : MyGuiScreenBase
	{
		public CFConfigGui()
			: base(new Vector2?(new Vector2(0.5f, 0.5f)), new Vector4?(MyGuiConstants.SCREEN_BACKGROUND_COLOR), new Vector2?(new Vector2(0.55f, 0.65f)), true, null, 0f, 0f, null)
		{
			CloseButtonEnabled = true;
			ChatFilterPlugin.LoadConfig();
			RecreateControls(true);
		}

		public override string GetFriendlyName()
		{
			return "ChatFilterConfig";
		}

		public override bool CloseScreen(bool isUnloading = false)
		{
			ChatFilterPlugin.SaveConfig();
			return base.CloseScreen(isUnloading);
		}

		private void OnOk(MyGuiControlButton button)
		{
			CloseScreen(false);
		}

		public override void RecreateControls(bool constructor)
		{
			base.RecreateControls(constructor);
			Vector2 value = new Vector2(0f, m_size.Value.Y / 12f);
			Vector2 value2 = new Vector2(0.02f, 0f);
			Vector2 position = new Vector2(-0.075f, -.1f);
			position -= value * 2.25f;
			AddCaption("Chat Filter Configuration", new Vector4?(Color.White.ToVector4()), null, 0.8f);
			m_HideGlobalCheckbox = new MyGuiControlCheckbox(new Vector2?(position), null, "Hide global chat", ChatFilterPlugin.Config.HideGlobal, MyGuiControlCheckboxStyleEnum.Default, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			m_HideGlobalLabel = new MyGuiControlLabel(new Vector2?(position + value2), null, "Hide global chat", null, 0.8f, "Blue", MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, false, float.PositiveInfinity, false);
			m_HideGlobalCheckbox.IsCheckedChanged = delegate
			{
				ChatFilterPlugin.Config.HideGlobal = m_HideGlobalCheckbox.IsChecked;
			};
			Controls.Add(m_HideGlobalCheckbox);
			Controls.Add(m_HideGlobalLabel);
			position += value;
			m_HideFactionCheckbox = new MyGuiControlCheckbox(new Vector2?(position), null, "Hide faction chat", ChatFilterPlugin.Config.HideFaction, MyGuiControlCheckboxStyleEnum.Default, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			m_HideFactionLabel = new MyGuiControlLabel(new Vector2?(position + value2), null, "Hide faction chat", null, 0.8f, "Blue", MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, false, float.PositiveInfinity, false);
			m_HideFactionCheckbox.IsCheckedChanged = delegate
			{
				ChatFilterPlugin.Config.HideFaction = m_HideFactionCheckbox.IsChecked;
			};
			Controls.Add(m_HideFactionCheckbox);
			Controls.Add(m_HideFactionLabel);
			position += value;
			m_HidePrivateCheckbox = new MyGuiControlCheckbox(new Vector2?(position), null, "Hide private messages", ChatFilterPlugin.Config.HidePrivate, MyGuiControlCheckboxStyleEnum.Default, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			m_HidePrivateLabel = new MyGuiControlLabel(new Vector2?(position + value2), null, "Hide private messages", null, 0.8f, "Blue", MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, false, float.PositiveInfinity, false);
			m_HidePrivateCheckbox.IsCheckedChanged = delegate
			{
				ChatFilterPlugin.Config.HidePrivate = m_HidePrivateCheckbox.IsChecked;
			};
			Controls.Add(m_HidePrivateCheckbox);
			Controls.Add(m_HidePrivateLabel);
			position += value;
			m_HideServerCheckbox = new MyGuiControlCheckbox(new Vector2?(position), null, "Hide messages from server", ChatFilterPlugin.Config.HideServer, MyGuiControlCheckboxStyleEnum.Default, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			m_HideServerLabel = new MyGuiControlLabel(new Vector2?(position + value2), null, "Hide server messages", null, 0.8f, "Blue", MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, false, float.PositiveInfinity, false);
			m_HideServerCheckbox.IsCheckedChanged = delegate
			{
				ChatFilterPlugin.Config.HideServer = m_HideServerCheckbox.IsChecked;
			};
			Controls.Add(m_HideServerCheckbox);
			Controls.Add(m_HideServerLabel);
			value2 = new Vector2(0.13f, 0f);
			position.X = 0f;
			position += value;
			namesLabel = new MyGuiControlLabel(new Vector2?(position - value2), text: "Unmuted Players", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			mutedNamesLabel = new MyGuiControlLabel(new Vector2?(position + value2), text: "Muted Players", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
			Controls.Add(namesLabel);
			Controls.Add(mutedNamesLabel);
			position += value * 2f;
			namesListbox = new MyGuiControlListbox
			{
				VisualStyle = MyGuiControlListboxStyleEnum.Terminal,
				VisibleRowsCount = 6,
				Size = new Vector2(0.2f, 0.2f),
				Position = position - value2,
				MultiSelect = false
			};
			namesListbox.SetToolTip("Double click a name to mute chat from that player.");
			namesListbox.ItemClicked += NameListboxClicked;
			Controls.Add(namesListbox);
			if (MySession.Static != null && MySession.Static.Players != null)
			{
				foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
				{
					MyGuiControlListbox.Item item = new MyGuiControlListbox.Item(new StringBuilder(player.DisplayName));
					namesListbox.Add(item);
					if (ChatFilterPlugin.Config.BlockedNames.Contains(player.DisplayName))
						item.Visible = false;
				}
			}
			mutedNamesListbox = new MyGuiControlListbox
			{
				VisualStyle = MyGuiControlListboxStyleEnum.Terminal,
				VisibleRowsCount = 6,
				Size = new Vector2(0.2f, 0.2f),
				Position = position + value2,
				MultiSelect = false
			};
			mutedNamesListbox.SetToolTip("Double click a name to unmute chat from that player.");
			mutedNamesListbox.ItemClicked += MutedNameListboxClicked;
			Controls.Add(mutedNamesListbox);
			foreach (string name in ChatFilterPlugin.Config.BlockedNames)
			{
				mutedNamesListbox.Add(new MyGuiControlListbox.Item(new StringBuilder(name)));
			}
			position += value * 2.4f;
			value2 = new Vector2(0.13f, 0f);
			muteButton = new MyGuiControlButton(null, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, "Mute the selected player", new StringBuilder("Mute"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_CURSOR_OVER, new Action<MyGuiControlButton>(MutePlayer), GuiSounds.MouseClick, 1f, null, false, false, false, null)
			{
				Position = position - value2,
				Enabled = false,
			};
			Controls.Add(muteButton);
			unmuteButton = new MyGuiControlButton(null, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, "Unmute the selected player", new StringBuilder("Unmute"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_CURSOR_OVER, new Action<MyGuiControlButton>(UnmutePlayer), GuiSounds.MouseClick, 1f, null, false, false, false, null)
			{
				Position = position + value2,
				Enabled = false,
			};
			Controls.Add(unmuteButton);
			position += value * 1.1f;
			m_okButton = new MyGuiControlButton(null, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, MyTexts.Get(MyCommonTexts.Ok), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_CURSOR_OVER, new Action<MyGuiControlButton>(OnOk), GuiSounds.MouseClick, 1f, null, false, false, false, null);
			m_okButton.Position = position;
			m_okButton.Size = new Vector2(0.06f, 0.03f);
			m_okButton.SetToolTip("Save changes and close window");
			Controls.Add(m_okButton);
		}

		private void NameListboxClicked(MyGuiControlListbox o)
		{
			if (namesListbox.SelectedItems.Count > 0)
			{
				mutedNamesListbox.SelectedItems.Clear();
				unmuteButton.Enabled = false;
				muteButton.Enabled = true;
			}
		}

		private void MutedNameListboxClicked(MyGuiControlListbox o)
		{
			if (mutedNamesListbox.SelectedItems.Count > 0)
			{
				namesListbox.SelectedItems.Clear();
				muteButton.Enabled = false;
				unmuteButton.Enabled = true;
			}
		}

		private void MutePlayer(MyGuiControlButton btn)
		{
			if (namesListbox.SelectedItems.Count == 0 || namesListbox.SelectedItems[0].Text == null)
				return;

			StringBuilder name = namesListbox.SelectedItems[0].Text;
			if (!ChatFilterPlugin.Config.BlockedNames.Contains(name.ToString()))
			{
				ChatFilterPlugin.Config.BlockedNames.Add(name.ToString());
				mutedNamesListbox.Add(new MyGuiControlListbox.Item(name));
				namesListbox.SelectedItems[0].Visible = false;
				muteButton.Enabled = false;
			}
		}

		private void UnmutePlayer(MyGuiControlButton btn)
		{
			if (mutedNamesListbox.SelectedItems.Count == 0 || mutedNamesListbox.SelectedItems[0].Text == null)
				return;

			StringBuilder name = mutedNamesListbox.SelectedItems[0].Text;
			if (ChatFilterPlugin.Config.BlockedNames.Contains(name.ToString()))
			{
				ChatFilterPlugin.Config.BlockedNames.Remove(name.ToString());
				int i = namesListbox.Items.FindIndex(x => x.Text == name);
				if (i >= 0)
					namesListbox.Items[i].Visible = true;
				mutedNamesListbox.SelectedItems[0].Visible = false;
				unmuteButton.Enabled = false;
			}
		}


		private MyGuiControlCheckbox m_HideFactionCheckbox;

		private MyGuiControlLabel m_HideFactionLabel;

		private MyGuiControlCheckbox m_HideGlobalCheckbox;

		private MyGuiControlLabel m_HideGlobalLabel;

		private MyGuiControlCheckbox m_HidePrivateCheckbox;

		private MyGuiControlLabel m_HidePrivateLabel;

		private MyGuiControlCheckbox m_HideServerCheckbox;

		private MyGuiControlLabel m_HideServerLabel;

		private MyGuiControlButton m_okButton;

		private MyGuiControlLabel namesLabel;

		private MyGuiControlListbox namesListbox;

		private MyGuiControlLabel mutedNamesLabel;

		private MyGuiControlListbox mutedNamesListbox;
		private MyGuiControlButton muteButton;
		private MyGuiControlButton unmuteButton;
	}
}
