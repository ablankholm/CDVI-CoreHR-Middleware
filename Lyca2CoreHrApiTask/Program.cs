using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco;
using NLog;
using Newtonsoft.Json;
using Lyca2CoreHrApiTask.Models;
using System.IO;
using System.Data.Common;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;
using Lyca2CoreHrApiTask.DAL;
using Microsoft.Extensions.CommandLineUtils;

namespace Lyca2CoreHrApiTask
{
    class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static IntegrationManager IntegrationManager = new IntegrationManager(Application.StartupPath);



        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "Lyca2CoreHrApiTask",
                Description = "Middleware that pulls together access control data from legacy systems and posts it to the CoreHR clocking API",
                FullName = "Automated Lyca to CoreHR Clocking API Middleware"
            };



            app.OnExecute(() => 
            {
                IntegrationManager.StartOrResume();
                return 0;
            });



            app.Command("RunDevTest", c => {
                c.Description = "Runs the currently configured test method for iterative development";
                c.HelpOption("-?|-h|--help");

                var silentOption = c.Option("--silent", "Run without prompting for user imput.", CommandOptionType.NoValue);

                c.OnExecute(() => {
                    log.Info($"Executing RunDevTest at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                    bool silentTesting = false;

                    if (silentOption.HasValue())
                    {
                        silentTesting = true;
                    }
                    IntegrationManager.Test(silentTesting);

                    return 0;
                });
            });



            return app.Execute(args);
        }
    }
}
