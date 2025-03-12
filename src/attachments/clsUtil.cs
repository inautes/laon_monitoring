using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Web.Security;
using System.Management;
using mshtml;
using System.Net.Configuration;
using System.Reflection;

namespace OSPAutoSearch_AutoLogin
{
    public class clsUtil
    {
        //파일저장 폴더경로
        public const string LOCAL_DIR = @"C:\evidence_img\";
        public const string ERROR_DIR = @"ErrorLog\";
        public static string RUNNING_OSP = string.Empty;

        public static int OspQuart = 0;

        [ComVisible(true), ComImport()]
        [GuidAttribute("0000010d-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IViewObject
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int Draw(
                [MarshalAs(UnmanagedType.U4)] UInt32 dwDrawAspect,
                int lindex,
                IntPtr pvAspect,
                [In] IntPtr ptd,
                IntPtr hdcTargetDev,
                IntPtr hdcDraw,
                [MarshalAs(UnmanagedType.Struct)] ref Rectangle lprcBounds,
                [MarshalAs(UnmanagedType.Struct)] ref Rectangle lprcWBounds,
                IntPtr pfnContinue,
                [MarshalAs(UnmanagedType.U4)] UInt32 dwContinue);

            [PreserveSig]
            int GetColorSet([In, MarshalAs(UnmanagedType.U4)] int dwDrawAspect,
               int lindex, IntPtr pvAspect, [In] IntPtr ptd,
                IntPtr hicTargetDev, [Out] IntPtr ppColorSet);

            [PreserveSig]
            int Freeze([In, MarshalAs(UnmanagedType.U4)] int dwDrawAspect,
                            int lindex, IntPtr pvAspect, [Out] IntPtr pdwFreeze);
            [PreserveSig]
            int Unfreeze([In, MarshalAs(UnmanagedType.U4)] int dwFreeze);
        }

        public static Image setCapture(WebBrowser web)
        {
            int nWidth = web.Width;
            int nHeight = web.Height;

            Rectangle imgRectangle = new Rectangle(0, 0, nWidth, nHeight);
            Rectangle docRectangle = new Rectangle(0, 0, nWidth, nHeight);

            Bitmap bmp = new Bitmap(nWidth, nHeight);
            IViewObject ivo = web.Document.DomDocument as IViewObject;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                ivo.Draw(1, -1, IntPtr.Zero, IntPtr.Zero,
                         IntPtr.Zero, hdc, ref imgRectangle,
                         ref docRectangle, IntPtr.Zero, 0);
                g.ReleaseHdc(hdc);
            }

            return bmp as Image;
        }

        public static Image setCapture2(Bitmap bmp, WebBrowser web,Microsoft.Web.WebView2.WinForms.WebView2 web2)
        {
            int nWidth = web2.Width;
            int nHeight = web2.Height;

            Rectangle imgRectangle = new Rectangle(0, 0, nWidth, nHeight);
            Rectangle docRectangle = new Rectangle(0, 0, nWidth, nHeight);
            
            IViewObject ivo = web.Document.DomDocument as IViewObject; //web.Document.DomDocument as IViewObject;
            
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                ivo.Draw(1, -1, IntPtr.Zero, IntPtr.Zero,
                         IntPtr.Zero, hdc, ref imgRectangle,
                         ref docRectangle, IntPtr.Zero, 0);
                g.ReleaseHdc(hdc);
            }

            return bmp as Image;
        }

       



        public static Image ScreenShot(Control con)
        {
            Bitmap bmp = new System.Drawing.Bitmap(con.Width, con.Height);
            con.DrawToBitmap(bmp, con.ClientRectangle);
            return bmp as Image;
        }

        public static Image ScreenShot2(Control con)
        {
            Rectangle rc;
            APIs.GetClientRect(con.Handle, out rc);

            Bitmap bmp = null;
            IntPtr hdcFrom = APIs.GetDC(con.Handle);
            IntPtr hdcTo = APIs.CreateCompatibleDC(hdcFrom);
            int Width = rc.Width;
            int Height = rc.Height;
            IntPtr hBitmap = APIs.CreateCompatibleBitmap(hdcFrom, Width, Height);
            if (hBitmap != IntPtr.Zero)
            {
                IntPtr hLocalBitmap = APIs.SelectObject(hdcTo, hBitmap);
                APIs.BitBlt(hdcTo, 0, 0, Width, Height,
                    hdcFrom, 0, 0, APIs.TernaryRasterOperations.SRCCOPY);
                APIs.SelectObject(hdcTo, hLocalBitmap);
                APIs.DeleteDC(hdcTo);
                APIs.ReleaseDC(con.Handle, hdcFrom);
                bmp = System.Drawing.Image.FromHbitmap(hBitmap);
                APIs.DeleteObject(hBitmap);
            }

            return bmp as Image;
        }

