using System.Linq;
using System.Collections.Generic;

namespace theMinecraftServerLauncher
{
    public class ConfigParser
    {
        private Dictionary<string, string> data = new Dictionary<string, string>();
        public ConfigParser(string filename)
        {
            string line;
            // Read the file and display it line by line.
            using (System.IO.StreamReader file = new System.IO.StreamReader(filename))
            {
                while ((line = file.ReadLine()) != null)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2) data.Add(parts[0], parts[1]);
                }
            }
        }

        public string GetValue(string key)
        {
            return data[key];
        }

        public void UpdateValue(string key, string newvalue)
        {
            try
            {
                data[key] = newvalue;
            }
            catch
            {
                data.Add(key, newvalue);
            }
        }

        public List<string> GetList()
        {
            List<string> keyslist = data.Keys.ToList();
            List<string> valueslist = data.Values.ToList();
            List<string> returnlist = new List<string>();

            for (int i = 0; i < data.Keys.Count; i++)
            {
                returnlist.Add(keyslist[i] + "=" + valueslist[i]);
            }

            return returnlist;
        }
    }
}
