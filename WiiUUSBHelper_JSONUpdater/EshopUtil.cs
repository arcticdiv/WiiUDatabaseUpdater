using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WiiUUSBHelper_JSONUpdater.Eshop;

namespace WiiUUSBHelper_JSONUpdater
{
    class EshopUtil : IDisposable
    {
        private const int TitleRequestLimit = 200;

        private const string TMDUrl = "http://ccs.cdn.c.shop.nintendowifi.net/";
        private const string TMDGamePath = "ccs/download/{0}/tmd";
        private const string TMDUpdatePath = "ccs/download/{0}/tmd.{1}";

        private const string SamuraiUrl = "https://samurai.ctr.shop.nintendo.net/";
        private const string SamuraiPath = "samurai/ws/{0}/titles?shop_id={1}&sort=new&limit={2}&offset={3}";

        private const string NinjaUrl = "https://ninja.ctr.shop.nintendo.net/";
        private const string NinjaPath = "ninja/ws/{0}/title/{1}/ec_info";

        private const string TagayaUrl = "https://tagaya.wup.shop.nintendo.net/";
        private const string TagayaPathLatestVersion = "tagaya/versionlist/EUR/EU/latest_version"; // region does not matter, this server ignores it
        private const string TagayaPathVersionList = "tagaya/versionlist/EUR/EU/list/{0}.versionlist";

        private const string Tagaya3DSUrl = "https://tagaya-ctr.cdn.nintendo.net/";
        private const string Tagaya3DSPath = "tagaya/versionlist";

        private ProgressManager progressManager;
        private readonly WebClient webClient;
        private readonly CertificateWebClient certWebClient;
        private readonly TitleDatabase titleDatabase;

        public int NewestWiiUUpdateListVersion { get; private set;  } = -1;

        public EshopUtil(TitleDatabase db, string sslKeyBagPath, string keyBagPassword) : this(db, new X509Certificate2(sslKeyBagPath, keyBagPassword)) { }

        public EshopUtil(TitleDatabase db, X509Certificate2 certificate)
        {
            titleDatabase = db;
            webClient = new WebClient();
            certWebClient = new CertificateWebClient(certificate);
            // client setup
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; }; // TODO
            SetupWebClient(webClient);
            SetupWebClient(certWebClient);
        }

        public void Dispose()
        {
            webClient.Dispose();
            certWebClient.Dispose();
        }
        
        public void SetProgressManager(ProgressManager progressManager)
        {
            this.progressManager = progressManager;
        }
        
