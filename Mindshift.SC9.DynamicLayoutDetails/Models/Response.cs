using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetPlaceholderRenderings;
using Sitecore.Pipelines.GetRenderingDatasource;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Compilation;
using System.Web.UI;

namespace Mindshift.SC9.DynamicLayoutDetails.Models {

	public static class Utility {

		private static string _placeholderRegex = null;
		private static string placeholderRegex {
			get {
				if (_placeholderRegex == null) {
					_placeholderRegex = @"\@Html\.Sitecore\(\).(?:Placeholder|DynamicPlaceholder)\(\""(.*?)[""\)].*";
				}
				return _placeholderRegex;
			}
		}



		// TODO: move this useful method somewhere!
		public static List<ResponsePlaceholder> FindPlaceholders(Item contextItem, LayoutDefinition layoutDefinition, RenderingItem renderingItem, string parentUniqueId = "") {
			var ret = new List<ResponsePlaceholder>();
			if (renderingItem != null) {
				return FindPlaceholders(contextItem, layoutDefinition, renderingItem.InnerItem["Path"], parentUniqueId);
			} else {
				return new List<ResponsePlaceholder>();
			}
		}

		public static List<ResponsePlaceholder> FindPlaceholders(Item contextItem, LayoutDefinition layoutDefinition, LayoutItem layoutItem, string parentUniqueId = "") {
			var ret = new List<ResponsePlaceholder>();
			return FindPlaceholders(contextItem, layoutDefinition, layoutItem.InnerItem["Path"], parentUniqueId);
		}