        public static Image ScreenShot(string strTitle)
        {
            IntPtr pHandle = APIs.FindWindow(null, strTitle);
            IntPtr hDC = APIs.GetWindowDC(pHandle);
            IntPtr Bitmaps = APIs.GetCurrentObject(hDC, 7);
            Image Image = Image.FromHbitmap(Bitmaps);

            return Image;
        }

        public static Image ScreenShot2(string strTitle)
        {
            IntPtr pHandle = APIs.FindWindow(null, strTitle);

            Rectangle rc;
            APIs.GetClientRect(pHandle, out rc);

            Bitmap bmp = null;
            IntPtr hdcFrom = APIs.GetDC(pHandle);
            IntPtr hdcTo = APIs.CreateCompatibleDC(hdcFrom);
            int Width = rc.Width;
            int Height = rc.Height;
            IntPtr hBitmap = APIs.CreateCompatibleBitmap(hdcFrom, Width, Height);
            if (hBitmap != IntPtr.Zero)
            {
                IntPtr hLocalBitmap = APIs.SelectObject(hdcTo, hBitmap);
                APIs.BitBlt(hdcTo, 0, 0, Width, Height,
                    hdcFrom, 0, 0, APIs.TernaryRasterOperations.SRCCOPY);
                APIs.SelectObject(hdcTo, hLocalBitmap);
                APIs.DeleteDC(hdcTo);
                APIs.ReleaseDC(pHandle, hdcFrom);
                bmp = System.Drawing.Image.FromHbitmap(hBitmap);
                APIs.DeleteObject(hBitmap);
            }

            return bmp as Image;
        }

        public static Image ScreenShot3(string strTitle)
        {
            IntPtr pHandle = APIs.FindWindow(null, strTitle);
            if (pHandle == null || pHandle == IntPtr.Zero) return null;

            APIs.RECT rc;
            APIs.GetWindowRect(pHandle, out rc); 

            //UTCK 크기, 위치 확인.
            if (rc.Width < 291 || rc.Height < 161 || rc.Top != 10 || rc.Left != 10)
            {
                // move position UTCK3 // 잘려 보이는 경우를 대비
                // 최상위로 올라오고, 사이즈 변화 없이 이동.
                //APIs.SetWindowPos(pHandle, (int)APIs.SetWindowPosFlags.HWND_TOPMOST, 10, 10, 0, 0, (uint)APIs.SetWindowPosFlags.SWP_NOSIZE);
                APIs.SetWindowPos(pHandle, (int)APIs.SetWindowPosFlags.HWND_TOPMOST, 10, 10, 291, 161, (uint)APIs.SetWindowPosFlags.SWP_SHOWWINDOW);

                // show UTCK3   // 최소화나 감추어져 있을경우를 대비
                APIs.ShowWindowAsync(pHandle, (int)APIs.ShowWindowAsyncFlags.SW_SHOWNORMAL);


                // 이동시 바로 캡쳐하면 검게 나오므로 한번만 약간 기다려준다.
                clsUtil.Delay(500);

                APIs.GetWindowRect(pHandle, out rc);
            }


            int Width = rc.Width;
            int Height = rc.Height;
            Bitmap bmp = new Bitmap(Width, Height);

            // Bitmap 이미지 변경을 위해 Graphics 객체 생성
            using (Graphics gr = Graphics.FromImage(bmp))
            {
                // 화면을 그대로 카피해서 Bitmap 메모리에 저장
                gr.CopyFromScreen(rc.Left, rc.Top, 0, 0, new Size(rc.Width, rc.Height));
            }

            return bmp as Image;
        }

        public static void SaveDataTable(string strOSPName, DataTable dtData)
        {
            try
            {
                MakeDirectory(string.Format("{0}\\OSP_SEARCH_DATA", GetCurrPath()));

                StreamWriter sw = new StreamWriter(String.Format("{0}\\OSP_SEARCH_DATA\\{1}_{2}.txt", GetCurrPath(), strOSPName, DateTime.Now.ToString("yyyyMMddHHmmss")), true, Encoding.UTF8);

                foreach (DataRow r in dtData.Rows)
                {
                    string strData = string.Empty;
                    for (int i = 0; i < dtData.Columns.Count; i++)
                    {
                        string strTemp = r[i].ToString();
                        strTemp = strTemp.Replace("\r\n", "");
                        strData += strTemp + "|||";
                    }

                    sw.WriteLine(strData);
                    sw.Flush();
                }

                sw.Close();
                sw.Dispose();
            }
            catch
            {
            }
        }

