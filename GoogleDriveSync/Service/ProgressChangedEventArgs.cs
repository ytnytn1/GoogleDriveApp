using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class ProgressChangedEventArgs:EventArgs
    {
        public long BytesDownloaded { get; }

        public ProgressChangedEventArgs(long bytesDownloaded)
        {
            BytesDownloaded = bytesDownloaded;
        }
    }
}
