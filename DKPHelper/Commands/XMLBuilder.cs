using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace DKPHelper.Commands
{
    internal partial class Command
    {
        public static void XMLBuilder(string file)
        {
            if (File.Exists(file))
            {
                List<string[]> entries = new List<string[]>();

                string data = File.ReadAllText(file);
                data = data.ReplaceLineEndings();

                string[] lines = data.Split(Environment.NewLine);

                bool isItemsTable = false;
                int i = 0;
                foreach (string line in lines)
                {
                    i++;
                    if (line == String.Empty)
                        continue;

                    if (line == "INSERT INTO `items` VALUES")
                        isItemsTable = true;

                    if (isItemsTable && line[0] == '(')
                    {
                        if (i == 26975)
                            ;

                        string s = line.Replace("(", String.Empty);
                        s = s.Replace(")", String.Empty);
                        entries.AddRange(s.Split(','));
                    }
                }

                FileStream fs = File.Open("ItemList.xml", FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                sw.WriteLine("<Items>");
                foreach (string[] item in entries)
                    sw.WriteLine("<" + item[2] + ", " + item[0] + " />");
                sw.WriteLine("</Items>");
                sw.Close();
                fs.Close();
            }
        }
    }
}