        public static bool IsDirectory(string sPath)
        {
            DirectoryInfo di = new DirectoryInfo(sPath);
            return di.Exists;
        }

        public static void MakeDirectory(string sPath)
        {
            DirectoryInfo di = new DirectoryInfo(sPath);

            if (di.Exists == false)
            {
                di.Create();
            }
        }

        public static void DeleteDirectory(string sPath)
        {
            DirectoryInfo di = new DirectoryInfo(sPath);

            if (di.Exists == true)
            {
                di.Delete(true);
            }
        }

        public static string GetCurrPath()
        {
            string sPath = Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;

            int iPos = sPath.LastIndexOf(@"\");

            sPath = sPath.Substring(0, iPos);

            return sPath;
        }

        //컨트롤이 포커스를 갖지않게함
        public static void NotFocus(Control con)
        {
            const int GWL_EXSTYLE = -20;
            const int WS_EX_NOACTIVATE = 0x08000000;

            APIs.SetWindowLong(con.Handle, GWL_EXSTYLE,
                APIs.GetWindowLong(con.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

        //문자열 -> 숫자 변환시 발생할수 있는 예외처리
        public static int IntParse(string strNumber)
        {
            int nNum = 0;

            try
            {
                string strRegex = Regex.Replace(strNumber, "[^-0-9]", "");
                nNum = int.Parse(strRegex);
            }
            catch (Exception) { }

            return nNum;
        }

        public static int IntParse(string strNumber, int nDefaultValue)
        {
            int nNum = nDefaultValue;

            try
            {
                string strRegex = Regex.Replace(strNumber, "[^-0-9]", "");
                nNum = int.Parse(strRegex);
            }
            catch (Exception) { }

            return nNum;
        }

        //문자열 -> 숫자 변환시 발생할수 있는 예외처리
        public static double DoubleParse(string strNumber)
        {
            double dNum = 0.0;

            try
            {
                string strRegex = Regex.Replace(strNumber, "[^-0-9.]", "");
                dNum = Double.Parse(strRegex);
            }
            catch (Exception) { }

            return dNum;
        }

        //문자열에서 숫자만 추출
        public static string TrimString(string strNumber)
        {
            try
            {
                return Regex.Replace(strNumber, "[^0-9]", "").Trim();
            }
            catch (Exception) { }

            return strNumber;
        }

        //문자열에서 특수문자만 추출
        public static string TrimSpecialLetters(string strStr)
        {
            try
            {
                return Regex.Replace(strStr, @"[^a-zA-Z0-9가-힣]", "", RegexOptions.Singleline).Trim();
            }
            catch (Exception) { }

            return strStr;
        }

        //문자열에서 개행관련 문자를 제거
        public static string TrimNewLine(string strStr)
        {
            try
            {
                strStr.Replace("\n", "");
                strStr.Replace("\t", "");
                return strStr;
            }
            catch (Exception) { }

            return strStr;
        }

        //문자열에서 숫자관련문자를 제거
        public static string TrimNumber(string strNumber)
        {
            try
            {
                return Regex.Replace(strNumber, "[0-9,.]", "").Trim();
            }
            catch (Exception) { }

            return strNumber;
        }

        //특정문자열 찾기
        public static string SubStringEx(string strStr, string strStartStr, int nDepth, string strEndStr)
        {
            if (strStr == null || strStr.Length <= 0) return "";

            int nStart = -1, nSubLength = -1;

            if (strStartStr.Length <= 0)
            {
                nStart = 0;
            }
            else
            {
                if (strStr.IndexOf(strStartStr) < 0)
                {
                    return strStr;
                }

                for (int i = 0; i < nDepth; i++)
                {
                    if (nStart < 0)
                        nStart = strStr.IndexOf(strStartStr) + strStartStr.Length;
                    else
                        nStart = strStr.IndexOf(strStartStr, nStart) + strStartStr.Length;
                }
            }

            if (strEndStr.Length <= 0)
                nSubLength = strStr.Length - nStart;
            else
                nSubLength = strStr.IndexOf(strEndStr, nStart) - nStart;

            if (nStart < 0 || nSubLength < 0) return strStr;

            return strStr.Substring(nStart, nSubLength).Trim();
        }

        public static bool isCompare(string strBase, string strCompare)
        {
            if (strBase == null) return false;

            if (String.Compare(strBase.Trim(), strCompare.Trim(), true) == 0)
            {
                return true;
            }

            return false;
        }

        public static bool StringContain(string strStr1, string strStr2)
        {
            if (strStr1 == null || strStr2 == null) return false;

            return strStr1.ToUpper().Contains(strStr2.ToUpper());
        }

        public static bool isContain(string strStr1, string strStr2, bool ignoreCase = false)
        {
            if (strStr1 == null || strStr2 == null) return false;

            if (strStr1.Length > strStr2.Length)
            {
                if (ignoreCase == true)
                {
                    return strStr1.ToUpper().Contains(strStr2.ToUpper());
                }
                else
                {
                    return strStr1.Contains(strStr2);
                }
            }
            else
            {
                if (ignoreCase == true)
                {
                    return strStr2.ToUpper().Contains(strStr1.ToUpper());
                }
                else
                {
                    return strStr2.Contains(strStr1);
                }
            }
        }

        public static bool isContain2(string strStr1, string strStr2)
        {
            if (strStr1 == null || strStr2 == null) return false;

            return strStr1.ToUpper().Contains(strStr2.ToUpper());
        }

        public static bool RunExe(string strPath, string strExeName)
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.Contains(strExeName) == true)
                    {
                        return true;
                    }
                }

                Process prExe = new Process();
                prExe.StartInfo.FileName = strPath;
                prExe.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                prExe.Start();
            }
            catch
            {
                return false;
            }

            return true;
        }

