using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class Configuration
    {
        public static Configuration Instance { get; private set; } = new Configuration();

        [Persistent]
        public string SimComplexityRegular = "10";
        [Persistent]
        public string SimComplexityOrbital = "10";
        [Persistent]
        public string SimComplexityLanded = "10";
        [Persistent]
        public string TrivialLimit = "10";
        [Persistent]
        public string CoreHourCost = "100";


        private const string FILENAME = "settings.cfg";
        private static string FILEDIR = KSPUtil.ApplicationRootPath + "/GameData/SimuLite/PluginData/";

        /// <summary>
        /// Saves the Configuration to its file
        /// </summary>
        public static void SaveToFile()
        {
            //make the PluginData folder
            System.IO.Directory.CreateDirectory(FILEDIR);
            ConfigNode node = Instance.AsConfigNode();
            node.Save(FILEDIR + FILENAME);
        }

        /// <summary>
        /// Loads the Configuration from its file
        /// </summary>
        public static void LoadFromFile()
        {
            if (System.IO.File.Exists(FILEDIR+FILENAME))
            {
                ConfigNode node = ConfigNode.Load(FILEDIR + FILENAME);
                FromConfigNode(node);
            }
        }

        /// <summary>
        /// Converts the Configuration to a ConfigNode
        /// </summary>
        /// <returns>The Configuration as a ConfigNode</returns>
        public ConfigNode AsConfigNode()
        {
            try
            {
                //Create a new Empty Node with the class name
                ConfigNode cnTemp = new ConfigNode(this.GetType().Name);
                //Load the current object in there
                cnTemp = ConfigNode.CreateConfigFromObject(this, cnTemp);
                return cnTemp;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                //Logging and return value?                    
                return new ConfigNode(this.GetType().Name);
            }
        }

        /// <summary>
        /// Loads a Configuration from a ConfigNode
        /// </summary>
        /// <param name="node">The ConfigNode to load from</param>
        /// <returns>The static Configuration.Instance</returns>
        public static Configuration FromConfigNode(ConfigNode node)
        {
            try
            {
                Instance = ConfigNode.CreateObjectFromConfig<Configuration>(node);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                Instance = new Configuration();
            }
            return Instance;
        }
    }
}
