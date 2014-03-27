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

namespace ROMResigner
{
    /// <summary>
    /// Подписывание файлов
    /// </summary>
    static class Signer
    {
        /// <summary>
        /// Подписывание пакета
        /// </summary>
        /// <param name="Package">Пакет</param>
        /// <returns>Путь до выходного файла</returns>
        public static string SignFile(PackageInfo Package)
        {
            string outputFile = Path.Combine(System.IO.Path.GetTempPath(), string.Format("{0}_signed{1}", Path.GetFileNameWithoutExtension(Package.Path), Path.GetExtension(Package.Path)));
            return SignFile(Package, outputFile);
        }

        /// <summary>
        /// Подписывание пакета
        /// </summary>
        /// <param name="Package">Пакет</param>
        /// <param name="outputFile">Путь до выходного пакета</param>
        /// <returns>Путь до выходного пакета</returns>
        public static string SignFile(PackageInfo Package, string outputFile)
        {
            Environment.Log(string.Format("Подписываем файл \"{0}\"...", Package.Path));
            if (Package.NewCert.CanSign)
            {
                if (File.Exists(outputFile))
                {
                    try { File.Delete(outputFile); }
                    catch (Exception ex) { Environment.Log(string.Format("Не удается удалить файл \"{0}\" Ошибка: {1}", outputFile, ex.Message)); }
                }

                Environment.javaProc.StartInfo.Arguments = string.Format(
                    Environment.signerArgs,
                    Environment.signerFile,
                    Package.NewCert.pemPath,
                    Package.NewCert.pk8Path,
                    Package.Path,
                    outputFile
                );

                Environment.javaProc.Start();

                string output = Environment.javaProc.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(output))
                    Environment.Log(output);

                Environment.javaProc.WaitForExit();
            }

            if (File.Exists(outputFile))
                return outputFile;
            Environment.Log(string.Format("Выходной файл \"{0}\" не был найден. Ошибка.", outputFile));
            return null;
        }
    }
}
