using System;
using System.Collections.Generic;
using System.Text;

namespace realtimeLogic
{
    public class DataManager
    {
        private static DataManager instance;
        Repository repoObserver;
        // data
        public string markerData;
        public string configData;

        private DataManager() { }

        public static DataManager GetInstance()
        {
            if (instance == null)
            {
                lock (typeof(DataManager))
                {
                    if (instance == null)
                    {
                        instance = new DataManager();
                    }
                }
            }
            return instance;
        }

        public void Subscribe(Repository repo)
        {
            repoObserver = repo;
        }

        public void Update(string folder, Dictionary<string, string> json)
        {
            if (folder == "marker")
            {
                markerData = DictionaryToString(json);
            }
            else if (folder == "config")
            {
                configData = DictionaryToString(json);
            }
        }

        private string DictionaryToString(Dictionary<string, string> dictionary)
        {
            string output = "{\n";
            foreach (var key in dictionary.Keys)
            {
                if (key == "listener")
                {
                    continue;
                }

                output += $" \"{key}\": {dictionary[key]},\n";
            }

            // Remove the trailing comma and newline, if any
            if (output.EndsWith(",\n"))
            {
                output = output.Substring(0, output.Length - 2) + "\n";
            }
            output += "}";

            return output;
        }
    }
}
