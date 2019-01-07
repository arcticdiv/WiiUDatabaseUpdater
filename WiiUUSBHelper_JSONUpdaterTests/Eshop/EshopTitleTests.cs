using Microsoft.VisualStudio.TestTools.UnitTesting;
using WiiUUSBHelper_JSONUpdater.Eshop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace WiiUUSBHelper_JSONUpdater.Eshop.Tests
{
    [TestClass]
    public class EshopTitleTests
    {
        [TestMethod]
        public void VersionStringTest()
        {
            EshopTitle title = new EshopTitle();
            Assert.AreEqual("", title.VersionString);

            foreach (TitleID id in new TitleID[] { TitleIDTests.id3DSGames, TitleIDTests.idWiiUGames })
            {
                title = new EshopTitle();
                title.Version = 42;
                title.TitleId = id;
                Assert.AreEqual("", title.VersionString);
            }

            foreach (TitleID id in TitleIDTests.updates.Concat(TitleIDTests.dlcs))
            {
                title = new EshopTitle();
                title.Version = 42;
                title.TitleId = id;
                Assert.AreEqual("42", title.VersionString);
            }
        }

        [TestMethod]
        public void JsonTypeTest()
        {
            EshopTitle title = new EshopTitle();
            Assert.AreEqual(DatabaseJsonType.None, title.JsonType);

            // make sure that titleID updates are reflected in the json type (if previous type is '.None')
            title.TitleId = new TitleID("0004000EA1B2C3D4");
            Assert.AreEqual(DatabaseJsonType.Updates3DS, title.JsonType);

            // make sure that the json type doesn't change once it was set to a value other than '.None'
            //  (this enforces that existing titles are saved back into the same file they were read from)
            title.TitleId = new TitleID("0005000EA1B2C3D4");
            Assert.AreEqual(DatabaseJsonType.Updates3DS, title.JsonType);
        }


        [TestMethod]
        public void FromJSONTest()
        {
            string jsonString = "{\"EshopId\":\"20010000000026\",\"IconUrl\":\"https:\\/\\/icon.url\\/test.jpg\",\"Name\":\"TestTitle\\u00ae Wii U\",\"Platform\":124,\"ProductCode\":\"WAHJ\",\"Region\":\"JPN\",\"Size\":\"391053332\",\"TitleId\":\"0005000E10100D00\",\"PreLoad\":false,\"Version\":\"42\",\"DiscOnly\":false}";
            JObject jobj = JObject.Parse(jsonString);
            EshopTitle title = jobj.ToObject<EshopTitle>();

            Assert.AreEqual("20010000000026", title.EshopId);
            Assert.AreEqual("https://icon.url/test.jpg", title.IconUrl);
            Assert.AreEqual("TestTitle\u00ae Wii U", title.Name);
            Assert.AreEqual((int)124, title.Platform);
            Assert.AreEqual("WAHJ", title.ProductCode);

            Assert.AreEqual("JPN", title.RegionString);
            Assert.AreEqual(Region.JPN, title.Region);

            Assert.AreEqual("391053332", title.SizeString);
            Assert.AreEqual((ulong)391053332, title.Size);

            Assert.AreEqual("0005000E10100D00", title.TitleIdString);
            Assert.AreEqual(new TitleID("0005000E10100D00"), title.TitleId);

            Assert.AreEqual("42", title.VersionString);
            Assert.AreEqual((int)42, title.Version);
        }

        [TestMethod]
        public void ToJSONTest()
        {
            EshopTitle title = new EshopTitle();
            title.EshopId = "20010000000026";
            title.IconUrl = "https://icon.url/test.jpg";
            title.Name = "TestTitle\u00ae Wii U";
            title.Platform = 124;
            title.ProductCode = "WAHJ";
            title.Region = Region.JPN;
            title.Size = 391053332;
            title.TitleId = new TitleID("0005000E10100D00");
            title.Version = 42;

            JObject jobj = JObject.FromObject(title);
            JArray jarr = new JArray(jobj);
            string jsonString = DatabaseJsonIO.JsonArrayToString(jarr, Formatting.None);
            Assert.AreEqual("[{\"EshopId\":\"20010000000026\",\"IconUrl\":\"https:\\/\\/icon.url\\/test.jpg\",\"Name\":\"TestTitle\\u00ae Wii U\",\"Platform\":124,\"ProductCode\":\"WAHJ\",\"Region\":\"JPN\",\"Size\":\"391053332\",\"TitleId\":\"0005000E10100D00\",\"PreLoad\":false,\"Version\":\"42\",\"DiscOnly\":false}]", jsonString);
        }


        [TestMethod]
        public void AddDataFromXmlTest()
        {
            XElement xml;
            EshopTitle title;

            // check samurai data
            xml = XElement.Parse("<title id=\"1234\">" +
                "<product_code>CTR-P-WXYZ</product_code>" +
                "<name>TestTitle &lt;br&gt;Newline &lt;br&gt;Newline2</name>" +
                "<icon_url>https://icon.url/test.jpg</icon_url>" +
                "<platform id=\"124\" device=\"CTR\"><name>Nintendo 3DS (Card/Download)</name></platform>" +
                "</title>");
            title = new EshopTitle();
            title.AddDataFromXml(xml);
            Assert.AreEqual("1234", title.EshopId);
            Assert.AreEqual("WXYZ", title.ProductCode);
            Assert.AreEqual("TestTitle Newline Newline2", title.Name);
            Assert.AreEqual("https://icon.url/test.jpg", title.IconUrl);
            Assert.AreEqual((int)124, title.Platform);

            // check ninja data
            xml = XElement.Parse("<title_ec_info>" +
                "<title_id>00050000101C9500</title_id>" +
                "<content_size>10551265752</content_size>" +
                "<title_version>0</title_version>" +
                "<disable_download>false</disable_download>" +
                "</title_ec_info>");
            title = new EshopTitle();
            title.AddDataFromXml(xml);
            Assert.AreEqual("00050000101C9500", title.TitleIdString);
            Assert.AreEqual((ulong)10551265752, title.Size);
            Assert.AreEqual("", title.VersionString);
        }


        [TestMethod]
        public void EqualsTest()
        {
            string idGame1 = "00050000101C9500", id2 = "0004000EA1B2C3D4", id3 = "0004000EC3D4E5F6";
            EshopTitle title1, title2;

            // test game equality (make sure that the version is not included in the checks)
            title1 = new EshopTitle()
            {
                TitleIdString = idGame1
            };
            title2 = new EshopTitle()
            {
                TitleIdString = idGame1,
                Version = 42
            };
            Assert.AreEqual(title1, title2);

            // test other title equality
            title1 = new EshopTitle()
            {
                TitleIdString = id2,
                Version = 42
            };
            title2 = new EshopTitle()
            {
                TitleIdString = id2,
                Version = 42
            };
            Assert.AreEqual(title1, title2);

            // test inequality when titleIDs are different
            title1 = new EshopTitle()
            {
                TitleIdString = id2,
                Version = 42
            };
            title2 = new EshopTitle()
            {
                TitleIdString = id3,
                Version = 42
            };
            Assert.AreNotEqual(title1, title2);

            // test inequality when versions are different
            title1 = new EshopTitle()
            {
                TitleIdString = id2,
                Version = 24
            };
            title2 = new EshopTitle()
            {
                TitleIdString = id2,
                Version = 42
            };
            Assert.AreNotEqual(title1, title2);
        }

        [TestMethod]
        public void CompareToTest()
        {
            string id1 = "0001000100000059", id2 = "000100010000005A";
            EshopTitle titleI1V1 = new EshopTitle()
            {
                TitleIdString = id1,
                VersionString = "96"
            };
            EshopTitle titleI2V1 = new EshopTitle()
            {
                TitleIdString = id2,
                VersionString = "96"
            };
            EshopTitle titleI1V2 = new EshopTitle()
            {
                TitleIdString = id1,
                VersionString = "112"
            };
            EshopTitle titleI2V2 = new EshopTitle()
            {
                TitleIdString = id2,
                VersionString = "112"
            };

            Assert.IsTrue(titleI1V1.CompareTo(titleI1V1) == 0);

            // test titleID only
            Assert.IsTrue(titleI1V1.CompareTo(titleI2V1) < 0);
            Assert.IsTrue(titleI2V1.CompareTo(titleI1V1) > 0);

            // test version only
            Assert.IsTrue(titleI1V1.CompareTo(titleI1V2) < 0);
            Assert.IsTrue(titleI1V2.CompareTo(titleI1V1) > 0);

            // test sorting
            EshopTitle[] expected = { titleI1V1, titleI1V2, titleI2V1, titleI2V2 };
            EshopTitle[] arr = { titleI2V1, titleI2V2, titleI1V2, titleI1V1 };
            Array.Sort(arr);
            CollectionAssert.AreEqual(expected, arr);
        }
    }
}