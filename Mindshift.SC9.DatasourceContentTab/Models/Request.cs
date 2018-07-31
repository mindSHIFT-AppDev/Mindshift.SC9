using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO: if this isn't used, get rid of it!
namespace Mindshift.SC9.DatasourceContentTab.Models {
	public class DynamicGetRequest {
		public string ItemId { get; set; }
		public string Database { get; set; }
	}

	public class DynamicSaveRequest : DynamicGetRequest { // just so I can pass it along
		public List<RequestLayoutType> LayoutTypes { get; set; }

	}

	public class RequestPlaceholder {
		public string Name { get; private set; }
		public string ParentUniqueId { get; private set; }
		public bool Exists { get; private set; }

		public List<RequestRendering> Renderings { get; set; }
	}


	public class RequestRendering {
		public string ItemId { get; set; }
		public string UniqueId { get; set; }
		public string ParentUniqueId { get; set; }

		public string Name { get; set; }
		public string DisplayName { get; set; }

		public string PlaceholderName { get; set; }
		public string PlaceholderPath { get; set; }

		public string DataSourcePath { get; set; }

		public List<RequestPlaceholder> Placeholders { get; set; }
	}


	public class RequestDevice {
		public string Id { get; set; }
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Icon { get; set; }
		public List<RequestPlaceholder> Placeholders { get; set; }
	}

	public class RequestLayoutType {
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public List<RequestDevice> Devices { get; set; }

	}


	//public class GetSelectRenderingRequest {


	//}


}
