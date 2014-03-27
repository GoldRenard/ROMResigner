// ======================================================================
// ROM RESIGNER
// Copyright (C) 2013 Ilya Egorov (goldrenard@gmail.com)

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
// ======================================================================

using System;
using System.IO;
using System.Windows;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace ROMResigner
{
    /// <summary>
    /// Класс работы с ZIP
    /// </summary>
    public class Zipper
    {
        /// <summary>
        /// Распаковывает указанный архив в указанную директорию
        /// </summary>
        /// <param name="fZip">Путь до архива</param>
        /// <param name="Destination">Директория назначения</param>
        /// <returns></returns>
        public static bool UnpackZIP(Stream stream, string Destination)
        {
            try
            {
                if (Directory.Exists(Destination))
                    Directory.Delete(Destination, true);
            }
            catch { return false; }

            ZipFile zf = null;
            bool IsSuccess = true;

            try
            {
                zf = new ZipFile(stream);
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                        continue;
                    byte[] buffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream(zipEntry);
                    string fullZipToPath = Path.Combine(Destination, zipEntry.Name.Replace('/', '\\'));
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
            catch { IsSuccess = false; }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true;
                    zf.Close();
                }
            }
            return IsSuccess;
        }
    }
}
