using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FtpLib;
using Renci.SshNet;
using System.Threading;

namespace OSPAutoSearch_AutoLogin
{
    public class clsSFtp
    {
        public const string mLOCALDIR = @"C:\evidence_img\";
        //public const string mSERVERDIR = "/raid/project/engine/apache-tomcat-7.0.68/webapps/img";
        public const string mSERVERDIR = "/mnt/sdb1/img";

        public const string mURL = "101.202.35.41";
        public const int mPort = 1022;

        public const string mServerType = @"_IMGC01";

        public const string mID = "laoncompany";
        public const string mPWD = "fkdhs!00";


        private SftpClient mFTP = null;

        public bool mIsConnect = false;

        public string mSiteID = string.Empty;

        public clsSFtp()
        {
        }

        public void Start(string strSiteID)
        {
            mSiteID = strSiteID;

            new Thread(new ThreadStart(Run)).Start();
        }

        private void Run()
        {
            Connect();
            
            while (mIsConnect == true)
            {
                FileUpload();

                Thread.Sleep(1000);
            }
        }

        private bool Connect()
        {
            if (mFTP != null)
            {
                mFTP.Disconnect();
                mFTP.Dispose();
                mFTP = null;
                mIsConnect = false;
            }

            mFTP = new SftpClient(mURL,mPort, mID, mPWD);

            try
            {                
                mFTP.Connect();
                mIsConnect = true;
                Console.WriteLine("ftp연결성공");
            }
            catch (Exception ex)
            {
                clsUtil.SetErrorLog(String.Format("FTP연결실패 : {0}", ex.Message));

                mIsConnect = false;
            }
            return mIsConnect;
        }

        public bool Connect(ref string strErrMsg)
        {
            if (mFTP != null)
            {
                mFTP.Disconnect();
                mFTP.Dispose();
                mFTP = null;
                mIsConnect = false;
            }

            mFTP = new SftpClient(mURL, mPort, mID, mPWD);

            try
            {
                mFTP.Connect();
                clsUtil.Delay(100);
                mIsConnect = true;             
            }
            catch (Exception ex)
            {
                clsUtil.SetErrorLog(String.Format("FTP연결실패 : {0}", ex.Message));
                strErrMsg = ex.Message;

                mIsConnect = false;
            }

            return mIsConnect;
        }

        public bool DisConnect()
        {
            mIsConnect = false;

            if (mFTP == null) return false;

            try
            {
                mFTP.Disconnect();
                mFTP.Dispose();
                mFTP = null;
            }
            catch { }

            return mIsConnect;
        }

        public bool Put(string strLocalPath, string strServerPath)
        {
            if (mFTP == null) return false;

            if (File.Exists(strLocalPath) == false) return false;

            if (mIsConnect == false)
            {
                Connect();
            }

            try
            {
                using (var fileStream = System.IO.File.OpenRead(strLocalPath))
                {
                    mFTP.UploadFile(fileStream, strServerPath);
                }
                //mFTP.UploadFile(strLocalPath, strServerPath);
                File.Delete(strLocalPath);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("예외[업로드] : " + ex.Message);

                //예외발생시 연결을 해제하고 다음 업로드시 재연결하도록 한다.
                DisConnect();

                return false;
            }

            return true;
        }

        public bool MakeDirectory(string strPath)
        {
            if (mFTP == null) return false;

            try
            {
                if (mFTP.Exists(mSERVERDIR + strPath) == false)
                {
                    mFTP.CreateDirectory(mSERVERDIR + strPath);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("예외[폴더생성] : " + ex.Message);
                return false;
            }

            return true;
        }

        public bool IsFile()
        {
            if (mIsConnect == false) return false;

            string strLocalDirPath = String.Format(@"{0}{1}\", mLOCALDIR, mSiteID);
            DirectoryInfo dir = new DirectoryInfo(strLocalDirPath);
            if (dir.Exists == true)
            {
                if (dir.GetFiles().Length > 0)
                    return true;
            }

            return false;
        }

        public void FileUpload()
        {
            string strLocalDirPath = String.Format(@"{0}{1}\", mLOCALDIR, mSiteID);
            DirectoryInfo dir = new DirectoryInfo(strLocalDirPath);
            if (dir.Exists == true)
            {
                foreach (System.IO.FileInfo file in dir.GetFiles())
                {
                    string strExtension = Path.GetExtension(file.Name);
                    if (clsUtil.isCompare(strExtension, ".jpg") == true)
                    {
                        //6237171FD14CF631C23FFD0C7E5F589F_20150916052610.jpg
                        string strTime = clsUtil.SubStringEx(file.Name, "_", 1, ".");
                        if (strTime.Length >= 8)
                        {
                            string strDirYear = "/" + strTime.Substring(0, 4);
                            string strDirMonth = strDirYear + "/" + strTime.Substring(4, 2);
                            string strDirDay = strDirMonth + "/" + strTime.Substring(6, 2);
                            MakeDirectory(strDirYear);
                            MakeDirectory(strDirMonth);
                            MakeDirectory(strDirDay);

                            string strLocalPath = String.Format(@"{0}{1}", strLocalDirPath, file.Name);
                            string strServerPath = String.Format(@"{0}{1}/{2}", mSERVERDIR, strDirDay, file.Name);
                            if (Put(strLocalPath, strServerPath) == false)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
