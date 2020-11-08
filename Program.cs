using System;
using System.Linq;
using Bannersoft.WindProxy.Http;
using Bannersoft.WindProxy.Commons;
using Bannersoft.WindProxy.Wind;

namespace Bannersoft.WindProxy
{
    class Program
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            init();

            Console.WriteLine("----------------------------");
            Console.WriteLine("MachineName:\t" + SystemUtil.MACHINE_NAME);
            Console.WriteLine("Address:\t" + SystemUtil.MAC_ADDRESS.First());
            Console.WriteLine("SystemUser:\t" + SystemUtil.SYS_USER);
            Console.WriteLine("----------------------------\n");

            //start proxy
            new ProxyServer().start();

            //init wind api
            WindUtil.getAPI();
        }

        static void init()
        {
            //init log4net
            string path = AppDomain.CurrentDomain.BaseDirectory + "log4net.config";
            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(path));
        }
    }
}
