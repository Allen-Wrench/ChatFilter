using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatFilter
{
	[Serializable]
	public class CFSettings
	{
		public List<string> BlockedNames = new List<string>();

		public bool HideFaction;

		public bool HideGlobal;

		public bool HidePrivate;

		public bool HideServer;

		public string LatestPatchNotes = "";
	}
}
