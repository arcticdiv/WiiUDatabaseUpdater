using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace WiiUUSBHelper_JSONUpdater.Eshop
{
    class EshopTitle : IEquatable<EshopTitle>, IComparable<EshopTitle>
    {
        #region Properties - JSON
        private string _version;

        public string EshopId { get; set; }
        public string IconUrl { get; set; }
        public string Name { get; set; }
        public int Platform { get; set; }
        public string ProductCode { get; set; }
        [JsonIgnore]
        public Region Region { get; set; }
        [JsonProperty(PropertyName = "Region")]
        public string RegionString
        {
            get => Region.GetName();
            set
            {
                if (Enum.TryParse(value, out Region region))
                {
                    Region = region;
                }
            }
        }
        public string Size { get; set; } // string instead of ulong is used to conform to original json structure
        [JsonIgnore]
        public TitleID TitleId { get; set; }
        [JsonProperty(PropertyName = "TitleId")]
        public string TitleIdString
        {
            get => TitleId.ToString();
            set => TitleId = new TitleID(value);
        }
        public bool PreLoad { get; set; } // always false? (except for 5 titles in injections.json)
        public string Version
        {
            get
            {
                return (JsonType == DatabaseJsonType.Games || JsonType == DatabaseJsonType.Games3DS) ? "" : _version;
            }
            set
            {
                _version = value;
            }
        }
        public bool DiscOnly
        {
            get
            {
                return (JsonType == DatabaseJsonType.Games || JsonType == DatabaseJsonType.Games3DS || JsonType == DatabaseJsonType.GamesWii)
                        && (String.IsNullOrEmpty(Size) || Size == "0");
            }
        }
        #endregion

        #region Properties - Database Json Type
        private bool didCacheJsonType = false;
        private DatabaseJsonType _jsonType;

        [JsonIgnore]
        public DatabaseJsonType JsonType
        {
            get
            {
                if (!didCacheJsonType)
                {
                    _jsonType = TitleId.JsonType;
                    if (_jsonType != DatabaseJsonType.None)
                        didCacheJsonType = true;
                }
                return _jsonType;
            }
            set
            {
                _jsonType = value;
                didCacheJsonType = true;
            }
        }
        #endregion

        [JsonIgnore]
        public bool IsNativeTitle
        {
            get
            {
                return Platform == 30 || Platform == 124 || Platform == 125 || Platform == 165 // WiiU Games
                    || Platform == 18 || Platform == 19 || Platform == 83 || Platform == 103 || Platform == 1001 || Platform == 1002 // 3DS/N3DS Games
                    || Platform == 171; // Wii Games
            }
        }

        public void AddDataFromXml(XElement titleXml)
        {
            XAttribute eshopIdAttribute = titleXml.Attribute("id");
            if (eshopIdAttribute != null){
                EshopId = eshopIdAttribute.Value;
            }

            foreach (XElement child in titleXml.Elements())
            {
                switch (child.Name.LocalName)
                {
                    // samurai data
                    case "product_code":
                        ProductCode = child.Value.Substring(child.Value.LastIndexOf("-")+1); // i.e. CTR-N-WXYZ -> WXYZ
                        break;
                    case "name":
                        Name = child.Value.Replace("<br>", "");
                        break;
                    case "icon_url":
                        IconUrl = child.Value;
                        break;
                    case "platform":
                        Platform = Int32.Parse(child.Attribute("id").Value);
                        break;

                    // ninja data
                    case "title_id":
                        TitleId = new TitleID(child.Value);
                        break;
                    case "content_size":
                        Size = UInt64.Parse(child.Value).ToString();
                        break;
                    case "title_version":
                        Version = child.Value;
                        break;
                }
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EshopTitle);
        }

        public bool Equals(EshopTitle other)
        {
            return other != null &&
                   EqualityComparer<TitleID>.Default.Equals(TitleId, other.TitleId) &&
                   Version == other.Version;
        }

        public override int GetHashCode()
        {
            var hashCode = 893656227;
            hashCode = hashCode * -1521134295 + EqualityComparer<TitleID>.Default.GetHashCode(TitleId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
            return hashCode;
        }

        public int CompareTo(EshopTitle other)
        {
            var comp1 = TitleId.CompareTo(other.TitleId);
            return comp1 != 0 ? comp1 : Version.CompareTo(other.Version);
        }
    }
}
