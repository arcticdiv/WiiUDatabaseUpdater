using System.IO;

namespace WiiUUSBHelper_JSONUpdater.Eshop
{
    public enum DatabaseJsonType
    {
        None,
        Customs,
        Dlcs,
        Dlcs3DS,
        Games,
        Games3DS,
        GamesWii,
        Injections,
        Updates,
        Updates3DS
    }

    public static class DatabaseJsonTypeExtensions
    {
        public static string GetFilePath(this DatabaseJsonType jsonType)
        {
            switch (jsonType)
            {
                case DatabaseJsonType.Customs:
                    return Path.Combine("data", "customs.json");
                case DatabaseJsonType.Dlcs:
                    return Path.Combine("data", "dlcs.json");
                case DatabaseJsonType.Dlcs3DS:
                    return Path.Combine("data", "dlcs3ds.json");
                case DatabaseJsonType.Games:
                    return Path.Combine("data", "games.json");
                case DatabaseJsonType.Games3DS:
                    return Path.Combine("data", "games3ds.json");
                case DatabaseJsonType.GamesWii:
                    return Path.Combine("data", "gamesWii.json");
                case DatabaseJsonType.Injections:
                    return Path.Combine("data", "injections.json");
                case DatabaseJsonType.Updates:
                    return Path.Combine("data", "updates.json");
                case DatabaseJsonType.Updates3DS:
                    return Path.Combine("data", "updates3ds.json");
                default:
                    return null;
            }
        }

        public static string GetBackupFilePath(this DatabaseJsonType jsonType)
        {
            return jsonType.GetFilePath() + ".bak";
        }
    }
}