        // exe 프로그램 종료
        // ex) iexplore.exe -> iexplore
        public static void ExitExe(string strExeName)
        {
            Process[] ListProcess = Process.GetProcessesByName(strExeName);

            if (ListProcess.Length > 0)
            {
                for (int i = 0; i < ListProcess.Length; i++)
                {
                    ListProcess[i].Kill();
                }
            }
        }
        public static DataTable toDataTable(DataGridView dgv)
        {
            DataTable dt = new DataTable("t1");
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                dt.Columns.Add(col.DataPropertyName);
            }
            return dt;
        }

        public static string toMD5(string strKey)
        {
            return FormsAuthentication.HashPasswordForStoringInConfigFile(strKey, "MD5");
        }

        public static void SaveData(string strDirName, string strFileName, string strData)
        {
            try
            {
                MakeDirectory(strDirName);
                using (StreamWriter sw = new StreamWriter(String.Format(@"{0}{1}.txt", strDirName, strFileName), true, Encoding.UTF8))
                {
                    sw.WriteLine(strData);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch { }
        }

        public static void SetErrorLog(string strError)
        {
            string strNowDay = DateTime.Now.ToString("ERROR_yyyyMMdd");
            string strNowTime = DateTime.Now.ToString("HH:mm:ss");
            SaveData(LOCAL_DIR + ERROR_DIR, strNowDay, String.Format("[{0}], OSP = [{1}], 메세지 = [{2}]", strNowTime, RUNNING_OSP, strError));
        }

        public static string GetFirstIPv4()
        {
            try
            {
                Regex regex = new Regex(@"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$");

                foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
                {
                    if (regex.IsMatch(ip.ToString()))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception) { }

            try
            {
                ManagementObjectSearcher query = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled='TRUE'");
                ManagementObjectCollection queryCol = query.Get();
                foreach (ManagementObject mo in queryCol)
                {
                    string[] address = (string[])mo["IPAddress"];
                    foreach (string ipaddress in address)
                    {
                        return ipaddress;
                    }
                }
            }
            catch (Exception) { }

            return "localhost";
        }

        public static string GetToday()
        {
            //             List<string> listResult = new List<string>();
            //             clsDBProc.GetToday(ref listResult);
            // 
            //             if (listResult.Count > 0)
            //             {
            //                 if (listResult[0].Length >= 14)
            //                 {
            //                     return listResult[0];
            //                 }
            //             }

            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public static void RateOfConcordanceParse(string strStr, ref List<string> listData, int nWordMinimumLength = 2)
        {
            try
            {
                //특수문자제거
                string strTemp = Regex.Replace(strStr, @"[^a-zA-Z0-9가-힣]", "|", RegexOptions.Singleline);

                //연속된 특수문자로 |가 여러개 있는경우 하나로 합침
                strTemp = Regex.Replace(strTemp, @"[|]+", "|", RegexOptions.Singleline).Trim(new char[] { '|' }).Trim();

                string[] arrTemp = strTemp.Split(new char[] { '|' });

                string strBuffer = string.Empty;
                foreach (string s in arrTemp)
                {
                    if (s.Length > 0)
                    {
                        strBuffer += s;

                        if (strBuffer.Length >= nWordMinimumLength)
                        {
                            int nCount = listData.Count;
                            bool isValue = false;
                            for (int i = 0; i < nCount; i++)
                            {
                                if (String.Compare(strBuffer, listData[i], false) == 0)
                                {
                                    isValue = true;
                                    break;
                                }
                            }

                            if (isValue == false)
                            {
                                listData.Add(strBuffer);
                            }

                            strBuffer = string.Empty;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        //AllowUnsafeHeaderParsing이값을 설정파일에서 줄경우 config파일이 필요해서
        //코드로 설정한다.
        public static bool ToggleAllowUnsafeHeaderParsing(bool enable)
        {
            //Get the assembly that contains the internal class
            Assembly assembly = Assembly.GetAssembly(typeof(SettingsSection));
            if (assembly != null)
            {
                //Use the assembly in order to get the internal type for the internal class
                Type settingsSectionType = assembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (settingsSectionType != null)
                {
                    //Use the internal static property to get an instance of the internal settings class.
                    //If the static instance isn't created already invoking the property will create it for us.
                    object anInstance = settingsSectionType.InvokeMember("Section",
                    BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });
                    if (anInstance != null)
                    {
                        //Locate the private bool field that tells the framework if unsafe header parsing is allowed
                        FieldInfo aUseUnsafeHeaderParsing = settingsSectionType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (aUseUnsafeHeaderParsing != null)
                        {
                            aUseUnsafeHeaderParsing.SetValue(anInstance, enable);
                            return true;
                        }

                    }
                }
            }

            return false;
        }

        public static void WindowClose(string strTitle)
        {
            IntPtr hWnd = APIs.FindWindow(null, strTitle);
            if (hWnd != IntPtr.Zero)
            {
                bool btmp = false;
                if (OspQuart == 37 || OspQuart == 39 ) // 돈디스크(PC) 이벤트 팝업 종료. 
                    APIs.PostMessage(hWnd, (uint)APIs.WindowMessages.KEYDOWN, (uint)0x0D, 0);   // keydown + enter 
                else
                    //APIs.PostMessage(hWnd, (uint)APIs.WindowMessages.CLOSE, 0, 0);
                    btmp = APIs.PostMessage(hWnd, (uint)APIs.WindowMessages.CLOSE, (uint)0x0D, 0);
                btmp = APIs.PostMessage(hWnd, (uint)APIs.WindowMessages.KEYDOWN, (uint)0x0D, 0);
            }
        }

        [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
        private static extern int UrlMkSetSessionOption(
            int dwOption, string pBuffer, int dwBufferLength, int dwReserved);

        const int URLMON_OPTION_USERAGENT = 0x10000001;

        public static void ChangeUserAgent()
        {
            List<string> userAgent = new List<string>();
            string ua = "Mozilla/5.0 (Linux; Android 4.4.2; Nexus 4 Build/KOT49H) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.114 Mobile Safari/537.36";
            //string ua = "Mozilla/5.0 (Linux; Android 4.4.2; Nexus 4 Build/KOT49H) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.114 Mobile Safari/537.36";

            //string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko";
            //string ua="Mozilla/5.0 (iPad; CPU OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5376e Safari/8536.25";

            //string ua = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_3) AppleWebKit/537.75.14 (KHTML, like Gecko) Version/7.0.3 Safari/7046A194A";
            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
        }
        public static void ChangeUserAgent2()
        {
            List<string> userAgent = new List<string>();
            //string ua = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko";

            // string ua="Mozilla/5.0 (iPad; CPU OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5376e Safari/8536.25";
            string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Whale/2.7.99.20 Safari/537.36";
            //string ua = "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 70.0.3538.110 Whale / 1.4.64.6 Safari / 537.36";
           //string ua = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_3) AppleWebKit/537.75.14 (KHTML, like Gecko) Version/7.0.3 Safari/7046A194A";
            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
        }


        public static void ChangeUserAgent_Safari()
        {
            List<string> userAgent = new List<string>();
            string ua = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/534.7 (KHTML, like Gecko) Chrome/7.0.517.44 Safari/534.7";
            //string ua = "Mozilla/5.0 (MSIE 10.0; Windows NT 6.1; Trident/5.0)";
            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
        }
        public static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }

        public static int GetFileCount(string strFilePath)
        {

            int nReturn = 0;
            if (Directory.Exists(strFilePath))
            {
                string[] pszfileEntries = Directory.GetFiles(strFilePath);
                foreach (string szfileName in pszfileEntries)
                {
                    nReturn = nReturn + 1;
                }
            }
            return nReturn;
        }
        // str 문자열에서 value 문자열을 찾아 bool type 반환
        public static bool Contains(string str, string value)
        {
            bool ret = str.Contains(value);
            return ret;
        }
    }
}
