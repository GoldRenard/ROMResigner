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
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace ROMResigner
{
    /// <summary>
    /// Контейнер информации пакета
    /// </summary>
    public class PackageInfo : INotifyPropertyChanged
    {
        public enum FileType
        {
            APK,
            JAR
        }


        private string _Name;
        public string Name
        {
            set { _Name = value; NotifyPropertyChanged("Name"); }
            get { return _Name; }
        }

        private ImageSource _Icon = null;
        public ImageSource Icon
        {
            set { _Icon = value; NotifyPropertyChanged("Icon"); }
            get { return _Icon; }
        }

        private string _Path;
        public string Path
        {
            set { _Path = value; NotifyPropertyChanged("Path"); }
            get { return _Path; }
        }

        public List<CertInfo> SignCertCollection
        {
            get {
                return CertReader.SignCerts;
            }
        }

        private CertInfo _Cert;
        public CertInfo Cert
        {
            set { _Cert = value; NotifyPropertyChanged("Cert"); }
            get { return _Cert; }
        }

        private CertInfo _NewCert;
        public CertInfo NewCert
        {
            set
            {
                if (IsChangeAllowed)
                {
                    _NewCert = value;
                    NotifyPropertyChanged("NewCert");
                    NotifyPropertyChanged("NewCertInfo");
                }
            }
            get
            {
                if (_NewCert == null)
                    return CertReader.SignCerts[0];
                return _NewCert;
            }
        }

        public string NewCertInfo
        {
            get
            {
                if (_NewCert == null || _NewCert.IsEmpty)
                {
                    if (Cert == null)
                        return "Файл не подписан сертификатом";
                    return Cert.Info;
                }
                return _NewCert.Info;
            }
        }

        private bool _IsChangeAllowed = false;
        public bool IsChangeAllowed
        {
            set { _IsChangeAllowed = value; NotifyPropertyChanged("IsChangeAllowed"); }
            get { return _IsChangeAllowed; }
        }

        private string _StatusText;
        public string StatusText
        {
            set { _StatusText = value; NotifyPropertyChanged("StatusText"); }
            get { return _StatusText; }
        }

        private bool _IsComboBoxVisible = true;
        public bool IsComboBoxVisible
        {
            set { _IsComboBoxVisible = value; NotifyPropertyChanged("IsComboBoxVisible"); }
            get { return _IsComboBoxVisible; }
        }

        public FileType Type { get; set; }

        #region Property Change Handler

        public void CallCollectionChanged()
        {
            NotifyPropertyChanged("SignCertCollection");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
