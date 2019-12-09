using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WiiUDatabaseUpdater.Eshop;

namespace WiiUDatabaseUpdater
{
    class Program
    {
        private static readonly string wiiUUpdateListVersionPath = Path.Combine("data", "wiiuUpdate.ver");
        private static int wiiuUpdateListVersion = -1;

        static void Main(string[] args)
        {
            if (!File.Exists("ctr-common-1.p12") || !File.Exists("ctr-common-1.pass"))
            {
                MessageBox.Show("Ensure that both 'ctr-common-1.p12' and 'ctr-common-1.pass' are present in the application directory.\n'ctr-common-1.pass' should be a text file containing the password for the .p12 file.\nThe .p12 file and the password can be found on the internet, instructions will not be provided here.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool titlesWiiU = Ask("[WiiU] Retrieve Wii U titles?");
            bool updatesWiiU = Ask("[WiiU] Retrieve Wii U updates?");
            bool dlcsWiiU = Ask("[WiiU] Retrieve Wii U DLCs?");
            bool titles3DS = false;// Ask("[3DS] Retrieve 3DS titles?");
            bool updates3DS = false;// Ask("[3DS] Retrieve 3DS updates?");
            bool dlcs3DS = false;// Ask("[3DS] Retrieve 3DS DLCs?");

            if (updatesWiiU)
            {
                wiiuUpdateListVersion = ReadWiiUUpdateListVersion();
            }

            Task.Run(async () => await DoUpdate(titlesWiiU, updatesWiiU, dlcsWiiU, titles3DS, updates3DS, dlcs3DS)).Wait();

            SaveWiiUUpdateListVersion(wiiuUpdateListVersion);

            Console.WriteLine("\n\nFinished.");
            Console.ReadLine();
        }

        private static bool Ask(string question)
        {
            while (true)
            {
                Console.Write("{0} [y/n]: ", question);
                string resp = Console.ReadLine().Trim();
                if (resp == "y")
                    return true;
                else if (resp == "n")
                    return false;
                Console.WriteLine("> Invalid response.");
            }
        }

        private static async Task DoUpdate(bool titlesWiiU, bool updatesWiiU, bool dlcsWiiU,
            bool titles3DS, bool updates3DS, bool dlcs3DS)
        {
            string keyBagPassword = File.ReadAllLines("ctr-common-1.pass")[0].Trim();

            ProgressManager progress = new ProgressManager();
            progress.SetTitle("Downloading title metadata ...");

            TitleDatabase db = new TitleDatabase();
            db.ReadAllDatabaseFiles();

            EshopUtil eshopUtil = new EshopUtil(db, "ctr-common-1.p12", keyBagPassword);
            eshopUtil.SetProgressManager(progress);

            // Games
            if (titlesWiiU) db.AddTitles(await eshopUtil.GetAllTitles(2));
            if (titles3DS) db.AddTitles(await eshopUtil.GetAllTitles(1));
            // Updates
            if (updatesWiiU) db.AddTitles(await eshopUtil.GetAllWiiUUpdates(wiiuUpdateListVersion));
            if (updates3DS) db.AddTitles(await eshopUtil.GetAll3DSUpdates());
            // DLCs
            if (dlcsWiiU) db.AddTitles(await eshopUtil.GetAllDLCsForTitles(db.GetTitleSet(DatabaseJsonType.Games).Titles));
            if (dlcs3DS) db.AddTitles(await eshopUtil.GetAllDLCsForTitles(db.GetTitleSet(DatabaseJsonType.Games3DS).Titles));

            db.WriteAllDatabaseFiles();
            wiiuUpdateListVersion = eshopUtil.NewestWiiUUpdateListVersion;

            eshopUtil.Dispose();
        }

        
        private static int ReadWiiUUpdateListVersion()
        {
            int version = -1;

            try
            {
                string text = File.ReadAllText(wiiUUpdateListVersionPath);
                int.TryParse(text, out version);

                if (version == -1)
                {
                    MessageBox.Show($"Unable to parse the newest WiiU update list version from \"{wiiUUpdateListVersionPath}\".\nText: \"{text}\"", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    DialogResult answer = MessageBox.Show(
                        $"A previous Wii U update list version was found. Do you want to skip all older lists? (last version: {version})",
                        "Wii U update list version found",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (answer == DialogResult.No)
                    {
                        version = -1;
                    }
                }
            }
            catch { }

            return version;
        }

        private static void SaveWiiUUpdateListVersion(int version)
        {
            try
            {
                if (version != -1)
                {
                    File.WriteAllText(wiiUUpdateListVersionPath, version.ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to save the newest WiiU update list version ({version}) to a file.\n\n{e.GetType()}\n{e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
