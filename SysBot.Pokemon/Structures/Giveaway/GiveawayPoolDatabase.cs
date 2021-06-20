using System;
using System.Collections.Generic;
using System.Data.SQLite;
using SysBot.Base;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class GiveawayPool
    {
        public readonly PokeTradeHubConfig HubConfig;
        public GiveawayPool(PokeTradeHubConfig hub)
        {
            HubConfig = hub;
            CreateTable();
        }
        private SQLiteConnection NewConnection()
        {
            string dbfile;
            if (HubConfig.Giveaway.GiveawayFolder != "")
            {
                dbfile = HubConfig.Giveaway.GiveawayFolder + "/giveawaypool.sqlite3";
            }
            else
            {
                dbfile = "giveawaypool.sqlite3";
            }
            string cs = @"URI = file:" + dbfile + ";Version=3; Foreign Keys=True;";
            LogUtil.LogInfo("Connecting to GiveawayPool DB: " + dbfile, nameof(GiveawayPool));

            var conn = new SQLiteConnection(cs);
            conn.Open();
            return conn;
        }
        private void CreateTable()
        {
            SQLiteConnection conn = NewConnection();
            try
            {
                using var cmd = new SQLiteCommand(conn)
                {
                    CommandText = @"CREATE TABLE IF NOT EXISTS pokemon_pool (
                                    Id    INTEGER NOT NULL,
                                    Name  TEXT NOT NULL,
                                    Pokemon   TEXT NOT NULL,
                                    Description   TEXT NOT NULL,
                                    Status    TEXT NOT NULL,
                                    PK8 TEXT NOT NULL,
                                    Tag   TEXT NOT NULL,
                                    Uploader  TEXT NOT NULL,
                                    PRIMARY KEY(Id))"

                };

                cmd.ExecuteNonQuery();
                
                using var cmd2 = new SQLiteCommand(conn)
                {
                    CommandText = @"CREATE TABLE IF NOT EXISTS item_pool(
                                    Id    INTEGER NOT NULL,
                                    Name  TEXT NOT NULL,
                                    Pokemon   TEXT NOT NULL,
                                    Description   TEXT NOT NULL,
                                    Status    TEXT NOT NULL,
                                    PK8 TEXT NOT NULL,
                                    Tag   TEXT NOT NULL,
                                    Uploader  TEXT NOT NULL,
                                    PRIMARY KEY(Id))"
                };
                cmd2.ExecuteNonQuery();

            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error creating Database tables Table: " + e.Message, nameof(GiveawayPool));
            }
            finally
            {
                conn.Close();
            }
            return;
        }
        public int NewEntry(GiveawayPoolEntry entry)
        {
            SQLiteConnection conn = NewConnection(); ;
            int result = -1;
            int entryID = 0;
            try
            {

                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "INSERT INTO " + entry.Pool + " (Name, Pokemon, Status, PK8, Tag, Uploader)" +
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
                Int32.TryParse(lastRow.ToString(), out entryID);
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error getting new entry id: " + e.Message, nameof(GiveawayPool));
                entryID = 0;
            }
            finally
            {
                conn.Close();
            }

            return entryID;
        }
        public GiveawayPoolEntry? GetEntry(string pool, int Id)
        {
            SQLiteConnection conn = NewConnection();
            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);

                cmd.Prepare();

                var CommandText = "SELECT * FROM " + pool + " WHERE Id = @id";
                cmd.CommandText = CommandText;
                cmd.Parameters.AddWithValue("@id", Id);
                LogUtil.LogInfo(pool, "SQL");
                LogUtil.LogInfo(cmd.CommandText, "SQL");
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    GiveawayPoolEntry entry = new();
                    Int32.TryParse(reader["Id"].ToString(), out var id);
                    entry.Id = id;
#pragma warning disable CS8601 // Possible null reference assignment.
                    if (reader["Name"] != null)
                        entry.Name = reader["Name"].ToString();
                    if (reader["Tag"] != null)
                        entry.Tag = reader["Tag"].ToString();
                    if (reader["Status"] != null)
                        entry.Status = reader["Status"].ToString();
                    if (reader["PK8"] != null)
                        entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    if (reader["Pokemon"] != null)
                        entry.Pokemon = reader["Pokemon"].ToString();
                    if (reader["Description"] != null)
                        entry.Description = reader["Description"].ToString();
                    if (reader["Uploader"] != null)
                        entry.Uploader = reader["Uploader"].ToString();