		// TODO: move this useful method somewhere!
		public static List<ResponsePlaceholder> FindPlaceholders(Item contextItem, LayoutDefinition layoutDefinition, string path, string parentUniqueId) {
			var ret = new List<ResponsePlaceholder>();
			var serverPath = System.Web.HttpContext.Current.Server.MapPath(path);

			//var serverPath2 = serverPath.Replace(".cshtml", "Display.cshtml"); // TODO: this is AWFUL :(

			//if (File.Exists(serverPath2)) {
			//	serverPath = serverPath2;
			//}

			if (File.Exists(serverPath)) {
				using (var sr = new StreamReader(serverPath)) {
					string strView = sr.ReadToEnd();


					//string test = Sitecore.Layouts.RuntimeHtml.Convert(strView);

					// TODO: this doesn't find anything on the maincontent...

					var regex = new Regex(placeholderRegex);
					var matches = regex.Matches(strView);
					foreach (Match match in matches) {
						var name = match.Groups[1].Value;
						if (!ret.Exists(p => p.Name == name && (!p.Dynamic || p.ParentUniqueId == parentUniqueId))) { // this happens when there's a duplicate placeholder. It's usually because of an if statement (IsExperienceEditor?) so the duplicate is not real and can be ignored.
							ret.Add(new ResponsePlaceholder(contextItem, layoutDefinition, name, parentUniqueId, true, match.Value.Contains(".DynamicPlaceholder(")));
						}
					}

					//@Html.Sitecore().Placeholder("sixbricks_3")
					//@Html.Sitecore().DynamicPlaceholder("sixbricks_3")
				}
			}
			return ret;
		}


	}




	public class ResponsePlaceholder {
		public string Name { get; private set; }
		public string ParentUniqueId { get; private set; }
		public bool Exists { get; private set; }
		public string Icon { get; private set; }
		public bool Dynamic { get; private set; }

		public bool DynamicRecommended { get; set; }
		//public string DynamicPlaceholderName { get; private set; }

		public List<ResponsePlaceholderRendering> ValidRenderings { get; private set; }

		public ResponsePlaceholder(Item contextItem, LayoutDefinition layoutDefinition, string name, string parentUniqueId, bool exists = true, bool dynamic = true) {
			Name = name;
			ParentUniqueId = parentUniqueId;
			Exists = exists;
			Icon = "<img src=\"/temp/IconCache/Business/16x16/table_selection_block.png\" alt=\"\" />";
			Dynamic = dynamic;

			//DynamicPlaceholderName = name + "-" + ParentUniqueId + "";

			ValidRenderings = new List<ResponsePlaceholderRendering>();
			// TODO: find all the valid renderings!
			GetPlaceholderRenderingsArgs args = new GetPlaceholderRenderingsArgs(Name, layoutDefinition.ToXml(), contextItem.Database);
			//args.ContextItemPath = contextItem.Paths.FullPath;
			//args.ShowDialogIfDatasourceSetOnRenderingItem = true;
			CorePipeline.Run("getPlaceholderRenderings", args);

			if (args.PlaceholderRenderings != null) {
				foreach (var placeholderRendering in args.PlaceholderRenderings) {
					ValidRenderings.Add(new ResponsePlaceholderRendering(placeholderRendering, layoutDefinition, contextItem));

				}
			}

		}


		private List<ResponseRendering> _Renderings = new List<ResponseRendering>();
		public ICollection<ResponseRendering> Renderings {
			get { return new ReadOnlyCollection<ResponseRendering>(_Renderings); }
		}

		internal ResponseRendering AddRendering(ResponseRendering responseRendering) {
			_Renderings.Add(responseRendering);
			return responseRendering;
		}
		internal List<ResponseRendering> AddRenderingRange(List<ResponseRendering> responseRenderingList) {
			_Renderings.AddRange(responseRenderingList);
			return responseRenderingList;
		}
	}

	public class ResponseRendering {
		public string ItemId { get; private set; }
		public string UniqueId { get; private set; }
		public string ParentUniqueId { get; private set; }

		public string Name { get; private set; }
		public string DisplayName { get; private set; }

		// note: PlaceholderName, PlaceholderSeed and ParentUniqueId path contain the important last placholder that DynamicPlaceholderPath has (it's .Last!)
		public string PlaceholderName { get; private set; }
		public string PlaceholderSeed { get; private set; }
		public string PlaceholderPath { get; private set; }
		public bool InvalidDynamicPlaceholder { get; set; }
		public DynamicPlaceholderPath DynamicPlaceholderPath { get; private set; }
		public string DataSourceId { get; private set; }
		public string DataSourcePath { get; private set; }

		public List<string> PossiblePlaceholderPaths { get; private set; }
		public string DataSourceEditFrameUrl { get; private set; }

		public string Icon { get; set; }

		public string Error { get; set; }

		public string DatasourceLocation { get; private set; }

		//// TODO: move this to some place common
		///// <summary>
		///// Gets a Control that will render a given Razor view path
		///// </summary>
		//public static Control GetControl(string viewPath) {
		//	Sitecore.Diagnostics.Assert.IsNotNullOrEmpty(viewPath, "ViewPath cannot be empty. The Rendering item in Sitecore needs to have a view path set.");

		//	Type viewType = BuildManager.GetCompiledType(viewPath);
		//	PropertyInfo typedModelProperty = viewType.GetProperties().FirstOrDefault(x => x.PropertyType != typeof(object) && x.Name == "Model");
		//	Type viewModelType = typedModelProperty != null ? typedModelProperty.PropertyType : typeof(object);

		//	var renderingType = typeof(RazorViewShim<>).MakeGenericType(viewModelType);

		//	var shim = (IRazorViewShim)Activator.CreateInstance(renderingType);

		//	shim.ViewPath = viewPath;

		//	return shim as Control;
		//}

		public ResponseRendering(RenderingReference renderingReference, LayoutDefinition layoutDefinition, Item contextItem) {
			PossiblePlaceholderPaths = new List<string>();
			var renderingItem = renderingReference.RenderingItem;
			if (renderingItem != null) {// Render missing?
				Name = renderingItem.Name;
				ItemId = renderingItem.ID.ToString().Replace("{", "").Replace("}", "").ToLower();
				DisplayName = renderingItem.DisplayName;
				Icon = ThemeManager.GetIconImage(renderingReference.RenderingItem.InnerItem, 32, 32, "", "");

				// datasourceLocation
				string datasourceLocationSource = renderingItem.InnerItem["Datasource Location"];
				//List<string> datasourceLocationsList = new List<string>();
				if (contextItem.Name != "__Standard Values") { // disable on Standard Values, for now.
					GetRenderingDatasourceArgs args = new GetRenderingDatasourceArgs(renderingItem.InnerItem, contextItem.Database);
					args.ContextItemPath = contextItem.Paths.FullPath;
					args.ShowDialogIfDatasourceSetOnRenderingItem = true;
					CorePipeline.Run("getRenderingDatasource", args);
					//var datasourceLocationsList = new List<string>();
					//rendering.DatasourceLocation
					if (args.DialogUrl != null) {
						DatasourceLocation = args.DialogUrl.Replace("/layouts/xmlcontrol.aspx", "/sitecore/shell/default.aspx");
					}
				}
				//foreach (var datasourceLocationItem in args.DatasourceRoots) {
				//	var dataSourceLocationId = datasourceLocationItem.ID.ToString();
				//	datasourceLocationsList.Add("sitecore://" + contextItem.Database.Name + "/" + dataSourceLocationId + "?lang=" + contextItem.Language.Name + "&var=" + contextItem.Version.Number.ToString());
				//}

				//DatasourceLocation = string.Join("|", datasourceLocationsList);


				//if (!string.IsNullOrEmpty(datasourceLocationSource)) {
				//	string[] arrDatasourceLocation = datasourceLocationSource.Split("|".ToCharArray());
				//	foreach (var datasourceLocation in arrDatasourceLocation) {
				//		var datasourceLocationItem = contextItem.Database.GetItem(datasourceLocation.Replace(".", contextItem.Paths.FullPath));
				//		if (datasourceLocationItem != null) {
				//			var dataSourceLocationId = datasourceLocationItem.ID.ToString();
				//			datasourceLocationsList.Add("sitecore://" + contextItem.Database.Name + "/" + dataSourceLocationId + "?lang=" + contextItem.Language.Name + "&var=" + contextItem.Version.Number.ToString());
				//			//args.CurrentDatasource = ds.Replace("./countryroot", countryRoot.SCItem.Paths.FullPath);
				//		}


				//	}
				//	DatasourceLocation = string.Join("|", datasourceLocationsList);

				//}
				// TODO: it wants the actual item IDs!
				// from: 
				// ./Content/Page Generic Image|./countryroot/Site Data/Page Content/Site Generic Image|/sitecore/content/Ricoh/Global/Data/Page Content/Global Generic Image
				// to:
				// sitecore://master/{C28482C3-DBFC-41BD-B2DA-C0693C471E2C}?lang=en&ver=1|sitecore://master/{87FA6B8D-0E11-4810-A007-5156BC625A72}?lang=en&ver=1|sitecore://master/{C59A3D62-0A40-4586-A862-4BF5DC13B86D}?lang=en&ver=1

			} else {
				Error = "Rendering Item was not found.";

			}
			PlaceholderPath = renderingReference.Placeholder;


			UniqueId = renderingReference.UniqueId.ToString().Replace("{", "").Replace("}", "").ToLower();


			DataSourceId = renderingReference.Settings.DataSource;
			if (!string.IsNullOrWhiteSpace(DataSourceId)) {
				var datasourceItem = renderingReference.Database.GetItem(DataSourceId);
				if (datasourceItem != null) {
					DataSourcePath = datasourceItem.Paths.FullPath;

					// TODO: verify thiss
					// TODO: created edit frame URL
					// TODO: display something when there's no "data source" and their should be
					// - e.g. "Created Data Source" button!
					// where to get the fild list and what is "form"
					//var fieldDescriptorList = new List<FieldDescriptor> {

					//	// TODO: add based on which ones are checked in the TemplateField!!! Easy!
					//	new FieldDescriptor(datasourceItem, "Heading")
					//};
					//var fieldEditorOptions = new Sitecore.Shell.Applications.ContentEditor.FieldEditorOptions(fieldDescriptorList);
					//DataSourceEditFrameUrl = fieldEditorOptions.ToUrlString().GetUrl();

				} else { // item not found, show the guid.
					DataSourcePath = DataSourceId;
				}
			}

			// TODO: find all placeholders on this rendering!
			//AddPlaceholder
			//var test = renderingReference.RenderingItem.

			// TODO: no idea if this will actually work...
			//var control = renderingReference.GetControl();
			//string path = renderingReference.RenderingItem.InnerItem["Path"];


			// so strange that this replace is needed.
			//var path = renderingReference.RenderingItem.InnerItem.Paths.Path.ToLower();//.Replace("/sitecore/layout/renderings", "");
			//var control = Sitecore.Layouts.ControlFactory.GetRenderingControl(path, false);

			//var control = renderingItem.GetControl(renderingReference.Settings);

			//if (control != null) {
			//	var placeHolderControlList = control.Controls.Cast<Control>()
			//		.Where(x => x is global::Sitecore.Web.UI.WebControls.Placeholder)
			//		.Cast<global::Sitecore.Web.UI.WebControls.Placeholder>().ToList();

			//	placeHolderControlList.ForEach(ph => AddPlaceholder(ph.RenderingName));
			//}


			// /maincontent/pinwheel_5~8400f0bbe7144aef9c8ef0a4cfd28644

			// TODO: ignore other parts of the path? For now, but we'll need it if we have a dynamic placeholder in multiple places!
			if (!string.IsNullOrWhiteSpace(PlaceholderPath)) {
				// TODO: might fail if not dynamic, how we know?
				DynamicPlaceholderPath = new DynamicPlaceholderPath(renderingReference.Placeholder);
				var placeholder = DynamicPlaceholderPath.DynamicPlaceholderPathElements.Last(); // 


				// TODO: this is no longer the case in SC9? Where is this defined now?
				// - old way:
				// /maincontent/pinwheel_5~8400f0bbe7144aef9c8ef0a4cfd28644
				// - new way:
				// /maincontent/tab-{95EF4582-265A-46E2-B35D-C20FF98A9366}-1234273131

				//				renderingReference.

				if (placeholder.ParentUniqueId != null) { // if the last one has a GUID, ignore the whole path - it's irrelevant - in fact, this will "fix" it.

					PlaceholderName = placeholder.Name;
					PlaceholderSeed = placeholder.Seed;

					ParentUniqueId = placeholder.ParentUniqueId;
				} else { // here we'll need to eventually find the parent by its whole path and then fix it. This can be done in the Controller by checking the ParentUniqueId...
					PlaceholderName = placeholder.Name; // TODO: what's the GUID here?
				}
			}
			//ViewRenderer test = new ViewRenderer();
			////test.Model = new { };
			//test.Rendering = new Rendering();
			//Sitecore.Layouts.RenderingDefinition test2 = new RenderingDefinition();

			//System.Web.Mvc.HtmlHelper test = new System.Web.Mvc.HtmlHelper()

			_Placeholders = Utility.FindPlaceholders(contextItem, layoutDefinition, renderingItem, UniqueId); // note: it's not ParentUniqueId - this is the parent

			//if (PlaceholderPath.Contains("~")) {

			//	var i = Placeholder.LastIndexOf("~");
			//	// TODO: but what if it's not LAST?

			//	string strGuid = Placeholder.Substring(i + 1, Placeholder.Length - i - 1);
			//	try {
			//		ParentUniqueId = Guid.ParseExact(strGuid, "N").ToString("D").ToLower();
			//	} catch (Exception) {
			//		throw new Exception("Once you use DynamicPlaceholder, all children are required to use it.");

			//	}
			//}

			//renderingReference.Placeholder; // which one? does it matter?

			// TODO: here is where we will parse the UniqueId...
		}


		internal ResponseRendering AddRendering(Item contextItem, LayoutDefinition layoutDefinition, ResponseRendering responseRendering) {

			var placeholder = _Placeholders.SingleOrDefault(p => p.Name == responseRendering.PlaceholderName && (!p.Dynamic || p.ParentUniqueId == responseRendering.ParentUniqueId)); // TODO: we're not using the seed here. But how can we?
			if (placeholder == null) {
				// TODO: but this would be a NOT FOUND situation?
				// - basically this means that that placeholder wasn't really on that rendering
				// - we 
				// TODO: the real question is, why wasn't maincontent already added?
				// TODO: UniqueId isn't the Guid this goes into, it's the Guid of what this is. Or what happened to be the first one found! It needs to be PARENT
				placeholder = new ResponsePlaceholder(contextItem, layoutDefinition, responseRendering.PlaceholderName, ParentUniqueId, true);
				_Placeholders.Add(placeholder);
			}
			var rendering = placeholder.AddRendering(responseRendering);
			return rendering;
		}

		internal List<ResponseRendering> AddRenderingRange(Item contextItem, LayoutDefinition layoutDefinition, List<ResponseRendering> responseRenderingList) {
			responseRenderingList.ForEach(r => AddRendering(contextItem, layoutDefinition, r));
			return responseRenderingList;
		}


		private List<ResponsePlaceholder> _Placeholders = new List<ResponsePlaceholder>();
		public ICollection<ResponsePlaceholder> Placeholders {
			get { return new ReadOnlyCollection<ResponsePlaceholder>(_Placeholders); }
		}

	}


	public class ResponseDevice {
		public string Id { get; private set; }
		public string Name { get; private set; }
		public string DisplayName { get; private set; }
		//public string Xml { get; set; } // commented because very large
		public string Icon { get; set; }

		public string LayoutId { get; set; }
		public string LayoutName { get; set; }
		public string LayoutDisplayName { get; set; }

		public ResponseDevice(Item contextItem, LayoutDefinition layoutDefinition, DeviceItem deviceItem, ID layoutID) {
			Id = deviceItem.ID.ToString().Replace("{", "").Replace("}", "").ToLower();
			Name = deviceItem.Name;
			DisplayName = deviceItem.DisplayName;

			LayoutItem layoutItem = deviceItem.Database.GetItem(layoutID);
			if (layoutItem != null) {
				LayoutName = layoutItem.Name;
				LayoutDisplayName = layoutItem.DisplayName;
				LayoutId = layoutItem.ID.ToString().Replace("{", "").Replace("}", "").ToLower();

				_Placeholders = Utility.FindPlaceholders(contextItem, layoutDefinition, layoutItem);

			}

			Icon = ThemeManager.GetIconImage(deviceItem.InnerItem, 32, 32, "", "");



			// TODO: until we figure this out - it will show as invalid... kind of - unless we don't worry about this level?
			//		var placeHolderControlList = layoutItem.Control.Controls.Cast<Control>()
			//.Where(x => x is global::Sitecore.Web.UI.WebControls.Placeholder)
			//.Cast<global::Sitecore.Web.UI.WebControls.Placeholder>().ToList();

			//		placeHolderControlList.ForEach(ph => AddPlaceholder(ph.RenderingName));



		}





		internal ResponseRendering AddRendering(Item contextItem, LayoutDefinition layoutDefinition, ResponseRendering responseRendering) {
			var placeholder = _Placeholders.SingleOrDefault(p => p.Name == responseRendering.PlaceholderName && (!p.Dynamic || p.ParentUniqueId == responseRendering.ParentUniqueId));
			if (placeholder == null) {
				// TODO: but this would be a NOT FOUND situation?
				// - basically this means that that placeholder wasn't really on that rendering
				// - or it is dynamic and not referenced dynamically!
				//var possiblePlaceholders = _Placeholders.Where(p => p.Dynamic && p.Name == responseRendering.PlaceholderName).ToList();
				//if (possiblePlaceholders.Count > 0) { // this means that we are dynamic, but we haven't built out our placeholder name with the guid, etc

				//	// TODO: what about seed?
				//	responseRendering.PossiblePlaceholderPaths.AddRange(possiblePlaceholders.Select(p => p.Name + "-" + p.ParentUniqueId)); // TODO: check if this causes any recursion problems?
				//	placeholder = new ResponsePlaceholder(contextItem, layoutDefinition, responseRendering.PlaceholderName, "", false, true);
				//} else {
				//}


				placeholder = new ResponsePlaceholder(contextItem, layoutDefinition, responseRendering.PlaceholderName, "", false, false);
				_Placeholders.Add(placeholder);
			}
			var rendering = placeholder.AddRendering(responseRendering);
			return rendering;
		}

		internal List<ResponseRendering> AddRenderingRange(Item contextItem, LayoutDefinition layoutDefinition, List<ResponseRendering> responseRenderingList) {
			responseRenderingList.ForEach(r => AddRendering(contextItem, layoutDefinition, r));
			return responseRenderingList;
		}


		private List<ResponsePlaceholder> _Placeholders = new List<ResponsePlaceholder>();
		public ICollection<ResponsePlaceholder> Placeholders {
			get { return new ReadOnlyCollection<ResponsePlaceholder>(_Placeholders); }
		}

		//internal ResponsePlaceholder AddPlaceholder(string name) {
		//	var responsePlaceholder = new ResponsePlaceholder(name, true);
		//	_Placeholders.Add(responsePlaceholder);
		//	return responsePlaceholder;
		//}


	}

	public class ResponseLayoutType {
		public string Name { get; private set; }
		public string DisplayName { get; private set; }

		public ResponseLayoutType(string name, string displayName) {
			Name = name;
			DisplayName = displayName;
		}

		private List<ResponseDevice> _Devices = new List<ResponseDevice>();
		public ICollection<ResponseDevice> Devices {
			get { return new ReadOnlyCollection<ResponseDevice>(_Devices); }
		}


		internal ResponseDevice AddDevice(Item contextItem, LayoutDefinition layoutDefinition, DeviceItem item, ID layoutID) {
			var layoutDevice = new ResponseDevice(contextItem, layoutDefinition, item, layoutID);
			_Devices.Add(layoutDevice);
			return layoutDevice;
		}


	}

	public class DynamicResponse {
		public string ItemId { get; private set; }
		public string Database { get; private set; }
		public DynamicResponse(string itemId, string database) {
			ItemId = itemId;
			Database = database;
		}


		private List<ResponseLayoutType> _LayoutTypes = new List<ResponseLayoutType>();
		public ICollection<ResponseLayoutType> LayoutTypes {
			get { return new ReadOnlyCollection<ResponseLayoutType>(_LayoutTypes); }
		}


		internal ResponseLayoutType AddLayoutType(string name, string displayName) {
			var layoutTypeItem = new ResponseLayoutType(name, displayName);
			_LayoutTypes.Add(layoutTypeItem);
			return layoutTypeItem;
		}

	}

	public class ResponsePlaceholderRendering {
		public string ItemId { get; private set; }
		public string UniqueId { get; private set; }

		public string Name { get; private set; }
		public string DisplayName { get; private set; }

		public string Icon { get; set; }

		private List<ResponsePlaceholder> _Placeholders = new List<ResponsePlaceholder>();
		public ICollection<ResponsePlaceholder> Placeholders {
			get { return new ReadOnlyCollection<ResponsePlaceholder>(_Placeholders); }
		}

		public ResponsePlaceholderRendering(Item renderingItem, LayoutDefinition layoutDefinition, Item contextItem) {
			if (renderingItem != null) {// Render missing?
				Name = renderingItem.Name;
				ItemId = renderingItem.ID.ToString().Replace("{", "").Replace("}", "").ToLower();
				DisplayName = renderingItem.DisplayName;
				Icon = ThemeManager.GetIconImage(renderingItem, 32, 32, "", "");

				UniqueId = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").ToLower(); // new guid to use for the UniqueId
				_Placeholders = Utility.FindPlaceholders(contextItem, layoutDefinition, (RenderingItem)renderingItem, UniqueId); // note: it's not ParentUniqueId - this is the parent

			}
		}
	}

	public class DynamicPlaceholderPathElement {
		public string Name { get; private set; }
		public string ParentUniqueId { get; private set; }
		public string Seed { get; private set; }
		public bool IsDynamic { get; private set; }

		public DynamicPlaceholderPathElement(string placeholderName) {
			// /maincontent/tab-{95EF4582-265A-46E2-B35D-C20FF98A9366}-1234273131
			// have to split: tab-{95EF4582-265A-46E2-B35D-C20FF98A9366}-1234273131
			// - do it by -{ and }-!
			// Note: will seed always not be guid?
			var startGuid = placeholderName.IndexOf("-{");
			var endGuid = placeholderName.IndexOf("}-");
			if (startGuid > 0 && endGuid > 0) {
				IsDynamic = true;

				Name = placeholderName.Substring(0, startGuid);
				try {
					ParentUniqueId = Guid.ParseExact(placeholderName.Substring(startGuid + 1, 38), "B").ToString("D").ToLower(); //N=no dashes or curlies, B=dashes and curlies
				} catch (Exception) {
					// TODO: disconnect this item from ANY placeholder...
					//throw new Exception("Once you use DynamicPlaceholder, all children are required to use it.");
				}
				Seed = placeholderName.Substring(endGuid + 2);


			} else {
				IsDynamic = false;
				Name = placeholderName;

			}
		}
	}
	public class DynamicPlaceholderPath {

		private List<DynamicPlaceholderPathElement> _DynamicPlaceholderPathElements = new List<DynamicPlaceholderPathElement>();
		public ICollection<DynamicPlaceholderPathElement> DynamicPlaceholderPathElements {
			get { return new ReadOnlyCollection<DynamicPlaceholderPathElement>(_DynamicPlaceholderPathElements); }
		}
		public DynamicPlaceholderPath(string placeholderPath) {
			placeholderPath.Split("/".ToCharArray()).Where(path => !string.IsNullOrWhiteSpace(path)).ToList().ForEach(path => _DynamicPlaceholderPathElements.Add(new DynamicPlaceholderPathElement(path)));
		}

	}

}
