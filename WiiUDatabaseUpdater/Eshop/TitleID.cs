using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiUDatabaseUpdater.Eshop
{
    class TitleID : IEquatable<TitleID>, IComparable<TitleID>
    {
        private readonly string titleIDString;

        public string High => titleIDString.Substring(0, 8);
        public string Low => titleIDString.Substring(8, 8);
        public string PlatformID => titleIDString.Substring(0, 4);
        public string CategoryID => titleIDString.Substring(4, 4);


        /* titleIDs (high 4 bytes):
         *   0001xxxx -> Wii
         *     00010001 -> Wii Games (including VC) -> gamesWii.json
         *     00010005 -> Wii DLC -> gamesWii.json
         *   0004xxxx -> 3DS
         *     00040000 -> 3DS Games -> games3ds.json
         *     00040002 -> 3DS Demos -> [none]
         *     0004000E -> 3DS Updates -> updates3ds.json
         *     0004008C -> 3DS DLCs -> dlcs3ds.json
         *     00048004 -> DSiWare -> games3ds.json
         *   0005xxxx -> Wii U
         *     00050000 -> Wii U Games -> games.json (for some reason also in updates.json; maybe duplicate?)
         *     00050002 -> Wii U Demo -> updates.json (just 1 demo is present, probably irrelevant)
         *     0005000C -> Wii U DLC -> dlcs.json
         *     0005000E -> Wii U Update -> updates.json
         */
        private DatabaseJsonType _jsonType;

        public DatabaseJsonType JsonType
        {
            get
            {
                if (_jsonType == default(DatabaseJsonType))
                {
                    switch (PlatformID)
                    {
                        case "0001":
                            _jsonType = DatabaseJsonType.GamesWii;
                            break;

                        case "0004":
                            switch (CategoryID)
                            {
                                case "0000":
                                    _jsonType = DatabaseJsonType.Games3DS;
                                    break;
                                case "000E":
                                    _jsonType = DatabaseJsonType.Updates3DS;
                                    break;
                                case "008C":
                                    _jsonType = DatabaseJsonType.Dlcs3DS;
                                    break;
                                case "8004":
                                    _jsonType = DatabaseJsonType.Games3DS;
                                    break;
                                default:
                                    _jsonType = DatabaseJsonType.None;
                                    break;
                            }
                            break;

                        case "0005":
                            switch (CategoryID)
                            {
                                case "0000":
                                    _jsonType = DatabaseJsonType.Games;
                                    break;
                                case "0002":
                                    _jsonType = DatabaseJsonType.Updates;
                                    break;
                                case "000C":
                                    _jsonType = DatabaseJsonType.Dlcs;
                                    break;
                                case "000E":
                                    _jsonType = DatabaseJsonType.Updates;
                                    break;
                                default:
                                    _jsonType = DatabaseJsonType.None;
                                    break;
                            }
                            break;

                        default:
                            _jsonType = DatabaseJsonType.None;
                            break;
                    }
                }
                return _jsonType;
            }
        }


        public bool IsGame => (JsonType == DatabaseJsonType.Games) || (JsonType == DatabaseJsonType.Games3DS) || (JsonType == DatabaseJsonType.GamesWii);
        public bool IsUpdate => (JsonType == DatabaseJsonType.Updates) || (JsonType == DatabaseJsonType.Updates3DS);
        public bool IsDLC => (JsonType == DatabaseJsonType.Dlcs) || (JsonType == DatabaseJsonType.Dlcs3DS);

        public TitleID GameID
        {
            get
            {
                if (IsGame)
                    return this;
                else if (JsonType == DatabaseJsonType.Updates || JsonType == DatabaseJsonType.Dlcs
                    || JsonType == DatabaseJsonType.Updates3DS || JsonType == DatabaseJsonType.Dlcs3DS)
                    return new TitleID(PlatformID + "0000" + Low);
                else
                    return null;
            }
        }
        public TitleID UpdateID
        {
            get
            {
                if (IsUpdate)
                    return this;
                else if (JsonType == DatabaseJsonType.Games || JsonType == DatabaseJsonType.Dlcs
                    || JsonType == DatabaseJsonType.Games3DS || JsonType == DatabaseJsonType.Dlcs3DS)
                    return new TitleID(PlatformID + "000E" + Low);
                else
                    return null;
            }
        }
        public TitleID DLCID
        {
            get
            {
                if (IsDLC)
                    return this;
                else if (JsonType == DatabaseJsonType.Games || JsonType == DatabaseJsonType.Updates)
                    return new TitleID(PlatformID + "000C" + Low);
                else if (JsonType == DatabaseJsonType.Games3DS || JsonType == DatabaseJsonType.Updates3DS)
                    return new TitleID(PlatformID + "008C" + Low);
                else
                    return null;
            }
        }


        public TitleID(string titleIDString)
        {
            if (titleIDString.Length != 16)
                throw new ArgumentException($"Title ID length must be 16 characters (received {titleIDString.Length} characters)");

            this.titleIDString = titleIDString.ToUpper();
            string pID = PlatformID, cID = CategoryID;
            if (   !(pID == "0001" && (cID == "0001" || cID == "0005")) // Wii titleIDs
                && !(pID == "0004" && (cID == "0000" || cID == "0002" || cID == "000E" || cID == "008C" || cID == "8004")) // 3DS titleIDs
                && !(pID == "0005" && (cID == "0000" || cID == "0002" || cID == "000C" || cID == "000E"))) // WiiU titleIDs
                throw new ArgumentException(String.Format("'{0}' is not a valid title ID.", titleIDString));
        }

        public override string ToString()
        {
            return titleIDString;
        }

        #region IEquatable methods
        public override bool Equals(object obj)
        {
            return Equals(obj as TitleID);
        }

        public bool Equals(TitleID other)
        {
            return other != null &&
                   titleIDString == other.titleIDString;
        }

        public override int GetHashCode()
        {
            return -1629438923 + EqualityComparer<string>.Default.GetHashCode(titleIDString);
        }
        #endregion

        public int CompareTo(TitleID other)
        {
            return titleIDString.CompareTo(other.titleIDString);
        }
    }
}