#pragma warning restore CS8601 // Possible null reference assignment.
                    return entry;
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }
            finally
            {
                conn.Close();
            }
            return null;
        }
        public int UpdateEntry(string pool, int entryID, string column, string newValue)
        {
            SQLiteConnection conn = NewConnection();
            int result = -1;
            try
            {

                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "UPDATE " + pool
                                + " SET " + column + " = @" + column
                                + " WHERE Id = @Id";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@Id", entryID);
                cmd.Parameters.AddWithValue("@pool", pool);
                cmd.Parameters.AddWithValue("@" + column, newValue);

                result = cmd.ExecuteNonQuery();
                
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error inserting new entry: " + e.Message, nameof(GiveawayPool));
                result = 0;
            }
            finally
            {
                conn.Close();
            }
            return result;
        }
        public List<GiveawayPoolEntry> GetPool(string pool, string status = "active")
        {
            List<GiveawayPoolEntry> entries = new();
            SQLiteConnection conn = NewConnection();

            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "SELECT * FROM " + pool + " WHERE Status = @status";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@status", status);
                LogUtil.LogInfo(pool, "SQL");
                LogUtil.LogInfo(cmd.CommandText, "SQL");
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    GiveawayPoolEntry entry = new();
                    Int32.TryParse(reader["Id"].ToString(), out var id);
                    entry.Id = id;
#pragma warning disable CS8601 // Possible null reference assignment.
                    if (reader["Name"] != null)
                        entry.Name = reader["Name"].ToString();
                    if (reader["Tag"] != null)
                        entry.Tag = reader["Tag"].ToString();
                    if (reader["Status"] != null)
                        entry.Status = reader["Status"].ToString();
                    if (reader["PK8"] != null)
                        entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    if (reader["Pokemon"] != null)
                        entry.Pokemon = reader["Pokemon"].ToString();
                    if (reader["Description"] != null)
                        entry.Description = reader["Description"].ToString();
                    if (reader["Uploader"] != null)
                        entry.Uploader = reader["Uploader"].ToString();
#pragma warning restore CS8601 // Possible null reference assignment.
                    entries.Add(entry);
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }
            finally
            {
                conn.Close();
            }
            return entries;
        }
        public List<GiveawayPoolEntry> SearchPool(string pool, string search)
        {
            List<GiveawayPoolEntry> entries = new List<GiveawayPoolEntry>();
            SQLiteConnection conn = NewConnection();
            var name = search;
            var tag = search;
            try
            {
                using SQLiteCommand cmd = new SQLiteCommand(conn);

                cmd.CommandText = "SELECT * FROM " + pool + " WHERE Name like @name OR Tag like @tag";
                cmd.Prepare();
                LogUtil.LogInfo(cmd.CommandText, "SQL");
                LogUtil.LogInfo("Pool: "+pool + " Search: " + name + "/" + tag, "SQL");
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("tag", tag);

                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    GiveawayPoolEntry entry = new GiveawayPoolEntry();

                    Int32.TryParse(reader["Id"].ToString(), out var id);
                    entry.Id = id;
#pragma warning disable CS8601 // Possible null reference assignment.
                    if (reader["Name"] != null)
                        entry.Name = reader["Name"].ToString();
                    if (reader["Tag"] != null)
                        entry.Tag = reader["Tag"].ToString();
                    if (reader["Status"] != null)
                        entry.Status = reader["Status"].ToString();
                    if (reader["PK8"] != null)
                        entry.PK8 = Utils.B64ToPK8(reader["PK8"].ToString());
                    if (reader["Pokemon"] != null)
                        entry.Pokemon = reader["Pokemon"].ToString();
                    if (reader["Description"] != null)
                        entry.Description = reader["Description"].ToString();
                    if (reader["Uploader"] != null)
                        entry.Uploader = reader["Uploader"].ToString();
#pragma warning restore CS8601 // Possible null reference assignment.
                    entries.Add(entry);
                }
            }
            catch (SQLiteException e)
            {
                LogUtil.LogInfo("Error querying database: " + e.Message, nameof(GiveawayPool));
            }
            finally
            {
                conn.Close();
            }
            return entries;
        }
        public class Utils
        {
            public static string PK8ToB64(PK8? pk8)
            {
                if (pk8 != null)
                {
                    return Convert.ToBase64String(pk8.Data);
                }
                return "";
            }
            public static PK8 B64ToPK8(string? encodedData)
            {
                if (encodedData != null)
                {
                    var decoded = new PK8(Convert.FromBase64String(encodedData));
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
