using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DKPHelper.Commands
{
    internal partial class Command
    {
        public static void NewRaid()
        {
            raid = new RaidSchema();
            raid.items = new List<AwardSchema>();
            raid.pool = new PoolSchema { description="Scars of Velious", name="SoV", order="2", poolid="0" };
            raid.ticks = new List<TickSchema>();

            raid.attendance = "1";
        }
            
        public static string GetRaidText()
        {
            string ret = raid.name;
            ret += "\r\n" + raid.timestamp;
            ret += "\r\n" +
            "\r\nPool:" +
            "\r\n\tDescription: " + raid.pool.description +
            "\r\n\tName: " + raid.pool.name +
            "\r\n\tOrder: " + raid.pool.order +
            "\r\n\tPoolId: " + raid.pool.poolid;

            ret += "\r\nTicks:";
            foreach (var tick in raid.ticks)
            {
                ret += tick.description;
                ret += "\r\n\tCharacters:" +
                "\r\n";
                foreach (string character in tick.characters)
                {
                    ret += "\r\n\t\t" + character;
                }
                ret +="\r\n\tValue: " + tick.value;
            }

            ret += "\r\nItem Awards:";
            foreach (var item in raid.items)
            {
                ret += "\r\nItemName: " + item.itemname +
                "\r\n\tCharacterName: " + item.charactername +
                "\r\n\tDkp: " + item.dkp +
                "\r\n\tGameItemId: " + item.itemid +
                "\r\n\tItemId: " + item.itemid +
                "\r\n\tNotes: " + item.notes;
            }

            return ret;
        }

        public static string GetRaidJSON()
        {
            string ret =
                "{" +
                "\r\n    \"Attendance\": " + raid.attendance + "," +
                "\r\n    \"Items\": [";
            foreach (var item in raid.items)
            {
                ret += "\r\n        {" +
                "\r\n            \"CharacterName\": \"" + item.charactername + "\"," +
                "\r\n            \"Dkp\": " + item.dkp + "," +
                "\r\n            \"GameItemId\": " + item.itemid + "," +
                "\r\n            \"ItemId\": " + item.itemid + "," +
                "\r\n            \"ItemName\": \"" + item.itemname + "\"," +
                "\r\n            \"Notes\": \"" + item.notes + "\"" +
                "\r\n        },";
            }
                ret += "\r\n    ]," +
                "\r\n    \"Name\": \"" + raid.name + "\"," +
                "\r\n    \"Pool\": {" +
                "\r\n        \"Description\": \"" + raid.pool.description + "\"," +
                "\r\n        \"Name\": \"" + raid.pool.name + "\"," +
                "\r\n        \"Order\": " + raid.pool.order + "," +
                "\r\n        \"PoolId\": " + raid.pool.poolid + "" +
                "\r\n    }," +
                "\r\n    \"Ticks\": [";
            foreach (var tick in raid.ticks)
            {
                ret += "\r\n        {" +
                "\r\n            \"Characters\": [" +
                "\r\n                {";
                foreach (string character in tick.characters)
                {
                ret += "\r\n                    \"Name\": \"" + character + "\",";
                }
                ret += "\r\n                }" +
                "\r\n            ]," +
                "\r\n            \"Description\": \"" + tick.description + "\"," +
                "\r\n            \"Value\": \"" + tick.value + "\"" +
                "\r\n        },";
            }
                ret += "\r\n    ]," +
                "\r\n    \"Timestamp\": \"" + raid.timestamp + "\"" +
                "\r\n}";

            return ret;
        }

        private static RaidSchema raid;

        private struct RaidSchema
        {
            public string attendance, name, timestamp;
            public List<AwardSchema> items;
            public PoolSchema pool;
            public List<TickSchema> ticks;
        }

        private struct AwardSchema
        {
            public string itemname, dkp, charactername, notes, itemid;
        }

        private struct PoolSchema
        {
            public string description, name, order, poolid;
        }

        private struct TickSchema
        {
            public string[] characters;
            public string description, value;
        }

        private struct ItemValue
        {
            public string name, dkp;
        }
        public static void SetRaidName(string name)
        {
            raid.name = name;
        }

        public static void ParseTicks(string directory)
        {
            string tickDescription, tickValue, tickTimestamp, tickDataField;

            foreach (string file in Directory.GetFiles(directory, "RaidTick*.txt"))
            {
                TickSchema schema = new TickSchema();
                List<string> chars = new List<string>();

                TextFieldParser parser = new TextFieldParser(file);
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters("\t");
                if (!parser.EndOfData)
                {
                    // Parse header
                    string[]? fields = parser.ReadFields(); // Aaaaand... ignored.

                    // Parse static fields (and first character)
                    fields = parser.ReadFields();
                    if (fields == null)
                        return; // Should never end up here, but I don't like warnings.
                    chars.Add(fields[0]); // Character name;
                    tickTimestamp = fields[3]; // Timestamp
                    tickDataField = fields[4]; // Data field

                    while (!parser.EndOfData)
                    {
                        // Process data row
                        fields = parser.ReadFields();
                        if (fields != null)
                            chars.Add(fields[0]);
                    }

                    string[] dataFields = tickDataField.Split(';');
                    tickDescription = dataFields[0];
                    if (dataFields.Length > 1)
                        tickValue = tickDataField.Split(';')[1];
                    else
                        tickValue = "0";

                    schema.characters = chars.ToArray();
                    schema.description = tickDescription;
                    schema.value = tickValue;

                    raid.timestamp = tickTimestamp.Split('_')[0] + "T";
                    DateTime dtNow = DateTime.Now;
                    DateTime dtNowUTC = dtNow.ToUniversalTime();
                    TimeSpan dtOffset = dtNow - dtNowUTC;
                    DateTime dt = DateTime.Parse(tickTimestamp.Split('_')[1].Replace('-', ':'));
                    dt = dt.AddHours(dtOffset.Hours * -1);
                    raid.timestamp += dt.TimeOfDay.ToString() + ".000Z";
                }

                raid.ticks.Add(schema);
            }
        }

        public static void ParseLog(string directory, string character, string date)
        {
            List<string> messages = new List<string>();
            date = date.Replace('-', ' ');
            string[] dateTokens = date.Split(' ');

            foreach (string file in Directory.GetFiles(directory, "eqlog_" + character + "*.txt"))
            {
                StreamReader streamReader = new StreamReader(file);

                while (!streamReader.EndOfStream)
                {
                    string? line = streamReader.ReadLine();
                    if (line != null)
                    {
                        string[] tokens = line.Split(']')[0].Split(' ');
                        if (dateTokens.All(tokens.Contains))
                            messages.Add(line);
                    }
                }
            }

            Dictionary<string, string> itemDict = new Dictionary<string, string>();
            FileStream fs = File.Open("ItemList.xml", FileMode.OpenOrCreate);
            StreamReader sr = new StreamReader(fs);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line != null)
                {
                    if (line.Contains("<'"))
                    {
                        string s = line.Replace("<'", String.Empty);
                        s = s.Replace("',", ",");
                        s = s.Replace(" />", String.Empty);
                        itemDict.Add(s.Split(", ")[1], s.Split(", ")[0]);
                    }
                }
            }

            foreach (string s in messages)
            {
                if (s.Contains("gratss") && s.Count(c => c == ';') >= 2)
                {
                    string message = s.Split("] ")[1];
                    string awarder = message.Split(' ')[0];
                    if (awarder == "You")
                        awarder = character;
                    string award = "";
                    if (message.Contains("tells the raid"))
                        award = message.Split(" tells the raid,  \'")[1].TrimEnd('\'');
                    else if (message.Contains("tell your raid"))
                        award = message.Split(" tell your raid, \'")[1].TrimEnd('\'');
                    string[] awardTokens = award.Split(';');

                    AwardSchema schema = new AwardSchema();
                    if (awardTokens.Length >= 3)
                    {
                        schema.itemname = awardTokens[0].Trim();
                        schema.dkp = awardTokens[1].Trim();
                        schema.charactername = awardTokens[2].Trim().Split(' ')[0];
                        if (awardTokens.Length >= 4)
                        {
                            string note = awardTokens[3];
                            for (int i = 4; i < awardTokens.Length; i++)
                                note += " ; " + awardTokens[i];
                            schema.notes = note.Trim();
                        }

                        schema.itemid = itemDict.First(s => s.Value == schema.itemname).Key;
                    }

                    raid.items.Add(schema);
                }
            }

            return;
        }
    }
}
