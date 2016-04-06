using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Model.Annotations;

namespace Model
{
    public class MyFile: INotifyPropertyChanged
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public long? Size { get; set; }

        public DateTime? CreationDate { get; set; }

        public bool IsFolder { get; set; }

        public string ParentId { get; set; }

        public string DownloadUrl { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
