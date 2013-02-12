using DBSyncLib;

namespace DBSyncApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (AppArgs.IsArgsConfirmed(args))
            {
                DBSync.SchemaSync(
                    AppArgs.ScriptFolder,
                    AppArgs.TargetServer, AppArgs.TargetDatabase, 
                    AppArgs.TargetUsername, AppArgs.TargetPassword,
                    AppArgs.DeployOnBuild, AppArgs.DeployPath
                );
            }
        }
    }
}
