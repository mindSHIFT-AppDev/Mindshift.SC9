using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Mindshift.SC9.DatasourceContentTab.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Services.Infrastructure.Web.Http;

namespace Mindshift.SC9.DatasourceContentTab.Controllers {

	[RoutePrefix("mindshiftAPI/DatasourceContentTab")]
	public class DatasourceContentTabController : ServicesApiController {
		[HttpOptions]
		[Route("*")]
		public HttpResponseMessage Options() {
			return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };


		}

		private List<DeviceItem> _devices = null;
		private List<DeviceItem> devices {
			get {
				if (_devices == null) {
					var master = Sitecore.Data.Database.GetDatabase("master"); // master will always have the Default Device.
					_devices = new List<DeviceItem>();
					// TODO: don't know a better way
					var deviceList = master.GetItem("/sitecore/layout/Devices").GetChildren();
					foreach (Item device in deviceList) {
						_devices.Add((DeviceItem)device);

					}
				}
				return _devices;
			}

		}

		/// <summary>
		/// This takes the Toolsfile.xml and adds our custom classes
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("GetRenderings")]
		public DynamicResponse GetRenderings(string id, string language, int version, string database) { //[FromUri]DynamicGetRequest request
			var ret = new DynamicResponse(id.Replace("{", "").Replace("}", "").ToLower(), database);

			var db = Sitecore.Data.Database.GetDatabase(database);
			var item = db.GetItem(id);


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
				foreach (var device in devices) {
					i++;
					// this holds a flat dictionary of all renderings so we can place our parent/child relationships correctly
					var allRenderings = new Dictionary<string, ResponseRendering>();
					var allRenderingsReversed = new Dictionary<string, List<ResponseRendering>>();



					var responseDevice = responseLayoutType.AddDevice(item, layoutDefinition, device, layoutField.GetLayoutID(device));


					//Sitecore.Layouts.DeviceDefinition device =	 layout.Devices[i] as Sitecore.Layouts.DeviceDefinition; 


					Sitecore.Layouts.RenderingReference[] renderings = layoutField.GetReferences(device);

					// if (renderings == null) return; // this item doesn't have any renderings under the Default device.

					//string layoutXml = layoutField.Value;

					//responseDevice.Xml = layoutField.Value;
					if (renderings != null) {
						// TODO: NEST! that's the WHOLE THING!
						foreach (var rendering in renderings) {
							var responseRendering = new ResponseRendering(rendering, layoutDefinition, item);// responseDevice.AddRendering(rendering); // TODO: no response needed!
																																															 // add to the proper rendering. Two cases here, depending on which comes first.
							if (!string.IsNullOrEmpty(responseRendering.ParentUniqueId) && allRenderings.ContainsKey(responseRendering.ParentUniqueId)) {
								allRenderings[responseRendering.ParentUniqueId].AddRendering(item, layoutDefinition, responseRendering);
							} else if (allRenderingsReversed.ContainsKey(responseRendering.UniqueId)) {
								//responseRendering.RemoveRenderingRange(allRenderingsReversed);
								responseRendering.AddRenderingRange(item, layoutDefinition, allRenderingsReversed[responseRendering.UniqueId]); // could have collected a bunch by now
							}

							if (string.IsNullOrEmpty(responseRendering.ParentUniqueId)) { // this means the path wasn't built with a GUID, so I couldn't find it. TODO: Fix it anyway!
								responseDevice.AddRendering(item, layoutDefinition, responseRendering);
							}

							// TODO: but I need to remove it when this happens. So don't add the ones with parents, duh
							// - they DEFINITELY don't go in the maincontent then lol (at least, not right now)

							allRenderings.Add(responseRendering.UniqueId, responseRendering); // always add it to the flat list!

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


	}
}
