using System;
using System.Collections.Generic;

namespace WiiUDatabaseUpdater.Eshop
{
    class TitleDatabase
    {
        private Dictionary<DatabaseJsonType, TitleSet> titleSets = new Dictionary<DatabaseJsonType, TitleSet>();

        public TitleDatabase()
        {
            foreach (DatabaseJsonType jsonType in Enum.GetValues(typeof(DatabaseJsonType)))
            {
                if (jsonType == DatabaseJsonType.None)
                    continue;
                titleSets.Add(jsonType, new TitleSet());
            }
        }


        // == I/O ==

        public void ReadDatabaseFile(DatabaseJsonType jsonType)
        {
            if (jsonType != DatabaseJsonType.None)
            {
                AddTitles(DatabaseJsonIO.ReadTitlesFromJsonFile(jsonType.GetFilePath()), jsonType);
                titleSets[jsonType].Modified = false;
            }
        }

        public void ReadAllDatabaseFiles()
        {
            foreach (DatabaseJsonType jsonType in Enum.GetValues(typeof(DatabaseJsonType)))
            {
                ReadDatabaseFile(jsonType);
            }
        }

        public void WriteDatabaseFile(DatabaseJsonType jsonType)
        {
            if (jsonType != DatabaseJsonType.None)
            {
                TitleSet titleSet = GetTitleSet(jsonType);
                if (titleSet.Modified)
                {
                    DatabaseJsonIO.WriteTitlesToJsonFile(titleSet.Titles, jsonType);
                }
            }
        }

        public void WriteAllDatabaseFiles()
        {
            foreach (DatabaseJsonType jsonType in Enum.GetValues(typeof(DatabaseJsonType)))
            {
                WriteDatabaseFile(jsonType);
            }
        }


        // == Database ==

        public TitleSet GetTitleSet(DatabaseJsonType jsonType)
        {
            return titleSets[jsonType];
        }


        public void AddTitle(EshopTitle title, DatabaseJsonType jsonType = DatabaseJsonType.None, bool overwrite = true)
        {
            if (jsonType != DatabaseJsonType.None)
            {
                // override so that titles read from disk stay in their respective files
                title.JsonType = jsonType;
            }
            else
            {
                // if passed type is None, try to determine it by titleID
                jsonType = title.JsonType;
                // if it's still None, we have nowhere to save the title
                if (jsonType == DatabaseJsonType.None)
                    return;
            }

            if (overwrite)
                titleSets[jsonType].Titles.Remove(title);
            titleSets[jsonType].Titles.Add(title);
            titleSets[jsonType].Modified = true;
        }

        public void AddTitles(ICollection<EshopTitle> gameTitles, DatabaseJsonType jsonType = DatabaseJsonType.None, bool overwrite = true)
        {
            foreach (EshopTitle title in gameTitles)
            {
                AddTitle(title, jsonType, overwrite);
            }
        }
    }
}
