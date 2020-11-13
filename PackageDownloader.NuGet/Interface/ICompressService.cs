using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDownloader.Service.Interface
{
    public interface ICompressService
    {
        bool CompressDirectory(string folderName, string zipedFileName);
    }
}
