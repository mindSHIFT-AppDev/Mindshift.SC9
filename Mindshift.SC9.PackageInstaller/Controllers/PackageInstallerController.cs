using Mindshift.SC9.PackageInstaller.Models;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Install;
using Sitecore.Install.Files;
using Sitecore.Install.Framework;
using Sitecore.Install.Items;
using Sitecore.Install.Utils;
using Sitecore.Publishing;
using Sitecore.SecurityModel;
using Sitecore.Services.Infrastructure.Web.Http;
using Sitecore.Web.UI.HtmlControls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;

namespace Mindshift.SC9.PackageInstaller.Controllers
{

    [RoutePrefix("mindshiftAPI")]
    public class PackageInstallerController : ServicesApiController
    {
        [HttpOptions]
        [Route("*")]
        public HttpResponseMessage Options()
        {
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };


        }

        /// <summary>
        /// Install a Package
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("InstallPackage")]
        // note: the dialog option is only because we're being lazy about what we pass along
        public InstallPackageResult InstallPackage(string packageName = null, bool publish = false)
        { //[FromUri]DynamicGetRequest request
            var ret = new InstallPackageResult();
            var installLog = "";

            //TODO: Look into the cloning mechanism fo SC 9
            //new ForceCloneAccept().RegisterEvent(false);
            //new SitemapFieldValues().RegisterEvent(false);

            //
            string packagePath = string.Concat(Sitecore.Configuration.Settings.DataFolder, "\\packages\\", packageName);

            //string packagePath = @"C:\_Projects\HenrySchein.CMS.Web\data\packages\Unnamed Package.zip";
            //TODO: figure out the return codes, and how to return them i.e throw HTTP error codes, or anything else
            if (packageName == null)
            {
                ret.Message = "ERROR: url param not found";
                ret.Successful = false;
                return ret;
            }

            if (string.IsNullOrWhiteSpace(packageName))
            {
                ret.Message = "ERROR: url param is empty";
                ret.Successful = false;
                return ret;
            }

            // TODO: huh?

            if (!File.Exists(packagePath))
            {
                ret.Message = "ERROR: file not found";
                ret.Successful = false;
                return ret;
            }

            //Sitecore.Configuration.Settings.Indexing.Enabled = false;
            try
            {
                Sitecore.Context.SetActiveSite("shell");

                using (new SecurityDisabler())
                {

                    //Proxy disabler has been deprecated in SC 9
                    //using (new ProxyDisabler())
                    //{
                    using (new SyncOperationContext())
                    {
                        var context = new SimpleProcessingContext();
                        var itemInstallerEvents = new DefaultItemInstallerEvents(new BehaviourOptions(InstallMode.Merge, MergeMode.Merge));
                        context.AddAspect(itemInstallerEvents);

                        var fileInstallerEvents = new DefaultFileInstallerEvents(true);
                        context.AddAspect(fileInstallerEvents);
                        new Sitecore.Install.Installer().InstallPackage(MainUtil.MapPath(packagePath), context);
                    }
                    //}
                }


                if (publish)
                {

                    List<Item> items = GetItemsFromPackage(packagePath, installLog);

                    /*
                    var items = new List<Item>();

                    // Get items from package
                    PackageProject packageProject = PackageGenerator.NewProjectFromPackage(packagePath);
                    var sources = from n in packageProject.Sources where n is ItemSource select n;
                    foreach (ItemSource source in sources)
                    {
                        if (source != null)
                        {

                            foreach (var entry in source.Name)
                            {
                                //ItemReference reference = ItemReference.Parse(entry);
                                //Item item = reference.GetItem();
                                //items.Add(item);
                            }
                    
                        }
                    }
                    */

                    // TODO: make this confurable?
                    string[] targets = new string[] { "web" };
                    foreach (string target in targets)
                    {
                        var targetDatabase = Sitecore.Configuration.Factory.GetDatabase(target);

                        installLog += "INSTALL PUBLISH to: " + targetDatabase.Name + "<br>";

                        // Publish items
                        foreach (var item in items)
                        {
                            installLog += "INSTALL PUBLISHING: " + item.Paths.FullPath + "<br>";
                            PublishManager.PublishItem(item, new[] { targetDatabase }, new[] { item.Language }, true, false);
                        }
                    }
                }
                /*
                string packageFile = @"C:\_Projects\HenryScheinSitecore\Development\Developers\Mickey\HenrySchein.CMS.Web\data\packages\test package.zip";
                TaskMonitor Monitor = new TaskMonitor();
                Monitor.Start(new ThreadStart(new AsyncHelper(packageFile, “”).Install));
                */

            }
            catch (Exception ex)
            {
                //new ForceCloneAccept().RegisterEvent(true);
                //new SitemapFieldValues().RegisterEvent(true);
                //Sitecore.Configuration.Settings.Indexing.Enabled = true;
                ret.Message = "ERROR: " + ex.Message + "<br>" + installLog;
                ret.StackTrace = ex.StackTrace;
                ret.Successful = false;

                return ret;
            }

            //Sitecore.Configuration.Settings.Indexing.Enabled = true;
            ret.Message = "SUCCESS";
            ret.Successful = true;
            return ret;

            //new ForceCloneAccept().RegisterEvent(true);
            //new SitemapFieldValues().RegisterEvent(true);
        }

        public List<Item> GetItemsFromPackage(string packagePath, string installLog)
        {
            var sources = GetSources(packagePath);

            var listview = GetListview(sources);

            installLog += "listview.Item.Count: " + ((Sitecore.Web.UI.HtmlControls.ListviewItem[])listview.Items).Length.ToString() + "<br>";

            var items = new List<Item>();
            foreach (ListviewItem listviewItem in listview.Items)
            {
                installLog += "installing: " + listviewItem.Header + "<br>";
                var item = GetItem(listviewItem.Header);
                if (item != null)
                {
                    installLog += "adding: " + item.Paths.FullPath + "<br>";
                    items.Add(item);
                }
            }

            return items;
        }

        public Item GetItem(string header)
        {
            try
            {
                var part1 = header.Split(new[] { '{' });
                var part2 = ((part1[1].Split(new[] { '}' }))[1]).Split(new[] { '/' });
                var id = (part1[1].Split(new[] { '}' }))[0];
                var language = part2[1];
                var version = part2[2];

                var master = Sitecore.Configuration.Factory.GetDatabase("master"); // TODO: make name configurable

                return master.GetItem(new ID(id), LanguageManager.GetLanguage(language));
                //return ItemManager.GetItem(new ID(id), LanguageManager.GetLanguage(language), null, master);
            }
            catch
            {
                return null;
            }
        }

        public Listview GetListview(List<ISource<PackageEntry>> sources)
        {
            var listview = new Listview();

            foreach (ISource<PackageEntry> source in sources)
            {

                var sink = new SinkHelper(listview, string.Empty);
                sink.Initialize(Installer.CreatePreviewContext());
                new EntrySorter(source).Populate(sink);
                if (sink.Count == 0)
                {
                    sink.AddItem(Translate.Text("There are no entries that match search criteria or the source is empty."));
                }

            }
            return listview;
        }

        public List<ISource<PackageEntry>> GetSources(string packagePath)
        {
            var sources = PackageGenerator.NewProjectFromPackage(packagePath).Sources;
            var count = sources.Count;
            var sourceList = new List<ISource<PackageEntry>>();
            for (var i = 0; i < count; i++)
            {
                if (sources[i] != null) sourceList.Add(sources[i]);
            }

            return sourceList;
        }
    }

    public class SinkHelper : BaseSink<PackageEntry>
    {
        private readonly string _filter;
        private readonly Listview _view;

        public SinkHelper(Listview view, string filter)
        {
            _view = view;
            _filter = filter;
        }

        public ListviewItem AddItem(string header)
        {
            var control = new ListviewItem { ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("I") };
            _view.Controls.Add(control);
            control.Header = header;
            return control;
        }

        private string FormatInstallOptions(BehaviourOptions options)
        {
            if (!options.IsDefined)
            {
                return Translate.Text("Undefined");
            }
            InstallMode itemMode = options.ItemMode;
            if (itemMode == InstallMode.Undefined)
            {
                return Translate.Text("Ask User");
            }
            var builder = new StringBuilder(50);
            builder.Append(Translate.Text(itemMode.ToString()));
            if (itemMode == InstallMode.Merge)
            {
                builder.Append(" / ");
                builder.Append(Translate.Text(options.ItemMergeMode.ToString()));
            }
            return builder.ToString();
        }

        public override void Put(PackageEntry entry)
        {
            if (entry.Key.IndexOf(_filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                string str;
                ListviewItem item = AddItem(entry.Key);
                item.ColumnValues["key"] = entry.Key;
                item.ColumnValues["source"] = PackageUtils.TryGetValue(entry.Properties, "source");
                item.ColumnValues["options"] = FormatInstallOptions(new BehaviourOptions(entry.Properties, Sitecore.Install.Constants.IDCollisionPrefix));
                if (entry.Attributes.TryGetValue("icon", out str))
                {
                    item.Icon = str;
                }
                Count++;
            }
        }

        public int Count { get; private set; }
    }
}

