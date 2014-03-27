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
    /// Класс выравнивания пакета - ZIPAlign
    /// </summary>
    static class Aligner
    {
        public static string AlignFile(string inputFile)
        {
            Environment.Log(string.Format("Зипалигним файл \"{0}\"...", inputFile));
            string outputFile = Path.Combine(System.IO.Path.GetTempPath(), string.Format("{0}_signed{1}", Path.GetFileNameWithoutExtension(inputFile), Path.GetExtension(inputFile)));
            if (File.Exists(outputFile))
            {
                try { File.Delete(outputFile); }
                catch (Exception ex) { Environment.Log(string.Format("Не удается удалить файл \"{0}\" Ошибка: {1}", outputFile, ex.Message)); }
            }

            Environment.zaProc.StartInfo.Arguments = string.Format(Environment.alignerArgs, inputFile, outputFile);
            Environment.zaProc.Start();

            string output = Environment.zaProc.StandardOutput.ReadToEnd();
            if (!string.IsNullOrEmpty(output))
                Environment.Log(output);

            Environment.zaProc.WaitForExit();

            if (File.Exists(outputFile))
                return outputFile;
            Environment.Log(string.Format("Выходной файл \"{0}\" не был найден. Ошибка.", outputFile));
            return null;
        }
    }
}
