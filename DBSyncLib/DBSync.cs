using System;
using RedGate.SQLCompare.Engine;
using RedGate.Shared.SQL.ExecutionBlock;
using System.Text;

namespace DBSyncLib
{
    public static class DBSync
    {
        public static void SchemaSync(string scriptFolder, string targetServerName, string targetDatabaseName, string targetUserName, string targetPassword, bool deployOnBuild, string deployPath)
        {
            SchemaSync(scriptFolder, new ConnectionProperties(targetServerName, targetDatabaseName, targetUserName, targetPassword), deployOnBuild, deployPath);
        }

        private static void SchemaSync(string scriptFolder, ConnectionProperties targetConnectionProperties, bool deployOnBuild, string deployPath)
        {
            using (Database sourceDatabaseScripts = new Database(), targetDatabase = new Database())
            {
                Console.WriteLine("Beginning schema sync\r\n");

                // Cleanup output dir so that script folder registration does not find duplicate Object definitions
                //CleanUpDBSyncScriptFolder(scriptFolder);

                // Establish the schema from the scripts stored in the sourceDatabaseScripts scripts folder
                // Passing in null for the database information parameter causes SQL Compare to read the
                // XML file supplied in the folder.
                Console.WriteLine("Registering Source Controlled-Path: {0}", scriptFolder);
                sourceDatabaseScripts.Register(scriptFolder, null, Options.Default);

                // Read the schema for the targetDatabase database
                Console.WriteLine("Registering Target DB: {0}", targetConnectionProperties.ServerName + " " + targetConnectionProperties.DatabaseName);
                targetDatabase.Register(targetConnectionProperties, Options.Default);

                // Compare the database against the scripts.
                // Comparing in this order makes the targetDatabase the second database
                Console.WriteLine("Performing comparison...\r\n");
                Differences sourceDatabaseScriptsVStargetDatabaseDiffs =
                    sourceDatabaseScripts.CompareWith(targetDatabase, Options.Default);

                // Select all of the differences for synchronization
                var outputStream = new StringBuilder();
                outputStream.AppendLine("/*");
                outputStream.AppendLine("Differences summary:");
                outputStream.AppendLine("Object Type       Diff   Object Name");
                outputStream.AppendLine("=============================================");
                int diffCount = 0;
                foreach (Difference difference in sourceDatabaseScriptsVStargetDatabaseDiffs)
                {
                    difference.Selected = IsIncludeObject(difference);

                    if (difference.Selected)
                    {
                        if (difference.Type != DifferenceType.Equal)
                        {
                            difference.Selected = true;
                            diffCount++;
                            outputStream.Append(difference.DatabaseObjectType.ToString().PadRight(18));
                            outputStream.Append(replaceDiffType(difference.Type.ToString()));
                            outputStream.Append(difference.Name + "\r\n");
                        }
                        else
                            difference.Selected = false;
                    }
                }
                outputStream.AppendLine("*/");

                if (diffCount == 0)
                {
                    Console.WriteLine("Schemas match!\r\n");
                    return;
                }

                // Calculate the work to do using sensible default options
                // The targetDatabase is to be updated, so the runOnTwo parameter is true
                Console.WriteLine("\r\nCalculating changes to make...\r\n");
                var work = new Work();
                work.BuildFromDifferences(sourceDatabaseScriptsVStargetDatabaseDiffs, Options.Default, true);

                // We can now access the messages and warnings
                Console.WriteLine("Messages:");

                foreach (Message message in work.Messages)
                {
                    Console.WriteLine(message.Text);
                }

                Console.WriteLine("Warnings:");

                foreach (Message message in work.Warnings)
                {
                    Console.WriteLine(message.Text);
                }

                // Disposing the execution block when it's not needed any more is important to ensure
                // that all the temporary files are cleaned up
                using (ExecutionBlock block = work.ExecutionBlock)
                {
                    // Display the SQL used to synchronize
                    Console.WriteLine("Synchronization SQL:");
                    //Console.WriteLine(block.GetString());
                    outputStream.AppendLine(block.GetString());
                    Console.WriteLine(outputStream.ToString());

                    WriteDBSyncScriptFile(outputStream.ToString(), scriptFolder, targetConnectionProperties, deployPath);

                    if (deployOnBuild)
                    {
                        // Finally, use a BlockExecutor to run the SQL against the WidgetProduction database
                        Console.WriteLine("Executing SQL...");
                        var executor = new BlockExecutor();
                        try
                        {
                            executor.ExecuteBlock(block, targetConnectionProperties.ServerName,
                                                  targetConnectionProperties.DatabaseName, false,
                                                  targetConnectionProperties.UserName, targetConnectionProperties.Password);
                            block.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error synchronizing schema:\r\n" + ex.Message);
                            throw (ex);
                        }
                        Console.WriteLine("Schema sync complete");
                    }
                }
            }
        }

