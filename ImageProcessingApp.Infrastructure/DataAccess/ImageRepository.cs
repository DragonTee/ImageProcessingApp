using System;
using System.Collections.Generic;
using System.Linq;
using ImageProcessingApp.Core;
using ImageProcessingApp.Core.Interfaces.DataAccess;

namespace ImageProcessingApp.Mobile.Infrastructure.DataAccess
{
    public class ImageRepository : IImageRepository
    {
        public ICollection<ImageFile> GetLatestImages(string imagesPath, int count)
        {
            if (!System.IO.Directory.Exists(imagesPath))
                System.IO.Directory.CreateDirectory(imagesPath);
            var files = System.IO.Directory.GetFiles(imagesPath);
            return files
                .OrderByDescending(x => System.IO.Directory.GetLastWriteTimeUtc(x))
                .Take(count)
                .Select(x => new ImageFile { Path = x })
                .ToList();
        }

        public ICollection<ImageFile> GetLatestImages(string imagesPath, int page, int pageSize)
        {
            if (!System.IO.Directory.Exists(imagesPath))
                System.IO.Directory.CreateDirectory(imagesPath);
            var files = System.IO.Directory.GetFiles(imagesPath);
            return files
                .OrderByDescending(x => System.IO.Directory.GetLastWriteTimeUtc(x))
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(x => new ImageFile { Path = x })
                .ToList();
        }
    }
}
