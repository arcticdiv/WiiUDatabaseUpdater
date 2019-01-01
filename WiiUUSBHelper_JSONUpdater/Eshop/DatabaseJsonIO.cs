﻿using Newtonsoft.Json;
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

        public static List<EshopTitle> ReadTitlesFromJsonFile(string filePath)
        {
            JArray titlesArray = JArray.Parse(File.ReadAllText(filePath));
            List<EshopTitle> titles = titlesArray.Select(t => t.ToObject<EshopTitle>()).ToList();
            Console.WriteLine("Read {0,5} titles from database file {1}", titles.Count, filePath);
            return titles;
        }

        public static void WriteTitlesToJsonFile(ICollection<EshopTitle> titles, DatabaseJsonType jsonType)
        {
            JArray json = new JArray(titles.Select(t => JObject.FromObject(t)).ToArray());
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
