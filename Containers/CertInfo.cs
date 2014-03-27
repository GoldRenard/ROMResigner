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
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO;

namespace ROMResigner
{
    /// <summary>
    /// Контекнер сертификата
    /// </summary>
    public class CertInfo : INotifyPropertyChanged
    {
        const string keyToolLine = "Serial number: ";
        public enum CertType { platform, media, shared, testkey, unknown };

        public bool IsEmpty = false;

        public CertInfo()
        {

        }

        /// <summary>
        /// Конструктор для создания копии контейнера
        /// </summary>
        /// <param name="c"></param>
        public CertInfo(CertInfo c)
        {
            this._OwnerC = c.OwnerC;
            this._FullInfo = c.FullInfo;
            this._OwnerCN = c.OwnerCN;
            this._OwnerEmail = c.OwnerEmail;
            this._OwnerL = c.OwnerL;
            this._OwnerO = c.OwnerO;
            this._OwnerOU = c.OwnerOU;
            this._OwnerST = c.OwnerST;
            this._pemPath = c.pemPath;
            this._pk8Path = c.pk8Path;
            this._SerialNumber = c.SerialNumber;
        }

        private string _OwnerEmail;
        public string OwnerEmail
        {
            set { _OwnerEmail = value; NotifyPropertyChanged("OwnerEmail"); }
            get { return _OwnerEmail; }
        }

        private string _OwnerCN;
        public string OwnerCN
        {
            set { _OwnerCN = value; NotifyPropertyChanged("OwnerCN"); }
            get { return _OwnerCN; }
        }

        private string _OwnerOU;
        public string OwnerOU
        {
            set { _OwnerOU = value; NotifyPropertyChanged("OwnerOU"); }
            get { return _OwnerOU; }
        }

        private string _OwnerO;
        public string OwnerO
        {
            set { _OwnerO = value; NotifyPropertyChanged("OwnerO"); }
            get { return _OwnerO; }
        }

        private string _OwnerL;
        public string OwnerL
        {
            set { _OwnerL = value; NotifyPropertyChanged("OwnerL"); }
            get { return _OwnerL; }
        }

        private string _OwnerST;
        public string OwnerST
        {
            set { _OwnerST = value; NotifyPropertyChanged("OwnerST"); }
            get { return _OwnerST; }
        }

        private string _OwnerC;
        public string OwnerC
        {
            set { _OwnerC = value; NotifyPropertyChanged("OwnerC"); }
            get { return _OwnerC; }
        }

        private string _SerialNumber;
        public string SerialNumber
        {
            set { _SerialNumber = value; NotifyPropertyChanged("SerialNumber"); }
            get { return _SerialNumber; }
        }

        private string _FullInfo;
        public string FullInfo
        {
            set { _FullInfo = value; NotifyPropertyChanged("FullInfo"); }
            get { return _FullInfo; }
        }

        private string _pk8Path;
        public string pk8Path
        {
            set { _pk8Path = value; NotifyPropertyChanged("pk8Path"); NotifyPropertyChanged("CanSign"); }
            get { return _pk8Path; }
        }

        private string _pemPath;
        public string pemPath
        {
            set { _pemPath = value; NotifyPropertyChanged("pemPath"); NotifyPropertyChanged("CanSign"); }
            get { return _pemPath; }
        }

        public bool CanSign
        {
            get { return File.Exists(pk8Path) && File.Exists(pemPath); }
        }

        /// <summary>
        /// Определение типа сертификата
        /// </summary>
        public CertType Type
        {
            get
            {
                if (SerialNumber == "b3998086d056cffa") //a783fdce62ab327f
                    return CertType.platform;
                else if (SerialNumber == "f2a73396bd38767a") //e1501f4f1e378609
                    return CertType.shared;
                else if (SerialNumber == "f2b98e6123572c4e") //e37968a84ac9e88a
                    return CertType.media;
                else if (SerialNumber == "936eacbe07f201df") //b6fe4ad4f49c3256
                    return CertType.testkey;
                else
                    return CertType.unknown;
            }
            set
            {
                if (value == CertType.platform)
                    SerialNumber = "b3998086d056cffa";
                else if (value == CertType.shared)
                    SerialNumber = "f2a73396bd38767a";
                else if (value == CertType.media)
                    SerialNumber = "f2b98e6123572c4e";
                else if (value == CertType.testkey)
                    SerialNumber = "936eacbe07f201df";
                else
                    SerialNumber = "unknown";
                NotifyPropertyChanged("Type");
            }
        }