        private static void WriteDBSyncScriptFile(string DDSyncSQLStr, string scriptFolder, ConnectionProperties targetConnectionProperties, string deployPath)
        {
            // Write the SQL used to synchronize to Disk
            Console.WriteLine("Writing Synchronization SQL to Disk...");
            var buildsOutputDir = System.IO.Path.Combine(scriptFolder, deployPath);
            if (!System.IO.Directory.Exists(buildsOutputDir))
                System.IO.Directory.CreateDirectory(buildsOutputDir);
            var buildsOutputFile = buildsOutputDir + string.Format("\\{0}_{1}_{2:yyyyMMdd}_{3:mmss}SyncScript.sql", targetConnectionProperties.ServerName, targetConnectionProperties.DatabaseName, DateTime.Now, DateTime.Now);
            System.IO.File.WriteAllText(buildsOutputFile, DDSyncSQLStr);
        }

        private static bool IsIncludeObject(Difference difference)
        {
            if (difference.DatabaseObjectType != ObjectType.User &&
                difference.DatabaseObjectType != ObjectType.Role &&
                difference.DatabaseObjectType != ObjectType.Queue &&
                difference.DatabaseObjectType != ObjectType.Service &&
                MeetExcludeSpecialCase(difference) == false)
            {
                return true;
            }

            return false;
        }

        private static bool MeetExcludeSpecialCase(Difference difference)
        {
            if ((difference.DatabaseObjectType == ObjectType.Table &&
                 difference.Name.ToLower().StartsWith("[dbo].[aspnet_sql")) ||
                (difference.DatabaseObjectType == ObjectType.StoredProcedure &&
                 difference.Name.ToLower().StartsWith("[dbo].[aspnet_sql")) ||
                (difference.DatabaseObjectType == ObjectType.StoredProcedure &&
                 difference.Name.ToLower().StartsWith("[dbo].[sqlquery")))
            {
                return true;
            }

            return false;
        }

        //replaces diff type 'words' with symbols
        public static string replaceDiffType(string diffType)
        {
            switch (diffType)
            {
                case "OnlyIn1":
                    //return ">>  ";
                    return "OnlyInSrc ";
                case "OnlyIn2":
                    //return "<<  ";
                    return "OnlyInDest ";
                case "Equal":
                    return "EQ ";
                case "Different":
                    //return "/=  ";
                    return "Diff ";
                default:
                    return "    ";
            }
        }

        //private static void CleanUpDBSyncScriptFolder(string scriptFolder)
        //{
        //    // Write the SQL used to synchronize to Disk
        //    Console.WriteLine("Cleaning previous DbSync output folder...");
        //    var buildsOutputDir = System.IO.Path.Combine(scriptFolder, AppArgs);
        //    if (System.IO.Directory.Exists(buildsOutputDir))
        //    {
        //        var dirbuildsOutputDir = new System.IO.DirectoryInfo(buildsOutputDir);
        //        foreach (System.IO.FileInfo file in dirbuildsOutputDir.GetFiles()) file.Delete();
        //        foreach (System.IO.DirectoryInfo subDirectory in dirbuildsOutputDir.GetDirectories()) subDirectory.Delete(true);
        //    }
        //}
    }
}
