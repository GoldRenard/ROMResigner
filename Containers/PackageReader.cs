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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace ROMResigner
{
    /// <summary>
    /// Класс чтения пакета
    /// </summary>
    public class PackageReader : INotifyPropertyChanged
    {
        public enum ListType
        {
            ALL,
            APK,
            JAR
        }

        public System.Windows.Threading.Dispatcher Dispatcher;
        public delegate void PackageInfoDelegate(PackageInfo pInfo);

        static ImageSource ApkIcon = new BitmapImage(new Uri("pack://application:,,,/Images/apk_file.png"));
        static ImageSource JarIcon = new BitmapImage(new Uri("pack://application:,,,/Images/jar_file.png"));

        /// <summary>
        /// Список считанных пакетов
        /// </summary>
        private ObservableCollection<PackageInfo> _ApkPackages = new ObservableCollection<PackageInfo>();
        private ObservableCollection<PackageInfo> _JarPackages = new ObservableCollection<PackageInfo>();
        private ObservableCollection<PackageInfo> _Packages = new ObservableCollection<PackageInfo>();
        public ObservableCollection<PackageInfo> Packages {
            get {
                switch (PackagesType)
                {
                    case ListType.ALL:
                        return _Packages;
                    case ListType.APK:
                        return _ApkPackages;
                    case ListType.JAR:
                        return _JarPackages;
                    default:
                        return null;
                }
            }
        }

        private ListType _Type = ListType.ALL;
        public ListType PackagesType
        {
            set
            {
                _Type = value;
                OnPackagesChanged();
            }

            get { return _Type; }
        }

        /// <summary>
        /// Список считанных пакетов, которые ИМЕЮТ ПОДПИСЬ
        /// </summary>
        private ObservableCollection<PackageInfo> _SignedPackages = null;
        public ObservableCollection<PackageInfo> SignedPackages
        {
            get
            {
                if (_SignedPackages == null)
                {
                    _SignedPackages = new ObservableCollection<PackageInfo>();
                    foreach (PackageInfo pInfo in Packages)
                        if (pInfo.Cert != null)
                            _SignedPackages.Add(pInfo);
                }
                return _SignedPackages;
            }
        }

        /// <summary>
        /// Отправка событий об изменении коллекции
        /// </summary>
        public void OnPackagesChanged()
        {
            _SignedPackages = null;
            NotifyPropertyChanged("Packages");
            NotifyPropertyChanged("SignedPackages");
        }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        public PackageReader()
        {
        }

        /// <summary>
        /// Удаляет указанные пакеты из всех коллекций
        /// </summary>
        /// <param name="pInfo">Пакет</param>
        public void Remove(PackageInfo pInfo)
        {
            _Packages.Remove(pInfo);
            _ApkPackages.Remove(pInfo);
            _JarPackages.Remove(pInfo);
            OnPackagesChanged();
        }

        /// <summary>
        /// Считывает информацию о файле и его сертификате
        /// </summary>
        /// <param name="fPath">Путь до файла</param>
        /// <returns>Структура информации о пакете</returns>
        public PackageInfo ReadFile(string fPath)
        {
            Environment.Log(string.Format("Читаем файл \"{0}\"...", fPath));
            if (!File.Exists(fPath))
            {
                Environment.Log("Файл не существует");
                return null;
            }

            //Ищем пакет в коллекции
            PackageInfo pInfoStored = _Packages.FirstOrDefault(p => p.Path == fPath);
            if (pInfoStored == null)
            {
                PackageInfo pInfo = new PackageInfo();
                pInfo.Path = fPath;
                pInfo.Name = Path.GetFileName(fPath);
                pInfo.Cert = CertReader.ReadZip(fPath);
                pInfo.Icon = IconHelper.GetApkIcon(fPath);

                if (pInfo.Icon == null)
                {
                    if (Path.GetExtension(fPath) == ".apk")
                    {
                        pInfo.Icon = ApkIcon;
                        pInfo.Type = PackageInfo.FileType.APK;
                    }
                    else if (Path.GetExtension(fPath) == ".jar")
                    {
                        pInfo.Icon = JarIcon;
                        pInfo.Type = PackageInfo.FileType.JAR;
                    }
                }

                if (Dispatcher != null && !Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new PackageInfoDelegate((_pInfo) =>
                    {
                        if (_pInfo.Type == PackageInfo.FileType.APK)
                            _ApkPackages.Add(_pInfo);
                        else if (_pInfo.Type == PackageInfo.FileType.JAR)
                            _JarPackages.Add(_pInfo);
                        _Packages.Add(_pInfo);
                        OnPackagesChanged();
                    }), pInfo);
                }
                else
                {
                    if (pInfo.Type == PackageInfo.FileType.APK)
                        _ApkPackages.Add(pInfo);
                    else if (pInfo.Type == PackageInfo.FileType.JAR)
                        _JarPackages.Add(pInfo);
                    _Packages.Add(pInfo);
                    OnPackagesChanged();
                }

                return pInfo;
            }
            else
                return pInfoStored;
        }

        /// <summary>
        /// Считывает все файлы из массива строк
        /// </summary>
        /// <param name="fPath">Путь до папки с файлами</param>
        /// <param name="Filter">Фильтр поиска файлов</param>
        /// <returns></returns>
        public List<PackageInfo> ReadFiles(string[] sFiles)
        {
            Environment.Log("Вызвана процедура чтения файлов из массива файлов");

            List<PackageInfo> lPackages = new List<PackageInfo>();
            for (int i = 0; i < sFiles.Length; i++)
            {
                OnFileReading(sFiles[i], i, sFiles.Length);
                PackageInfo pInfo = ReadFile(sFiles[i]);
                if (pInfo != null)
                    lPackages.Add(pInfo);
            }
            return lPackages;
        }

        /// <summary>
        /// Считывает все файлы из указанной папки и сохраняет информацию о них
        /// </summary>
        /// <param name="fPath">Путь до папки с файлами</param>
        /// <param name="Filter">Фильтр поиска файлов</param>
        /// <returns></returns>
        public List<PackageInfo> ReadFolder(string fPath, string Filter)
        {
            Environment.Log(string.Format("Вызвана процедура чтения файлов из папки \"{0}\"", fPath));

            List<PackageInfo> lPackages = new List<PackageInfo>();
            string[] sFiles = getFiles(fPath, Filter, SearchOption.AllDirectories);
            for (int i = 0; i < sFiles.Length; i++)
            {
                OnFileReading(sFiles[i], i, sFiles.Length);
                PackageInfo pInfo = ReadFile(sFiles[i]);
                if (pInfo != null)
                    lPackages.Add(pInfo);
            }
            return lPackages;
        }

        #region Прочее

        public delegate void FileReadingHandler(string fPath, int CurNum, int MaxNum);
        public event FileReadingHandler FileReading;
        protected void OnFileReading(string fPath, int CurNum, int MaxNum)
        {
            if (FileReading != null)
                FileReading(fPath, CurNum, MaxNum);
        }

        /// <summary>
        /// Возвращает имена файлов указанного каталога с множественным фильтром
        /// </summary>
        /// <param name="SourceFolder">Папка с файлами</param>
        /// <param name="Filter">Фильтры, разделенные символом |</param>
        /// <param name="searchOption">File.IO.SearchOption, 
        /// может быть AllDirectories или TopDirectoryOnly</param>
        /// <returns>Массив строк путей файлов директории</returns>
        static string[] getFiles(string SourceFolder, string Filter, SearchOption searchOption)
        {
            ArrayList alFiles = new ArrayList();
            string[] MultipleFilters = Filter.Split('|');
            foreach (string FileFilter in MultipleFilters)
                alFiles.AddRange(Directory.GetFiles(SourceFolder, FileFilter, searchOption));
            return (string[])alFiles.ToArray(typeof(string));
        }

        /// <summary>
        /// Уведомляет об изменении коллекции новых сертификатов
        /// </summary>
        public void CertCollectionChanged()
        {
            foreach (PackageInfo pInfo in Packages)
                pInfo.CallCollectionChanged();
        }

        /// <summary>
        /// Указывает, разрешена ли сортировка
        /// </summary>
        bool IsSortingAllowed = false;

        /// <summary>
        /// Разрешает или запрещает изменение нового сертификата
        /// </summary>
        /// <param name="IsAllowed">Запретить или разрешить</param>
        public void CertChanging(bool IsAllowed)
        {
            IsSortingAllowed = IsAllowed;
            foreach (PackageInfo pInfo in Packages)
                pInfo.IsChangeAllowed = IsAllowed;
        }

        /// <summary>
        /// Разрешает или запрещает изменение нового сертификата
        /// </summary>
        /// <param name="IsAllowed">Запретить или разрешить</param>
        public void ShowComboBoxes()
        {
            foreach (PackageInfo pInfo in Packages)
                pInfo.IsComboBoxVisible = true; ;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Sort<TType>(Func<PackageInfo, TType> keySelector, ListSortDirection direction)
        {
            if (!IsSortingAllowed)
                return;
            List<PackageInfo> sortedList;
            switch (_Type)
            {
                case ListType.ALL:
                    {
                        if (direction == ListSortDirection.Ascending)
                            sortedList = _Packages.OrderBy(keySelector).ToList();
                        else
                            sortedList = _Packages.OrderByDescending(keySelector).ToList();
                        _Packages.Clear();
                        foreach (var sortedItem in sortedList)
                            _Packages.Add(sortedItem);
                        break;
                    }
                case ListType.APK:
                    {
                        if (direction == ListSortDirection.Ascending)
                            sortedList = _ApkPackages.OrderBy(keySelector).ToList();
                        else
                            sortedList = _ApkPackages.OrderByDescending(keySelector).ToList();
                        _ApkPackages.Clear();
                        foreach (var sortedItem in sortedList)
                            _ApkPackages.Add(sortedItem);
                        break;
                    }
                case ListType.JAR:
                    {
                        if (direction == ListSortDirection.Ascending)
                            sortedList = _JarPackages.OrderBy(keySelector).ToList();
                        else
                            sortedList = _JarPackages.OrderByDescending(keySelector).ToList();
                        _JarPackages.Clear();
                        foreach (var sortedItem in sortedList)
                            _JarPackages.Add(sortedItem);
                        break;
                    }

                default:
                    break;
            }

        }

        #endregion
    }
}