        /// <summary>
        /// Получение имени типа сертификата
        /// </summary>
        public string ShortInfo
        {
            get
            {
                if (IsEmpty)
                    return "Не менять";
                switch (Type)
                {
                    case CertType.media:
                        return "AOSP Media";
                    case CertType.platform:
                        return "AOSP Platform";
                    case CertType.shared:
                        return "AOSP Shared";
                    case CertType.testkey:
                        return "AOSP Testkey";
                    default:
                        return string.Format("{0} ({1})", OwnerCN, SerialNumber);
                }
            }
        }

        /// <summary>
        /// Получение информации сертификата
        /// </summary>
        public string Info
        {
            get {
                string outstr = string.Empty;
                if (!string.IsNullOrEmpty(OwnerO))
                    outstr += string.Format("Владелец: {0}, ", OwnerO);
                if (!string.IsNullOrEmpty(OwnerEmail))
                    outstr += string.Format("E-Mail: {0}, ", OwnerEmail);
                if (!string.IsNullOrEmpty(OwnerCN))
                    outstr += string.Format("CN: {0}, ", OwnerCN);
                if (!string.IsNullOrEmpty(SerialNumber))
                    outstr += string.Format("SN: {0} ", SerialNumber);
                return outstr;
            }
        }

        /// <summary>
        /// Парсинг информации о подписи из keytool
        /// </summary>
        /// <param name="Data">Выходные данные утилиты keytool</param>
        /// <returns>Структура с информацией о подписи</returns>
        public static CertInfo Parse(string Data)
        {
            //Извлекаем серийный номер
            string sNum = null;
            foreach (string line in Data.Split('\n'))
                if (line.Contains(keyToolLine))
                {
                    sNum = line.Replace(keyToolLine, string.Empty);
                    break;
                }

            if (sNum == null)
                return null;

            //Если не нашли, создаем новый и добавляем его в коллекцию
            CertInfo cInfo = new CertInfo() { SerialNumber = sNum, FullInfo = Data };

            //Извлекаем инфу о владельце
            Regex rx_EMAIL = new Regex("(EMAILADDRESS=)(.*?)(,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex rx_CN = new Regex("(CN=)(.*?)(,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex rx_OU = new Regex("(OU=)(.*?)(,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex rx_O = new Regex("(O=)(.*?)(,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex rx_L = new Regex("(L=)(.*?)(,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex rx_ST = new Regex("(ST=)(.*?)(,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex rx_C = new Regex("(C=)(.*?)(,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m;

            foreach (string line in cInfo.FullInfo.Split('\n'))
            {
                if (line.Contains("Owner: "))
                {
                    m = rx_EMAIL.Match(line);
                    if (m.Success)
                        cInfo.OwnerEmail = m.Groups[2].ToString();
                    m = rx_CN.Match(line);
                    if (m.Success)
                        cInfo.OwnerCN = m.Groups[2].ToString();
                    m = rx_OU.Match(line);
                    if (m.Success)
                        cInfo.OwnerOU = m.Groups[2].ToString();
                    m = rx_O.Match(line);
                    if (m.Success)
                        cInfo.OwnerO = m.Groups[2].ToString();
                    m = rx_L.Match(line);
                    if (m.Success)
                        cInfo.OwnerL = m.Groups[2].ToString();
                    m = rx_ST.Match(line);
                    if (m.Success)
                        cInfo.OwnerST = m.Groups[2].ToString();
                    m = rx_C.Match(line);
                    if (m.Success)
                        cInfo.OwnerC = m.Groups[2].ToString();
                }
            }

            return cInfo;
        }

        #region Property Change Handler
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

}
