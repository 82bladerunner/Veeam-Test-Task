using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using Timer = System.Timers.Timer;

namespace VeeamTestTask
{
    class FolderSynchronizer
    {
        private Timer syncTimer;
        private string sourceFolder;
        private string replicaFolder;
        private int syncInterval;
        private TraceSource logger;

        public FolderSynchronizer(string sourceFolder, string replicaFolder, int syncInterval, string logFilePath)
        {
            this.sourceFolder = sourceFolder;
            this.replicaFolder = replicaFolder;
            this.syncInterval = syncInterval;

            // Initialize logger
            logger = new TraceSource("FolderSyncApp");

            // Set up trace listeners
            var consoleListener = new ConsoleTraceListener();
            var fileListener = new TextWriterTraceListener(logFilePath);

            // Configure trace listeners
            logger.Listeners.Add(consoleListener);
            logger.Listeners.Add(fileListener);
            logger.Switch = new SourceSwitch("SourceSwitch", "Verbose");

            // Set trace output options
            consoleListener.TraceOutputOptions = TraceOptions.DateTime;
            fileListener.TraceOutputOptions = TraceOptions.DateTime;

            // Trace initialization message
            logger.TraceEvent(TraceEventType.Information, 0, "Logging set up successfully.");

            // Set up the timer to call the OnTimedEvent method periodically
            syncTimer = new Timer(syncInterval);
            syncTimer.Elapsed += OnTimedEvent;
            syncTimer.AutoReset = true;
            syncTimer.Enabled = true;
        }

        public static void Main(string[] args)
        {
            Console.Write("Enter the source folder path: ");
            string sourceFolder = Console.ReadLine();
            Console.Write("Enter the replica folder path: ");
            string replicaFolder = Console.ReadLine();
            Console.Write("Enter the synchronization interval in milliseconds: ");
            int syncInterval = int.Parse(Console.ReadLine());
            Console.Write("Enter the log file path: ");
            string logFilePath = Console.ReadLine();

            FolderSynchronizer synchronizer = new FolderSynchronizer(sourceFolder, replicaFolder, syncInterval, logFilePath);

            Console.WriteLine("Sync service started. Press Enter to exit...");
            Console.ReadLine();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            SynchronizeFolders();
            logger.TraceEvent(TraceEventType.Information, 0, $"Sync completed at {e.SignalTime.ToString("HH:mm:ss.fff")}");
        }

        private void SynchronizeFolders()
        {
            try
            {
                // Ensure the replica folder exists
                if (!Directory.Exists(replicaFolder))
                {
                    Directory.CreateDirectory(replicaFolder);
                    logger.TraceEvent(TraceEventType.Information, 0, $"Created directory: {replicaFolder}");
                }

                // Get all files from the source folder
                var sourceFiles = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
                foreach (var sourceFile in sourceFiles)
                {
                    var relativePath = Path.GetRelativePath(sourceFolder, sourceFile);
                    var replicaFile = Path.Combine(replicaFolder, relativePath);

                    // Ensure the directory in the replica folder exists
                    var replicaDir = Path.GetDirectoryName(replicaFile);
                    if (!Directory.Exists(replicaDir))
                    {
                        Directory.CreateDirectory(replicaDir);
                        logger.TraceEvent(TraceEventType.Information, 0, $"Created directory: {replicaDir}");
                    }

                    // Copy the file if it doesn't exist or is different
                    if (!File.Exists(replicaFile) || !Methods.FilesAreEqual(sourceFile, replicaFile))
                    {
                        File.Copy(sourceFile, replicaFile, true);
                        logger.TraceEvent(TraceEventType.Information, 0, $"Copied: {sourceFile} to {replicaFile}");
                    }
                }

                // Delete files in replica that aren't in the source
                var replicaFiles = Directory.GetFiles(replicaFolder, "*.*", SearchOption.AllDirectories);
                foreach (var replicaFile in replicaFiles)
                {
                    var relativePath = Path.GetRelativePath(replicaFolder, replicaFile);
                    var sourceFile = Path.Combine(sourceFolder, relativePath);

                    if (!File.Exists(sourceFile))
                    {
                        File.Delete(replicaFile);
                        logger.TraceEvent(TraceEventType.Information, 0, $"Deleted: {replicaFile}");
                    }
                }

                // Clean up empty directories in the replica
                Methods.DeleteEmptyDirectories(replicaFolder);
            }
            catch (Exception ex)
            {
                logger.TraceEvent(TraceEventType.Error, 0, $"An error occurred during sync: {ex.Message}");
            }
        }
    }
}
