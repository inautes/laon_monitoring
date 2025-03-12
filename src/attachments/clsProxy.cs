using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace OSPAutoSearch_AutoLogin
{
    public class clsProxy
    {
        #region 프록시설정시 사용하는 상수값 및 구조체

        const int INTERNET_OPTION_REFRESH = 37;
        const int INTERNET_OPTION_PROXY = 38;
        const int INTERNET_OPTION_SETTINGS_CHANGED = 39;

        //Internet Explorer의 설정을 사용합니다.
        const int INTERNET_OPEN_TYPE_PRECONFIG = 0;
        //프록시 서버를 사용하지 않습니다.
        const int INTERNET_OPEN_TYPE_DIRECT = 1;
        //지정한 프록시 서버 설정을 사용합니다.
        const int INTERNET_OPEN_TYPE_PROXY = 3;

        public struct INTERNET_PROXY_INFO
        {
            public int dwAccessType;
            public IntPtr proxy;
            public IntPtr proxyBypass;
        };

        public struct IP_PORT
        {
            public string IP;
            public int PORT;
        };

        #endregion

        public delegate void ProxyConnnected(bool isResult, string strResultMsg);
        public event ProxyConnnected ProxyConnectedEventHandler;

        private const int CONNECT_SUCCESS_TIME = 4000;
        private const int PROXY_TIMEOUT = 5000;

        private ManualResetEvent mTimeoutEvent = new ManualResetEvent(true);
        private bool mIsSuccess = false;

        private List<IP_PORT> mProxyIP = new List<IP_PORT>();
        private string mTestURL = string.Empty;
        private bool mIsAutoRedirect = true;

        public bool mIsProxyRun = false;

        public clsProxy()
        {
            LoadProxy();
        }

        public void LoadProxy()
        {
            string strPath = Application.StartupPath + @"\proxylist.txt";
            if (File.Exists(strPath) == false) return;

            string[] arrProxyList = File.ReadAllLines(strPath);
            foreach (string s in arrProxyList)
            {
                string[] arrTemp = s.Split(new char[] { ':' });
                if (arrTemp.Length >= 2)
                {
                    IP_PORT info;
                    info.IP = arrTemp[0];
                    info.PORT = clsUtil.IntParse(arrTemp[1]);
                    mProxyIP.Add(info);
                }
            }
        }

        public bool SetProxy(int nIndex)
        {
            mTestURL = GetTestURL(nIndex);
            mIsAutoRedirect = IsAutoRedirect(nIndex);

            mIsProxyRun = true;
            new Thread(SetProxyRun).Start();

            return true;
        }

        public void SetProxyRun()
        {
            if (mProxyIP.Count <= 0)
            {
                if (ProxyConnectedEventHandler != null && mIsProxyRun == true)
                    ProxyConnectedEventHandler(true, "프록시 정보없음");

                return;
            }

            if (mTestURL.Contains("http://") == false)
            {
                mTestURL = "http://" + mTestURL;
            }

            //현재아이피를 이용해서 테스트시 정상이면 프록시를 설정하지않는다.
            long lNowIPTime;
            bool isNowIPConn = IsConnectTest(mTestURL, "", 0, mIsAutoRedirect, out lNowIPTime);
            if (isNowIPConn == true && lNowIPTime < CONNECT_SUCCESS_TIME)
            {
                if (ProxyConnectedEventHandler != null && mIsProxyRun == true)
                    ProxyConnectedEventHandler(true, "기본연결 성공");

                return;
            }

            int nCount = mProxyIP.Count;
            if (nCount > 10)
                nCount = 10;

            for (int i = 0; i < nCount; i++)
            {
                if (mIsProxyRun == false)
                {
                    return;
                }

                long lTime;
                bool isConn = IsConnectTest(mTestURL, mProxyIP[i].IP, mProxyIP[i].PORT, mIsAutoRedirect, out lTime);
                if (isConn == true && lTime < CONNECT_SUCCESS_TIME)
                {
                    SetProxyLocal(mProxyIP[i].IP, mProxyIP[i].PORT);

                    if (ProxyConnectedEventHandler != null && mIsProxyRun == true)
                        ProxyConnectedEventHandler(true, "프록시 연결성공");

                    return;
                }
                else
                {
                    if (lTime < PROXY_TIMEOUT)
                    {
                        Thread.Sleep((int)(PROXY_TIMEOUT - lTime));
                    }
                }
            }

            if (ProxyConnectedEventHandler != null && mIsProxyRun == true)
                ProxyConnectedEventHandler(false, "프록시 연결실패");
        }

        private void CallBackMethod(IAsyncResult asyncresult)
        {
            try
            {
                if (mIsSuccess == false)
                {
                    HttpWebRequest request = (HttpWebRequest)asyncresult.AsyncState;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        StreamReader sr = new StreamReader(response.GetResponseStream());
                        string strHtml = sr.ReadToEnd();
                        sr.Close();

                        if (strHtml.Length < 500 && strHtml.Contains("refresh") == false && strHtml.Contains("reload") == false
                            && strHtml.Contains("document.location.href") == false && strHtml.Contains("302 Found") == false)
                            mIsSuccess = false;
                        else
                            mIsSuccess = true;
                    }
                }

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            mTimeoutEvent.Set();
        }

        private bool IsConnectTest(string strURL, string strIP, int nPort, bool isAutoRedirect, out long lTime)
        {
            Stopwatch timeCheck = new Stopwatch();
            timeCheck.Start();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.80 Safari/537.36";
                request.Headers.Add("Accept-Language", "ko-KR,ko;q=0.8,en-US;q=0.6,en;q=0.4");
                request.Headers.Add("Accept-Encoding", "gzip, deflate, sdch");
                request.AllowAutoRedirect = isAutoRedirect;                
                if (nPort > 0)
                {
                    request.Proxy = new WebProxy(strIP, nPort);
                }
                request.Timeout = PROXY_TIMEOUT;
                request.BeginGetResponse(new AsyncCallback(CallBackMethod), request);
                mTimeoutEvent.Reset();
                mTimeoutEvent.WaitOne(PROXY_TIMEOUT, false); 
                if (mIsSuccess == false)
                {
                    mIsSuccess = true;
                    request.Abort();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                timeCheck.Stop();
                lTime = timeCheck.ElapsedMilliseconds;
            }

            return true;
        }

        //현재PC에서 웹통신시 프록시적용
        private bool SetProxyRegistry(string strIP, int nPort)
        {
            string key = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            string strProxy = strIP + ":" + nPort.ToString();

            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(key, true);

            RegKey.SetValue("ProxyServer", strProxy);
            RegKey.SetValue("ProxyEnable", 1);

            return true;
        }

        //현재프로그램에서 사용하는 웹통신에만 프록시적용
        public void SetProxyLocal(string strIP, int nPort)
        {
            string strProxy = strIP + ":" + nPort.ToString();
            //string strProxy = "220.230.51.104:2004";
            //string strProxy = "211.174.103.140:5040";            

            INTERNET_PROXY_INFO proxyInfo;

            // Filling in structure 
            proxyInfo.dwAccessType = INTERNET_OPEN_TYPE_PROXY;
            proxyInfo.proxy = Marshal.StringToHGlobalAnsi(strProxy);
            proxyInfo.proxyBypass = Marshal.StringToHGlobalAnsi("local");

            // Allocating memory 
            IntPtr pMemBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(proxyInfo));

            // Converting structure to IntPtr 
            Marshal.StructureToPtr(proxyInfo, pMemBuffer, true);

            APIs.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY, pMemBuffer, Marshal.SizeOf(proxyInfo));

            APIs.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            APIs.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            Marshal.FreeCoTaskMem(pMemBuffer);
        }

        private string GetTestURL(int nIndex)
        {
            #region 테스트URL 관리
            switch (nIndex)
            {
                case 0:
                    // 0 : 파일함
                    return "www.fileham.com";
                case 1:
                    // 1 : 피디팝
                    return "www.pdpop.com";
                case 2:
                    // 2 : 쉐어박스
                    return "sharebox.co.kr";
                case 3:
                    // 3 : 위디스크
                    //프록시 사용불가
                    return "www.wedisk.co.kr";
                    //return "";
                case 4:
                    // 4 : 메가파일
                    return "www.megafile.co.kr/main/index.php";
                case 5:
                    // 5 : 파일시티
                    return "www.filecity.co.kr";
                case 6:
                    // 6 : 파일와        
                    return "www.filewa.com/main/index.php";
                case 7:
                    // 7 : 파일노리
                    return "www.filenori.com";
                case 8:
                    // 8 : 파일혼
                    return "filehon.com";
                case 9:
                    // 9 : 럭키월드
                    return "www.luckyworld.net";
                case 10:
                    // 10 : 프루나
                    return "webhard.pruna.com";
                case 11:
                    // 11 : G파일
                    return "www.gfile.co.kr/main/index_new.php";
                case 12:
                    // 12 : 빅파일
                    return "www.bigfile.co.kr";
                case 13:
                    //13 : 티플
                    return "www.tple.co.kr";
                case 14:
                    //14 : 엠파일
                    return "www.mfile.co.kr/index.php?act=main";
                case 15:
                    //15 : 미투디스크
                    return "me2disk.com";
                case 16:
                    //16 : 파일동
                    return "www.filedong.co.kr";
                case 17:
                    //17 : 파일쿠키
                    return "www.filekuki.com";
                case 18:
                    //18 : 엘티이파일
                    return "www.ltefile.com/main.php";
                case 19:
                    //19 : 아이팝클럽
                    return "www.ipopclub.co.kr";
                case 20:
                    //20 : 쏘디스크
                    return "www.sodisk.co.kr/main/main.asp";
                case 21:
                    //21 : 파일24
                    return "www.file24.co.kr/index.php?act=main";
                /******************** 비계약 OSP ********************/
                case 22:
                    //22 : 파일독
                    return "www.filedok.com/main/main_html.php";
                case 23:
                    //23 : 파일조
                    return "www.filejo.com/main/main_html.php";
                case 24:
                    //24 : 애플파일
                    return "www.applefile.com";
                case 25:
                    //25 : 예스파일.
                    return "www.yesfile.com";
                case 26:
                    //26 : 본디스크
                    return "www.bondisk.com";
                case 27:
                    //27 : 큐다운
                    return "www.qdown.com";
                case 28:
                    //28 : 베가디스크
                    return "vegadisk.com";
                case 29:
                    //29 : 따오기
                    return "www.daoki.com";
                case 30:
                    //30 : 지오파일
                    return "www.ziofile.com/index.php";
                case 31:
                    //31 : 온디스크
                    return "www.ondisk.co.kr/index.php";
                case 32:
                    //32 : 케이디스크
                    return "www.kdisk.co.kr/index.php";
                case 33:
                    //33 : 조이파일
                    return "www.joyfile.co.kr";
                case 34:
                    //34 : 티지튠즈
                    return "www.tgtunes.co.kr";
                case 35:
                    //35 : 클럽넥스
                    return "www.clubnex.co.kr";
                case 36:
                    //36 : 새디스크
                    return "sedisk.com/index.php?act=main";
                case 37:
                    //37 : 투디스크
                    return "www.todisk.com/";
                case 38:
                    //38 : 스마트파일
                    return "smartfile.co.kr";                    
                case 39:
                    //39 : 돈디스크
                    return "www.dondisk.co.kr";                    
                case 40:
                    //40 : 파일투어
                    return "www.filetour.com/index.php?act=main";                 
                case 41:
                    //41 : 오라디스크
                    return "oradisk.com";                    
                case 42:
                    //42 : 파일캐스트
                    return "filecast.co.kr/www/home/main";                   
                case 43:
                    //43 : 위디스크_모바일
                    return "m.wedisk.co.kr/mobile/login.jsp";
                case 44:
                    //44 : 메가파일_모바일
                    return "m.megafile.co.kr/user/login.php";                    
                case 45:
                    //45 : 파일조_모바일
                    return "m.filejo.com/?m=member_login";
            }
            #endregion

            return "";
        }

        private bool IsAutoRedirect(int nIndex)
        {
            switch (nIndex)
            {
                case 16:
                    // 16 : 파일동
                    return false;
            }
            return true;
        }

        int mCurrentIndex = -1;

        public void SetProxy2()
        {
            mCurrentIndex++;
            new Thread(SetProxyRun2).Start();
        }
        public void SetProxyRun2()
        {
            if (mProxyIP.Count <= 0) return;

            int retry_cnt = 5;

            string key = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(key, true);
            string strIP = mProxyIP[0].IP + ":" + mProxyIP[0].PORT.ToString();
            
            for (; retry_cnt > 0; retry_cnt-- )
            {
                RegKey.SetValue("ProxyServer", strIP);
                Thread.Sleep(2000);
                object getIP = RegKey.GetValue("ProxyServer");
                if (strIP.CompareTo(getIP) == 0)
                {
                    // 사용한 프록시 IP 제거....
                    string strtmp = "";
                    for (int i =  1; i < mProxyIP.Count; i++)
                        strtmp += (mProxyIP[i].IP + ":" + mProxyIP[i].PORT.ToString() + "\r\n");
                    string strPath = Application.StartupPath + @"\proxylist.txt";
                    System.IO.File.WriteAllText(strPath, strtmp);
                    ProxyConnectedEventHandler(true, "프록시 변경 성공 : " + getIP);
                    clsUtil.SetErrorLog(String.Format("프록시변경 : {0}", getIP));
                    
                    return;
                }
            }
        }
    }
}
