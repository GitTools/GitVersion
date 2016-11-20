namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public abstract class FileUpdateBase : IDisposable
    {
        protected readonly List<Action> restoreBackupTasks = new List<Action>();
        protected readonly List<Action> cleanupBackupTasks = new List<Action>();

        public void Dispose()
        {
            foreach (var restoreBackup in restoreBackupTasks)
            {
                restoreBackup();
            }

            cleanupBackupTasks.Clear();
            restoreBackupTasks.Clear();
        }

        public void DoNotRestoreFiles()
        {
            foreach (var cleanupBackupTask in cleanupBackupTasks)
            {
                cleanupBackupTask();
            }
            cleanupBackupTasks.Clear();
            restoreBackupTasks.Clear();
        }
    }
}