using Microsoft.VisualStudio.TestTools.UnitTesting;
using WiiUUSBHelper_JSONUpdater.Eshop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiUUSBHelper_JSONUpdater.Eshop.Tests
{
    [TestClass]
    public class TitleIDTests
    {
        internal static readonly TitleID idWiiGames     = new TitleID("00010001A1B2C3D4");
        internal static readonly TitleID idWiiDLCs      = new TitleID("00010005A1B2C3D4");

        internal static readonly TitleID id3DSGames     = new TitleID("00040000A1B2C3D4");
        internal static readonly TitleID id3DSDemos     = new TitleID("00040002A1B2C3D4");
        internal static readonly TitleID id3DSUpdates   = new TitleID("0004000EA1B2C3D4");
        internal static readonly TitleID id3DSDLCs      = new TitleID("0004008CA1B2C3D4");
        internal static readonly TitleID id3DSDSiWare   = new TitleID("00048004A1B2C3D4");

        internal static readonly TitleID idWiiUGames    = new TitleID("00050000A1B2C3D4");
        internal static readonly TitleID idWiiUDemo     = new TitleID("00050002A1B2C3D4");
        internal static readonly TitleID idWiiUDLCs     = new TitleID("0005000CA1B2C3D4");
        internal static readonly TitleID idWiiUUpdates  = new TitleID("0005000EA1B2C3D4");

        internal static TitleID[] games => new TitleID[] { idWiiGames, id3DSGames, idWiiUGames, id3DSDSiWare };
        internal static TitleID[] updates => new TitleID[] { id3DSUpdates, idWiiUUpdates };
        internal static TitleID[] dlcs => new TitleID[] { id3DSDLCs, idWiiUDLCs };

        [TestMethod]
        public void ConstructorInvalidTest()
        {
            // test length
            Assert.ThrowsException<ArgumentException>(() => new TitleID("0123456789ABCD"));

            // test invalid platform
            Assert.ThrowsException<ArgumentException>(() => new TitleID("0002000000000000"));

            // test invalid wii category
            Assert.ThrowsException<ArgumentException>(() => new TitleID("0001123400000000"));

            // test invalid 3ds category
            Assert.ThrowsException<ArgumentException>(() => new TitleID("0004123400000000"));

            // test invalid wiiu category
            Assert.ThrowsException<ArgumentException>(() => new TitleID("0005123400000000"));
        }

        [TestMethod]
        public void ConstructorCaseInsensitiveTest()
        {
            Assert.AreEqual(new TitleID("0004008CA1B2C3D4"), new TitleID("0004008ca1b2c3d4"));
        }


        [TestMethod]
        public void JsonTypeTest()
        {
            Assert.AreEqual(DatabaseJsonType.GamesWii, idWiiGames.JsonType);
            Assert.AreEqual(DatabaseJsonType.GamesWii, idWiiDLCs.JsonType);

            Assert.AreEqual(DatabaseJsonType.Games3DS, id3DSGames.JsonType);
            Assert.AreEqual(DatabaseJsonType.None, id3DSDemos.JsonType);
            Assert.AreEqual(DatabaseJsonType.Updates3DS, id3DSUpdates.JsonType);
            Assert.AreEqual(DatabaseJsonType.Dlcs3DS, id3DSDLCs.JsonType);
            Assert.AreEqual(DatabaseJsonType.Games3DS, id3DSDSiWare.JsonType);

            Assert.AreEqual(DatabaseJsonType.Games, idWiiUGames.JsonType);
            Assert.AreEqual(DatabaseJsonType.Updates, idWiiUDemo.JsonType);
            Assert.AreEqual(DatabaseJsonType.Dlcs, idWiiUDLCs.JsonType);
            Assert.AreEqual(DatabaseJsonType.Updates, idWiiUUpdates.JsonType);
        }


        [TestMethod]
        public void ToStringTest()
        {
            Assert.AreEqual("0004008CA1B2C3D4", new TitleID("0004008CA1B2C3D4").ToString().ToUpper());
        }

        [TestMethod]
        public void EqualsTest()
        {
            string id1 = "0001000100000000", id2 = "0001000500000000";
            Assert.AreEqual(new TitleID(id1), new TitleID(id1));
            Assert.AreNotEqual(new TitleID(id1), new TitleID(id2));
        }

        [TestMethod]
        public void CompareToTest()
        {
            string id1 = "0001000100000059", id2 = "000100010000005A";
            Assert.IsTrue(new TitleID(id1).CompareTo(new TitleID(id1)) == 0);
            Assert.IsTrue(new TitleID(id1).CompareTo(new TitleID(id2)) < 0);
            Assert.IsTrue(new TitleID(id2).CompareTo(new TitleID(id1)) > 0);
        }


        [TestMethod]
        public void IsGameUpdateDLCTest()
        {
            Assert.IsTrue(games.All(id => id.IsGame && !id.IsUpdate && !id.IsDLC));
            Assert.IsTrue(updates.All(id => !id.IsGame && id.IsUpdate && !id.IsDLC));
            Assert.IsTrue(dlcs.All(id => !id.IsGame && !id.IsUpdate && id.IsDLC));
        }

        [TestMethod]
        public void GameIDTest()
        {
            Assert.AreEqual(idWiiUGames, idWiiUGames.GameID);
            Assert.AreEqual(idWiiUGames, idWiiUUpdates.GameID);
            Assert.AreEqual(idWiiUGames, idWiiUDLCs.GameID);
        }
        [TestMethod]
        public void UpdateIDTest()
        {
            Assert.AreEqual(idWiiUUpdates, idWiiUGames.UpdateID);
            Assert.AreEqual(idWiiUUpdates, idWiiUUpdates.UpdateID);
            Assert.AreEqual(idWiiUUpdates, idWiiUDLCs.UpdateID);

            Assert.IsNull(idWiiGames.UpdateID);
        }
        [TestMethod]
        public void DLCIDTest()
        {
            Assert.AreEqual(idWiiUDLCs, idWiiUGames.DLCID);
            Assert.AreEqual(idWiiUDLCs, idWiiUUpdates.DLCID);
            Assert.AreEqual(idWiiUDLCs, idWiiUDLCs.DLCID);

            Assert.AreEqual(id3DSDLCs, id3DSGames.DLCID);
            Assert.AreEqual(id3DSDLCs, id3DSUpdates.DLCID);
            Assert.AreEqual(id3DSDLCs, id3DSDLCs.DLCID);
        }
    }
}