        private static void SetupWebClient(WebClient client)
        {
            client.Headers.Add("User-Agent", "WiiU/PBOS-1.1");
            client.Encoding = Encoding.UTF8;
        }

        
        /// <summary>
        /// Retrieves a list of all titles from all available regions.
        /// </summary>
        /// <param name="shopID">Use 1 for the 3DS eShop, 2 for the WiiU eShop.</param>
        /// <returns>A list of titles</returns>
        public async Task<List<EshopTitle>> GetAllTitles(int shopID)
        {
            // get title counts for all regions
            Console.WriteLine("Getting title count for {0} regions ...", Enum.GetValues(typeof(Region)).Length - 2);
            Dictionary<Region, int> titleCounts = new Dictionary<Region, int>();
            foreach (Region region in Enum.GetValues(typeof(Region)))
            {
                if (region == Region.None || region == Region.ALL)
                    continue;
                await Retry(async () => {
                    titleCounts.Add(region, await Samurai.GetTitleCountForRegion(webClient, region, shopID));
                });
            }

            int totalTitleCount = titleCounts.Values.Sum();
            Console.WriteLine("Downloading metadata for {0} titles ...", totalTitleCount);
            progressManager.SetTitle(string.Format("Downloading metadata for {0} titles ...", totalTitleCount));
            progressManager.Reset(totalTitleCount);

            DateTime currentDate = DateTime.Today;
            List<EshopTitle> titleList = new List<EshopTitle>();

            // loop through regions
            foreach (KeyValuePair<Region, int> pair in titleCounts)
            {
                Region region = pair.Key;
                int titleCount = pair.Value;

                // get titles from samurai
                for (int offset = 0; offset < titleCount; offset += TitleRequestLimit)
                {
                    XDocument titlesXml = null;
                    await Retry(async () => {
                        titlesXml = await Samurai.GetTitlesXmlForRegion(webClient, region, shopID, TitleRequestLimit, offset);
                    });

                    /*  structure:
                     *  <eshop><contents ...>
                     *      <content index=1><title ...>[title info]</title></content>
                     *      <content index=2><title ...>[title info]</title></content>
                     *  </contents></eshop>
                     */
                    XElement contentsElement = titlesXml.Root.Element("contents");

                    // iterate over titles in xml
                    foreach (XElement titleElement in contentsElement.Elements().Select(e => e.Element("title")))
                    {
                        // check release date
                        string releaseDateString = titleElement.Element("release_date_on_eshop")?.Value;
                        if (!String.IsNullOrEmpty(releaseDateString))
                        {
                            DateTime releaseDate;
                            if (!DateTime.TryParseExact(releaseDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out releaseDate))
                                continue;
                            if (releaseDate > currentDate)
                                continue;
                        }

                        // create title and add data
                        EshopTitle title = new EshopTitle();
                        title.Region = region;
                        // set title fields from xml (incomplete, some fields must be retrieved from ninja)
                        title.AddDataFromXml(titleElement);

                        progressManager.Step(string.Format("{0}-{1}", region.ToCountryCode(), title.EshopId));

                        // some exceptions:
                        if (title.Platform == 63 || title.Platform == 1003) // ignore 3DS software updates
                            continue;
                        if (title.Platform == 143) // ignore some unknown platform with just 1 title
                            continue;

                        // get remaining data from ninja
                        XElement titleECInfoElement;
                        await Retry(async () => {
                            titleECInfoElement = await Ninja.GetECInfoForRegionAndTitleID(certWebClient, region, title.EshopId);
                            title.AddDataFromXml(titleECInfoElement);
                            if (title.JsonType != DatabaseJsonType.None)
                                titleList.Add(title);
                        });
                    }
                }
            }

            // merge titles that occur in all regions into one title with the 'ALL' region
            IEnumerable<EshopTitle> titleListEnumerable = titleList;
            var allRegions = Enum.GetValues(typeof(Region)).OfType<Region>().Where(r => r != Region.None && r != Region.ALL);
            var groups = titleListEnumerable.GroupBy(t => t.TitleId).Select(g => g.ToList()).ToList();
            foreach (var group in groups)
            {
                var groupRegions = group.Select(t => t.Region);
                if (allRegions.All(groupRegions.Contains))
                {
                    titleListEnumerable = titleListEnumerable.Except(group.Skip(1));
                    group[0].Region = Region.ALL;
                }
            }

            titleList = titleListEnumerable.ToList();

            return titleList;
        }
        
        /// <summary>
        /// Retrieves a list of all WiiU updates
        /// </summary>
        /// <param name="currentListVersion">The update list version to start from. Use 1 to download everything.</param>
        /// <returns>A list of update titles</returns>
        public async Task<List<EshopTitle>> GetAllWiiUUpdates(int currentListVersion = 1)
        {
            if (currentListVersion < 1)
            {
                currentListVersion = 1;
            }

            Console.Write("Getting latest update list version ...");
            int listVersion = -1;
            await Retry(async () => {
                listVersion = await Tagaya.GetLatestListVersion(webClient);
            });

            NewestWiiUUpdateListVersion = listVersion;
            Console.WriteLine(" {0}.", listVersion);

            progressManager.Reset(listVersion-currentListVersion);
            Console.WriteLine("Downloading {0} update lists ...", listVersion - currentListVersion);
            progressManager.SetTitle(string.Format("Downloading {0} update lists ...", listVersion - currentListVersion));

            HashSet<EshopTitle> updateSet = new HashSet<EshopTitle>();

            // download all update lists
            for (int i = currentListVersion + 1; i <= listVersion; i++)
            {
                progressManager.Step("Downloading WiiU update list ...");

                /*  structure:
                 *  <version_list ...><titles>
                 *      <title><id>[titleID]</id><version>[updateVersion]</version></title>
                 *      ...
                 *  </titles></version_list>
                 */

                XDocument updatesXml = null;
                bool success = await Retry(async () =>
                {
                    updatesXml = await Tagaya.GetUpdatesXmlForListVersion(webClient, i);
                },
                shouldStopRetrying: (e) =>
                {
                    // for some list versions a 403 error is received, ignore
                    return e is WebException we && we.Response is HttpWebResponse resp && resp.StatusCode == HttpStatusCode.Forbidden;
                });
                if (!success)
                    continue;

                // iterate over update version in xml
                XElement titlesElement = updatesXml.Root.Element("titles");
                foreach (XElement updateElement in titlesElement.Elements())
                {
                    EshopTitle update = new EshopTitle();
                    update.TitleId = new TitleID(updateElement.Element("id").Value);
                    update.VersionString = updateElement.Element("version").Value;
                    update.JsonType = DatabaseJsonType.Updates;

                    updateSet.Add(update);
                }
            }

            await AddSizesToTitles(updateSet);

            return updateSet.ToList();
        }

