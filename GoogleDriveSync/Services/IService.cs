using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;

namespace Services
{
    public interface IService
    {
        Task<string> GetRootId();
        Task<List<MyFile>> GetFileListAsync();
        Task<StatusOfDownload> DownloadFile(MyFile fileResource, string saveTo);
    }
}
