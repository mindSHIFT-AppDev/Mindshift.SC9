using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Shell.Framework.Commands;

namespace Mindshift.SC9.Common {
	public abstract class OpenDialogCommand : Command {
		protected void OpenDialog(string dialogName, Dictionary<string, string> parameters) { 
			// Can we combine this with something common?
			// just a baseclass or method would help

			string queryString = string.Join("&",parameters.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)));

			// TODO: parameters?
			Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(string.Format("/sitecore%20modules/Mindshift.SC/Dialog/dialog.html?dialog={1}&{0}", queryString, dialogName), "1000", "600", "Select Version", false, "1000", "600", true);
		}
	}
}