        /// <summary>
        /// Retrieves a list of all 3DS updates
        /// </summary>
        /// <returns>A list of update titles</returns>
        /// <exception cref="InvalidDataException">Thrown if response from server is invalid</exception>
        public async Task<List<EshopTitle>> GetAll3DSUpdates()
        {
            Console.WriteLine("Downloading 3DS update list ...");
            progressManager.SetTitle("Downloading 3DS update list ...");

            byte[] versionListData = null;
            await Retry(async () => {
                versionListData = await Tagaya3DS.GetVersionListData(webClient);
            });

            // see: http://3dbrew.org/wiki/Home_Menu#VersionList
            // first byte should always be 1
            if (versionListData[0] != 0x01)
                throw new InvalidDataException("3DS versionlist response is invalid.");

            List<EshopTitle> updateList = new List<EshopTitle>();

            using (MemoryStream memoryStream = new MemoryStream(versionListData))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                // go to start of version entries
                memoryStream.Seek(0x10, SeekOrigin.Begin);

                while (memoryStream.Position != memoryStream.Length)
                {
                    EshopTitle title = new EshopTitle();
                    title.TitleId = new TitleID(binaryReader.ReadUInt64().ToString("X16"));
                    title.VersionString = binaryReader.ReadUInt32().ToString();

                    if (title.JsonType != DatabaseJsonType.Games3DS && title.JsonType != DatabaseJsonType.None) // for some reason there are a few games in the versionlist, skip them
                        updateList.Add(title);

                    binaryReader.ReadBytes(4); // skip 4 bytes, unknown data
                }
            }

            await AddSizesToTitles(updateList);

