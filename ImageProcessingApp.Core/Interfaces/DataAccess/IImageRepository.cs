using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessingApp.Core.Interfaces.DataAccess
{
    public interface IImageRepository
    {
        ICollection<ImageFile> GetLatestImages(string imagesPath, int count);
        ICollection<ImageFile> GetLatestImages(string imagesPath, int page, int pageSize);
    }
}
