using System;
using System.Collections.Generic;
using System.Data.SQLite;
using SysBot.Base;
using PKHeX.Core;
using System.IO;

/*
 * TODO:
 *   1) Add description field for each entry
 *   2) Add "Status" field to allow enabling/disabling items
 *   3) We aren't going to use files at all anymore, dump that PK8 data into each entry.
 *   4) Create commands for Updating an entry, getting entry details
 *   5) No files == no reloads, get rid of all that.
 */
namespace SysBot.Pokemon
{
    public class GiveawayPool
    {
        public SQLiteConnection Database;
        public readonly PokeTradeHubConfig HubConfig;
        public GiveawayPool(PokeTradeHubConfig hub)
        {
            HubConfig = hub;
            Database = NewConnection();
            CreateTable();
        }
        private SQLiteConnection NewConnection()
        {
            string dbfile = HubConfig.Giveaway.GiveawayFolder + "/giveawaypool.sqlite3";
            string cs = @"URI = file:" + dbfile + ";Version=3; Foreign Keys=True;";
            LogUtil.LogInfo("Connecting to GiveawayPool DB: " + dbfile, nameof(GiveawayPool));

            var conn = new SQLiteConnection(cs);
            conn.Open();
            return conn;
        }
        private void CreateTable()
        {
            SQLiteConnection conn = Database;
            try
            {
                using var cmd = new SQLiteCommand(conn)
                {
                    CommandText = @"CREATE TABLE IF NOT EXISTS giveawaypool (
                                    Id INTEGER PRIMARY KEY NOT NULL UNIQUE,
                                    Name TEXT NOT NULL,
                                    Description TEXT,
                                    Status TEXT NOT NULL,
                                    PK8 TEXT NOT NULL, 
                                    Tag TEXT NOT NULL, 
                                    Uploader TEXT NOT NULL)"

                };

                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error creating Giveawaypool Table: " + e.Message, nameof(GiveawayPool));
            }

            return;
        }
        public int NewEntry(GiveawayPoolEntry entry)
        {
            SQLiteConnection conn = Database;
            int result = -1;
            int entryID = 0;
            try
            {

                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "INSERT INTO giveawaypool(Name, Pokemon, Status, PK8, Tag, Uploader)" +
                    "VALUES (@name, @pokemon, @status, @pk8, @tag, @uploader)";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@name", entry.Name);
                cmd.Parameters.AddWithValue("@pokemon", entry.Pokemon);
                cmd.Parameters.AddWithValue("@status", entry.Status);
                cmd.Parameters.AddWithValue("@pk8", Utils.PK8ToB64(entry.PK8));
                cmd.Parameters.AddWithValue("@tag", entry.Tag);
                cmd.Parameters.AddWithValue("@uploader", entry.Uploader);

                result = cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error inserting new entry: " + e.Message, nameof(GiveawayPool));
                return 0;
            }
            try
            {
                using SQLiteCommand getRow = new SQLiteCommand(conn);
                getRow.CommandText = "SELECT last_insert_rowid()";
                var lastRow = getRow.ExecuteScalar();
                entryID = Int32.Parse(lastRow.ToString());
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error getting new entry id: " + e.Message, nameof(GiveawayPool));
                return 0;
            }

            return entryID;
        }
        public GiveawayPoolEntry GetEntryByName(string name)
        {
            GiveawayPoolEntry entry = new GiveawayPoolEntry();
            SQLiteConnection conn = Database;

            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "SELECT * FROM giveawaypool where Name = @name";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@Name", name);
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    entry.Id = Int32.Parse(reader["Id"].ToString());
                    entry.Name = reader["Name"].ToString();
                    entry.Pokemon = reader["Pokemon"].ToString();
                    entry.Description = reader["Description"].ToString();
                    entry.Status = reader["Status"].ToString();
                    entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    entry.Uploader = reader["Uploader"].ToString();
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }

            return entry;
        }
        public GiveawayPoolEntry GetEntryById(int Id)
        {
            GiveawayPoolEntry entry = new GiveawayPoolEntry();
            SQLiteConnection conn = Database;

            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "SELECT * FROM giveawaypool where Id = @Id";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@Id", Id);
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    entry.Id = Int32.Parse(reader["Id"].ToString());
                    entry.Name = reader["Name"].ToString();
                    entry.Pokemon = reader["Pokemon"].ToString();
                    entry.Description = reader["Description"].ToString();
                    entry.Status = reader["Status"].ToString();
                    entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    entry.Tag = reader["Tag"].ToString();
                    entry.Uploader = reader["Uploader"].ToString();
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }

            return entry;
        }
        public PK8 GetEntryPK8(int Id)
        {
            SQLiteConnection conn = Database;
            
            try
            {
                using SQLiteCommand getRow = new SQLiteCommand(conn);
                getRow.CommandText = "SELECT PK8 FROM giveawaypool WHERE Id = @id";
                getRow.Prepare();
                getRow.Parameters.AddWithValue("@id", Id);
                var reader = getRow.ExecuteScalar();
                var encodedData = reader.ToString();
                PK8? pk8 = Utils.B64ToPK8(encodedData);
                if (pk8 == null)
                {
                    LogUtil.LogInfo("Pokemon Data not valid", nameof(GiveawayPool));
                    return new PK8();
                } 
                else
                {

                    LogUtil.LogInfo("PK8 Data Loaded: " + (Species)pk8.Species, nameof(GiveawayPool));
                    return pk8;

                }

            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error getting new entry id: " + e.Message, nameof(GiveawayPool));
            }
            return new PK8();
        }
        public int UpdateEntry(int entryID, string column, string newValue)
        {
            SQLiteConnection conn = Database;
            int result = -1;
            try
            {

                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "UPDATE giveawaypool "
                                + "SET " + column + " = @" + column
                                + " WHERE Id = @Id";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@Id", entryID);
                cmd.Parameters.AddWithValue("@" + column, newValue);

                result = cmd.ExecuteNonQuery();
                
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error inserting new entry: " + e.Message, nameof(GiveawayPool));
                return 0;
            }

            return result;
        }
        public List<GiveawayPoolEntry> GetPool(string status = "active")
        {
            List<GiveawayPoolEntry> entries = new List<GiveawayPoolEntry>();
            SQLiteConnection conn = Database;

            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.Prepare();
                switch (status)
                {
                    case "active":
                        cmd.CommandText = "SELECT Id,Name,Tag,Status,PK8 FROM giveawaypool WHERE status = @status AND Tag != 'Item'";
                        cmd.Parameters.AddWithValue("@status", status);
                        break;
                    case "inactive":
                        cmd.CommandText = "SELECT Id,Name,Tag,Status,PK8 FROM giveawaypool WHERE status = @status AND Tag != 'Item'";
                        cmd.Parameters.AddWithValue("@status", status);
                        break;
                    default:
                        cmd.CommandText = "SELECT Id,Name,Tag,Status,PK8 FROM giveawaypool AND Tag != 'Item'";
                        break;
                }
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    GiveawayPoolEntry entry = new GiveawayPoolEntry();
                   
                    entry.Id = Int32.Parse(reader["Id"].ToString());
                    entry.Name = reader["Name"].ToString();
                    entry.Tag = reader["Tag"].ToString();
                    entry.Status = reader["Status"].ToString();
                    entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    entries.Add(entry);
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }

            return entries;
        }
        public List<GiveawayPoolEntry> GetPoolByTag(string tag)
        {
            List<GiveawayPoolEntry> entries = new List<GiveawayPoolEntry>();
            SQLiteConnection conn = Database;

            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.Prepare();
                cmd.CommandText = "SELECT Id,Name,Tag,Status,PK8 FROM giveawaypool WHERE tag = @tag";
                cmd.Parameters.AddWithValue("@tag", tag);
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    GiveawayPoolEntry entry = new GiveawayPoolEntry();

                    entry.Id = Int32.Parse(reader["Id"].ToString());
                    entry.Name = reader["Name"].ToString();
                    entry.Tag = reader["Tag"].ToString();
                    entry.Status = reader["Status"].ToString();
                    entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    entries.Add(entry);
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }

            return entries;
        }
        public List<GiveawayPoolEntry> SearchPool(string search)
        {
            List<GiveawayPoolEntry> entries = new List<GiveawayPoolEntry>();
            SQLiteConnection conn = Database;

            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.Prepare();
                cmd.CommandText = "SELECT Id,Name,Tag,Status,PK8 FROM giveawaypool WHERE Name like @name OR Tag like @tag";
                cmd.Parameters.AddWithValue("name", search);
                cmd.Parameters.AddWithValue("tag", search);

                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    GiveawayPoolEntry entry = new GiveawayPoolEntry();

                    entry.Id = Int32.Parse(reader["Id"].ToString());
                    entry.Name = reader["Name"].ToString();
                    entry.Tag = reader["Tag"].ToString();
                    entry.Status = reader["Status"].ToString();
                    entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    entries.Add(entry);
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }

            return entries;
        }

        public class Utils
        {
            public static string PK8ToB64(PK8 pk8)
            {
                return Convert.ToBase64String(pk8.Data);
            }
            public static PK8 B64ToPK8(string encodedData)
            {
                var decoded = new PK8(Convert.FromBase64String(encodedData));
                if (decoded != null)
                {
                    return decoded;
                }
                else
                {
                    return new PK8();
                }

            }
        }
    }
}
