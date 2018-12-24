using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WiiUUSBHelper_JSONUpdater.Eshop
{
    class DatabaseJsonIO
    {
        private const Formatting jsonFormattingOptions = Formatting.Indented;

        // == Reading ==

        public static EshopTitle TitleFromJObject(JObject jobj)
        {
            // have to manually parse since the original json files use different default/empty values for Region
            EshopTitle title = new EshopTitle();
            title.EshopId = (string)jobj["EshopId"];
            title.IconUrl = (string)jobj["IconUrl"];
            title.Name = (string)jobj["Name"];
            title.Platform = (int)jobj["Platform"];
            title.ProductCode = (string)jobj["ProductCode"];
            title.Size = (string)jobj["Size"];
            title.TitleId = new TitleID((string)jobj["TitleId"]);
            title.PreLoad = (bool)jobj["PreLoad"];
            title.Version = (string)jobj["Version"];

            Region region;
            Enum.TryParse((string)jobj["Region"], out region);
            title.Region = region;

            return title;
        }

        public static List<EshopTitle> ReadTitlesFromJsonFile(string filePath)
        {
            JArray titlesArray = JArray.Parse(File.ReadAllText(filePath));
            List<EshopTitle> titles = titlesArray.Select(t => TitleFromJObject((JObject)t)).ToList();
            Console.WriteLine("Read {0,5} titles from database file {1}", titles.Count, filePath);
            return titles;
        }

        // == Writing ==

        public static JObject TitleToJObject(EshopTitle title)
        {
            // all default values were determined by looking at the different files;
            //  no guarantee that these are accurate & complete
            bool isGame = title.TitleId.IsGame;
            string defaultEmptyValue = isGame ? null : "";
            JObject jobj = new JObject();

            jobj.Add("EshopId", title.EshopId ?? defaultEmptyValue);

            string iconUrlString = String.IsNullOrEmpty(title.IconUrl)
                ? (isGame ? "#N/A" : "")
                : title.IconUrl;
            jobj.Add("IconUrl", iconUrlString);

            jobj.Add("Name", title.Name ?? defaultEmptyValue);
            jobj.Add("Platform", title.Platform);

            string productCodeString = String.IsNullOrEmpty(title.ProductCode)
                ? (isGame ? "-" : null)
                : title.ProductCode;
            jobj.Add("ProductCode", productCodeString);

            string regionString = title.Region == Region.None
                ? (isGame ? "N/A" : "")
                : Enum.GetName(typeof(Region), title.Region);
            jobj.Add("Region", regionString);

            jobj.Add("Size", title.Size ?? "0");
            jobj.Add("TitleId", title.TitleId.ToString());
            jobj.Add("PreLoad", title.PreLoad);
            jobj.Add("Version", title.Version ?? "");
            jobj.Add("DiscOnly", title.DiscOnly);
            return jobj;
        }

        public static void WriteTitlesToJsonFile(ICollection<EshopTitle> titles, DatabaseJsonType jsonType)
        {
            JArray json = new JArray(titles.Select(t => TitleToJObject(t)).ToArray());
            string filePath = jsonType.GetFilePath();

            DialogResult shouldCreateBackup = MessageBox.Show(String.Format("Do you want to create a backup of {0} ?", filePath), "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (shouldCreateBackup == DialogResult.Cancel)
            {
                return;
            }
            else if (shouldCreateBackup == DialogResult.Yes)
            {
                string backupFilePath = jsonType.GetBackupFilePath();
                if (File.Exists(backupFilePath))
                {
                    if (MessageBox.Show("The backup file already exists.\nDo you want to overwrite it?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        File.Delete(backupFilePath);
                    }
                    else
                    {
                        return;
                    }
                }
                File.Move(filePath, backupFilePath);
            }

            string jsonString = JsonArrayToString(json);
            File.WriteAllText(filePath, jsonString);

            Console.WriteLine("Wrote {0,5} titles to database file {1}", titles.Count, filePath);
        }

        private static string JsonArrayToString(JArray titles)
        {
            JsonSerializer ser = new JsonSerializer();
            ser.Formatting = jsonFormattingOptions;
            ser.Converters.Add(new StringEscapeJsonConverter());

            using (StringWriter stringWriter = new StringWriter())
            {
                ser.Serialize(stringWriter, titles);
                return stringWriter.ToString();
            }

            throw new JsonSerializationException("Could not serialize title array to json string");
        }
    }
}
