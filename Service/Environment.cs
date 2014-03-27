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
using System.Diagnostics;

namespace ROMResigner
{
    /// <summary>
    /// Класс окружения приложения
    /// </summary>
    static class Environment
    {
        #region Пути
        /// <summary>
        /// Возвращает путь с папкой приложения
        /// </summary>
        public static string AppPath
        {
            get { return System.IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory); }
        }

        /// <summary>
        /// Возвращает путь с папкой утилит
        /// </summary>
        public static string TempPath
        {
            get { return Path.Combine(Path.GetTempPath(), "ROMResigner"); }
        }

        /// <summary>
        /// Возвращает путь с папкой утилит
        /// </summary>
        public static string ToolsPath
        {
            get { return Path.Combine(TempPath, "Tools"); }
        }

        #endregion

        const string STR_FILE_NOT_EXIST = "Файл \"{0}\" не найден!";
        const string STR_FILE_NOT_EXIST_CAPTION = "Отсутствуют необходимые файлы";
        const string STR_CANT_DELETE_OLD_DIRS = "Не удается удалить директорию \"{0}\"!";

        public static string logFile = "ROMResigner.log";

        static string alignerFile = "zipalign.exe";
        static string keytoolFile = "keytool.exe";
        static string aaptFile = "aapt.exe";
        public static string signerFile = "signapk.jar";
        public const string alignerArgs = "-fv 4 \"{0}\" \"{1}\"";
        public const string signerArgs = "-jar \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\""; //java -jar SignApk.jar testkey.x509.pem testkey.pk8 semcmusic.apk semcmusic_signed.apk
        public const string keytoolArgs = "-printcert -v -file \"{0}\"";

        public static Process ktProc = new Process();
        public static Process aaptProc = new Process();
        public static Process javaProc = new Process();
        public static Process zaProc = new Process();

        /// <summary>
        /// Метод проверки и инициализации окружения утилиты
        /// </summary>
        /// <returns>True если проверка и инициализация успешна</returns>
        public static bool Init()
        {
            System.Windows.Resources.StreamResourceInfo sri = Application.GetResourceStream(new Uri("pack://application:,,,/Tools.zip"));
            if (sri != null)
            {
                using (Stream s = sri.Stream)
                {
                    Zipper.UnpackZIP(s, ToolsPath);
                }
            }

            //Удаляем старый лог-файл
            logFile = Path.Combine(AppPath, logFile);
            if (File.Exists(logFile))
            {
                try { File.Delete(logFile); }
                catch { }
            }

            //Инициализация процесса JRE
            string JavaLocation = GetJavaInstallationPath();
            if (string.IsNullOrEmpty(JavaLocation))
            {
                Log("Java Runtime Environment не найден. Завершение.");
                MessageBox.Show("Для работы приложения требуется Java Runtime Environment", "Java Runtime Environment", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else
            {
                Log(string.Format("Java Runtime Environment найден по пути \"{0}\"", JavaLocation));
                javaProc.StartInfo.FileName = Path.Combine(JavaLocation, "bin", "java.exe");
                javaProc.StartInfo.UseShellExecute = false;
                javaProc.StartInfo.CreateNoWindow = true;
                javaProc.StartInfo.RedirectStandardOutput = true;
            }

            signerFile = Path.Combine(ToolsPath, signerFile);
            if (!File.Exists(signerFile))
            {
                Log(string.Format(STR_FILE_NOT_EXIST, signerFile));
                MessageBox.Show(string.Format(STR_FILE_NOT_EXIST, signerFile), STR_FILE_NOT_EXIST_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //Инициализация процесса keytool
            string aapt = Path.Combine(ToolsPath, aaptFile);
            if (!File.Exists(aapt))
            {
                Log(string.Format(STR_FILE_NOT_EXIST, aaptFile));
                MessageBox.Show(string.Format(STR_FILE_NOT_EXIST, aaptFile), STR_FILE_NOT_EXIST_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                aaptProc.StartInfo.FileName = aapt;
                aaptProc.StartInfo.UseShellExecute = false;
                aaptProc.StartInfo.CreateNoWindow = true;
                aaptProc.StartInfo.RedirectStandardOutput = true;
            }

            //Инициализация зипалигнера
            alignerFile = Path.Combine(ToolsPath, alignerFile);
            if (!File.Exists(alignerFile))
            {
                Log(string.Format(STR_FILE_NOT_EXIST, alignerFile));
                MessageBox.Show(string.Format(STR_FILE_NOT_EXIST, alignerFile), STR_FILE_NOT_EXIST_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                zaProc.StartInfo.FileName = alignerFile;
                zaProc.StartInfo.UseShellExecute = false;
                zaProc.StartInfo.CreateNoWindow = true;
                zaProc.StartInfo.RedirectStandardOutput = true;
            }

            //Инициализация процесса keytool
            string ktFile = Path.Combine(JavaLocation, "bin", keytoolFile);
            if (!File.Exists(ktFile))
            {
                Log(string.Format(STR_FILE_NOT_EXIST, ktFile));
                MessageBox.Show(string.Format(STR_FILE_NOT_EXIST, ktFile), STR_FILE_NOT_EXIST_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                ktProc.StartInfo.FileName = ktFile;
                ktProc.StartInfo.UseShellExecute = false;
                ktProc.StartInfo.CreateNoWindow = true;
                ktProc.StartInfo.RedirectStandardOutput = true;
            }

            //Читаем все сертификаты, доступные в папке Tools
            foreach (string pemFile in Directory.GetFiles(ToolsPath, "*.pem"))
            {
                Log(string.Format("Добавляем сертификат \"{0}\"", pemFile));
                if (CertReader.ReadCert(pemFile, true) == null)
                    Log(string.Format("Сертификат не был добавлен\"{0}\". Отсутствует файл pk8.", pemFile));
            }

            //Читаем все сертификаты, доступные в папке с программой Tools
            foreach (string pemFile in Directory.GetFiles(AppPath, "*.pem"))
            {
                Log(string.Format("Добавляем сертификат \"{0}\"", pemFile));
                if (CertReader.ReadCert(pemFile, true) == null)
                    Log(string.Format("Сертификат не был добавлен\"{0}\". Отсутствует файл pk8.", pemFile));
            }

            return true;
        }

        /// <summary>
        /// Логирование в файл
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Исходный текст</returns>
        public static string Log(string text)
        {
            try { File.AppendAllText(logFile, string.Format("[{0}] {1}", DateTime.Now, text + System.Environment.NewLine)); }
            catch { }
            return text;
        }

        /// <summary>
        /// Получение пути инсталляции JRE из реестра или переменной окружения JAVA_HOME
        /// </summary>
        /// <returns>Путь до JRE</returns>
        public static string GetJavaInstallationPath()
        {
            string environmentPath = System.Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(environmentPath))
            {
                return environmentPath;
            }

            string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment\\";
            try
            {
                using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey))
                {
                    string currentVersion = rk.GetValue("CurrentVersion").ToString();
                    using (Microsoft.Win32.RegistryKey key = rk.OpenSubKey(currentVersion))
                    {
                        return key.GetValue("JavaHome").ToString();
                    }
                }
            }
            catch { return null; }
        }
    }
}
