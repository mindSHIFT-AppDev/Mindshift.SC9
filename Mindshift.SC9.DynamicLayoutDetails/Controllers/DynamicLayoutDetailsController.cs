using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
using Mindshift.SC9.DynamicLayoutDetails.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Services.Infrastructure.Web.Http;

namespace Mindshift.SC9.DynamicLayoutDetails.Controllers {

	[RoutePrefix("mindshiftAPI/DynamicLayoutDetails")]
	public class DynamicLayoutDetailsController : ServicesApiController {
		[HttpOptions]
		[Route("*")]
		public HttpResponseMessage Options() {
			return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };


		}

		private List<DeviceItem> _Devices = null;
		private List<DeviceItem> Devices {
			get {
				if (_Devices == null) {
					var master = Sitecore.Data.Database.GetDatabase("master"); // master will always have the Default Device.
					_Devices = new List<DeviceItem>();
					// TODO: don't know a better way
					var deviceList = master.GetItem("/sitecore/layout/Devices").GetChildren();
					foreach (Item device in deviceList) {
						_Devices.Add((DeviceItem)device);

					}
				}
				return _Devices;
			}

		}
		//[HttpGet]
		//[Route("GetRenderings")]
		//// note: the dialog option is only because we're being lazy about what we pass along
		//public DynamicResponse GetRenderings() { //[FromUri]DynamicGetRequest request
		//	return new DynamicResponse("110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9".ToLower(), "web");
		//}
		/// <summary>
		/// This takes the Toolsfile.xml and adds our custom classes
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("GetRenderings")]
		// note: the dialog option is only because we're being lazy about what we pass along
		public DynamicResponse GetRenderings([FromUri]DynamicGetRequest request) { //[FromUri]DynamicGetRequest request
			var ret = new DynamicResponse(request.ItemId.Replace("{", "").Replace("}", "").ToLower(), request.Database);


			var language = LanguageManager.GetLanguage(request.Language);

			var db = Database.GetDatabase(request.Database);
			var item = db.GetItem(request.ItemId, language);

			ret.AddLayoutType("shared", "Shared");
			ret.AddLayoutType("final", "Final");


			foreach (var responseLayoutType in ret.LayoutTypes) {
				ID layoutFieldId = null;
				switch (responseLayoutType.Name) {
					case "final":
						layoutFieldId = Sitecore.FieldIDs.FinalLayoutField;
						break;
					case "shared":
						layoutFieldId = Sitecore.FieldIDs.LayoutField;
						break;
					default:
						layoutFieldId = Sitecore.FieldIDs.LayoutField; // shouldn't be needed, but just in case
						break;
				}

				// yes, there are two things holding the layout type - but that is because this one on the next line is for Response only
				//var responseLayoutType = ret.AddLayoutType(layoutTypeName);

				Sitecore.Data.Fields.LayoutField layoutField = item.Fields[layoutFieldId];
				var layoutDefinition = Sitecore.Layouts.LayoutDefinition.Parse(item[layoutFieldId]);

				int i = 0;
				foreach (var device in Devices) {
					i++;
					// this holds a flat dictionary of all renderings so we can place our parent/child relationships correctly
					var allRenderings = new Dictionary<string, ResponseRendering>();
					var allRenderingsReversed = new Dictionary<string, List<ResponseRendering>>();



					var responseDevice = responseLayoutType.AddDevice(item, layoutDefinition, device, layoutField.GetLayoutID(device));


					//Sitecore.Layouts.DeviceDefinition device =	 layout.Devices[i] as Sitecore.Layouts.DeviceDefinition; 

					// TODO: issue: we don't do this for the layout also!


					Sitecore.Layouts.RenderingReference[] renderings = layoutField.GetReferences(device);

					// if (renderings == null) return; // this item doesn't have any renderings under the Default device.

					//string layoutXml = layoutField.Value;

					//responseDevice.Xml = layoutField.Value;
					if (renderings != null) {

						// add akk if them as responseRenderings first, in case my parent is after my child!
						foreach (var rendering in renderings)
						{
							var responseRendering = new ResponseRendering(rendering, layoutDefinition, item);// responseDevice.AddRendering(rendering); // TODO: no response needed!
							allRenderings.Add(responseRendering.UniqueId, responseRendering); // always add it to the flat list!
						}
								
						foreach (var responseRendering in allRenderings.Values) {
								// add to the proper rendering. Two cases here, depending on which comes first.

							// TODO: this line only works on Dyanmic - can we check to see if it's dynamic first?
							// - or rather we need to "try again" to find the used rendering
							if (!string.IsNullOrEmpty(responseRendering.ParentUniqueId) && allRenderings.ContainsKey(responseRendering.ParentUniqueId)) {
								allRenderings[responseRendering.ParentUniqueId].AddRendering(item, layoutDefinition, responseRendering);
							} else if (string.IsNullOrEmpty(responseRendering.ParentUniqueId)) {
								var possiblePlaceholders = allRenderings.SelectMany(r => r.Value.Placeholders.ToList().Where(p => p.Name == responseRendering.PlaceholderName)).ToList();
								if (possiblePlaceholders.Count > 0) {
									// this means we have the placeholder, but it's missing the ParentUniqueId
									var effectivePlaceholder = possiblePlaceholders.Last(); // this is where Sitecore will put it anyway, the last match.
									if (effectivePlaceholder.Dynamic) {
										responseRendering.InvalidDynamicPlaceholder = true;
									}

									if (!effectivePlaceholder.Dynamic && possiblePlaceholders.Count > 1) {
										effectivePlaceholder.DynamicRecommended = true;
									}

									effectivePlaceholder.AddRendering(responseRendering);

									responseRendering.PossiblePlaceholderPaths.AddRange(possiblePlaceholders.Select(p => p.Name + "-" + p.ParentUniqueId));
								} else { // we just add it to the device if it's really not found. Due to rendering order OR it's really invalid
									responseDevice.AddRendering(item, layoutDefinition, responseRendering);
								}

							} else if (allRenderingsReversed.ContainsKey(responseRendering.UniqueId)) { // I am not sure what this was even for? It will never come here now!
								//responseRendering.RemoveRenderingRange(allRenderingsReversed);
								responseRendering.AddRenderingRange(item, layoutDefinition, allRenderingsReversed[responseRendering.UniqueId]); // could have collected a bunch by now
							}

							// TODO: but I need to remove it when this happens. So don't add the ones with parents, duh
							// - they DEFINITELY don't go in the maincontent then lol (at least, not right now)


							if (!string.IsNullOrEmpty(responseRendering.ParentUniqueId)) {
								if (!allRenderingsReversed.ContainsKey(responseRendering.ParentUniqueId)) {
									allRenderingsReversed.Add(responseRendering.ParentUniqueId, new List<ResponseRendering>());
								}
								allRenderingsReversed[responseRendering.ParentUniqueId].Add(responseRendering);
							}
						}

						// TODO: loop through and "arange"

					}
				}
			}

			return ret;


			//HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK)
			//{
			//	Content = new StringContent("")
			//};
			//result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml"); //application/octet-stream
			//return result;
		}

		

		// TODO: this is old - what needs to be done to make it work with the new stuff
		// - need to take a hard look at the XML
		// - all I'm saving now is the same, so that should be OK? maybe?
		[HttpPost]
		[Route("SaveRenderings")] ///{itemid}/{database} do these still need to be here?
		public DynamicResponse SaveRenderings(DynamicSaveRequest request) {

			var ret = GetRenderings(request);

			// TODO: compare (or keep a log of changes to send on the front end?)

			// this first iteration assumes the same structure with only updates.

			// TODO: SAVE it!
			// wait - we don't care about hierarchy! we only care about updating the placeholder!

			// start with the placeholer path and the datasource field...
			// which needs to be reverse mapped, maybe
			var language = LanguageManager.GetLanguage(request.Language);

			var db = Database.GetDatabase(request.Database);
			var item = db.GetItem(request.ItemId, language);


			foreach (var layoutType in request.LayoutTypes) {
				ID layoutFieldId = null;
				switch (layoutType.Name) {
					case "final":
						layoutFieldId = Sitecore.FieldIDs.FinalLayoutField;
						break;
					case "shared":
						layoutFieldId = Sitecore.FieldIDs.LayoutField;
						break;
					default:
						layoutFieldId = Sitecore.FieldIDs.LayoutField; // shouldn't be needed, but just in case
						break;
				}

				Sitecore.Data.Fields.LayoutField layoutField = item.Fields[layoutFieldId];
				var layoutDefinition = Sitecore.Layouts.LayoutDefinition.Parse(item[layoutFieldId]);

				string layoutXml = layoutField.Value;


				var xmlLayoutDoc = new XmlDocument();
				xmlLayoutDoc.LoadXml(layoutXml);

				foreach (var device in layoutType.Devices) {

					UpdateRendering(device.Placeholders, xmlLayoutDoc, db, device);

				}


				item.Editing.BeginEdit();
				layoutField.Value = xmlLayoutDoc.OuterXml;
				//var test = xmlLayoutDoc.OuterXml;
				item.Editing.EndEdit();


			}
			return GetRenderings(request);
		}


		private void UpdateRendering(List<RequestPlaceholder> requestPlaceholders, XmlDocument xmlLayoutDoc, Database db, RequestDevice device) {
			foreach (var requestPlaceholder in requestPlaceholders) {
				foreach (var requestRendering in requestPlaceholder.Renderings) {
					var renderingElement = xmlLayoutDoc.SelectSingleNode("//r[@uid='{" + requestRendering.UniqueId.ToUpper() + "}']") as XmlElement;

					if (renderingElement == null) { // this is new!
						var deviceElement = xmlLayoutDoc.SelectSingleNode("//d[@id='{" + device.Id.ToUpper() + "}']") as XmlElement;
						renderingElement = xmlLayoutDoc.CreateElement("r");
						renderingElement.SetAttribute("uid", "{" + requestRendering.UniqueId.ToUpper() + "}");
						renderingElement.SetAttribute("id", "{" + requestRendering.ItemId.ToUpper() + "}");
						deviceElement.AppendChild(renderingElement);
					} 

					// TODO: rebuild this path... from parents
					renderingElement.SetAttribute("ph", requestRendering.PlaceholderPath); // TODO: should I use requestPlaceholder and not assume the child has been updated? (it's a lot to keep synced)

					string ds = "";
					if (!string.IsNullOrWhiteSpace(requestRendering.DataSourcePath)) {
						var item = db.GetItem(requestRendering.DataSourcePath); // language not important, only life important?
						if (item != null) {
							ds = item.ID.ToString();
						}
					}

					renderingElement.SetAttribute("ds", ds);
					
					UpdateRendering(requestRendering.Placeholders, xmlLayoutDoc, db, device);
				}
			}
		}
	}
}
