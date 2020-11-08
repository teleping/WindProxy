using System;
using System.Collections.Generic;
using System.Configuration;
using System.Management;
using System.Net;

namespace Bannersoft.WindProxy.Commons
{
    class ConfigUtil
    {
        public static string getStr(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            return value != null && value.Trim().Length > 0 ? value.Trim() : "";
        }

        public static bool getBool(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            return value != null && value.Trim().Length > 0 ? bool.Parse(value.Trim()) : false;
        }

        public static int getInt(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            return value != null && value.Trim().Length > 0 ? int.Parse(value.Trim()) : -1;
        }

        public static double getDouble(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            return value != null && value.Trim().Length > 0 ? double.Parse(value.Trim()) : -1;
        }

        public static void setConfig(string key, string value)
        {
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfg.AppSettings.Settings[key].Value = value;
            cfg.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }
    }

    class SystemUtil
    {
        public static string MACHINE_NAME = Environment.MachineName;
        public static string SYS_USER = Environment.UserName;
        public static List<string> MAC_ADDRESS = getMacAddress();

        public static List<string> getMacAddress()
        {
            List<string> macs = new List<string>();
            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"])
                    {
                        string mac = mo["MacAddress"].ToString();
                        macs.Add(mac);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return macs;
        }

        /*获取本地IP地址*/
        public static string getIPAddress()
        {
            ///获取本地的IP地址
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;
        }
    }
}