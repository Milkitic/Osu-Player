using System.Configuration;
using System.IO;

namespace Milkitic.OsuPlayer.Utils
{
    public static class Util
    {
        public static void UpdateConnectionStringsConfig(string key, string conString)
        {
            ConnectionStringSettings mySettings = new ConnectionStringSettings(key, conString)
            {
                ProviderName = "System.Data.SQLite.EF6"
            };
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (ConfigurationManager.ConnectionStrings[key] != null)
            {
                config.ConnectionStrings.ConnectionStrings.Remove(key);
            }
            // 将新的连接串添加到配置文件中. 
            config.ConnectionStrings.ConnectionStrings.Add(mySettings);
            // 保存对配置文件所作的更改 
            config.Save(ConfigurationSaveMode.Modified);
            // 强制重新载入配置文件的ConnectionStrings配置节  
            ConfigurationManager.RefreshSection("connectionStrings");
        }

        /// <summary>
        /// Copy resource to folder
        /// </summary>
        /// <param name="filename">File name in resource.</param>
        /// <param name="path">Path to save.</param>
        public static void ExportResource(string filename, string path)
        {
            System.Resources.ResourceManager rm = Properties.Resources.ResourceManager;
            byte[] obj = (byte[])rm.GetObject(filename, null);
            if (obj == null) return;

            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                fs.Write(obj, 0, obj.Length);
                fs.Close();
            }
        }
    }
}