            return updateList;
        }

        /// <summary>
        /// Retrieves a list of DLCs for the specified titles
        /// </summary>
        /// <param name="titles">The titles for which DLC info is downloaded for</param>
        /// <returns>A list of DLC titles</returns>
        public async Task<List<EshopTitle>> GetAllDLCsForTitles(ICollection<EshopTitle> titles)
        {
            int gameCount = titles.Count(t => (t.JsonType == DatabaseJsonType.Games || t.JsonType == DatabaseJsonType.Games3DS) && t.IsNativeTitle);
            progressManager.Reset(gameCount);
            Console.WriteLine("Downloading DLC info for {0} titles ...", gameCount);
            progressManager.SetTitle(string.Format("Downloading DLC info for {0} titles ...", gameCount));

            List<EshopTitle> dlcList = new List<EshopTitle>();

            foreach (EshopTitle title in titles.Where(t => (t.JsonType == DatabaseJsonType.Games || t.JsonType == DatabaseJsonType.Games3DS) && t.IsNativeTitle))
            {
                progressManager.Step("Downloading DLC info ...");

                EshopTitle dlc = new EshopTitle();
                dlc.TitleId = title.TitleId.DLCID;

                await Retry(async () =>
                {
                    dlc.Size = await GetContentSizeForTitle(dlc);
                    dlcList.Add(dlc); // if no exception is thrown, the title does have a DLC
                },
                shouldStopRetrying: (e) =>
                {
                    // 404 -> no DLC
                    return e is WebException we && we.Response is HttpWebResponse resp && resp.StatusCode == HttpStatusCode.NotFound;
                });
            }
            return dlcList;
        }

        /// <summary>
        /// Adds the size to each title in the collection that doesn't already have a size > 0
        /// </summary>
        /// <param name="titles">Collection of titles to add sizes to</param>
        public async Task AddSizesToTitles(ICollection<EshopTitle> titles)
        {
            // only get size for titles that do not already have a size
            int calcsRequired = titles.Count(t => t.Size == 0);
            Console.WriteLine("Getting title sizes for {0} titles ...", calcsRequired);
            progressManager.SetTitle(string.Format("Getting title sizes for {0} titles ...", calcsRequired));
            progressManager.Reset(calcsRequired);

            foreach (EshopTitle title in titles.Where(t => t.Size == 0))
            {
                progressManager.Step(title.TitleId.ToString());

                await Retry(async () =>
                {
                    title.Size = await GetContentSizeForTitle(title);
                },
                shouldStopRetrying: (e) =>
                {
                    return e is WebException we && we.Response is HttpWebResponse resp && resp.StatusCode == HttpStatusCode.NotFound;
                });
            }
        }

        /// <summary>
        /// Downloads the tmd for the title and calculates the size from the tmd's contents
        /// </summary>
        /// <param name="title">Title for which the size is calculated</param>
        /// <returns>The size of the given title</returns>
        /// <exception cref="InvalidDataException">Thrown if response from server is invalid</exception>
        public async Task<ulong> GetContentSizeForTitle(EshopTitle title)
        {
            if (title.JsonType == DatabaseJsonType.None)
                return 0;

            bool isUpdate = title.TitleId.IsUpdate;
            string tmdPath = isUpdate
                ? String.Format(TMDUpdatePath, title.TitleId, title.VersionString)
                : String.Format(TMDGamePath, title.TitleId);
            byte[] tmd = await webClient.DownloadDataTaskAsync(TMDUrl + tmdPath);

            // see: https://3dbrew.org/wiki/Title_metadata
            // sanity checks
            if (!(new TitleID(BitConverter.ToUInt64(tmd, 0x18c).SwapEndianness().ToString("X16")).Equals(title.TitleId)))
                throw new InvalidDataException("TMD's titleID does not match");
            if (isUpdate && BitConverter.ToUInt16(tmd, 0x1dc).SwapEndianness().ToString() != title.VersionString)
                throw new InvalidDataException("TMD's version does not match");

            int contentCount = BitConverter.ToUInt16(tmd, 0x1de).SwapEndianness();
            ulong totalSize = 0;

            using (MemoryStream memoryStream = new MemoryStream(tmd))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                // go to start of content entries
                memoryStream.Seek(0xb04, SeekOrigin.Begin);

                for (int i = 0; i < contentCount; i++)
                {
                    binaryReader.ReadBytes(0x8); // skip contentID, index, type
                    totalSize += binaryReader.ReadUInt64().SwapEndianness();
                    binaryReader.ReadBytes(0x20); // sha256
                }
            }

            return totalSize;
        }

        /// <summary>
        /// Retries the specified action multiple times (until the action doesn't throw an exception), and waits inbetween tries.
        /// </summary>
        /// <param name="action">The action to execute until it succeeds</param>
        /// <param name="retryWaitMs">The number of milliseconds inbetween tries</param>
        /// <param name="retries">The number of times the action will be retried if unsuccessful</param>
        /// <param name="writeToConsole">Specifies if each retry should be printed to the console</param>
        /// <param name="shouldStopRetrying">A function deciding if further retries should occur (return true if the retries should stop immediately, or false if the action should be retried again)</param>
        /// <returns>True if the action succeeded, or false if <paramref name="shouldStopRetrying"/> returned true after the action was unsuccessful</returns>
        internal static async Task<bool> Retry(Func<Task> action, int retryWaitMs = 5000, int retries = 3, bool writeToConsole = true, Func<Exception, bool> shouldStopRetrying = null)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    await action();
                    return true;
                }
                catch (Exception e)
                {
                    if (shouldStopRetrying != null && shouldStopRetrying(e))
                        return false;

                    if (retryCount == retries)
                        throw;
                    if (writeToConsole)
                        Console.WriteLine($"> Retrying in {retryWaitMs} ms. ({retryCount+1}/{retries})");
                    retryCount++;
                    await Task.Delay(retryWaitMs);
                }
            }
        }


        private class Samurai
        {
            public static async Task<int> GetTitleCountForRegion(WebClient client, Region region, int shopID)
            {
                XDocument xml = await GetTitlesXmlForRegion(client, region, shopID, 1, 0);
                return Int32.Parse(xml.Descendants("contents").First().Attribute("total").Value);
            }

            public static async Task<XDocument> GetTitlesXmlForRegion(WebClient client, Region region, int shopID, int limit, int offset)
            {
                string xmlString = await client.DownloadStringTaskAsync(SamuraiUrl + String.Format(SamuraiPath, region.ToCountryCode(), shopID, limit, offset));
                return XDocument.Parse(xmlString);
            }
        }

        private class Ninja
        {
            public static async Task<XElement> GetECInfoForRegionAndTitleID(CertificateWebClient client, Region region, string eshopID)
            {
                string xmlString = await client.DownloadStringTaskAsync(NinjaUrl + String.Format(NinjaPath, region.ToCountryCode(), eshopID));
                return XDocument.Parse(xmlString).Root.Element("title_ec_info");
            }
        }

        private class Tagaya
        {
            public static async Task<int> GetLatestListVersion(WebClient client)
            {
                string xmlString = await client.DownloadStringTaskAsync(TagayaUrl + TagayaPathLatestVersion);
                return Int32.Parse(XDocument.Parse(xmlString).Root.Element("version").Value);
            }

            public static async Task<XDocument> GetUpdatesXmlForListVersion(WebClient client, int listVersion)
            {
                string xmlString = await client.DownloadStringTaskAsync(TagayaUrl + String.Format(TagayaPathVersionList, listVersion));
                return XDocument.Parse(xmlString);
            }
        }

        private class Tagaya3DS
        {
            public static async Task<byte[]> GetVersionListData(WebClient client)
            {
                return await client.DownloadDataTaskAsync(Tagaya3DSUrl + Tagaya3DSPath);
            }
        }
    }
}
