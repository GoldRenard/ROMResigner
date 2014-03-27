# About

ROMResigner is an utility for signing whole Android firmwares and/or just individual apk/jar files.
 * You can selectively sign apk/jar files, whole directory with apk/jar or ZIP-update with the firmware (ZIP can be signed too).
 * You can select your own pk8+pem cert (or put it in a folder with the utility, they will be added automatically)
 * You can view the info about current certificate.
 * You can automatically assign new certificates using key files:
  > Platform: framework-res.apk or Phone.apk
  > Media: DownloadProviderUi.apk or DownloadProvider.apk
  > Shared: ApplicationsProvider.apk
  > Testkey: HTMLViewer.apk