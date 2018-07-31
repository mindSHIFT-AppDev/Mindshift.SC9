using Mindshift.SC9.Common;
using Sitecore.Shell.Framework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindshift.SC9.DynamicLayoutDetails.Commands {
	public class DynamicLayoutDetailsDialogCommand : OpenDialogCommand {
		public override void Execute(CommandContext context) {

			OpenDialog("DynamicLayoutDetails", new Dictionary<string, string> { { "itemId", context.Items[0].ID.ToString() }, { "database", context.Items[0].Database.Name }, { "language", context.Items[0].Language.Name }, { "version", context.Items[0].Version.ToString() } });
		}
	}
}