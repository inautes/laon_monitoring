using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace OSPAutoSearch_AutoLogin
{
    class clsSimpleHttp
    {
        public delegate void DownloadEventHandler(string strFilename, int intPersent);
        public event DownloadEventHandler eDownloadEvent;

        public delegate void DownloadFailedEventHandler(string strFilename, string strErrorMsg);
        public event DownloadFailedEventHandler eDownloadFailed;

        private WebRequest webRequest = null;
        private WebResponse webResponse = null;
        private Stream webStream = null;
        private int bufferSize = 2048;

        public clsSimpleHttp() { }

        public void download(string strDownloadUrl, string remoteFileName, string localFile)
        {
            try
            {
                webRequest = WebRequest.Create(strDownloadUrl);
                webResponse = webRequest.GetResponse();
                webStream = webResponse.GetResponseStream();

                int intFilesize = (int)webResponse.ContentLength;
                
                FileStream localFileStream = new FileStream(localFile, FileMode.Create);

                long lSumbyte = 0;
                byte[] byteBuffer = new byte[bufferSize];
                int bytesRead = webStream.Read(byteBuffer, 0, bufferSize);
                lSumbyte += bytesRead;
                int intTemp = (int)((double)lSumbyte / (double)intFilesize * 100.0);       
                eDownloadEvent(remoteFileName, intTemp);
                
                try
                {
                    while (bytesRead > 0)
                    {
                        localFileStream.Write(byteBuffer, 0, bytesRead);
                        bytesRead = webStream.Read(byteBuffer, 0, bufferSize);

                        lSumbyte += bytesRead;
                        intTemp = (int)((double)lSumbyte / (double)intFilesize * 100.0);
                        eDownloadEvent(remoteFileName, intTemp);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                webRequest.BeginGetRequestStream(null, null);


                localFileStream.Close();
                webStream.Close();
                webResponse.Close();
                webRequest = null;
            }
            catch (Exception ex) 
            {
                eDownloadFailed(remoteFileName, ex.Message);
            }
        }        
    }
}
