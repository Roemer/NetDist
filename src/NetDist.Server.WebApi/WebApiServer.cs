using Microsoft.Owin.Hosting;
using System;
using System.IO;
using NetDist.Core.Utilities;

namespace NetDist.Server.WebApi
{
    public class WebApiServer : ServerBase
    {
        private IDisposable _app;

        protected override bool InternalStart()
        {
            const string baseAddress = "http://localhost:9000/";
            Logger.Info("Starting OWIN at '{0}'", baseAddress);
            _app = WebApp.Start<Startup>(new StartOptions(baseAddress));



            AddHandler(new ZipUtility().Compress(@"..\..\..\SimpleCalculator\bin\Debug\SimpleCalculator.dll"));
            AddJobLogic(File.ReadAllText(@"..\..\..\SimpleCalculator\Jobs\CalculatorJobLogicAdd.cs"));
            var guid = Guid.NewGuid();
            RemoveJobLogic(guid);


            return true;
        }

        protected override bool InternalStop()
        {
            if (_app != null)
            {
                Logger.Info("Server stopped");
                _app.Dispose();
            }
            return true;
        }
    }
}
