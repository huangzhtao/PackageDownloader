using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using PackageDownloader.Service.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PackageDownloader.Service.Compress
{
    public class CompressService: ICompressService
	{
        public bool CompressDirectory(string folderName, string zipedFileName)
        {
            if (!Directory.Exists(folderName))
            {
                return false;
            }

			try
			{
				string[] filenames = Directory.GetFiles(folderName);

				// 'using' statements guarantee the stream is closed properly which is a big source
				// of problems otherwise.  Its exception safe as well which is great.
				using (ZipOutputStream s = new ZipOutputStream(File.Create(zipedFileName)))
				{
					s.SetLevel(9); // 0 - store only to 9 - means best compression
					byte[] buffer = new byte[4096];

					foreach (string file in filenames)
					{
						var entry = new ZipEntry(Path.GetFileName(file));
						entry.DateTime = DateTime.Now;
						s.PutNextEntry(entry);

						using (FileStream fs = File.OpenRead(file))
						{
							// Using a fixed size buffer here makes no noticeable difference for output
							// but keeps a lid on memory usage.
							int sourceBytes;
							do
							{
								sourceBytes = fs.Read(buffer, 0, buffer.Length);
								s.Write(buffer, 0, sourceBytes);
							} while (sourceBytes > 0);
						}
					}
					s.Finish();
					s.Close();
					return true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception during processing {0}", ex);
				return false;
			}
		}
    }
}
