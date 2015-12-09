﻿using System.Web.Http;
using Flatwhite.WebApi;
using log4net;

namespace Flatwhite.WebApi2
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            log4net.Config.XmlConfigurator.Configure();
            Global.Logger = new FlatwhiteLog4netAdaptor(LogManager.GetLogger(typeof(Global)));


            GlobalConfiguration.Configure(WebApiConfig.Register);
            //NOTE: These is what you need for WebApi2
            GlobalConfiguration.Configure(x => x.UseFlatwhiteCache());
        }
    }
}
