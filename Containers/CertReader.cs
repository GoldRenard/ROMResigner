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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace ROMResigner
{
    class CertReader
    {
        /// <summary>
        /// Список считанных сертификатов
        /// </summary>
        public static List<CertInfo> Certs;

        /// <summary>
        /// Список сертификатов для подписи
        /// </summary>
        public static List<CertInfo> SignCerts;

        static CertReader()
        {
            Certs = new List<CertInfo>();
            SignCerts = new List<CertInfo>();
            SignCerts.Add(new CertInfo() { IsEmpty = true });
        }

        /// <summary>
        /// Считывает информацию о текущей подписи из ZIP-архива
        /// </summary>
        /// <param name="fPath">Путь до файла (*.zip, *.apk, *.jar)</param>
        /// <returns></returns>
        public static CertInfo ReadZip(string fPath)
        {
            string cFile = Path.Combine(Environment.TempPath, Path.GetFileName(fPath) + "_CERT.RSA");
            ZipFile zf = null;
            FileStream fs = null;

            try
            {
                fs = File.OpenRead(fPath);
                zf = new ZipFile(fs);
            }
            catch { return null; }

            //ищем файл подписи
            bool IsExtracted = false;
            foreach (ZipEntry zipEntry in zf)
            {
                if (!zipEntry.IsFile)
                    continue;
                if (zipEntry.Name == "META-INF/CERT.RSA")
                {
                    Environment.Log(string.Format("Найден файл подписи, извлекаем его в \"{0}\"...", cFile));
                    //Если нашли, извлекаем его во временную директорию
                    byte[] buffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream(zipEntry);
                    using (FileStream streamWriter = File.Create(cFile))
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    IsExtracted = true;
                }
            }

            if (!IsExtracted)
            {
                Environment.Log("Файл подписи не найден");
                return null;
            }

            CertInfo cInfo = ReadCert(cFile, false);

            try { File.Delete(cFile); }
            catch (Exception ex) { Environment.Log(string.Format("Не удалось удалить временный файл \"{0}\". Ошибка: {1}", cFile, ex.Message)); }

            return cInfo;
        }

        /// <summary>
        /// Считывает информацию о текущей подписи из файла сертификата (*.pem или *.RSA)
        /// </summary>
        /// <param name="fPath">Путь до файла (*.pem или *.RSA)</param>
        /// <returns></returns>
        public static CertInfo ReadCert(string fPath, bool IsPk8Required)
        {
            if (!File.Exists(fPath))
                return null;

            //Получаем вывод keytool
            string ktOutput = null;
            try
            {
                Environment.ktProc.StartInfo.Arguments = string.Format(Environment.keytoolArgs, fPath);
                Environment.Log(string.Format("Запускаем \"{0}\" с аргументами \"{1}\"", Environment.ktProc.StartInfo.FileName, Environment.ktProc.StartInfo.Arguments));
                Environment.ktProc.Start();
                ktOutput = Environment.ktProc.StandardOutput.ReadToEnd();
                Environment.ktProc.WaitForExit();
            }
            catch (Exception ex) {
                Environment.Log(string.Format("Запуск не удался. Ошибка: \"{0}\"", ex.Message));
                return null;
            }

            //Если вывод пуст - фейл
            if (string.IsNullOrEmpty(ktOutput))
                return null;

            //Парсим
            CertInfo cInfo = CertInfo.Parse(ktOutput);
            if (cInfo == null)
                return null;

            //Проверяем наличие рядом файла pk8. Если есть - добавляем пути, тем самым помечаем пригодным для подписания
            string pk8File = Path.Combine(Path.GetDirectoryName(fPath), fPath.Substring(0, fPath.IndexOf('.')) + ".pk8");
            if (File.Exists(pk8File))
            {
                Environment.Log(string.Format("Найден pk8 \"{0}\". Добавляем как пригодный для подписи", pk8File));
                cInfo.pemPath = fPath;
                cInfo.pk8Path = pk8File;
                CertInfo cnInfoStored = SignCerts.Find(c => (c.SerialNumber == cInfo.SerialNumber && cInfo.pemPath == cInfo.pemPath && c.pk8Path == cInfo.pk8Path));
                if (cnInfoStored == null)
                {
                    SignCerts.Add(cInfo);
                    return cInfo;
                }
                else
                    return cnInfoStored;
            }
            else
            {
                if (IsPk8Required)
                {
                    Environment.Log(string.Format("Требуется pk8 \"{0}\", однако он не был найден.", pk8File));
                    return null;
                }
            }

            //Ищем подпись в коллекции
            CertInfo cInfoStored = Certs.Find(c => (c.SerialNumber == cInfo.SerialNumber));
            if (cInfoStored == null)
            {
                Certs.Add(cInfo);
                return cInfo;
            }
            else
                return cInfoStored;
        }
    }
}
