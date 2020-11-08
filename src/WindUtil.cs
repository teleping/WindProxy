using System;
using System.Runtime.CompilerServices;
using WAPIWrapperCSharp;

namespace Bannersoft.WindProxy.Wind
{
    // Wind API 工具类
    class WindUtil
    {
        private static WindAPI api = null;

        // 获取并启动API
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static WindAPI getAPI()
        {
            if (api == null || !api.isconnected())
            {
                api = new WindAPI();
                try
                {
                    int result = (int)api.start();
                    if (result == 0)
                    {
                        Console.WriteLine(" Wind API 登录成功！");
                    }
                    else
                    {
                        Console.WriteLine(" Wind API 登录失败: " + api.getErrorMsg(result));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return api;
        }

        //关闭API
        public static void stopAPI()
        {
            if (api != null && api.isconnected())
            {
                try
                {
                    api.stop();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
