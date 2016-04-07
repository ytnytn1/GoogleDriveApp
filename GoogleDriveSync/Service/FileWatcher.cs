using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class FileWatcher
    {
        private readonly string _path;

        public FileWatcher(string path)
        {
            _path = path;
            fileSystemWatcher = new FileSystemWatcher();           
            fileSystemWatcher.Filter = "*.*";
            fileSystemWatcher.Path = path + "\\";
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.Created += fileSystemWatcher_Created;
            fileSystemWatcher.Changed += fileSystemWatcher_Changed;
        }

        private FileSystemWatcher fileSystemWatcher;

        void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {           
            // Occurs when the contents of the file change.            
        }

        void fileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            // FullPath is the new file's path.           
        }

        void fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            // FullPath is the location of where the file used to be.
        }

        void fileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            // FullPath is the new file name.           
        }
    }
}
