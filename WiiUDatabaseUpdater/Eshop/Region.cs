using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

namespace WiiUDatabaseUpdater.Eshop
{
    public enum Region
    {
        None,

        USA,

        EUR,

        JPN,

        KOR,

        ALL
    }

    public static class RegionExtensions
    {
        public static string ToCountryCode(this Region region)
        {
            switch (region)
            {
                case Region.USA:
                    return "US";
                case Region.EUR:
                    return "GB";
                case Region.JPN:
                    return "JP";
                case Region.KOR:
                    return "KR";
                default:
                    return null;
            }
        }

        public static string GetName(this Region region)
        {
            return region == Region.None ? string.Empty : Enum.GetName(typeof(Region), region);
        }
    }
}
