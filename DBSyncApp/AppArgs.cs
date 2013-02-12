using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBSyncApp
{
    public static class AppArgs
    {
        public static string ScriptFolder = "";
        public static string TargetServer = "";
        public static string TargetDatabase = "";
        public static string TargetUsername = "";
        public static string TargetPassword = "";
        public static bool DeployOnBuild = true;
        public static string DeployPath = "../Deploys";

        public static bool IsArgsConfirmed(string[] args)
        {
            var retVal = false;
            //process arguments
            foreach (string s in args)
            {
                if (!SetParameter(s))
                {
                    DisplayUsage();
                    break;
                }
                retVal = true;
            }
            return retVal;
        }

        //Takes a command line argument and assigns the value to the correct parameter
        static private bool SetParameter(string s)
        {
            if (s.Length < 3)
            {
                Console.WriteLine("Invalid option: " + s);
                return false;
            }
            switch (s.Substring(0, 2).ToLower())
            {
                case "/f":
                    ScriptFolder = s.Substring(3);
                    break;
                case "/s":
                    TargetServer = s.Substring(3);
                    break;
                case "/d":
                    TargetDatabase = s.Substring(3);
                    break;
                case "/u":
                    TargetUsername = s.Substring(3);
                    break;
                case "/p":
                    TargetPassword = s.Substring(3);
                    break;
                case "/e":
                    DeployOnBuild = (s.Substring(3) == "true");
                    break;
                case "/o":
                    DeployPath = s.Substring(3);
                    break;
                //case "/l":
                //    LogfilePath = s.Substring(3);
                //    break;
                //case "/t":
                //    SetTableList(s.Substring(3));
                //    break;
                default:
                    Console.WriteLine("Unrecognised option: " + s.Substring(0, 2));
                    return false;
            }

            return true;
        }

        //Displays commandline usage to the user
        static private void DisplayUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("/f:<sourcefolder>  - Set the source folder to use");
            Console.WriteLine("/s:<servername>    - Set the target SQL Server/Instance name");
            Console.WriteLine("/d:<database>      - Set the target Database name");
            Console.WriteLine("/u:<username>      - Optional. Set the Username for SQL Auth. Must be combined with /p");
            Console.WriteLine("/p:<password>      - Optional. Must be combined with /u");
            Console.WriteLine("/e:<true/false>    - Optional. Execute DeployOnBuild? if false, script would have to be ran manually. Default is true.");
            Console.WriteLine("/o:<deploypath>    - Optional. Deploy path. Relative to source path. Default is ../Deploys.");
            //Console.WriteLine("/l:<logfile>       - Optional. Path + filename for logfile output");
            //Console.WriteLine("/t:<tb1>,<tb2>...  - Optional. Comma-separated list of tables for data sync");
        }

    }
}
