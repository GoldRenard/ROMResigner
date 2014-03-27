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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Windows.Input;
using ROMResigner.Service;

namespace ROMResigner
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        bool IsZip = false;
        string ZipFile;
        string ZipRootPath;
        string RootPath;
        string[] ReadFiles;

        PackageReader pReader = new PackageReader();
        ComboBox TypeCB = null;

        BackgroundWorker bwReadSystem = new BackgroundWorker();
        BackgroundWorker bwReadFiles = new BackgroundWorker();
        BackgroundWorker bwSignFiles = new BackgroundWorker() { WorkerSupportsCancellation = true };
        BackgroundWorker bwUnZIP = new BackgroundWorker();

        System.Windows.Forms.FolderBrowserDialog SystemBrowser = new System.Windows.Forms.FolderBrowserDialog() { Description = "Выберите папку system прошивки", ShowNewFolderButton = false };
        System.Windows.Forms.OpenFileDialog FileBrowser = new System.Windows.Forms.OpenFileDialog() { Title = "Выберите файлы", Filter = "Файлы APK/JAR (*.apk, *.jar)|*.apk;*.jar", CheckFileExists = true, Multiselect = true };
        System.Windows.Forms.OpenFileDialog CertBrowser = new System.Windows.Forms.OpenFileDialog() { Title = "Выберите файл сертификата", Filter = "Файлы сертификата (*.pem)|*.pem", CheckFileExists = true };
        System.Windows.Forms.OpenFileDialog OpenZipBrowser = new System.Windows.Forms.OpenFileDialog() { Title = "Выберите ZIP-архив с прошивкой", Filter = "ZIP-файл (*.zip)|*.zip", CheckFileExists = true };
        System.Windows.Forms.SaveFileDialog SaveZipBrowser = new System.Windows.Forms.SaveFileDialog() { Title = "Выберите, куда необходимо сохранить подписанную прошивку", Filter = "ZIP-файл (*.zip)|*.zip" };

        public delegate void StatusDelegate(string fPath, int CurNum, int MaxNum, bool IsIndeterminate);
        const string STR_STATUS_DONE = "Готово";
        const string STR_STATUS_DO_HIND = "Вы можете назначить файлам новые сертификаты нажатием пинтограммы \"Определить сертификаты\".";
        const string STR_STATUS_OPEN = "Добавьте файлы в очередь, откройте ZIP-файл или папку с прошивкой";
        const string STR_STATUS_UNPACKING_FILE = "Извлекаем \"{0}\"...";
        const string STR_STATUS_PACKING_FILE = "Запаковываем \"{0}\"...";
        const string STR_STATUS_READING_FILE = "Чтение файла \"{0}\"...";
        const string STR_STATUS_SIGNING_FILE = "Подписываем \"{0}\"...";
        const string STR_STATUS_ALIGNING_FILE = "ZipAligning \"{0}\"...";
        const string STR_STATUS_COPYING_FILE = "Копируем в \"{0}\"...";

        const string STR_INLINE_SKIP = "Пропущено...";
        const string STR_INLINE_CLEANING = "Очистка...";
        const string STR_INLINE_DONE = "Готово!";

        const string STR_INLINE_SIGNING = "Подписываем...";
        const string STR_INLINE_SIGNING_ERROR = "Ошибка подписания";
        const string STR_INLINE_ALIGNING = "Zipalign...";
        const string STR_INLINE_ALIGNING_ERROR = "Ошибка Zipalign";
        const string STR_INLINE_COPYING = "Копируем...";
        const string STR_INLINE_COPYING_ERROR = "Ошибка копирования";

        const string STR_START_SIGNING_HK = "Начать обработку (CTRL+S)";
        const string STR_START_SIGNING = "Начать обработку";

        const string STR_STOP_SIGNING_HK = "Остановить обработку (CTRL+S)";
        const string STR_STOP_SIGNING = "Остановить обработку";

        ImageSource iFile, iFile_Gray;
        ImageSource iAddCert, iAddCert_Gray;
        ImageSource iDetectCert, iDetectCert_Gray;
        ImageSource iFolder, iFolder_Gray;
        ImageSource iZip, iZip_Gray;
        ImageSource iStart, iStart_Gray;
        ImageSource iStop, iStop_Gray;
        ImageSource iDelete, iDelete_Gray;
        ImageSource iCert, iCert_Gray;

        bool IsBlocked = false;

        #endregion

        #region Commands

        RelayCommand _openFilesCommand;
        public ICommand OpenFilesCommand
        {
            get
            {
                if (_openFilesCommand == null)
                    _openFilesCommand = new RelayCommand(p => this.OpenFiles_Click(p, null), p => OpenFilesMenu.IsEnabled);
                return _openFilesCommand;
            }
        }

        RelayCommand _openCommand;
        public ICommand OpenCommand
        {
            get
            {
                if (_openCommand == null)
                    _openCommand = new RelayCommand(p => this.OpenFolder_Click(p, null), p => FolderImage.IsEnabled);
                return _openCommand;
            }
        }

        RelayCommand _openZipCommand;
        public ICommand OpenZipCommand
        {
            get
            {
                if (_openZipCommand == null)
                    _openZipCommand = new RelayCommand(p => this.OpenZIP_Click(p, null), p => ZIPImage.IsEnabled);
                return _openZipCommand;
            }
        }

        RelayCommand _openCertCommand;
        public ICommand OpenCertCommand
        {
            get
            {
                if (_openCertCommand == null)
                    _openCertCommand = new RelayCommand(p => this.AddCert_Click(p, null), p => AddCertImage.IsEnabled);
                return _openCertCommand;
            }
        }

        RelayCommand _detCertsCommand;
        public ICommand DetectCertsCommand
        {
            get
            {
                if (_detCertsCommand == null)
                    _detCertsCommand = new RelayCommand(p => this.DetectCerts_Click(p, null), p => DetectCertImage.IsEnabled);
                return _detCertsCommand;
            }
        }

        RelayCommand _signCommand;
        public ICommand SignCommand
        {
            get
            {
                if (_signCommand == null)
                    _signCommand = new RelayCommand(p => this.Start_Click(p, null), p => StartImage.IsEnabled);
                return _signCommand;
            }
        }

        RelayCommand _aboutCommand;
        public ICommand AboutCommand
        {
            get
            {
                if (_aboutCommand == null)
                    _aboutCommand = new RelayCommand(p => this.AboutMenu_Click(p, null), p => true);
                return _aboutCommand;
            }
        }

        RelayCommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                if (_deleteCommand == null)
                    _deleteCommand = new RelayCommand(p => this.RemoveItem_Click(p, null), p => RemoveMenu.IsEnabled);
                return _deleteCommand;
            }
        }
        #endregion

        /// <summary>
        /// Конструктор. Инициализация ресурсов и обработчиков
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.Title = string.Format(this.Title, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            iFile = new BitmapImage(new Uri("pack://application:,,,/Images/files.png"));
            iFile_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/files_gray.png"));
            iAddCert = new BitmapImage(new Uri("pack://application:,,,/Images/add_cert.png"));
            iAddCert_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/add_cert_gray.png"));
            iDetectCert = new BitmapImage(new Uri("pack://application:,,,/Images/detect_certs.png"));
            iDetectCert_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/detect_certs_gray.png"));
            iFolder = new BitmapImage(new Uri("pack://application:,,,/Images/folder.png"));
            iFolder_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/folder_gray.png"));
            iZip = new BitmapImage(new Uri("pack://application:,,,/Images/zip.png"));
            iZip_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/zip_gray.png"));
            iStart = new BitmapImage(new Uri("pack://application:,,,/Images/start.png"));
            iStart_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/start_gray.png"));
            iStop = new BitmapImage(new Uri("pack://application:,,,/Images/stop.png"));
            iStop_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/stop_gray.png"));
            iDelete = new BitmapImage(new Uri("pack://application:,,,/Images/delete.png"));
            iDelete_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/delete_gray.png"));
            iCert = new BitmapImage(new Uri("pack://application:,,,/Images/cert.png"));
            iCert_Gray = new BitmapImage(new Uri("pack://application:,,,/Images/cert_gray.png"));

            FileImage.Source = iFile;
            FolderImage.Source = iFolder;
            ZIPImage.Source = iZip;
            AddCertImage.Source = iAddCert;
            DetectCertImage.Source = iDetectCert_Gray;
            StartImage.Source = iStart_Gray;

            ExitMenu.Icon = new System.Windows.Controls.Image { Source = new BitmapImage(new Uri("pack://application:,,,/Images/Exit.png")) };
            AboutMenu.Icon = new System.Windows.Controls.Image { Source = new BitmapImage(new Uri("pack://application:,,,/Images/About.png")) };
            OpenFilesMenu.Icon = new System.Windows.Controls.Image { Source = iFile };
            OpenSystemMenu.Icon = new System.Windows.Controls.Image { Source = iFolder };
            OpenZIPMenu.Icon = new System.Windows.Controls.Image { Source = iZip };
            AddCertMenu.Icon = new System.Windows.Controls.Image { Source = iAddCert };
            DetectCertsMenu.Icon = new System.Windows.Controls.Image { Source = iDetectCert_Gray };
            StartSignMenu.Icon = new System.Windows.Controls.Image { Source = iStart_Gray };
            RemoveMenu.Icon = new System.Windows.Controls.Image { Source = iDelete_Gray };
            ShowCertMenu.Icon = new System.Windows.Controls.Image { Source = iCert_Gray };

            SetStatus(STR_STATUS_OPEN, 0, 100, false);
            this.Closing += MainWindow_Closing;

            ZipSign.ItemsSource = CertReader.SignCerts;
            ZipSign.SelectedIndex = 0;

            bwReadSystem.DoWork += bwReadSystem_DoWork;
            bwSignFiles.DoWork += bwSignFiles_DoWork;
            bwUnZIP.DoWork += bwUnZIP_DoWork;
            bwReadFiles.DoWork += bwReadFiles_DoWork;

            pReader.FileReading += pReader_FileReading;
            pReader.Dispatcher = this.Dispatcher;
            PackagesLW.DataContext = pReader;

            this.InputBindings.Add(new KeyBinding() { Command = OpenFilesCommand, Gesture = new KeyGesture(Key.O, ModifierKeys.Control) });
            this.InputBindings.Add(new KeyBinding() { Command = OpenCommand, Gesture = new KeyGesture(Key.O, ModifierKeys.Alt | ModifierKeys.Control) });
            this.InputBindings.Add(new KeyBinding() { Command = OpenZipCommand, Gesture = new KeyGesture(Key.O, ModifierKeys.Shift | ModifierKeys.Control) });
            this.InputBindings.Add(new KeyBinding() { Command = DetectCertsCommand, Gesture = new KeyGesture(Key.E, ModifierKeys.Control) });
            this.InputBindings.Add(new KeyBinding() { Command = OpenCertCommand, Gesture = new KeyGesture(Key.E, ModifierKeys.Shift | ModifierKeys.Control) });
            this.InputBindings.Add(new KeyBinding() { Command = SignCommand, Gesture = new KeyGesture(Key.S, ModifierKeys.Control) });
            this.InputBindings.Add(new KeyBinding() { Command = AboutCommand, Gesture = new KeyGesture(Key.F1) });
            this.InputBindings.Add(new KeyBinding() { Command = DeleteCommand, Gesture = new KeyGesture(Key.Delete) });
        }

        #region Zipping/Unzipping
        /// <summary>
        /// Распаковка указанного архива с прошивкой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bwUnZIP_DoWork(object sender, DoWorkEventArgs e)
        {
            Block(true);
            ZipRootPath = Path.Combine(Environment.TempPath, Path.GetFileNameWithoutExtension(ZipFile));
            Environment.Log(string.Format("Открыт ZIP \"{0}\". Распаковка...", ZipFile));
            if (UnpackZIP(ZipFile, ZipRootPath))
                LoadSystem(Path.Combine(ZipRootPath, "system"));
            else
                Block(false);
        }

        /// <summary>
        /// Распаковывает указанный архив в указанную директорию
        /// </summary>
        /// <param name="fZip">Путь до архива</param>
        /// <param name="Destination">Директория назначения</param>
        /// <returns></returns>
        private bool UnpackZIP(string fZip, string Destination)
        {
            //Удаляем старую директорию
            try
            {
                if (Directory.Exists(Destination))
                    Directory.Delete(Destination, true);
            }
            catch (Exception ex)
            {
                string Err = string.Format("Обнаружена директория \"{0}\", однако попытка удаления завершилась неудачей. Ошибка: {1}", Destination, ex.Message);
                Environment.Log(Err);
                MessageBox.Show(Err, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            ZipFile zf = null;
            FileStream fs = null;

            bool IsSuccess = true;

            try
            {
                //Открываем файл и распаковываем
                fs = File.OpenRead(fZip);
                zf = new ZipFile(fs);

                int z_num = 1;
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
                    SetStatus(string.Format(STR_STATUS_UNPACKING_FILE, fullZipToPath), z_num, (int)zf.Count, false);
                    z_num++;
                }
            }
            catch (Exception ex)
            {
                string Err = string.Format("Не удалось распаковать ZIP \"{0}\". Ошибка: {1}", ZipFile, ex.Message);
                Environment.Log(Err);
                MessageBox.Show(Err, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                IsSuccess = false;
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true;
                    zf.Close();
                }
                if (fs != null)
                    fs.Close();
            }
            return IsSuccess;
        }

        /// <summary>
        /// Запаковывает указанную папку в указанный ZIP-архив
        /// </summary>
        /// <param name="RootDir">Корневая папка для запаковки</param>
        /// <param name="DectinationZip">Путь, где будет создан новый зип-файл</param>
        /// <returns></returns>
        private bool PackZIP(string RootDir, string DestinationZip)
        {
            FileStream fsOut = File.Create(DestinationZip);
            ZipOutputStream zipStream = new ZipOutputStream(fsOut);
            zipStream.SetLevel(3);
            int folderOffset = RootDir.Length + (RootDir.EndsWith("\\") ? 0 : 1);
            FileNum = 0;
            FileCount = Directory.GetFiles(RootDir, "*.*", SearchOption.AllDirectories).Length;
            CompressFolder(RootDir, zipStream, folderOffset);
            zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
            zipStream.Close();


            if (bwSignFiles.CancellationPending)
            {
                try { File.Delete(DestinationZip); }
                catch { }
            }

            return true;
        }

        int FileNum = 0;
        int FileCount = 0;

        /// <summary>
        /// Рекурсивно сжимает файлы из указанной директории, отправляя в выходной поток
        /// </summary>
        /// <param name="path">Путь до директории</param>
        /// <param name="zipStream">Выходной поток</param>
        /// <param name="folderOffset">Смещение пути</param>
        private bool CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            if (bwSignFiles.CancellationPending)
                return false;
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                if (bwSignFiles.CancellationPending)
                    return false;
                SetStatus(string.Format(STR_STATUS_PACKING_FILE, filename), FileNum, FileCount, false);
                FileInfo fi = new FileInfo(filename);
                string entryName = filename.Substring(folderOffset); //Делает имя в зипе основанным на папке
                entryName = ZipEntry.CleanName(entryName);           //Убирает букву диска из имени и фиксит слеши
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime;
                zipStream.UseZip64 = UseZip64.Off;
                newEntry.Size = fi.Length;
                zipStream.PutNextEntry(newEntry);
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                zipStream.CloseEntry();
                FileNum++;
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                if (!CompressFolder(folder, zipStream, folderOffset))
                    return false;
            }
            return true;
        }

        #endregion

        #region Load Section
        /// <summary>
        /// Очистка всех списков
        /// </summary>
        private void UnloadAll()
        {
            if (IsZip && !string.IsNullOrEmpty(ZipRootPath))
            {
                if (Directory.Exists(ZipRootPath))
                {
                    try { Directory.Delete(ZipRootPath, true); }
                    catch { }
                }
            }
            Environment.Log("Очищаем все списки...");
            ZipSign.SelectedIndex = 0;
            IsZip = false;
            ZipFile = null;
            ZipRootPath = null;
            RootPath = null;
            CertReader.Certs.Clear();
            pReader.Packages.Clear();
            PackagesLW.AllowDrop = true;
        }

        /// <summary>
        /// Загрузка файлов из указанной директории system
        /// </summary>
        /// <param name="path">Путь к папке c прошивкой</param>
        /// <returns>Правильность указанной директории</returns>
        private bool LoadSystem(string path)
        {
            Environment.Log(string.Format("Была выбрана папка \"{0}\"", path));
            RootPath = path;
            bwReadSystem.RunWorkerAsync();
            return false;
        }

        /// <summary>
        /// Загрузка файлов из массива
        /// </summary>
        /// <param name="files">Массив файлов</param>
        private void LoadFiles(string[] files)
        {
            Environment.Log("Были дропнуты файлы в окно");
            List<string> lFiles = new List<string>();

            foreach (string file in files)
                if (Path.GetExtension(file) == ".apk" || Path.GetExtension(file) == ".jar")
                    lFiles.Add(file);

            if (lFiles.Count > 0)
            {
                ReadFiles = lFiles.ToArray();
                bwReadFiles.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Обработчик чтения файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bwReadSystem_DoWork(object sender, DoWorkEventArgs e)
        {
            Block(true);
            pReader.ReadFolder(RootPath, "*.apk|*.jar");
            Block(false);
            SetStatus(STR_STATUS_DO_HIND, 0, 100, false);
        }

        /// <summary>
        /// Обработчик чтения файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bwReadFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            Block(true);
            pReader.ReadFiles(ReadFiles);
            Block(false);
            SetStatus(STR_STATUS_DO_HIND, 0, 100, false);
        }
        #endregion

        #region Signing
        /// <summary>
        /// Основная процедура подписывания файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bwSignFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            //Блокируем интерфейс
            Block(true);
            StartButtonMode(true);
            int ErrorCounter = 0;

            int FileNum = 0;
            int FileCount = 0;
            foreach (PackageInfo pInfo in pReader.Packages)
                if (pInfo.NewCert.CanSign)
                    FileCount++;

            foreach (PackageInfo pInfo in pReader.Packages)
            {
                // ==================================================================================

                //Показываем статусную строку и скроллим к итему
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                {
                    PackagesLW.ScrollIntoView(pInfo);
                    pInfo.IsComboBoxVisible = false;
                }));

                //Пропускаем файлы с явно неуказанными сертификатами
                if (!pInfo.NewCert.CanSign)
                {
                    Environment.Log(string.Format("У файла \"{0}\" не выбран пригодный для подписи сертификат. Пропуск...", pInfo.Name));
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { pInfo.StatusText = STR_INLINE_SKIP; }));
                    continue;
                }

                // ==================================================================================

                //Устанавливаем статус подписи
                pInfo.StatusText = STR_INLINE_SIGNING;
                SetStatus(string.Format(STR_STATUS_SIGNING_FILE, pInfo.Path), FileNum, FileCount, false);
                //Подписываем
                string SignedFile = Signer.SignFile(pInfo);

                //Если нулл или пустая - фейл
                if (string.IsNullOrEmpty(SignedFile))
                {
                    ErrorCounter++;
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { pInfo.StatusText = STR_INLINE_SIGNING_ERROR; }));
                    continue;
                }

                //Также если файла нет - фейл
                if (!File.Exists(SignedFile))
                {
                    ErrorCounter++;
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { pInfo.StatusText = STR_INLINE_SIGNING_ERROR; }));
                    continue;
                }

                // ==================================================================================

                //Устанавливаем статус алигнинга
                pInfo.StatusText = STR_INLINE_ALIGNING;
                SetStatus(string.Format(STR_STATUS_ALIGNING_FILE, pInfo.Path), FileNum, FileCount, false);
                //Зипалигним
                string SignedAlignedFile = Aligner.AlignFile(SignedFile);

                //Если нулл или пустая - фейл
                if (string.IsNullOrEmpty(SignedAlignedFile))
                {
                    ErrorCounter++;
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { pInfo.StatusText = STR_INLINE_ALIGNING_ERROR; }));
                    continue;
                }

                //Также если файла нет - фейл
                if (!File.Exists(SignedAlignedFile))
                {
                    ErrorCounter++;
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { pInfo.StatusText = STR_INLINE_ALIGNING_ERROR; }));
                    continue;
                }

                // ==================================================================================

                //Устанавливаем статус копирования
                pInfo.StatusText = STR_INLINE_COPYING;
                SetStatus(string.Format(STR_STATUS_COPYING_FILE, pInfo.Path), FileNum, FileCount, false);

                Environment.Log(string.Format("Копируем временный файл \"{0}\" на исходное место \"{1}\"...", SignedAlignedFile, pInfo.Path));
                //Копируем на место
                try { File.Copy(SignedAlignedFile, pInfo.Path, true); }
                catch (Exception ex)
                {
                    ErrorCounter++;
                    Environment.Log(string.Format("Не удается скопировать файл \"{0}\" Ошибка: {1}", SignedAlignedFile, ex.Message));
                }

                // ==================================================================================

                //Устанавливаем статус очистки
                pInfo.StatusText = STR_INLINE_CLEANING;

                //удаляем мусор
                try
                {
                    File.Delete(SignedFile);
                    File.Delete(SignedAlignedFile);
                }
                catch (Exception ex) { Environment.Log(string.Format("Не удается удалить временные файлы. Ошибка: {0}", ex.Message)); }

                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { pInfo.StatusText = STR_INLINE_DONE; }));
                FileNum++;

                if (bwSignFiles.CancellationPending)
                {
                    Environment.Log("Вызвана остановка операции");
                    break;
                }
            }

            if (ErrorCounter > 0)
            {
                if (File.Exists(Environment.logFile))
                {
                    if (MessageBox.Show("Обработка некоторых файлов потерпела неудачу. Хотите посмотреть логи?", "Обработка некоторых файлов потерпела неудачу", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                        {
                            CertInfoWindow ErrorInfo = new CertInfoWindow(File.ReadAllText(Environment.logFile), "Лог");
                            ErrorInfo.Show();
                        }));
                    }
                }
            }
            else if (IsZip && !bwSignFiles.CancellationPending) //Если указан зип и не сказано остановиться - пакуемся
            {

                //Спрашиваем куда сохранить
                bool result = false;
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() {
                    SaveZipBrowser.FileName = Path.GetFileNameWithoutExtension(ZipFile) + "_signed";
                    result = SaveZipBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                }));
                if (result)
                {
                    //Получаем выбранный сертификат
                    CertInfo SignCert = new CertInfo();
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { SignCert = ((CertInfo)ZipSign.SelectedValue); }));
                    //И смотри требуется ли подпись
                    if (SignCert.CanSign)
                    {
                        //Если нужно, запаковываем сперва во временную папку
                        string tempZip = Path.Combine(Environment.TempPath, Path.GetFileNameWithoutExtension(SaveZipBrowser.FileName) + "_zipunsigned.zip");
                        PackZIP(ZipRootPath, tempZip);
                        //Создаем экземпляр пакета
                        PackageInfo zipPackage = new PackageInfo();
                        zipPackage.Path = tempZip;
                        zipPackage.IsChangeAllowed = true;
                        zipPackage.NewCert = SignCert;

                        //Устанавливаем статус подписи
                        SetStatus(string.Format(STR_STATUS_SIGNING_FILE, SaveZipBrowser.FileName), 100, 100, true);
                        //Подписываем пакет уже по указанному ранее пути
                        Signer.SignFile(zipPackage, SaveZipBrowser.FileName);

                        //Устанавливаем статус очистки
                        SetStatus(STR_INLINE_CLEANING, 100, 100, true);
                        //Удаляем мусор
                        try { File.Delete(tempZip); }
                        catch { }
                    }
                    else //Если подписывать не нужно, сразу запаковываем по указанному пути
                        PackZIP(ZipRootPath, SaveZipBrowser.FileName);
                }
            }

            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { pReader.ShowComboBoxes(); }));
            SetStatus(STR_STATUS_DONE, 0, 100, false);
            Block(false);
            StartButtonMode(false);
        }
        #endregion

        #region Interface

        #region Sorting
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private void PackagesLWGridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                        direction = ListSortDirection.Ascending;
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                            direction = ListSortDirection.Descending;
                        else
                            direction = ListSortDirection.Ascending;
                    }

                    string header = headerClicked.Column.Header as string;
                    Sort(header, direction);
                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            switch (sortBy)
            {
                case "Файл":
                    {
                        pReader.Sort(p => p.Name, direction);
                        break;
                    }
                case "Сертификат":
                    {
                        pReader.Sort(p => p.NewCert.SerialNumber, direction);
                        break;
                    }
                case "Информация":
                    {
                        pReader.Sort(p => p.NewCert.Info, direction);
                        break;
                    }
            }
        }
        #endregion

        /// <summary>
        /// Процедура показа информации о сертификате
        /// </summary>
        /// <param name="pInfo">Пакет</param>
        private void ShowCertInfo(PackageInfo pInfo)
        {
            if (pInfo.Cert != null)
            {
                CertInfoWindow cInfoWnd = new CertInfoWindow(pInfo.Cert.FullInfo, "Информация о сертификате");
                cInfoWnd.Show();
            }
        }

        /// <summary>
        /// Обновление статуса по извлечению
        /// </summary>
        /// <param name="fPath">Путь до файла</param>
        /// <param name="CurNum">Текущий файл</param>
        /// <param name="MaxNum">Максимальное количество файлов</param>
        void pReader_FileReading(string fPath, int CurNum, int MaxNum)
        {
            SetStatus(string.Format(STR_STATUS_READING_FILE, fPath), CurNum, MaxNum, false);
        }

        /// <summary>
        /// Обработчик открытия информации о сертификате по дабл-клику файла в списке
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PackagesLW_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PackagesLW.SelectedItem != null)
                ShowCertInfo(((PackageInfo)PackagesLW.SelectedItem));
        }

        /// <summary>
        /// Установка данных в статусной строке
        /// </summary>
        /// <param name="Text">Текст статуса</param>
        /// <param name="PBValue">Значение прогресс-бара</param>
        /// <param name="PBMaximum">Максимальное значение прогресс-бара</param>
        private void SetStatus(string Text, int PBValue, int PBMaximum, bool IsIndeterminate)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new StatusDelegate((_Text, _Value, _Maximum, _IsIndeterminate) =>
            {
                ProgressBar.Maximum = _Maximum;
                if (_Value > _Maximum)
                    _Value = _Maximum;
                ProgressBar.Value = _Value;
                ProgressBar.IsIndeterminate = _IsIndeterminate;
                StatusText.Text = _Text;
            }), Text, PBValue, PBMaximum, IsIndeterminate);
        }

        /// <summary>
        /// Обработчик выбора файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFiles_Click(object sender, RoutedEventArgs e)
        {
            if (FileBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UnloadAll();
                LoadFiles(FileBrowser.FileNames);
            }
        }

        /// <summary>
        /// Обработчик выбора папки с прошивкой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (SystemBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UnloadAll();
                LoadSystem(SystemBrowser.SelectedPath);
            }
        }

        /// <summary>
        /// Обработчик открытия зип-файла с прошивкой 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenZIP_Click(object sender, RoutedEventArgs e)
        {
            if (OpenZipBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UnloadAll();
                IsZip = true;
                ZipFile = OpenZipBrowser.FileName;
                bwUnZIP.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Обработчик добавления нового сертификата
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddCert_Click(object sender, RoutedEventArgs e)
        {
            if (CertBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (CertReader.ReadCert(CertBrowser.FileName, true) == null)
                    MessageBox.Show("Сертификат не был добавлен. Возможно, рядом с ним не обнаружен *.pk8", "Сертификат не был добавлен", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                else
                    pReader.CertCollectionChanged();
            }
        }

        /// <summary>
        /// Вызывает диалог назначение AOSP сертификатов файлам
        /// </summary>
        /// <param name="sender">Объект-отправитель</param>
        /// <param name="e">Информация о состоянии и данные события</param>
        private void DetectCerts_Click(object sender, RoutedEventArgs e)
        {

            CertChoose cChooser = new CertChoose(pReader);
            cChooser.ShowDialog();
            if (cChooser.DialogResult)
            {
                foreach (PackageInfo pInfo in pReader.Packages)
                {
                    if (pInfo.Cert == null)
                        continue;
                    if (pInfo.Cert.SerialNumber == cChooser.PlatformPackage.Cert.SerialNumber)
                        pInfo.NewCert = cChooser.PlatformKey;
                    if (pInfo.Cert.SerialNumber == cChooser.MediaPackage.Cert.SerialNumber)
                        pInfo.NewCert = cChooser.MediaKey;
                    if (pInfo.Cert.SerialNumber == cChooser.SharedPackage.Cert.SerialNumber)
                        pInfo.NewCert = cChooser.SharedKey;
                    if (pInfo.Cert.SerialNumber == cChooser.TestPackage.Cert.SerialNumber)
                        pInfo.NewCert = cChooser.TestKey;
                }
            }
        }

        /// <summary>
        /// Обработчик старта подписывания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (bwSignFiles.IsBusy)
                bwSignFiles.CancelAsync();
            else
                bwSignFiles.RunWorkerAsync();
        }

        /// <summary>
        /// Решим кнопки "старт"
        /// </summary>
        /// <param name="IsStop">В режиме "стоп" ли кнопка</param>
        private void StartButtonMode(bool IsStop)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                StartSign.IsEnabled = true;
                StartSignMenu.IsEnabled = true;
                StartImage.Source = IsStop ? iStop : iStart;
                StartSignMenu.Icon = new Image() { Source = IsStop ? iStop : iStart };
                StartSign.ToolTip = IsStop ? STR_STOP_SIGNING_HK : STR_START_SIGNING_HK;
                StartSignMenu.Header = IsStop ? STR_STOP_SIGNING : STR_START_SIGNING;
            }));
        }

        /// <summary>
        /// (Раз)Блокирование интерфейса
        /// </summary>
        /// <param name="IsBlocked">Указывает, необходимо заблокировать (true) или разблокировать (false)</param>
        private void Block(bool IsBlocked)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                pReader.CertChanging(!IsBlocked);

                OpenFiles.IsEnabled = OpenFilesMenu.IsEnabled = !IsBlocked;
                OpenSystem.IsEnabled = OpenSystemMenu.IsEnabled = !IsBlocked;
                OpenZIP.IsEnabled = OpenZIPMenu.IsEnabled = !IsBlocked;
                DetectCerts.IsEnabled = DetectCertsMenu.IsEnabled = !IsBlocked;
                StartSign.IsEnabled = StartSignMenu.IsEnabled = !IsBlocked;
                PackagesLW.AllowDrop = !IsBlocked;
                RemoveMenu.IsEnabled = !IsBlocked;

                if (TypeCB != null)
                    TypeCB.IsEnabled = !IsBlocked;

                if (IsBlocked) {
                    ZipSign.IsEnabled = false;
                } else {
                    if (IsZip)
                    {
                        ZipSign.IsEnabled = true;
                        PackagesLW.AllowDrop = false;
                        RemoveMenu.IsEnabled = false;
                    }
                }

                RemoveMenu.Icon = new System.Windows.Controls.Image { Source = RemoveMenu.IsEnabled ? iDelete : iDelete_Gray };
                OpenFilesMenu.Icon = new System.Windows.Controls.Image { Source = IsBlocked ? iFile_Gray : iFile };
                OpenSystemMenu.Icon = new System.Windows.Controls.Image { Source = IsBlocked ? iFolder_Gray : iFolder };
                OpenZIPMenu.Icon = new System.Windows.Controls.Image { Source = IsBlocked ? iZip_Gray : iZip };
                DetectCertsMenu.Icon = new System.Windows.Controls.Image { Source =  IsBlocked ? iDetectCert_Gray : iDetectCert };
                StartSignMenu.Icon = new System.Windows.Controls.Image { Source = IsBlocked ? iStart_Gray : iStart };

                FileImage.Source = IsBlocked ? iFile_Gray : iFile;
                FolderImage.Source = IsBlocked ? iFolder_Gray : iFolder;
                ZIPImage.Source = IsBlocked ? iZip_Gray : iZip;
                DetectCertImage.Source = IsBlocked ? iDetectCert_Gray : iDetectCert;
                StartImage.Source = IsBlocked ? iStart_Gray : iStart;

                if (IsBlocked)
                    Environment.Log("Интерфейс заблокирован");
                else
                    Environment.Log("Интерфейс разброкирован");

                this.IsBlocked = IsBlocked;
            }));
        }

        /// <summary>
        /// Закрытие программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitMenu_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Процедура закрытия программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                e.Cancel = true;
            else
            {
                UnloadAll();
                try { Directory.Delete(Environment.TempPath, true); }
                catch { }
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Процедура обработки Drag'n'Drop файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PackagesLW_Drop_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                LoadFiles((string[])e.Data.GetData(DataFormats.FileDrop));
        }

        /// <summary>
        /// Обработка открытия окна "О программе"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            About.AboutBox about = new About.AboutBox(this);
            about.ShowDialog();
        }

        /// <summary>
        /// Обработка типа вывода списка
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListType_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (TypeCB == null)
                return;
            switch (TypeCB.SelectedIndex)
            {
                case 0:
                    pReader.PackagesType = PackageReader.ListType.ALL;
                    break;
                case 1:
                    pReader.PackagesType = PackageReader.ListType.APK;
                    break;
                case 2:
                    pReader.PackagesType = PackageReader.ListType.JAR;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Получение объекта комбобокса типа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListType_Loaded_1(object sender, RoutedEventArgs e)
        {
            TypeCB = (ComboBox)sender;
        }

        /// <summary>
        /// Обработчик контекстного меню удаления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            //Это зип, мы не можем ничего удалять.
            if (IsZip)
                return;

            //Собираем список на удаление
            List<PackageInfo> pList = new List<PackageInfo>();
            foreach (PackageInfo pInfo in PackagesLW.SelectedItems)
                pList.Add(pInfo);

            //Удаляем
            foreach (PackageInfo pInfo in pList)
                pReader.Remove(pInfo);

        }

        /// <summary>
        /// Обработчик контекстного меню показа сертификата
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowCertMenu_Click(object sender, RoutedEventArgs e)
        {
            foreach (PackageInfo pInfo in PackagesLW.SelectedItems)
                ShowCertInfo(pInfo);
        }

        /// <summary>
        /// Обработчик изменения выделенного в списке
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PackagesLW_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            //Удалять всегда запрещено, пока интерфейс заблокирован или это ZIP
            RemoveMenu.IsEnabled = IsBlocked || IsZip ? false : PackagesLW.SelectedIndex != -1;
            ShowCertMenu.IsEnabled = PackagesLW.SelectedIndex != -1; 

            RemoveMenu.Icon = new System.Windows.Controls.Image { Source = RemoveMenu.IsEnabled ? iDelete : iDelete_Gray };
            ShowCertMenu.Icon = new System.Windows.Controls.Image { Source = ShowCertMenu.IsEnabled ? iCert : iCert_Gray};
        }
        #endregion
    }
    
    #region FixedWidthColumn
    /// <summary>
    /// Класс выравнивания столбцов
    /// </summary>
    public class FixedWidthColumn : GridViewColumn
    {
        static FixedWidthColumn()
        {
            WidthProperty.OverrideMetadata(typeof(FixedWidthColumn),
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceWidth)));
        }

        private static object OnCoerceWidth(DependencyObject o, object baseValue)
        {
            FixedWidthColumn fwc = o as FixedWidthColumn;
            if (fwc != null)
                return fwc.FixedWidth;
            return baseValue;
        }

        public double FixedWidth
        {
            get { return (double)GetValue(FixedWidthProperty); }
            set { SetValue(FixedWidthProperty, value); }
        }

        public static readonly DependencyProperty FixedWidthProperty =
            DependencyProperty.Register(
                "FixedWidth",
                typeof(double),
                typeof(FixedWidthColumn),
                new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnFixedWidthChanged)));

        private static void OnFixedWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            FixedWidthColumn fwc = o as FixedWidthColumn;
            if (fwc != null)
                fwc.CoerceValue(WidthProperty);
        }
    }
    #endregion
}
