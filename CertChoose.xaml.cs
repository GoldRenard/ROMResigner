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
using System.Windows;
using System.Windows.Controls;

namespace ROMResigner
{
    /// <summary>
    /// Окно выбора сертификатов
    /// </summary>
    public partial class CertChoose : Window
    {
        public new bool DialogResult = false;
        PackageReader pReader;

        /// <summary>
        /// Убирает иконку окна
        /// </summary>
        /// <param name="e">Аргументы события</param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            IconHelper.RemoveIcon(this);
            base.OnSourceInitialized(e);
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="pReader">Объект считывания пакетов</param>
        public CertChoose(PackageReader pReader)
        {
            this.pReader = pReader;
            InitializeComponent();

            cbnP.DataContext = pReader;
            cbnM.DataContext = pReader;
            cbnS.DataContext = pReader;
            cbnT.DataContext = pReader;
            cbcP.ItemsSource = CertReader.SignCerts;
            cbcM.ItemsSource = CertReader.SignCerts;
            cbcS.ItemsSource = CertReader.SignCerts;
            cbcT.ItemsSource = CertReader.SignCerts;

            //Определим сертификаты для замены
            foreach (CertInfo cInfo in CertReader.SignCerts)
            {
                switch (cInfo.Type)
                {
                    case CertInfo.CertType.platform:
                        {
                            cbcP.SelectedValue = cInfo;
                            break;
                        }
                    case CertInfo.CertType.media:
                        {
                            cbcM.SelectedValue = cInfo;
                            break;
                        }
                    case CertInfo.CertType.shared:
                        {
                            cbcS.SelectedValue = cInfo;
                            break;
                        }
                    case CertInfo.CertType.testkey:
                        {
                            cbcT.SelectedValue = cInfo;
                            break;
                        }
                }
            }

            //Определим носители исходного сертификата
            foreach (PackageInfo pInfo in pReader.Packages)
            {
                switch (pInfo.Name)
                {
                    case "Phone.apk":
                    case "framework-res.apk":
                        {
                            cbnP.SelectedValue = pInfo;
                            break;
                        }
                    case "DownloadProviderUi.apk":
                    case "DownloadProvider.apk":
                        {
                            cbnM.SelectedValue = pInfo;
                            break;
                        }
                    case "ApplicationsProvider.apk":
                        {
                            cbnS.SelectedValue = pInfo;
                            break;
                        }
                    case "HTMLViewer.apk":
                        {
                            cbnT.SelectedValue = pInfo;
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Событие нажатия кнопки выбора
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (cbnP.SelectedValue != null && cbnM.SelectedValue != null && cbnS.SelectedValue != null && cbnT.SelectedValue != null)
            {
                DialogResult = true;
                this.Hide();
            }
            else
                MessageBox.Show("Пожалуйста, выберите ключевые файлы", "Выберите файлы", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public CertInfo PlatformKey
        {
            get { return (CertInfo)cbcP.SelectedValue; }
        }

        public CertInfo MediaKey
        {
            get { return (CertInfo)cbcM.SelectedValue; }
        }

        public CertInfo SharedKey
        {
            get { return (CertInfo)cbcS.SelectedValue; }
        }

        public CertInfo TestKey
        {
            get { return (CertInfo)cbcT.SelectedValue; }
        }

        public PackageInfo PlatformPackage
        {
            get { return (PackageInfo)cbnP.SelectedValue; }
        }

        public PackageInfo MediaPackage
        {
            get { return (PackageInfo)cbnM.SelectedValue; }
        }

        public PackageInfo SharedPackage
        {
            get { return (PackageInfo)cbnS.SelectedValue; }
        }

        public PackageInfo TestPackage
        {
            get { return (PackageInfo)cbnT.SelectedValue; }
        }
    }
}
