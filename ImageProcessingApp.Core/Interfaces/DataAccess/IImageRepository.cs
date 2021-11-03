using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessingApp.Core.Interfaces.DataAccess
{
    interface IImageRepository
    {
        ICollection<ImageFile> GetLatestImages(int count);
        ICollection<ImageFile> GetLatestImages(int page, int pageSize);
    }
}
