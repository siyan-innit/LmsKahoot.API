using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(LmsKahoot.API.Startup))]

namespace LmsKahoot.API
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // This is where we'll plug in SignalR later
            app.MapSignalR();

            // If later we need other middleware, we add it here.
        }
    }
}