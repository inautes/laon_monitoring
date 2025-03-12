using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using mshtml;
using Oracle.ManagedDataAccess.Client;
using System.Security.Cryptography;
using Tamir.SharpSsh.java.io;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Net;

using Microsoft.Web.WebView2;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;


namespace OSPAutoSearch_AutoLogin
{

    public partial class frmMain : Form
    {
        private Font mGFont = new Font("굴림", 30, FontStyle.Bold);

        string m_html = string.Empty;
        private delegate void SafeCallDelegate(string text);

        private IOSPCrawler mOSPCrawler;
        private IOSPCrawlerEdge mOSPCrawlerEdge;

        //채증프로그램 기본정보
        private string mLocalIP = string.Empty;
        private string mSearchGenre = string.Empty;
        private string mSearchSTime = string.Empty;
        private string mSearchETime = string.Empty;
        //옵션
        private int mLogLineCount = 0;  //로그 초기화용
        //옵션 2
        private int mOptInt = 0;
        private string mOptStr = string.Empty;
        private int mLoginInitTime = 0;//로그인페이지 자체가 로드되지않고 있는시간        
        private int mLoginRefreshTime = 0;//로그인이후 정상적인처리까지 걸리는시간
        private int mLoginFailedTime = 0;//로그인으로 넘어가지않고 있는시간
        private int mJobType = 0;        //0 : 크롤링, 1 : 재수집
        //OSP정보
        private OSP_INFO mOSPInfo = new OSP_INFO();
        //FTP방식 업로드
        private clsSFtp mFTP = new clsSFtp();
        //채증 초기화 단계
        private int mNowStatus = -1;
        //페이지관련
        private int mLimitePage = 10;
        private int mPageIndex = -1;
        //팝업관련 인덱스
        private int mPopupIndex = -1;
        //Timer를 이용해 게시판을 분석하기때문에 브라우저이벤트를 사용할수없어 분석관련정보를 저장한다.        
        private bool mInitDocumentCompleted = false;  //게시판 초기화유무       
        //Timer를 이용해 게시판을 분석하기때문에 브라우저이벤트를 사용할수없어 분석관련정보를 저장한다.
        private string mPopupURL = string.Empty;  //팝업 URL저장        
        private int mHomeURLIndex = 0;//시작 URL목록 저장용 DataTable 및 인덱스
        private DataTable mHomeURL = null;
        private DataTable mSearchData = null;//게시물 저장용 DataTable
        private Image mImgBoardList = null;//게시물목록 캡쳐이미지        
        private Image mImgCapture = null;//게시물 캡쳐 완성본        
        private clsProxy mProxyList = null;//프록시정보 로드 및 설정        
        private bool mProcessExitFlag = false;//프로세서가 종료될 경우 실행중인 쓰레드를 종료시키기위한 플레그
        private int mCategoryMoveType = 0;
        // 추가적으로 ID 정보 지우기.
        private bool mRemoveID = false;
        private Rectangle mRemoveRect;
        System.Drawing.Color mBrushColor = Color.White;
        private string mBoardNo = string.Empty;
        List<string> listPopup = new List<string>();//게시물목록        
        List<string> listPopup2 = new List<string>();//상세페이지
        public string mstrpage;
        private const string InternetExplorerRootKey = @"Software\Microsoft\Internet Explorer";
        private const string BrowserEmulationKey = InternetExplorerRootKey + @"\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
        public const string strMobileUA = "Mozilla/5.0 (Linux; Android 4.4.2; Nexus 4 Build/KOT49H) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.114 Mobile Safari/537.36"; int m_nIndex = 1, m_nJob = 0;
        bool bFirst = true;
        int m_nDelayTime = 0;
        int m_nDelayTimeGenre = 0;
        int mCategoryCount = 0;
        int mCategory = 0;
        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            clsUtil.ToggleAllowUnsafeHeaderParsing(true);//빅파일에서 인터넷체크시 문제발생떄문에 처리
            clsUtil.MakeDirectory(clsSFtp.mLOCALDIR);
            mLocalIP = clsUtil.GetFirstIPv4();
            mSearchData = clsUtil.toDataTable(dgvSearchData);//DataTable을 미리 바인딩시켜놓고 데이타만 갱신시킨다.
            dgvSearchData.DataSource = mSearchData;
            cbOSPType.SelectedIndex = 0;
            cbLimitePage.SelectedIndex = 0;
            cbSearchTime.SelectedIndex = 0;
            clsUtil.RunExe(@"C:\Program Files (x86)\KRISS\UTCk3.1\UTCk3.exe", "UTCk3");//실행 프로그램 실행
            trInit.Enabled = true;
        }
        private void trInit_Tick(object sender, EventArgs e)
        {
            trInit.Enabled = false;
            StartSearch(m_nIndex, m_nJob);
            
        }
        private void StartSearch(int nIndex, int nJob)
        {
            //int nOSPType = 38;   //스마트파일
            //int nOSPType = 4; //메가파일
            //int nOSPType = 48;   //스마트파일 모바일
            //int nOSPType = 17;   //파일쿠키
            //int nOSPType = 12;   //빅파일
            //int nOSPType = 42;    // 파일캐스트
            //int nOSPType = 94;      // 토토디스크
            //int nOSPType = 132;        // 파일몽
            //int nOSPType = 2;         //쉐어박스
            //int nOSPType = 5;       //파일시티
           int nOSPType = 23;          // 파일조
            
            //int nOSPType = 7;    // 파일노리

            int nLimitePage = 4;
            int nHomeURLIndex = 0;
            int nJobType = 0; // 0 일반수집 , 1 재수집
            int nUseProxy = 0;
            string[] arrArgs = Environment.GetCommandLineArgs();
            int nArgc = arrArgs.Length;
            if (nArgc >= 4)
            {
                for (int i = 1; i < nArgc; i++)
                {
                    switch (i)
                    {
                        case 1: nOSPType = clsUtil.IntParse(arrArgs[i]); break;
                        case 2: nLimitePage = clsUtil.IntParse(arrArgs[i]); break;
                        case 3: nHomeURLIndex = clsUtil.IntParse(arrArgs[i]); break;
                        case 4: nJobType = clsUtil.IntParse(arrArgs[i]); break;
                        case 5: nUseProxy = clsUtil.IntParse(arrArgs[i]); break;
                    }
                }
            }
            cbOSPType.SelectedIndex = nOSPType;
            mHomeURLIndex = nHomeURLIndex;
            cbLimitePage.SelectedIndex = nLimitePage;
            mJobType = nJobType;
            if (nUseProxy > 0)
            {
                mProxyList = new clsProxy();
                mProxyList.ProxyConnectedEventHandler += new clsProxy.ProxyConnnected(mProxyList_ProxyConnectedEventHandler);
                mProxyList.SetProxy(cbOSPType.SelectedIndex);
                mProxyList.SetProxyRun2();
                Console.WriteLine("Use Proxy");
            }
            else
            {
                btnRun_Click(null, null);
                Console.WriteLine("Run Click");

            }
        }

        public void mProxyList_ProxyConnectedEventHandler(bool isResult, string strResultMsg)
        {
            if (isResult == true)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    btnRun_Click(null, null);
                }));
            }
            else
            {
                clsUtil.SetErrorLog("프록시 연결실패");
                Close();
            }
        }
        private bool readAuthInfo() // 설정파일 읽기
        {
            bool ret = false;
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(@"./Config/AuthConfig.ini");

                string line;
                string[] authInfo;
                string selOspStr = cbOSPType.Items[cbOSPType.SelectedIndex].ToString().Trim();

                while ((line = file.ReadLine()) != null)
                {
                    if (line.IndexOf("DelayTime") != -1)
                    {
                        m_nDelayTime = Convert.ToInt32(clsUtil.SubStringEx(line, "DelayTime=", 1, ""));
                    }
                    else if (line.IndexOf("GenreDT") != -1)
                    {
                        m_nDelayTimeGenre = Convert.ToInt32(clsUtil.SubStringEx(line, "GenreDT=", 1, ""));
                    }
                    else
                    {
                        authInfo = line.Split('/');
                        if (authInfo[0].Trim() == selOspStr)
                        {
                            tbID.Text = authInfo[1].Trim();
                            if (authInfo[2].Trim() != "null") tbPwd.Text = authInfo[2].Trim();
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show("AuthConfig.ini 로드 실패!");
            }
            return ret;
        }
        private bool readId()
        {
            bool ret = false;
            {
                System.IO.StreamReader file = new System.IO.StreamReader(@"./Config/AuthConfig_ID.ini");
                System.IO.StreamReader file2 = new System.IO.StreamReader(@"./Config/AuthConfig_ID.ini");

                string line;
                string[] authInfo;
                string selOspStr = cbOSPType.Items[cbOSPType.SelectedIndex].ToString().Trim();
                Random rand = new Random();
                int nCount = 0;
                while (file2.ReadLine() != null)
                {
                    nCount++;
                }
                int nRandom = rand.Next(0, nCount);
                nCount = 0;
                while ((line = file.ReadLine()) != null)
                {
                    if (nCount != nRandom)
                    {
                        nCount++;
                        continue;
                    }
                    authInfo = line.Split('/');
                    tbID.Text = authInfo[0].Trim();
                    tbPwd.Text = authInfo[1].Trim();
                    break;
                }
            }
            return ret;
        }

        private void cbOSPType_SelectedIndexChanged(object sender, EventArgs e)
        {

            clsUtil.OspQuart = cbOSPType.SelectedIndex;       

            //tbID.Text = "";
            //tbPwd.Text = "";

            if (readAuthInfo()) return;    // 정보가 없거나 read 실패시 기존 루트를 따라간다..
            switch (cbOSPType.SelectedIndex)
            {
                #region OSP_tbID&Pwd                
                case 1: tbID.Text = "vneld33a"; tbPwd.Text = "qlqlvks123"; break; // 1 : 피디팝
                case 2: tbID.Text = "wlsxhdwp12"; tbPwd.Text = "asdfg56789"; break;  // 2 : 쉐어박스
                case 3: tbID.Text = "rkanfcl14"; tbPwd.Text = "cvbnm123$5"; break;  // 3 : 위디스크                    
                case 4: tbID.Text = "TT12RR"; tbPwd.Text = "qwer1234"; break; // 4 : 메가파일
                case 5: tbID.Text = "zjvldndb124"; tbPwd.Text = "erty3454a"; break;// 5 : 파일시티                
                case 7: tbID.Text = "xhlrms12"; tbPwd.Text = "asdxc!234aa"; break;  // 7 : 파일노리
                case 12: tbID.Text = "fhqhzkq123"; tbPwd.Text = "tyui1234"; break; //12 : 빅파일
                case 13: tbID.Text = "dmsgodskan1"; tbPwd.Text = "qweas1234"; break; //13 : 티플
                case 15: tbID.Text = "tmxpsfl12"; tbPwd.Text = "hjkl7890"; break; // 15 : 미투디스크
                case 17: tbID.Text = "dnwnxkagja1"; tbPwd.Text = "dfgh!234a!"; break; //17 : 파일쿠키                
                case 23: tbID.Text = "qhtm23"; tbPwd.Text = "wsxedc1234a!!"; break; // 23 : 파일조
                //case 23: tbID.Text = "lala250306"; tbPwd.Text = "lala250306!"; break; // 23 : 파일조

                case 24: tbID.Text = "rndrmrdml13"; tbPwd.Text = "sdfg1234"; break; //24 : 애플파일
                case 25: tbID.Text = "soqnwk13"; tbPwd.Text = "qwer7890!"; break; //25 : 예스파일
                case 31: tbID.Text = "anwkd123"; tbPwd.Text = "mnb1234!"; break; // 31 : 온디스크                    
                case 32: tbID.Text = "skapzh123"; tbPwd.Text = "asdf!234aa"; break;//32 : 케이디스크                
                case 37: tbID.Text = "rnfmarhkwk23"; tbPwd.Text = "lkjh!234"; break; // 37 : 투디스크                    
                case 38: tbID.Text = "dbgksao@naver.com"; tbPwd.Text = "er1234ty98"; break; // 38 : 스마트파일  
                case 42: tbID.Text = "ehfrhfo23@naver.com"; tbPwd.Text = "asd1134zzx"; break; //42 : 파일캐스트
                case 43: tbID.Text = "tnddj1234"; tbPwd.Text = "wefgvb12#4"; break; //43 : 위디스크_모바일
                case 44: tbID.Text = "ahstmxj11"; tbPwd.Text = "asdf1234"; break; // 44 : 메가파일_모바일
                case 45: tbID.Text = "zmstkqkf14"; tbPwd.Text = "rksmdgks12"; break; //45 : 파일조_모바일
                case 46: tbID.Text = "dlfqhsdugod1"; tbPwd.Text = "xcvb!234zz"; break;//46 : 케이디스크_모바일
                case 48: tbID.Text = "rhkswjfxhd12@naver.com"; tbPwd.Text = "qwert0987!"; break; // 48 : 스마트파일_모바일
                case 49: tbID.Text = "thstnrjs123"; tbPwd.Text = "zxcas!23aa"; break; // 49 : 파일노리_모바일
                case 50: tbID.Text = "akfrmdskf12"; tbPwd.Text = "distks33aa"; break; // 50 : 피디팝_모바일
                case 51: tbID.Text = "qldhsmsskf12"; tbPwd.Text = "asdf1234!"; break; // 51 : 티플_모바일
                case 53: tbID.Text = "dpselvpswjd1"; tbPwd.Text = "asdf0987!"; break; // 53 : 예스파일_모바일
                case 54: tbID.Text = "rjseka1233"; tbPwd.Text = "ghjk123411"; break; //54 : 빅파일_모바일
                case 55: tbID.Text = "rkaehdwndml1"; tbPwd.Text = "zxcv!234"; break; // 55 : 온디스크_모바일
                case 56: tbID.Text = "smdfurwk13"; tbPwd.Text = "ppqwe123"; break; // 56 : 애플파일_모바일      
                case 64: tbID.Text = "dnemxhvlr12"; tbPwd.Text = "vbnm1234"; break; // 64 : 미투디스크_모바일
                case 65: tbID.Text = "qkskskdndb33"; tbPwd.Text = "cvbnd231a"; break; // 65 : 파일시티_모바일
                case 75: tbID.Text = "anfxltb123"; tbPwd.Text = "asqw123$"; break; // 75 : 투디스크_모바일
                case 79: tbID.Text = "dntks123aa"; tbPwd.Text = "!!vlfdygksa"; break; // 79 : 파일쿠키_모바일
                case 85: tbID.Text = "qhtmwjs12"; tbPwd.Text = "fhjk1234"; break; // 85 : 지디스크
                case 86: tbID.Text = "sa9c"; tbPwd.Text = "ghjk1234"; break; // 86 : 지디스크_모바일
                case 88: tbID.Text = "rmsdbrxhd123"; tbPwd.Text = "zxcvb12345"; break; //88 : 쉐어박스_모바일                    
                case 89: tbID.Text = "qkqvmffjtm44@naver.com"; tbPwd.Text = "yhntgb565a"; break; // 89 : 파일캐스트_모바일
                case 90: tbID.Text = "dnjstnddl12"; tbPwd.Text = "qazwsx123"; break; // 90 : 파일맨
                case 91: tbID.Text = "gnsals123"; tbPwd.Text = "ijnuhb123"; break; // 91 : 파일맨_모바일 
                case 94: tbID.Text = "wlsxhdwp134"; tbPwd.Text = "sder1234"; break; //94 : 토토로사_모바일                
                case 100: tbID.Text = "dksrudwkqdl12"; tbPwd.Text = "asdf!234"; break; //100 : 유뷰
                case 102: tbID.Text = "ehlwlqk14s"; tbPwd.Text = "edcrfv#$as"; break; //102 : 파일이즈
                case 103: tbID.Text = "tkqkfaus12a"; tbPwd.Text = "lkjh789$aa"; break; //103 : 파일이즈_모바일                   
                case 126: tbID.Text = "apfhsk12a"; tbPwd.Text = "edc2345ab!"; break; //126 : 파일썬
                case 127: tbID.Text = "qlqlzlr13a"; tbPwd.Text = "!2asd23bb"; break; //127 : 파일썬_모바일
                case 132: tbID.Text = "ausqhd123"; tbPwd.Text = "tfcygv123aa"; break; //132 : 파일몽
                case 133: tbID.Text = "rltnfwk123"; tbPwd.Text = "okmnji09811"; break; //133 : 파일몽_모바일
                case 136: tbID.Text = "gmflsgksmf2"; tbPwd.Text = "rtyu1234"; break; //136 : 파일마루
                case 137: tbID.Text = "dkcjzldnrl123"; tbPwd.Text = "okmijn1234"; break; //137 : 파일마루_모바일
                case 138: tbID.Text = "zkdn123a"; tbPwd.Text = "asdf1234bb"; break; //138 : 파일보고
                case 139: tbID.Text = "aortla123"; tbPwd.Text = "zxcv1234zz"; break; //139 : 파일보고_모바일
                case 144: tbID.Text = "akfncl123"; tbPwd.Text = "asdf12346!"; break; //144 : 오뜨
                case 145: tbID.Text = "wjdvna14"; tbPwd.Text = "poiu123!"; break; //145 : 오뜨_모바일
                case 146: tbID.Text = "rpqhrcl13"; tbPwd.Text = "1234asdf!"; break; //146 : 싸다파일
                case 147: tbID.Text = "dydrltk12"; tbPwd.Text = "0987qwer!"; break; //147 : 싸다파일_모바일
                case 148: tbID.Text = "tjsvndrl12@naver.com"; tbPwd.Text = "ghjkl34567"; break;//148 : 유씨씨
                case 152: tbID.Text = "tmslzjwm12@naver.com"; tbPwd.Text = "poiu789a"; break; //152 : 파일스타
                case 153: tbID.Text = "vpsej12a"; tbPwd.Text = "erdf3456a"; break; //153 : 파일스타_모바일
                case 154: tbID.Text = "vkrhlwk123"; tbPwd.Text = "zxcv1234"; break; // 154 : 메타파일
                case 155: tbID.Text = "tmajvm123"; tbPwd.Text = "uiop7890"; break; // 155 : 메타파일_모바일
                case 156: tbID.Text = "akdxorl12"; tbPwd.Text = "akdxorl12"; break; // 154 : 파일고수
                case 157: tbID.Text = "ekffuraos1"; tbPwd.Text = "ppasdf1234"; break; // 155 : 파일고수_모바일
            }
        }
        #endregion
        public void WebPopupClose()
        {
            while (mProcessExitFlag == true) //팝업창이 있는경우 자동으로 팝업창을 종료함
            {
                clsUtil.WindowClose("웹 페이지 메시지");
                clsUtil.WindowClose("빈 페이지 - Internet Explorer");
                Thread.Sleep(1000);
            }
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {

            if (cbOSPType.SelectedIndex == 140 || cbOSPType.SelectedIndex == 141)
                trTimeoutClose.Enabled = false;
            else
                trTimeoutClose.Enabled = true;

            //웹팝업이 뜰경우 자동으로 닫는 쓰레드 생성
            mProcessExitFlag = true;
            new Thread(new ThreadStart(WebPopupClose)).Start();

            //로그인전 상태로 세팅
            mNowStatus = 0;
            mLoginRefreshTime = 0;
            mLoginFailedTime = 0;

            mOSPInfo.OSP_TYPE = "SITE0010";


            //// WebView2가 초기화될 때까지 대기
            //await webMain2.EnsureCoreWebView2Async(null);

            //// 쿠키 삭제 로직을 추가 (쉐어박스 선택 시)
            //if (cbOSPType.SelectedIndex == 2)
            //{
            //    // 브라우저 캐시 삭제
            //    await ClearCacheUsingJS();  // 캐시 삭제


            //    await DeleteAllCookiesUsingJS();  // WebView2가 초기화된 후에 쿠키 삭제
            //                                      // 쿠키가 삭제되었는지 확인
            //    //await AreCookiesDeletedAsync("https://sharebox.co.kr");
                
            //}


            #region 로그인페이지 및 초기화
            switch (cbOSPType.SelectedIndex)
            {
                case 1: mOSPInfo.OSP_ID = "PDPOP"; mOSPCrawlerEdge = new clsPdPop(); break; // 1 : 피디팝
                case 2: mOSPInfo.OSP_ID = "SHAREBOX"; mOSPCrawlerEdge = new clsShareBox(); break; // 2 : 쉐어박스
                case 3: mOSPInfo.OSP_ID = "WEDISK"; mOSPCrawlerEdge = new clsWeDisk(); break;// 3 : 위디스크
                case 4: mOSPInfo.OSP_ID = "MEGAFILE"; mOSPCrawlerEdge = new clsMegaFile(); break; // 4 : 메가파일
                case 5: mOSPInfo.OSP_ID = "FILECITY"; mOSPCrawlerEdge = new clsFileCity(); break; // 5 : 파일시티
                case 7: mOSPInfo.OSP_ID = "FILENORI"; mOSPCrawlerEdge = new clsFileNori(); break; // 7 : 파일노리
                case 12: mOSPInfo.OSP_ID = "BIGFILE"; mOSPCrawlerEdge = new clsBigFile(); break;// 12 : 빅파일
                case 13: mOSPInfo.OSP_ID = "TPLE"; mOSPCrawlerEdge = new clsTPle(); break; //13 : 티플
                case 15: mOSPInfo.OSP_ID = "M2DISK"; mOSPCrawlerEdge = new clsMe2Disk(); break; //15 : 미투디스크
                case 17: mOSPInfo.OSP_ID = "FILEKUKI"; mOSPCrawlerEdge = new clsFileKuki(); break; //17 : 파일쿠키
                case 23: mOSPInfo.OSP_ID = "FILEJO"; mOSPCrawlerEdge = new clsFileJo(); break;//23 : 파일조
                case 24: mOSPInfo.OSP_ID = "APPLEFILE"; mOSPCrawlerEdge = new clsAppleFile(); break;//24 : 애플파일
                case 25: mOSPInfo.OSP_ID = "YESFILE"; mOSPCrawlerEdge = new clsYesfileEdge(); break;//25 : 예스파일
                case 31: mOSPInfo.OSP_ID = "ONDISK"; mOSPCrawlerEdge = new clsOnDisk(); break; //31 : 온디스크
                case 32: mOSPInfo.OSP_ID = "KDISK"; mOSPCrawlerEdge = new clsKDisk(); break;//32 : 케이디스크
                case 37: mOSPInfo.OSP_ID = "TODISK"; mOSPCrawlerEdge = new clsToDisk(); break;//37 : 투디스크
                case 38: mOSPInfo.OSP_ID = "SMARTFILE"; mOSPCrawlerEdge = new clsSmartFile(); break; //38 : 스마트파일
                case 42: mOSPInfo.OSP_ID = "FILECAST"; mOSPCrawlerEdge = new clsFileCast(); break; //42 : 파일캐스트
                case 43: mOSPInfo.OSP_ID = "WEDISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsWeDisk_M(); break; //43 : 위디스크_모바일
                case 44: mOSPInfo.OSP_ID = "MEGAFILE"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsMegaFileEdge_M(); break; //44 : 메가파일_모바일
                case 45: mOSPInfo.OSP_ID = "FILEJO"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFileJo_M(); break; //45 : 파일조_모바일
                case 46: mOSPInfo.OSP_ID = "KDISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsKDisk_M(); break;   //46 : 케이디스크_모바일
                case 48: mOSPInfo.OSP_ID = "SMARTFILE"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsSmartFileEdge_M(); break; //48 : 스마트파일_모바일
                case 49: mOSPInfo.OSP_ID = "FILENORI"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFileNori_M(); break; //49 : 파일노리_모바일
                case 50: mOSPInfo.OSP_ID = "PDPOP"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsPdPop_M(); mCategoryMoveType = 1; break; //50 : 피디팝_모바일
                case 51: mOSPInfo.OSP_ID = "TPLE"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsTPle_M(); break; //51 : 티플_모바일
                case 53: mOSPInfo.OSP_TYPE = "SITE0020"; mOSPInfo.OSP_ID = "YESFILE"; mOSPCrawlerEdge = new clsYesFile_M(); break; //53 : 예스파일_모바일
                case 54: mOSPInfo.OSP_ID = "BIGFILE"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsBigFile_M(); break; //54 : 빅파일_모바일                    
                case 55: mOSPInfo.OSP_ID = "ONDISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsOnDisk_M(); break; //55 : 온디스크_모바일
                case 56: mOSPInfo.OSP_ID = "APPLEFILE"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsAppleFile_M(); break; //56 : 애플파일_모바일
                case 64: mOSPInfo.OSP_ID = "M2DISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsMe2Disk_M(); break; //64 : 미투디스크_모바일
                case 65: mOSPInfo.OSP_ID = "FILECITY"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFileCity_M(); break; //65 : 파일시티_모바일                   
                case 75: mOSPInfo.OSP_ID = "TODISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsToDisk_M(); break; //75 : 투디스크_모바일
                case 79: mOSPInfo.OSP_ID = "FILEKUKI"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFileKuki_M(); mCategoryMoveType = 1; break; //79 : 파일쿠키_모바일
                case 85: mOSPInfo.OSP_ID = "GDISK"; mOSPCrawlerEdge = new clsGdisk(); break; //85 : 지디스크
                case 86: mOSPInfo.OSP_ID = "GDISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsGDisk_M(); break; //86 : 지디스크_모바일
                case 88: mOSPInfo.OSP_ID = "SHAREBOX"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsShareBox_M(); mCategoryMoveType = 1; break; //88 : 쉐어박스_모바일
                case 89: mOSPInfo.OSP_ID = "FILECAST"; mOSPInfo.OSP_TYPE = "SITE0020"; mCategoryMoveType = 1; mOSPCrawlerEdge = new clsFileCast_M(); break; //89 : 파일캐스트_모바일
                case 90: mOSPInfo.OSP_ID = "FILEMAN"; mOSPCrawlerEdge = new clsFileMan(); clsFileMan clsFileMan = mOSPCrawlerEdge as clsFileMan; break; //90 : 파일맨
                case 91: mOSPInfo.OSP_ID = "FILEMAN"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFileMan_M(); break; //91 : 파일맨_모바일
                case 94: mOSPInfo.OSP_ID = "TOTODISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsTotorosa_M(); break; //94 : 토토로사_모바일                    
                case 100: mOSPInfo.OSP_ID = "YOUVIEW"; mOSPCrawlerEdge = new clsYouview(); break;//100 : 유뷰
                case 102: mOSPInfo.OSP_ID = "FILEIS"; mOSPCrawlerEdge = new clsFileis(); break;//102 : 파일이즈
                case 103: mOSPInfo.OSP_ID = "FILEIS"; mOSPCrawlerEdge = new clsFileis_M(); mOSPInfo.OSP_TYPE = "SITE0020"; break;//103 : 파일이즈_모바일
                case 126: mOSPInfo.OSP_ID = "FILESUN"; mOSPCrawlerEdge = new clsFilesun(); break;//126 : 파일썬
                case 127: mOSPInfo.OSP_ID = "FILESUN"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFilesunEdge_M(); break;//127 : 파일썬_모바일 
                case 132: mOSPInfo.OSP_ID = "FILEMONG"; mOSPCrawlerEdge = new clsFilemong(); break;//132 : 파일몽                    
                case 133: mOSPInfo.OSP_ID = "FILEMONG"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFilemong_M(); break;//133 : 파일몽_모바일
                case 136: mOSPInfo.OSP_ID = "FILEMARU"; mOSPCrawlerEdge = new clsFilemaru(); break;//136 : 파일마루
                case 137: mOSPInfo.OSP_ID = "FILEMARU"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFilemaru_M(); break;//137 : 파일마루_모바일
                case 138: mOSPInfo.OSP_ID = "FILEBOGO"; mOSPCrawlerEdge = new clsFilebogo(); break; //138 : 파일보고 
                case 139: mOSPInfo.OSP_ID = "FILEBOGO"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFilebogo_M(); break; //139 : 파일보고_모바일                                   
                case 144: mOSPInfo.OSP_ID = "ANTDISK"; mOSPCrawlerEdge = new clsOottx(); break;//144 : 앤트디스크                     
                case 145: mOSPInfo.OSP_ID = "ANTDISK"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsOottx_M(); break; //145 : 앤트디스크_모바일
                case 146: mOSPInfo.OSP_ID = "SSADAFILE"; mOSPCrawlerEdge = new clsSsadafile(); break; //146 : 싸다파일
                case 147: mOSPInfo.OSP_ID = "SSADAFILE"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsSsadafile_M(); break;//147 : 싸다파일_모바일
                case 148: mOSPInfo.OSP_ID = "UCC"; mOSPCrawlerEdge = new clsUcc(); break;//146 : 유씨씨                
                case 152: mOSPInfo.OSP_ID = "FILESTAR"; mOSPCrawlerEdge = new clsFilestar(); break;//152 : 파일스타
                case 153: mOSPInfo.OSP_ID = "FILESTAR"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFilestar_M(); break;//153 : 파일스타_모바일
                case 154: mOSPInfo.OSP_ID = "METAFILE"; mOSPCrawlerEdge = new clsMetaFile(); break;//154 : 메타파일
                case 155: mOSPInfo.OSP_ID = "METAFILE"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsMetaFile_M(); break;//155 : 메타파일_모바일
                case 156: mOSPInfo.OSP_ID = "FILEGOSU"; mOSPCrawlerEdge = new clsFilegosu(); break;//154 : 파일고수
                case 157: mOSPInfo.OSP_ID = "FILEGOSU"; mOSPInfo.OSP_TYPE = "SITE0020"; mOSPCrawlerEdge = new clsFilegosu_M(); break;//155 : 파일고수_모바일
            }
            #endregion


            if (clsUtil.isCompare(mOSPInfo.OSP_TYPE, "SITE0020") == true)
            {
                if (cbOSPType.SelectedIndex != 46 && cbOSPType.SelectedIndex != 49 && cbOSPType.SelectedIndex != 53 && cbOSPType.SelectedIndex != 56 && cbOSPType.SelectedIndex != 89 && cbOSPType.SelectedIndex != 87 && cbOSPType.SelectedIndex != 44)
                {
                    await webMain2.EnsureCoreWebView2Async(null);
                    ChangeUserAgent();
                }
            }
            else clsUtil.ChangeUserAgent2();
            clsUtil.RUNNING_OSP = mOSPInfo.OSP_ID;
            GetOSPInfo();
            GetHomeURL();
            mFTP.Start(mOSPInfo.SITE_ID);//프로그램 실행시 FTP를 연결한다.
            mSearchSTime = clsUtil.GetToday();//검색 시작시간을 저장한다. 채증완료 후 로그를 기록할때 사용한다.
            mSearchETime = string.Empty;
            //쌓여있는 이미지를 먼저 보내고 시작(이미지 전송 오류가 나면 사용)
            if (false)
            {
                string mLOCALDIR = @"C:\evidence_img\";
                string strLocalDirPath = String.Format(@"{0}{1}\", mLOCALDIR, mOSPInfo.SITE_ID);
                while (true)
                {
                    DirectoryInfo dir = new DirectoryInfo(strLocalDirPath);
                    if (dir.Exists == true)
                    {
                        if (dir.GetFiles().Length > 0)
                        {
                            mFTP.Start(mOSPInfo.SITE_ID);
                            clsUtil.Delay(10000);
                        }
                        else
                        {
                            break;
                        }

                    }
                    else
                    {
                        break;
                    }
                }
            }
            await SetStartPage();
        }

        public void ChangeUserAgent()
        {
            webMain2.CoreWebView2.Settings.UserAgent = strMobileUA;
        }

        private async Task ClearCacheUsingJS()
        {
            try
            {
                if (webMain2.CoreWebView2 != null)
                {
                    string clearCacheScript = @"
                if ('caches' in window) {
                    caches.keys().then(function(keyList) {
                        return Promise.all(keyList.map(function(key) {
                            return caches.delete(key);
                        }));
                    });
                }
            ";

                    // JavaScript를 실행하여 브라우저 캐시 삭제
                    await webMain2.CoreWebView2.ExecuteScriptAsync(clearCacheScript);
                    MessageBox.Show("브라우저 캐시가 삭제되었습니다.");
                }
                else
                {
                    MessageBox.Show("WebView2가 초기화되지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"캐시 삭제 중 오류 발생: {ex.Message}");
            }
        }

        private async Task DeleteAllCookiesUsingJS()
        {
            try
            {
                if (webMain2.CoreWebView2 != null)
                {
                    string deleteCookiesScript = @"
                var cookies = document.cookie.split(';');
                for (var i = 0; i < cookies.length; i++) {
                    var cookie = cookies[i];
                    var eqPos = cookie.indexOf('=');
                    var name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
                    // 모든 경로와 도메인에 대해 쿠키 삭제 시도
                    document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/';
                    document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;domain=.sharebox.co.kr;path=/';
                    document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;domain=' + location.hostname;
                    document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;domain=.' + location.hostname;
                }
            ";

                    // JavaScript를 실행하여 모든 쿠키를 삭제
                    await webMain2.CoreWebView2.ExecuteScriptAsync(deleteCookiesScript);
                    MessageBox.Show("모든 쿠키가 삭제되었습니다.");
                }
                else
                {
                    MessageBox.Show("WebView2가 초기화되지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿠키 삭제 중 오류 발생: {ex.Message}");
            }
        }


        private async Task<bool> AreCookiesDeletedAsync(string url)
        {
            try
            {
                if (webMain2.CoreWebView2 != null)
                {
                    var cookieManager = webMain2.CoreWebView2.CookieManager;

                    // 해당 URL의 쿠키 가져오기
                    var cookies = await cookieManager.GetCookiesAsync(url);

                    // 쿠키가 하나도 없다면 모든 쿠키가 삭제된 상태
                    if (cookies.Count == 0)
                    {
                        MessageBox.Show("모든 쿠키가 삭제되었습니다.");
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("아직 남아있는 쿠키가 있습니다.");
                        foreach (var cookie in cookies)
                        {
                            Console.WriteLine($"남아있는 쿠키: {cookie.Name} = {cookie.Value}");
                        }
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show("WebView2가 초기화되지 않았습니다.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿠키 확인 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SetStartPage()
        {
            mInitDocumentCompleted = false;
            await webMain2.EnsureCoreWebView2Async(null);

            #region 로그인페이지



            switch (cbOSPType.SelectedIndex)
            {
                case 1: webMain2.CoreWebView2.Navigate("https://member.pdpop.com/login/index_re.html"); break; // 1 : 피디팝
                case 2: webMain2.CoreWebView2.Navigate("https://sharebox.co.kr"); break;// 2 : 쉐어박스
                case 3: webMain2.CoreWebView2.Navigate("https://www.wedisk.co.kr/"); clsUtil.Delay(1000); webMain2.Refresh(); break;// 3 : 위디스크
                case 4: webMain2.CoreWebView2.Navigate("https://www.megafile.co.kr/main/index.php"); break;// 4 : 메가파일
                case 5: webMain2.CoreWebView2.Navigate("http://renew.filecity.co.kr/"); break;// 5 : 파일시티    
                case 7: webMain2.CoreWebView2.Navigate("http://www.filenori.com/"); break;// 7 : 파일노리
                case 11: webMain2.CoreWebView2.Navigate("https://www.gfile.co.kr/main/index_new.php"); break;// 11 : G파일
                case 12: webMain2.CoreWebView2.Navigate("https://www.bigfile.co.kr/"); break;// 12 : 빅파일
                case 13: webMain2.CoreWebView2.Navigate("https://www.tple.co.kr/_renew/memberShip.php?todo=login"); break;//13 : 티플
                case 15: webMain2.CoreWebView2.Navigate("https://me2disk.com/"); break; //15 : 미투디스크
                case 17: webMain2.CoreWebView2.Navigate("https://www.filekuki.com/kuki/main.jsp"); break; //17 : 파일쿠키
                case 23: webMain2.CoreWebView2.Navigate("https://www.filejo.com/main/main_html.php"); break; //23 : 파일조
                //case 23: webMain2.CoreWebView2.Navigate("https://www.filejo.com/main/"); break; //23 : 파일조
                case 24: webMain2.CoreWebView2.Navigate("http://www.applefile.com/"); break;//24 : 애플파일
                case 25: webMain2.CoreWebView2.Navigate("https://www.yesfile.com"); break;//25 : 예스파일                
                case 31: webMain2.CoreWebView2.Navigate("https://www.ondisk.co.kr/index.php"); break;//31 : 온디스크 
                case 32: webMain2.CoreWebView2.Navigate("https://www.kdisk.co.kr/index.php"); break;//32 : 케이디스크
                case 37: webMain2.CoreWebView2.Navigate("http://www.todisk.com/_main"); break;//37 : 투디스크
                case 38: webMain2.CoreWebView2.Navigate("https://smartfile.co.kr"); break;//38 : 스마트파일
                case 42: webMain2.ZoomFactor = 0.9; webMain2.CoreWebView2.Navigate("https://filecast.co.kr/www/home/main"); break;//42 : 파일캐스트
                case 43: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.wedisk.co.kr/mobile/login.jsp"); break;//43 : 위디스크_모바일
                case 44: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.megafile.co.kr/include/sidebar_menu.php"); break;//44 : 메가파일_모바일
                case 45: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.filejo.com/?m=member_login"); break; //45 : 파일조_모바일
                case 46: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.kdisk.co.kr/web/member/login.html"); break; //46 : 케이디스크_모바일                                                                            
                case 48: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.smartfile.co.kr/realrank/?rnk_cate1=2"); break; //48 : 스마트파일_모바일
                case 49: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.filenori.com/mobile/login.jsp"); break;//49 : 파일노리_모바일
                case 50: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.pdpop.com/_renew/login"); break;//50 : 피디팝_모바일                    
                case 51: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.tple.co.kr/_renew/memberShip.php?todo=login"); break;//51 : 티플_모바일
                case 53: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.yesfile.com/login"); break;//53 : 예스파일_모바일                                                            
                case 54: webMain2.CoreWebView2.Navigate("https://m.bigfile.co.kr/mobile/account/loginAp.php"); break;//54 : 빅파일_모바일
                case 55: webMain2.CoreWebView2.Navigate("https://m.ondisk.co.kr/web/member/login.html"); break; //55 : 온디스크_모바일
                case 56: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.applefile.com/#login=login"); break;//56 : 애플파일
                case 64: await webMain2.EnsureCoreWebView2Async(null); ChangeUserAgent(); webMain2.CoreWebView2.Navigate("http://m.me2disk.com/#list_1"); break;//64 : 미투디스크_모바일                    
                case 65: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.filecity.co.kr/#etc=login"); break; //65 : 파일시티                    
                case 75: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("http://m.todisk.com/mobile/main.php"); break;//75 : 투디스크_모바일                    
                case 79: webMain2.CoreWebView2.Navigate("https://m.filekuki.com/mobile/login.jsp"); break; //79 : 파일쿠키_모바일                    
                case 85: webMain2.CoreWebView2.Navigate("http://g-disk.co.kr/"); break;//85 : 지디스크
                case 86: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("http://m.g-disk.co.kr/index.html"); break; //86 : 지디스크_모바일
                case 88: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.sharebox.co.kr"); break; //88 : 쉐어박스_모바일                    
                case 89: webMain2.CoreWebView2.Navigate("https://m.filecast.co.kr"); break;//89 : 파일캐스트_모바일
                case 90: webMain2.CoreWebView2.Navigate("http://fileman.co.kr/"); break;//90 : 파일맨
                case 91: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.fileman.co.kr/member/login.html"); break; //91 : 파일맨_모바일
                case 94: webMain2.CoreWebView2.Navigate("https://m.totodisk.com/member/login.php?returnUrl=%2F"); break;//94 : 토토디스크_모바일
                case 100: webMain2.CoreWebView2.Navigate("https://www.youview.co.kr/login.do"); break;//100 : 유뷰                    
                case 102: webMain2.CoreWebView2.Navigate("https://fileis.com/"); break; //102 : 파일이즈                    
                case 103: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.fileis.com/doc/member/login_signin.php?viewLayerNo=1&target=page"); break;//103 : 파일이즈_모바일 
                case 126: webMain2.CoreWebView2.Navigate("http://www.filesun.com"); break;//126 : 파일썬                    
                case 127: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("http://m.filesun.com/"); break;//127 : 파일썬_모바일                                                    
                case 132: webMain2.CoreWebView2.Navigate("https://filemong.com/"); break;//132 : 파일몽
                case 133: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.filemong.com/member/login.html"); break;//133 : 파일몽_모바일
                case 134: webMain2.CoreWebView2.Navigate("https://gfile.co.kr/"); break;//134 : 지파일
                case 135: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.gfile.co.kr/"); break;//135 : 지파일_모바일
                case 136: webMain2.CoreWebView2.Navigate("https://www.filemaru.com/index.php"); break;//136 : 파일마루
                case 137: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.filemaru.com/"); break;//137 : 파일마루_모바일                    
                case 138: webMain2.CoreWebView2.Navigate("https://filebogo.com/"); break; //138 : 파일보고
                case 139: webMain2.CoreWebView2.Navigate("https://m.filebogo.com/doc.php?act=login"); break; //139 : 파일보고_모바일
                case 144: webMain2.CoreWebView2.Navigate("https://antdisk.kr/");  webMain2.ZoomFactor = 1.0; break; //144 : 오뜨
                case 145: webMain2.CoreWebView2.Navigate("https://m.antdisk.kr/"); break; //145 : 오뜨_모바일
                case 146: webMain2.CoreWebView2.Navigate("https://ssadafile.com/"); break;//146 : 싸다파일
                case 147: webMain2.CoreWebView2.Navigate("https://m.ssadafile.com/"); break;//147 : 싸다파일_모바일
                case 148: webMain2.CoreWebView2.Navigate("https://ucc.co.kr/"); break;//148 :유씨씨
                case 150: webMain2.CoreWebView2.Navigate("https://allplz.com/login.php"); break;//150 : 올플즈
                case 151: ChangeUserAgent(); webMain2.CoreWebView2.Navigate("https://m.allplz.com/login.php"); break;//151 : 올플즈_모바일
                case 152: webMain2.CoreWebView2.Navigate("https://filestar.co.kr"); break;//152 : 파일스타
                case 153: webMain2.CoreWebView2.Navigate("https://m.filestar.co.kr"); break;//153 : 파일스타_모바일
                case 154: webMain2.CoreWebView2.Navigate("https://metafile.co.kr/"); break; //154 : 메타파일
                case 155: webMain2.CoreWebView2.Navigate("https://m.metafile.co.kr/member/login.html"); break; //155 : 메타파일 모바일
                case 156: webMain2.CoreWebView2.Navigate("https://filegosu.com/"); break; //154 : 파일고수
                case 157: webMain2.CoreWebView2.Navigate("https://m.filegosu.com/"); break; //155 : 파일고수 모바일
            }
            trOSPBoardParsing.Enabled = true;
            mInitDocumentCompleted = true;
            return true;
            #endregion
        }

        private void GetOSPInfo()
        {//SITE0010 = PC            //SITE0020 = MOBILE(스트리밍)            //SITE0030 = MOBILE(다운로드)
            List<string> listResult = new List<string>();
            DataTable dtTemp = clsDBProc.GetOSPInfo(mOSPInfo.OSP_TYPE, mOSPInfo.OSP_ID, ref listResult);
            Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
            if (dtTemp != null && dtTemp.Rows.Count > 0)
            {
                mOSPInfo.SITE_ID = dtTemp.Rows[0].ItemArray[0].ToString(); //SITE_ID
                mOSPInfo.SITE_NAME = dtTemp.Rows[0].ItemArray[1].ToString(); //SITE_NAME
                mOSPInfo.SITE_TYPE = dtTemp.Rows[0].ItemArray[2].ToString(); //SITE_TYPE
                string strJobText = "자동수집";
                if (mJobType == 1) strJobText = "재수집";
                this.Text = mOSPInfo.SITE_NAME + "|" + strJobText;
                mOSPInfo.SITE_EQU = dtTemp.Rows[0].ItemArray[4].ToString();
                if (mOSPInfo.SITE_EQU.Length >= 8)
                {
                    mOSPInfo.CHECK_BOARD_ID = mOSPInfo.SITE_EQU.Substring(0, 1).CompareTo("1") == 0 ? true : false;
                    mOSPInfo.CHECK_UPLOADER_ID = mOSPInfo.SITE_EQU.Substring(1, 1).CompareTo("1") == 0 ? true : false;
                    mOSPInfo.CHECK_TITLE = mOSPInfo.SITE_EQU.Substring(2, 1).CompareTo("1") == 0 ? true : false;
                    mOSPInfo.CHECK_FILE_SIZE = mOSPInfo.SITE_EQU.Substring(3, 1).CompareTo("1") == 0 ? true : false;
                    mOSPInfo.CHECK_GENRE = mOSPInfo.SITE_EQU.Substring(4, 1).CompareTo("1") == 0 ? true : false;
                }
            }
        }

        private void GetHomeURL()
        {
            List<string> listResult = new List<string>();
            mHomeURL = clsDBProc.GetHomeURL(mOSPInfo.SITE_ID, ref listResult);
            Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
        }

        private async void HomeURLApply(Microsoft.Web.WebView2.WinForms.WebView2 web)
        {
            web.Stop();
            clsDBProc.PopClose(mOSPInfo.SITE_ID, false, ref listPopup);//페이지목록            
            clsDBProc.PopClose(mOSPInfo.SITE_ID, true, ref listPopup2);//상세페이지
            //페이지변경전에 이미 Timer가 돌고있기때문에 이전페이지를 수집해버리는문제가있어 페이지갱신여부를 확인한다.
            mInitDocumentCompleted = false;

            if (mHomeURL != null && mHomeURL.Rows.Count > 0)
            {
                if (mHomeURL.Rows.Count <= mHomeURLIndex)
                {
                    mHomeURLIndex = 0;
                }
                string strHomeType = mHomeURL.Rows[mHomeURLIndex]["HOME_TYPE"].ToString();
                {
                    if (cbOSPType.SelectedIndex == 48)
                    {
                        trOSPBoardParsing.Enabled = true; mInitDocumentCompleted = true;
                        webMain2.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
                        webMain2.CoreWebView2.Navigate(mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString());
                    }
                    if (cbOSPType.SelectedIndex == 38)
                    {
                        trOSPBoardParsing.Enabled = true; mInitDocumentCompleted = true;
                        string[] arrData = mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString().Split(new char[] { '|' });
                        string strScript = arrData[0] + "('" + arrData[1] + "')";
                        string html = await webMain2.CoreWebView2.ExecuteScriptAsync(strScript);
                    }
                    else if (cbOSPType.SelectedIndex == 88)
                    {
                        trOSPBoardParsing.Enabled = true; mInitDocumentCompleted = true;
                        webMain2.CoreWebView2.Navigate(mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString());
                    }
                    else
                    {
                        trOSPBoardParsing.Enabled = true; mInitDocumentCompleted = true;
                        webMain2.CoreWebView2.Navigate(mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString());
                    }
                }
                SetLog(String.Format("조회시작 => OSP : {0}, 장르 : {1}, URL : {2}", mOSPInfo.SITE_NAME,
                        mHomeURL.Rows[mHomeURLIndex]["TOP_GENRE"].ToString(), mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString()));
                lbGenre.Text = String.Format("{0}, {1}",
                    mHomeURL.Rows[mHomeURLIndex]["TOP_GENRE"].ToString(), mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString());
                //검색 장르를 저장한다. 채증완료 후 로그를 기록할때 사용한다.
                mSearchGenre = mHomeURL.Rows[mHomeURLIndex]["TOP_GENRE"].ToString(); 

                if (mCategoryMoveType == 1)
                {
                    mInitDocumentCompleted = true;
                    trOSPBoardParsing.Enabled = true;
                }
            }
            else SetLog("시작URL 정보없음");
        }

        private async void SearchStart()
        {
            
            switch (cbLimitePage.SelectedIndex)
            {
                case 0:
                case 1:
                case 2:
           
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    mLimitePage = cbLimitePage.SelectedIndex + 1;
                    break;
                case 10: mLimitePage = 20; break;
                case 11: mLimitePage = 30; break;
                case 12: mLimitePage = 40; break;
                case 13: mLimitePage = 50; break;
                case 14: mLimitePage = 100; break;
            }
            mPageIndex = 1;
            mSearchData.Rows.Clear();

            //페이지 갱신 후 웹브라우저 이벤트를 받아서 게시물을 분석하지않고 정보의 유효성으로 게시물을 분석한다.
            //OSP 시작페이지 갱신
            HomeURLApply(webMain2);
            



            if (cbOSPType.SelectedIndex == 88)  // 쉐어박스 모바일 배너광고 없애기 2023.01.30 추가
            {
                string strIDStr = "document.getElementsByTagName('ul')[0].remove()";
                string POPBanner = "document.getElementsByClassName('blackbg_gra')[0].remove()";
                clsUtil.Delay(500);
                string strResult = await webMain2.CoreWebView2.ExecuteScriptAsync(strIDStr); clsUtil.Delay(500);
                string strResult2 = await webMain2.CoreWebView2.ExecuteScriptAsync(POPBanner); clsUtil.Delay(500);
            }
            if (cbOSPType.SelectedIndex == 48 )  // 스마트파일 모바일 배너광고 없애기 2023.01.30 추가
            {               
                await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('today_close')[0].click()"); clsUtil.Delay(500);
                await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('check_layer_back')[0].remove()"); clsUtil.Delay(500);
            }
            if (cbOSPType.SelectedIndex == 38)  // 스마트파일 모바일 배너광고 없애기 2023.01.30 추가
            {                
                await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('close')[0].click()"); clsUtil.Delay(500);
            }
            if (cbOSPType.SelectedIndex == 49) //파일노리모바일 광고제거
            {
                string strPOP1 = "$('#event328Layer').remove();utilFN.setCookie('event328', 'off', 1)";
                string strPOP2 = "$('#event290Layer').remove();utilFN.setCookie('event290', 'off', 1)";
                clsUtil.Delay(500);
                await webMain2.CoreWebView2.ExecuteScriptAsync(strPOP1);
                await webMain2.CoreWebView2.ExecuteScriptAsync(strPOP2);
            }
            if (cbOSPType.SelectedIndex == 45)
            {
                await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('wtpopup_inner')[0].remove()");
                await webMain2.CoreWebView2.ExecuteScriptAsync("javascript:mecross_close('emos2015','7')");
            }

        }

        private void ReCollectionStart()
        {
            mPageIndex = 1;
            mLimitePage = 1;

            mSearchData.BeginLoadData();
            mSearchData.Clear();
            clsDBProc.PopClose(mOSPInfo.SITE_ID, true, ref listPopup2);
            string strNowTime = clsUtil.GetToday();

            List<string> listResult = new List<string>();
            DataTable dtTemp = clsDBProc.GetReCollectionList(mOSPInfo.SITE_ID, 1, ref listResult);

            if (dtTemp != null && dtTemp.Rows.Count > 0)
            {
                for (int i = 0; i < dtTemp.Rows.Count; i++)
                {
                    int nReCollactionCount = clsUtil.IntParse(dtTemp.Rows[i]["RECOLLACTION_COUNT"].ToString());
                    string strCWLDate = dtTemp.Rows[i]["CWL_DATE"].ToString();

                    object[] obj = new object[] {
                        (i + 1).ToString(),
                        dtTemp.Rows[i]["POST_ID"],
                        dtTemp.Rows[i]["PHS_LICENSE"],
                        dtTemp.Rows[i]["POST_NAME"],
                        dtTemp.Rows[i]["FILE_AMOUNT"],
                        dtTemp.Rows[i]["PHS_PRICE"],
                        dtTemp.Rows[i]["POST_GENRE"],
                        dtTemp.Rows[i]["UPLOADER_ID"],
                        strNowTime,
                        dtTemp.Rows[i]["DESC_URL"],
                        dtTemp.Rows[i]["CRAWL_ID"],
                        dtTemp.Rows[i]["RECOLLACTION_COUNT"]
                    };
                    mSearchData.Rows.Add(obj);
                }
            }
            //20221214 재수집테스트
            /*
            object[] obj2 = new object[] {
                        "1" ,"59792386"   ,"Y" ,"	[MX] 슈타인즈 게이트.E14.720p-HD (자체자막)"   ,1638 ,1500    ,"CG003","sos*"  ,"20230202114349"    ,
                "http://m.fileis.com/?doc=board_view&idx=59792386"  ,"A96FC4C35A36472B18107B4CF5E2706E"  ,0
            };
            mSearchData.Rows.Add(obj2);
            */

            mSearchData.EndLoadData();
            //검색 시작시간을 저장한다. 채증완료 후 로그를 기록할때 사용한다.
            mSearchSTime = strNowTime;
            mSearchETime = string.Empty;
            mSearchGenre = "재수집";
            //게시물목록의 캡쳐 후 상세화면과 합성한다.
            BoardListCapture();
            //상세조회 시작
            mPopupIndex = -1;
            NextReCollection();
        }
        int login_check = 0;
        private async void trOSPBoardParsing_Tick(object sender, EventArgs e)
        {
            if (mInitDocumentCompleted == true)
            {
                trOSPBoardParsing.Enabled = false;
                if (cbOSPType.SelectedIndex == 141 && !bFirst) SearchStart();
                if (mNowStatus == 0)
                {//기본페이지가 활성화되면 로그인시도    

                    if (cbOSPType.SelectedIndex == 42)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("GO_MENU('login', 'search');"); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 138)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("LgoinLayerView()"); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 12)
                    {
                        string loginbtn = "document.getElementsByClassName('btn')[0].childNodes[0].click()";
                        await webMain2.CoreWebView2.ExecuteScriptAsync(loginbtn); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 146)
                    {
                        string loginbtn = "document.getElementsByClassName('btn login')[0].click()";
                        await webMain2.CoreWebView2.ExecuteScriptAsync(loginbtn); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 147)
                    {
                        string loginbtn = "document.getElementsByClassName('login')[0].click()";
                        await webMain2.CoreWebView2.ExecuteScriptAsync(loginbtn); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 152)
                    {
                        string loginbtn = "document.getElementsByClassName('btn login btn-disk-login')[0].click()";
                        await webMain2.CoreWebView2.ExecuteScriptAsync(loginbtn); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 153)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("GO_LOGIN(this);"); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 4)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("login_box_open();"); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 89)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("FC_APP_FUN.onclickLoginFormOpen(this);");clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 148)
                    {
                        string loginbtn = "document.getElementsByClassName('login-t')[0].click()";
                        await webMain2.CoreWebView2.ExecuteScriptAsync(loginbtn); clsUtil.Delay(500);
                    }
                    if (cbOSPType.SelectedIndex == 17) clsUtil.Delay(3000);
                    /*if (cbOSPType.SelectedIndex == 155)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('header')[0].childNodes[5].click()");
                    }*/
                    if (cbOSPType.SelectedIndex == 156)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("fc_open_layer_login()");
                    }
                    if (cbOSPType.SelectedIndex == 157)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('all_btn_link')[0].click()");
                    }
                    if (cbOSPType.SelectedIndex == 144)
                    {
                        await webMain2.CoreWebView2.ExecuteScriptAsync("fnMainCheck(jQuery(this).attr('open-url'),'Y');"); clsUtil.Delay(1000);
                    }

                    bool btmp = await mOSPCrawlerEdge.setLogin(webMain2, tbID.Text.Trim(), tbPwd.Text.Trim());
                    if (btmp)
                    {
                        SetLog("로그인값 입력");
                        mInitDocumentCompleted = false;
                        clsUtil.Delay(1500);
                        mNowStatus = 1;
                        mInitDocumentCompleted = true;
                        trOSPBoardParsing.Enabled = true;
                        
                        login_check++;
                        if (cbOSPType.SelectedIndex == 103) // 파일이즈 모바일_IP자동 변경 적용 2023.04.04 , 2023.06.07 수정
                        {                           
                            if (login_check == 3)
                            {
                                mProxyList = new clsProxy();
                                mProxyList.ProxyConnectedEventHandler += new clsProxy.ProxyConnnected(mProxyList_ProxyConnectedEventHandler);
                                mProxyList.SetProxy2();                                
                            }
                            if (login_check == 10)
                                Environment.Exit(0);
                            string ProxyTest = await webMain2.CoreWebView2.ExecuteScriptAsync("document.body.innerHTML");
                            if (ProxyTest.IndexOf("프록시") != -1)
                            {
                                mProxyList = new clsProxy();
                                mProxyList.ProxyConnectedEventHandler += new clsProxy.ProxyConnnected(mProxyList_ProxyConnectedEventHandler);
                                mProxyList.SetProxy2();
                            }
                        }
                        
                        //로그인 체크하지 않고 skip
                        if (cbOSPType.SelectedIndex == 43 || cbOSPType.SelectedIndex == 144 || cbOSPType.SelectedIndex == 32 ||
                            cbOSPType.SelectedIndex == 88 || cbOSPType.SelectedIndex == 85 || cbOSPType.SelectedIndex == 15 || cbOSPType.SelectedIndex == 54 ||
                            cbOSPType.SelectedIndex == 31 || cbOSPType.SelectedIndex == 38 )
                        {
                            mNowStatus = 2;
                        }                       
                    }
                    else
                    {
                        bool bRet = await mOSPCrawlerEdge.isLogin(webMain2);
                        if (bRet)
                        {
                            SetLog("이미 로그인중");
                            //20221004
                            {
                                await webMain2.EnsureCoreWebView2Async(null);
                                webMain2.CoreWebView2.Navigate(mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString());
                            }
                            mNowStatus = 1;
                            trFileLogin.Enabled = false;
                        }
                        else
                        {
                            SetLog("로그인값 입력실패");

                            //우클릭 방지해제
                            if (cbOSPType.SelectedIndex == 17 || cbOSPType.SelectedIndex == 7 || cbOSPType.SelectedIndex == 3 || cbOSPType.SelectedIndex == 4)
                            {
                                string strScript = "javascript:function r(d){d.oncontextmenu=null;d.onselectstart=null;d.ondragstart=null;d.onkeydown=null;d.onmousedown=null;}function unify(w){try{r(w.document);}catch(e){}try{r(w.document.body);}catch(e){}try{var divs=w.document.getElementsByTagName(\"div\");for(var i=0;i<divs.length;i++){try{r(divs[i]);}catch(e){}}}catch(e){}for(var i=0;i<w.frames.length;i++){try{unify(w.frames[i].window);}catch(e){}}}unify(self);";
                                await webMain2.CoreWebView2.ExecuteScriptAsync(strScript);
                            }
                            mLoginInitTime++;
                            if (mLoginInitTime >= 5)
                            {
                                mLoginInitTime = 0;
                                mNowStatus = 0;//로그인 대기시간을 넘어간경우 로그인로직을 재시작한다.
                                await SetStartPage();
                                return;
                            }
                        }
                        trOSPBoardParsing.Enabled = true;
                    }
                }
                else if (mNowStatus == 1)
                {
                    if (cbOSPType.SelectedIndex == 54||cbOSPType.SelectedIndex == 102 || cbOSPType.SelectedIndex == 38 || cbOSPType.SelectedIndex == 44 ||
                        cbOSPType.SelectedIndex == 89 || cbOSPType.SelectedIndex == 42 || cbOSPType.SelectedIndex == 53|| cbOSPType.SelectedIndex == 126 ||
                         cbOSPType.SelectedIndex == 48 || cbOSPType.SelectedIndex == 88 || cbOSPType.SelectedIndex == 56 || cbOSPType.SelectedIndex == 137
                        ) webMain2.Reload();

                    if (cbOSPType.SelectedIndex == 94)
                    {
                        webMain2.CoreWebView2.ExecuteScriptAsync("javascript:FamilyProPopClick('T');");
                        webMain2.Reload();
                    }
                    if (cbOSPType.SelectedIndex == 23)// 파일조 팝업 제거 시도
                    {
                        webMain2.Refresh();
                        webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('sevenbtn')[0].childNodes[0].click()");
                        webMain2.CoreWebView2.ExecuteScriptAsync("javascript:HTEVT_close(7);");
                        await webMain2.CoreWebView2.ExecuteScriptAsync("movePage('/movepage.php?location=old');"); clsUtil.Delay(1000);

                    }
                    if (cbOSPType.SelectedIndex == 126 || cbOSPType.SelectedIndex == 2 || cbOSPType.SelectedIndex == 4) // 계정 이벤트 강제 스킵
                    {
                        clsUtil.Delay(500);
                        webMain2.CoreWebView2.Navigate(mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString());
                        clsUtil.Delay(500);
                    }
                    bool bRet = await mOSPCrawlerEdge.isLogin(webMain2);
                    if (bRet)
                    {
                        trFileLogin.Enabled = false;
                        SetLog("로그인성공 3초 딜레이");
                        mLoginRefreshTime++;
                        if (mLoginRefreshTime >= 2)
                        {
                            mNowStatus = 2;
                        }

                    }
                    else
                    {
                        SetLog("로그인실패 3초대기");
                        trFileLogin.Enabled = false;
                        mLoginFailedTime++;
                        if (mLoginFailedTime >= 1)
                        {
                            mLoginFailedTime = 0;
                            mNowStatus = 0;//로그인 대기시간을 넘어간경우 로그인로직을 재시작한다.
                            await SetStartPage();
                            return;
                        }
                    }
                    trOSPBoardParsing.Enabled = true;
                }
                else if (mNowStatus == 2)
                {
                    trFileLogin.Enabled = false;
                    mNowStatus = 3;
                    SearchStart();
                }
                else
                {
                    bool bRet = await GetOSPBoardParsing();
                    if (bRet == false)
                    {
                        trOSPBoardParsing.Enabled = true;
                    }
                }
            }
        }

        private async Task<bool> GetOSPBoardParsing()
        {
            //새디스크 모바일 : 페이지불러오는시간이 오래걸려서 예외처리
            if (mPageIndex > 0)
            {
                clsUtil.Delay(3000);
                //일부 OSP는 딜레이를 주지않으면 html가 로드되기전에 타이머가 시작되어 delay를 추가
                if (cbOSPType.SelectedIndex == 76 || cbOSPType.SelectedIndex == 55 ) clsUtil.Delay(3000);
                if (cbOSPType.SelectedIndex == 151 || cbOSPType.SelectedIndex == 12) { clsUtil.Delay(5000); };
                SetLog(String.Format("이동 => 페이지3 : {0}", mPageIndex.ToString()));

                //애플파일 모바일 재수집시 isPageCheck를 하게 되면 캡쳐화면이 이상하게 나오는 문제가 있어서 애플파일모바일 재수집시에는 isPageCheck를 하지 않고 진행하도록 예외처리
                if ((mJobType == 1) || (mOSPCrawlerEdge.isPageCheck(webMain2, mSearchGenre, mPageIndex.ToString()) == true))
                {
                    SetLog(String.Format("이동 => 페이지3-1 : {0}", mPageIndex.ToString()));
                    //필요한 정보가 모두 로드되었으면 웹페이지를 더이상 로드하지 않는다.
                    webMain2.Stop();

                    SetLog(String.Format("이동 => 페이지3-2 : {0}", mPageIndex.ToString()));
                    if (mJobType == 1)
                    {
                        if (cbOSPType.SelectedIndex == 146) clsUtil.Delay(3000);

                        ReCollectionStart();
                        return true;
                    }
                    SetLog(String.Format("이동 => 페이지4 : {0}", mPageIndex.ToString()));
                    mSearchData.BeginLoadData();
                    mSearchData.Clear();
                    
                    SetLog(String.Format("이동 => 페이지5 : {0}", mPageIndex.ToString()));

                    if (cbOSPType.SelectedIndex == 134)
                    {
                        string script = "document.getElementsByClassName('content')[0].innerText.indexOf('실시간 인기검색어')";
                        string indexResult = await webMain2.CoreWebView2.ExecuteScriptAsync(script);

                        if (int.TryParse(indexResult, out int index) && index != -1)
                        {
                            webMain2.CoreWebView2.Navigate(mHomeURL.Rows[mHomeURLIndex]["HOME_URL"].ToString());
                        }

                    }
                    string html = string.Empty;
                    //파일쿠키,위디스크는 iframe을 로드해야함.
                    if (cbOSPType.SelectedIndex == 17 || cbOSPType.SelectedIndex == 7 || cbOSPType.SelectedIndex == 3)
                    {
                        var script = @"
                            var result = '';
                            function traverseFrames(win) {
                            if (win.frames.length > 0) {
                                for (var i = 0; i < win.frames.length; i++) {
                                traverseFrames(win.frames[i]);
                                }
                            }
                            result += win.document.documentElement.outerHTML;
                            }
                            traverseFrames(window);
                            result;
                            ";
                        html = await webMain2.CoreWebView2.ExecuteScriptAsync(script);
                        html = Regex.Unescape(html);
                        html = html.Remove(0, 1);
                        html = html.Remove(html.Length - 1, 1);
                    }
                    else
                    {
                        html = await webMain2.ExecuteScriptAsync("document.documentElement.outerHTML");
                        html = Regex.Unescape(html);    
                        html = html.Remove(0, 1);
                        html = html.Remove(html.Length - 1, 1);
                    }
                    bool bRet = false;
                    //유씨씨 bbs(게시물번호)값 가져오기위해서는 내부함수를 호출해야함..
                    if (cbOSPType.SelectedIndex == 148)
                    {
                        int nFile = Convert.ToInt32(await webMain2.CoreWebView2.ExecuteScriptAsync("subList['_data'].cListItems.length"));
                        string[] bbs = new string[nFile];
                        string strBbs = string.Empty;
                        for (int i = 0; i < nFile; i++)
                        {
                            strBbs += (await webMain2.CoreWebView2.ExecuteScriptAsync("subList['_data'].cListItems[" + i.ToString() + "].idx")).Replace("\"", "") + "|";
                        }
                        bRet = mOSPCrawlerEdge.Parse(html, mPageIndex, ref mSearchData, listPopup, strBbs);
                    }
                    else
                    {
                        bRet = mOSPCrawlerEdge.Parse(html, mPageIndex, ref mSearchData, listPopup, webMain2.Source.ToString());
                    }

                    if (bRet == false)
                    {
                        mSearchData.EndLoadData();
                        return false;
                    }
                    if (cbOSPType.SelectedIndex == 138)
                        clsUtil.Delay(3000);
                    BoardListCapture();
                    SetLog(String.Format("이동 => 페이지6 : {0}", mPageIndex.ToString()));
                    mSearchData.EndLoadData();
                    SetLog(String.Format("이동 => 페이지7 : {0}", mPageIndex.ToString()));
                    //게시물목록의 캡쳐 후 상세화면과 합성한다.
                    SetLog(String.Format("이동 => 페이지8 : {0}", mPageIndex.ToString()));

                    //상세조회 시작                    
                    mPopupIndex = -1;
                    trNextJob.Enabled = true;
                    SetLog(String.Format("이동 => 페이지9 : {0}", mPageIndex.ToString()));
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        //private void trNextPageAndPopup_Tick(object sender, EventArgs e)
        //{
        //    trNextPageAndPopup.Enabled = false;

        //    trOSPBoardParsing.Enabled = false;
        //    trPopupParsing.Enabled = false;

        //    if (mPopupIndex >= mSearchData.Rows.Count || mPopupIndex < 0)
        //    {
        //        SetLog("페이지 이동실패, 다음 페이지 이동");
        //    }
        //    else
        //    {
        //        SetLog("팝업 조회실패, 다음 상세페이지 조회시작");
        //        if (cbOSPType.SelectedIndex == 152 || cbOSPType.SelectedIndex == 147) webMain2.Reload(); // 파일스타 PC 재수집시 삭제게시물있으면 상세페이지 조회 안되는 현상 수정
        //        string strTitle = mSearchData.Rows[mPopupIndex]["TITLE"].ToString();
        //        string strSubURL = mSearchData.Rows[mPopupIndex]["SUBURL"].ToString();
        //    }

        //    //재수집중 조회실패인경우 삭제로 판단한다.
        //    //추후 삭제와 조회실패를 구분하도록 한다.
        //    if (mJobType == 1)
        //    {
        //        if (mPopupIndex < mSearchData.Rows.Count)
        //        {
        //            BOARD_INFO boardInfo = new BOARD_INFO();
        //            boardInfo.CRAWL_ID = mSearchData.Rows[mPopupIndex]["CRAWL_ID"].ToString();
        //            boardInfo.MONEY = "-1";
        //            boardInfo.RESULT_STATUS = "00";
        //            // DB 요청으로 REG_DATE 추가.
        //            boardInfo.REG_DATE = clsUtil.GetToday();

        //            string strCWLDesc = String.Format("[{0}] 회차 재수집", clsUtil.IntParse(mSearchData.Rows[mPopupIndex]["RECOLLECTION_COUNT"].ToString()) + 1);

        //            List<string> listResult = new List<string>();

        //            clsDBProc.InsertReCollectionHist(boardInfo, "채증", strCWLDesc, ref listResult, false);
        //            Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
        //        }
        //    }
        //    trNextJob.Enabled = true;
        //}

        //private void trNextPageAndPopup_Tick(object sender, EventArgs e)
        //{
        //    trNextPageAndPopup.Enabled = false;
        //    trOSPBoardParsing.Enabled = false;
        //    trPopupParsing.Enabled = false;

        //    // Popup Index와 데이터 개수 확인
        //    SetLog($"mPopupIndex: {mPopupIndex}, mSearchData.Rows.Count: {mSearchData.Rows.Count}");

        //    // mPopupIndex가 범위를 벗어났는지 확인
        //    if (mPopupIndex >= mSearchData.Rows.Count || mPopupIndex < 0)
        //    {
        //        SetLog("페이지 이동실패, 다음 페이지 이동");
        //    }
        //    else
        //    {
        //        // 현재 팝업 제목과 URL을 확인
        //        string strTitle = mSearchData.Rows[mPopupIndex]["TITLE"].ToString();
        //        string strSubURL = mSearchData.Rows[mPopupIndex]["SUBURL"].ToString();
        //        SetLog($"팝업 조회실패, 다음 상세페이지 조회시작 (Title: {strTitle}, SubURL: {strSubURL})");

        //        // 파일스타 PC 재수집 로직 확인
        //        if (cbOSPType.SelectedIndex == 152 || cbOSPType.SelectedIndex == 147)
        //        {
        //            SetLog("파일스타 PC 재수집을 위해 웹페이지 리로드");
        //            webMain2.Reload(); // 파일스타 PC 재수집시 삭제게시물있으면 상세페이지 조회 안되는 현상 수정
        //        }

        //        // URL과 Title 값이 올바른지 확인
        //        SetLog($"현재 팝업정보 - Title: {strTitle}");
        //        SetLog($"현재 팝업정보 - subURL: {strSubURL}");
        //    }

        //    // 재수집 로직 확인
        //    if (mJobType == 1)
        //    {
        //        if (mPopupIndex < mSearchData.Rows.Count)
        //        {
        //            BOARD_INFO boardInfo = new BOARD_INFO();
        //            boardInfo.CRAWL_ID = mSearchData.Rows[mPopupIndex]["CRAWL_ID"].ToString();
        //            boardInfo.MONEY = "-1";
        //            boardInfo.RESULT_STATUS = "00";
        //            boardInfo.REG_DATE = clsUtil.GetToday();

        //            string strCWLDesc = String.Format("[{0}] 회차 재수집", clsUtil.IntParse(mSearchData.Rows[mPopupIndex]["RECOLLECTION_COUNT"].ToString()) + 1);
        //            SetLog($"재수집 정보 - CRAWL_ID: {boardInfo.CRAWL_ID}, 회차: {strCWLDesc}");

        //            List<string> listResult = new List<string>();

        //            clsDBProc.InsertReCollectionHist(boardInfo, "채증", strCWLDesc, ref listResult, false);
        //            Console.WriteLine($"결과코드 = {listResult[0]}, 결과메세지 = {listResult[1]}");
        //        }
        //    }

        //    trNextJob.Enabled = true;
        //}

        private void trNextPageAndPopup_Tick(object sender, EventArgs e)
        {
            // 타이머 비활성화
            trNextPageAndPopup.Enabled = false;
            trOSPBoardParsing.Enabled = false;
            trPopupParsing.Enabled = false;

            Console.WriteLine("(trNextPageAndPopup)trPopupParsing.Enabled = " + trPopupParsing.Enabled);

            // 디버그: 현재 상태 확인
            SetLog($"[DEBUG] trNextPageAndPopup_Tick 호출 - mPopupIndex: {mPopupIndex}, Rows.Count: {mSearchData.Rows.Count}");
            Console.WriteLine($"[DEBUG] trNextPageAndPopup_Tick 호출 - mPopupIndex: {mPopupIndex}, Rows.Count: {mSearchData.Rows.Count}");

            // mPopupIndex가 데이터 범위를 벗어났는지 확인
            if (mPopupIndex >= mSearchData.Rows.Count || mPopupIndex < 0)
            {
                SetLog("[ERROR] 페이지 이동 실패 - 다음 페이지로 이동");
            }
            else
            {
                // 팝업 데이터 확인
                string strTitle = mSearchData.Rows[mPopupIndex]["TITLE"].ToString();
                string strSubURL = mSearchData.Rows[mPopupIndex]["SUBURL"].ToString();

                SetLog($"[INFO] 현재 팝업 데이터 - Title: {strTitle}, SubURL: {strSubURL}");

                // 팝업 데이터 누락 확인
                if (string.IsNullOrEmpty(strTitle) || string.IsNullOrEmpty(strSubURL))
                {
                    SetLog("[ERROR] 팝업 데이터 누락 - Title 또는 SubURL 값이 없습니다.");
                }
                else
                {
                    // 팝업 로직이 필요한 경우 추가 처리
                    SetLog("[INFO] 다음 팝업 조회 준비 중...");
                }

                // 특정 OSPType 처리 (파일스타 PC 예외 처리)
                if (cbOSPType.SelectedIndex == 152 || cbOSPType.SelectedIndex == 147)
                {
                    SetLog("[INFO] 파일스타 PC 예외 처리 - 웹 페이지 리로드");
                    webMain2.Reload(); // 재수집을 위한 리로드
                }
            }

            // 재수집 처리 로직
            if (mJobType == 1 && mPopupIndex < mSearchData.Rows.Count)
            {
                BOARD_INFO boardInfo = new BOARD_INFO();
                boardInfo.CRAWL_ID = mSearchData.Rows[mPopupIndex]["CRAWL_ID"].ToString();
                boardInfo.MONEY = "-1";
                boardInfo.RESULT_STATUS = "00";
                boardInfo.REG_DATE = clsUtil.GetToday();

                string strCWLDesc = String.Format("[{0}] 회차 재수집", clsUtil.IntParse(mSearchData.Rows[mPopupIndex]["RECOLLECTION_COUNT"].ToString()) + 1);
                SetLog($"[INFO] 재수집 정보 - CRAWL_ID: {boardInfo.CRAWL_ID}, 회차: {strCWLDesc}");

                List<string> listResult = new List<string>();
                clsDBProc.InsertReCollectionHist(boardInfo, "채증", strCWLDesc, ref listResult, false);
                Console.WriteLine($"결과코드 = {listResult[0]}, 결과메세지 = {listResult[1]}");
            }

            // 다음 작업을 위한 타이머 활성화
            trNextJob.Enabled = true;
        }




        //// original function
        //private async void Popup_Navigate(string strURL)
        //{
        //    if (cbOSPType.SelectedIndex == 136)
        //    {
        //        string strNumber = "contentViewOpen(" + clsUtil.SubStringEx(strURL, "idx=", 1, "") + ")";
        //        await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
        //    }

        //    else if (cbOSPType.SelectedIndex == 137)
        //    {
        //        string strNumber = "contentViewOpen(" + clsUtil.SubStringEx(strURL, "idx=", 1, "") + ")";
        //        await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
        //    }

        //    else if (cbOSPType.SelectedIndex == 46) // 케이디스크_모바일
        //    {
        //        // 쿠키의 content_id를 체크하도록 사이트가 변경 되었음.
        //        APIs.InternetSetCookie(strURL, "content_id", clsUtil.SubStringEx(strURL, "idx=", 1, ""));
        //        webMain2.CoreWebView2.Navigate(strURL);
        //    }
        //    else if (cbOSPType.SelectedIndex == 150)
        //    {
        //        webMain2.CoreWebView2.Navigate(strURL);
        //    }
        //    else if (cbOSPType.SelectedIndex == 148)
        //    {
        //        webMain2.CoreWebView2.Navigate(strURL);
        //        clsUtil.Delay(3000);
        //    }
        //    else if (cbOSPType.SelectedIndex == 46)
        //    {
        //        APIs.InternetSetCookie(strURL, "content_id", clsUtil.SubStringEx(strURL, "idx=", 1, ""));
        //        webMain2.CoreWebView2.Navigate(strURL);
        //    }
        //    else if (cbOSPType.SelectedIndex == 75)
        //    {
        //        string strtmp = string.Empty;
        //        string strNumber_pop = clsUtil.SubStringEx(strURL, "https://m.todisk.com/mobile/storage.php?act=view&idx=", 1, "");
        //        string strNumber = "php_base64_encode(" + clsUtil.SubStringEx(strURL, "https://m.todisk.com/mobile/storage.php?act=view&idx=", 1, "") + ")";

        //        strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
        //        //strtmp = clsUtil.SubStringEx(strtmp, "", 1, "");
        //        //strtmp = strtmp.Replace("\"", "");
        //        string TO_M_URL = strURL + "&eidx=" + strtmp;

        //        //webMain2.CoreWebView2.Navigate(strURL + "&eidx=" + strtmp);
        //        webMain2.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
        //        webMain2.Source = new Uri(TO_M_URL);

        //    }
        //    else if (cbOSPType.SelectedIndex == 37)
        //    {
        //        string strtmp = string.Empty;
        //        int strURLNum = "https://www.todisk.com/_main/popup.php?doc=bbsInfo&idx=".Length;
        //        string strNumber2 = string.Format("php_base64_encode({0})", strURL.Substring(strURLNum - 1).Trim());
        //        strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber2);
        //        webMain2.CoreWebView2.Navigate(strURL + "&eidx=" + strtmp);
        //    }
        //    else if (cbOSPType.SelectedIndex == 132)
        //    {
        //        string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://www.filemong.com/", 1, "") + ")";
        //        strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);
        //    }
        //    else if (cbOSPType.SelectedIndex == 4 || cbOSPType.SelectedIndex == 48) clsUtil.Delay(1000);
        //    else if (cbOSPType.SelectedIndex == 152) 
        //    {
        //        //if (mJobType == 1) { // 파일스타PC 재수집시 상세페이지 접속
        //            string strtmp = string.Empty;
        //            int strURLNum = "https://filestar.co.kr/#!action=contents&idx=".Length;
        //            string strNumber = strURL.Substring(strURLNum).Trim();

        //            string strScript = "WEB_COMMON_GO.openContents(" + strNumber + ",'','','')";
        //            await webMain2.CoreWebView2.ExecuteScriptAsync(strScript);
        //        //}

        //        //else webMain2.CoreWebView2.Navigate(strURL);
        //    }
        //    else if (cbOSPType.SelectedIndex == 154)
        //    {
        //        string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://metafile.co.kr/", 1, "") + ")";
        //        strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);

        //    }
        //    else if (cbOSPType.SelectedIndex == 156)
        //    {
        //        string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://filegosu.com/", 1, "") + ")";
        //        strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);

        //    }
        //    else
        //    {
        //        webMain2.CoreWebView2.Navigate(strURL);
        //    }

        //    if (cbOSPType.SelectedIndex == 98) return;
        //    if (cbOSPType.SelectedIndex == 86) clsUtil.Delay(1000);  // 지디스크 팝업 조회시 로딩 대기..
        //    if (cbOSPType.SelectedIndex == 53) clsUtil.Delay(3000);  // 파일시티 팝업 조회시 로딩 대기..
        //    if (cbOSPType.SelectedIndex == 3) clsUtil.Delay(3000);

        //    trPopupParsing.Enabled = true;
        //}



        private async void Popup_Navigate(string strURL)
        {
            try
            {
                SetLog($"Popup_Navigate 시작 - URL: {strURL}, OSPType: {cbOSPType.SelectedIndex}");

                // URL 누락 여부 확인
                if (string.IsNullOrEmpty(strURL))
                {
                    SetLog("[ERROR] Popup_Navigate 호출 실패 - URL이 비어 있습니다.");
                    return;
                }

                // OSPType에 따른 분기 처리
                if (cbOSPType.SelectedIndex == 136 || cbOSPType.SelectedIndex == 137)
                {
                    string strNumber = "contentViewOpen(" + clsUtil.SubStringEx(strURL, "idx=", 1, "") + ")";
                    await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
                }
                else if (cbOSPType.SelectedIndex == 46)
                {
                    APIs.InternetSetCookie(strURL, "content_id", clsUtil.SubStringEx(strURL, "idx=", 1, ""));
                    webMain2.CoreWebView2.Navigate(strURL);
                }
                else if (cbOSPType.SelectedIndex == 150 || cbOSPType.SelectedIndex == 148)
                {
                    webMain2.CoreWebView2.Navigate(strURL);
                    if (cbOSPType.SelectedIndex == 148)
                        clsUtil.Delay(3000); // 추가 대기 시간 적용
                }
                else if (cbOSPType.SelectedIndex == 75)
                {
                    string strNumber = "php_base64_encode(" + clsUtil.SubStringEx(strURL, "https://m.todisk.com/mobile/storage.php?act=view&idx=", 1, "") + ")";
                    string strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
                    string TO_M_URL = strURL + "&eidx=" + strtmp;

                    webMain2.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
                    webMain2.Source = new Uri(TO_M_URL);
                }
                else if (cbOSPType.SelectedIndex == 37)
                {
                    int strURLNum = "https://www.todisk.com/_main/popup.php?doc=bbsInfo&idx=".Length;
                    string strNumber2 = $"php_base64_encode({strURL.Substring(strURLNum - 1).Trim()})";
                    string strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber2);
                    webMain2.CoreWebView2.Navigate(strURL + "&eidx=" + strtmp);
                }
                else if (cbOSPType.SelectedIndex == 132)
                {
                    string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://www.filemong.com/", 1, "") + ")";
                    SetLog($"132번 OSPType: 스크립트 실행 - {strtmp}");
                    strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);
                    clsUtil.Delay(3000);
                    string isLoaded = await webMain2.CoreWebView2.ExecuteScriptAsync("document.readyState");
                    SetLog($"팝업 로드 상태: {isLoaded}");
                }
                else if (cbOSPType.SelectedIndex == 152)
                {
                    int strURLNum = "https://filestar.co.kr/#!action=contents&idx=".Length;
                    string strNumber = strURL.Substring(strURLNum).Trim();
                    string strScript = $"WEB_COMMON_GO.openContents({strNumber}, '', '', '')";
                    await webMain2.CoreWebView2.ExecuteScriptAsync(strScript);
                }
                else if (cbOSPType.SelectedIndex == 154 || cbOSPType.SelectedIndex == 156)
                {
                    string baseURL = (cbOSPType.SelectedIndex == 154) ? "https://metafile.co.kr/" : "https://filegosu.com/";
                    string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, baseURL, 1, "") + ")";
                    await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);
                }
                else if (cbOSPType.SelectedIndex == 38)
                {
                    string kind = clsUtil.SubStringEx(strURL, "gg=", 1, "");
                    string idx = clsUtil.SubStringEx(strURL, "idx=", 1, "");
                    string flag_adult = "0";

                    string script = $@"
                (function() {{
                    window.name = 'contentsView_Smartfile';
                    viewContents('{kind}', '{idx}', '{flag_adult}');
                }})();
            ";
                    await webMain2.CoreWebView2.ExecuteScriptAsync(script);
                }
                else
                {
                    webMain2.CoreWebView2.Navigate(strURL);
                }

                // 페이지 로드 대기 및 상태 확인
                clsUtil.Delay(2000); // 기본 대기 시간 적용
                string loadStatus = await webMain2.CoreWebView2.ExecuteScriptAsync("document.readyState");
                if (!loadStatus.Contains("complete"))
                {
                    SetLog($"[ERROR] 페이지 로드 실패 - URL: {strURL}, 상태: {loadStatus}");
                    return;
                }

                SetLog($"Popup_Navigate 종료 - URL: {strURL}");
            }
            catch (Exception ex)
            {
                SetLog($"[ERROR] Popup_Navigate 호출 중 예외 발생: {ex.Message}");
            }
        }





        //private async void Popup_Navigate(string strURL)
        //{
        //    try
        //    {



        //        SetLog($"Popup_Navigate 시작 - URL: {strURL}, OSPType: {cbOSPType.SelectedIndex}");

        //        if (cbOSPType.SelectedIndex == 136)
        //        {
        //            string strNumber = "contentViewOpen(" + clsUtil.SubStringEx(strURL, "idx=", 1, "") + ")";
        //            await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
        //        }
        //        else if (cbOSPType.SelectedIndex == 137)
        //        {
        //            string strNumber = "contentViewOpen(" + clsUtil.SubStringEx(strURL, "idx=", 1, "") + ")";
        //            await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
        //        }
        //        else if (cbOSPType.SelectedIndex == 46) // 케이디스크_모바일
        //        {
        //            APIs.InternetSetCookie(strURL, "content_id", clsUtil.SubStringEx(strURL, "idx=", 1, ""));
        //            webMain2.CoreWebView2.Navigate(strURL);
        //        }
        //        else if (cbOSPType.SelectedIndex == 150)
        //        {
        //            webMain2.CoreWebView2.Navigate(strURL);
        //        }
        //        else if (cbOSPType.SelectedIndex == 148)
        //        {
        //            webMain2.CoreWebView2.Navigate(strURL);
        //            clsUtil.Delay(3000);
        //        }
        //        else if (cbOSPType.SelectedIndex == 46)
        //        {
        //            APIs.InternetSetCookie(strURL, "content_id", clsUtil.SubStringEx(strURL, "idx=", 1, ""));
        //            webMain2.CoreWebView2.Navigate(strURL);
        //        }
        //        else if (cbOSPType.SelectedIndex == 75)
        //        {
        //            string strtmp = string.Empty;
        //            string strNumber_pop = clsUtil.SubStringEx(strURL, "https://m.todisk.com/mobile/storage.php?act=view&idx=", 1, "");
        //            string strNumber = "php_base64_encode(" + clsUtil.SubStringEx(strURL, "https://m.todisk.com/mobile/storage.php?act=view&idx=", 1, "") + ")";

        //            strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
        //            string TO_M_URL = strURL + "&eidx=" + strtmp;

        //            webMain2.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
        //            webMain2.Source = new Uri(TO_M_URL);
        //        }
        //        else if (cbOSPType.SelectedIndex == 37)
        //        {
        //            string strtmp = string.Empty;
        //            int strURLNum = "https://www.todisk.com/_main/popup.php?doc=bbsInfo&idx=".Length;
        //            string strNumber2 = string.Format("php_base64_encode({0})", strURL.Substring(strURLNum - 1).Trim());
        //            strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber2);
        //            webMain2.CoreWebView2.Navigate(strURL + "&eidx=" + strtmp);
        //        }
        //        //else if (cbOSPType.SelectedIndex == 132)
        //        //{
        //        //    string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://www.filemong.com/", 1, "") + ")";
        //        //    SetLog($"132번 OSPType: 스크립트 실행 - {strtmp}");
        //        //    strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);
        //        //}
        //        else if (cbOSPType.SelectedIndex == 132)
        //        {
        //            string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://www.filemong.com/", 1, "") + ")";
        //            SetLog($"132번 OSPType: 스크립트 실행 - {strtmp}");
        //            strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);

        //            // 팝업 로드 후 3초 대기 (시간은 필요에 따라 조정 가능)
        //            clsUtil.Delay(3000);

        //            // 페이지 로드 완료 여부를 확인하는 로직 추가 가능
        //            string isLoaded = await webMain2.CoreWebView2.ExecuteScriptAsync("document.readyState");
        //            SetLog($"팝업 로드 상태: {isLoaded}");
        //        }

        //        else if (cbOSPType.SelectedIndex == 4 || cbOSPType.SelectedIndex == 48)
        //        {
        //            clsUtil.Delay(1000);
        //        }
        //        else if (cbOSPType.SelectedIndex == 152)
        //        {
        //            string strtmp = string.Empty;
        //            int strURLNum = "https://filestar.co.kr/#!action=contents&idx=".Length;
        //            string strNumber = strURL.Substring(strURLNum).Trim();

        //            string strScript = "WEB_COMMON_GO.openContents(" + strNumber + ",'','','')";
        //            await webMain2.CoreWebView2.ExecuteScriptAsync(strScript);
        //        }
        //        else if (cbOSPType.SelectedIndex == 154)
        //        {
        //            string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://metafile.co.kr/", 1, "") + ")";
        //            strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);
        //        }
        //        else if (cbOSPType.SelectedIndex == 156)
        //        {
        //            string strtmp = "contents_layer_open(" + clsUtil.SubStringEx(strURL, "https://filegosu.com/", 1, "") + ")";
        //            strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strtmp);
        //        }
        //        //else if (cbOSPType.SelectedIndex == 38)
        //        //{
        //        //    // 스마트파일(38)의 상세 페이지 호출 처리
        //        //    string kind = clsUtil.SubStringEx(strURL, "gg=", 1, "");  // kind 추출
        //        //    string idx = clsUtil.SubStringEx(strURL, "idx=", 1, "");  // idx 추출

        //        //    // 스마트파일의 전체 URL을 구성
        //        //    string fullURL = $"https://smartfile.co.kr/contents/view.php?gg={kind}&idx={idx}";
        //        //    string subURL = $"/contents/view.php?gg={kind}&idx={idx}";
        //        //    string flag_adult = "0";  // flag_adult 기본값 설정


        //        //    // contents_layer_open으로 전체 URL을 레이어에서 열기
        //        //    string script = $"viewContents('{kind}', '{idx}', '{flag_adult}')";
        //        //    //string script = $"contents_layer_open('{fullURL}');";
        //        //    //string script = $"window.open('{subURL}');";

        //        //    // JavaScript 실행
        //        //    string result = await webMain2.CoreWebView2.ExecuteScriptAsync(script);
        //        //}
        //        else if (cbOSPType.SelectedIndex == 38)
        //        {
        //            // 스마트파일(38)의 상세 페이지 호출 처리
        //            string kind = clsUtil.SubStringEx(strURL, "gg=", 1, "");  // kind 추출
        //            string idx = clsUtil.SubStringEx(strURL, "idx=", 1, "");  // idx 추출
        //            string flag_adult = "0";  // flag_adult 기본값 설정

        //            // 팝업 창처럼 보이도록 window.name 설정 및 viewContents 함수 호출
        //            string script = @"
        //            (function() {
        //            // window.name을 팝업 창과 동일하게 설정
        //            window.name = 'contentsView_Smartfile';

        //            // viewContents 함수 호출
        //            var kind = '" + kind + @"';
        //            var idx = '" + idx + @"';
        //            var flag_adult = '" + flag_adult + @"';

        //            viewContents(kind, idx, flag_adult);
        //           })();
        //             ";

        //            // JavaScript 실행
        //            string result = await webMain2.CoreWebView2.ExecuteScriptAsync(script);
        //        }
        //        else
        //        {
        //            webMain2.CoreWebView2.Navigate(strURL);
        //        }



        //        // 추가 지연 및 팝업 조회 처리
        //        if (cbOSPType.SelectedIndex == 98) return;
        //        if (cbOSPType.SelectedIndex == 86) clsUtil.Delay(1000);  // 지디스크 팝업 조회시 로딩 대기..
        //        if (cbOSPType.SelectedIndex == 53) clsUtil.Delay(3000);  // 파일시티 팝업 조회시 로딩 대기..
        //        if (cbOSPType.SelectedIndex == 3) clsUtil.Delay(3000);

        //        trPopupParsing.Enabled = true;

        //        SetLog($"Popup_Navigate 종료 - URL: {strURL}");
        //    }
        //    catch (Exception ex)
        //    {
        //        SetLog($"[ERROR] Popup_Navigate 호출 중 예외 발생: {ex.Message}");
        //    }
        //}

        private void NextReCollection()
        {

            SetLog("NextReCollection 시작");

            //상세페이지가 정상조회된경우 타임아웃을 해제한다.
            trNextPageAndPopup.Enabled = false;

            mPopupIndex++;

            DataTable dt = mSearchData;

            SetLog($"현재 mPopupIndex: {mPopupIndex}, 총 데이터 개수: {dt.Rows.Count}");

            if (mPopupIndex < dt.Rows.Count)
            {
                string strSeqNo = dt.Rows[mPopupIndex]["SEQNO"].ToString();
                string strTitle = dt.Rows[mPopupIndex]["TITLE"].ToString();
                string strSubURL = dt.Rows[mPopupIndex]["SUBURL"].ToString();

                //상세페이지 타임아웃을 시작한다.
                trNextPageAndPopup.Enabled = true;
                Console.WriteLine("SUBURL == " + strSubURL);
                mOSPCrawlerEdge.scriptRun(webMain2, strSeqNo);

                SetLog(String.Format("팝업 => 제목 : {0}, URL : {1}", strTitle, strSubURL));

                Popup_Navigate(strSubURL);
            }
            else
            {
                SetLog("모든 팝업 조회 완료, 검색 종료 처리 시작");
                //검색 종료시간을 저장한다. 채증완료 후 로그를 기록할때 사용한다.
                DriveInfo drv = new DriveInfo("C:\\");
                string str = drv.AvailableFreeSpace.ToString(); // 하드디스크의 사용가능용량
                double dDriveSize = Convert.ToDouble(str);
                dDriveSize /= 1048576;

                string strFilePath = "C:\\evidence_img\\" + mOSPInfo.SITE_ID;
                int nFileCount = clsUtil.GetFileCount(strFilePath);
                mSearchETime = clsUtil.GetToday();

                List<string> listResult = new List<string>();
                listResult.Clear();
                //clsDBProc.InsertSiteHist(mOSPInfo.SITE_ID, mSearchGenre, mSearchSTime, mSearchETime, mLocalIP, ref listResult);
                clsDBProc.InsertSiteHist2(mOSPInfo.SITE_ID, mSearchGenre, mSearchSTime, mSearchETime, mLocalIP, dDriveSize.ToString("0"), nFileCount.ToString(), ref listResult);
                Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);

                //어떠한 이유든 60분안에 게시물조회 및 데이터저장이 완료되지 않는경우
                //재시작을 위해서 사용한다. 여기라인까지 작업이 완료된경우
                //리스타트 타이머를 중지시킨다.
                trTimeoutClose.Enabled = false;

                Close();
            }
        }

        private void trNextJob_Tick(object sender, EventArgs e)
        {
            trNextJob.Enabled = false;
            NextPopupAndNextPage();
        }

        private async void NextPopupAndNextPage()
        {

            SetLog("NextPopupAndNextPage 시작");

            //상세페이지가 정상조회된경우 타임아웃을 해제한다.
            trNextPageAndPopup.Enabled = false;
            mPopupIndex++;

            DataTable dt = mSearchData;

            SetLog($"현재 mPopupIndex: {mPopupIndex}, 총 데이터 개수: {dt.Rows.Count}");
            Console.WriteLine($"현재 mPopupIndex: {mPopupIndex}, 총 데이터 개수: {dt.Rows.Count}");

            if ((mPopupIndex < dt.Rows.Count))
            {
                //이미등록된 게시물은 조회하지 않는다.
                while (mPopupIndex < dt.Rows.Count)
                {
                    string strSeqNo = dt.Rows[mPopupIndex]["SEQNO"].ToString();
                    string strTitle = dt.Rows[mPopupIndex]["TITLE"].ToString();
                    string strSubURL = dt.Rows[mPopupIndex]["SUBURL"].ToString();

#if false    // Image Test
                    if (false)
#else                // TEST CODE(170419) false = 채증 가능안한 것만 , true = 채증 한것도 채증 그래서 캡처를 뜨지 않는 경우가 발생할 수도 있음. true test 용
                    if (IsCrawl4(mOSPInfo.SITE_ID, strSeqNo, strTitle, mPageIndex.ToString()) == false )
#endif
                    {
                        //상세페이지 타임아웃을 시작한다.
                        trNextPageAndPopup.Enabled = true;

                        mOSPCrawlerEdge.scriptRun(webMain2, strSeqNo);
                        if (cbOSPType.SelectedIndex == 3)
                        {
                            string strScript = "openDnWin(" + strSeqNo + ",'N')";
                            await webMain2.CoreWebView2.ExecuteScriptAsync(strScript);
                            clsUtil.Delay(3000);
                        }

                        if (cbOSPType.SelectedIndex == 4)
                        {
                            string strBase = "document.forms[5].";
                            string strId = strBase + "id.value = '" + clsUtil.SubStringEx(strSubURL, "id=", 1, "") + "'";
                            string strAction = strBase + "action = '" + clsUtil.SubStringEx(strSubURL, "co.kr", 1, "admin&id=") + "'";
                            string strTarget = strBase + "target = '_parent'";
                            string strSubmit = strBase + "submit();";
                            await webMain2.CoreWebView2.ExecuteScriptAsync(strId);
                            await webMain2.CoreWebView2.ExecuteScriptAsync(strAction);
                            await webMain2.CoreWebView2.ExecuteScriptAsync(strTarget);
                            await webMain2.CoreWebView2.ExecuteScriptAsync(strSubmit);
                            clsUtil.Delay(1500);
                        }
                        //20221004
                        if (cbOSPType.SelectedIndex == 48)
                        {
                            string strScript = "_disp(" + strSeqNo + ")";
                            await webMain2.ExecuteScriptAsync(strScript);
                        }
                        if (cbOSPType.SelectedIndex == 7)
                        {
                            String contentsList_View = String.Format("contentsList_View(true, {0}, 'N');", strSeqNo);                            
                            await webMain2.ExecuteScriptAsync(contentsList_View);
                            clsUtil.Delay(3000);
                        }

                        SetLog(String.Format("팝업 => 제목 : {0}, URL : {1}", strTitle, strSubURL));
                        Popup_Navigate(strSubURL);
                        return;
                    }
                    else if (mJobType == 1)
                    {
                        trNextPageAndPopup.Enabled = true;
                        //mOSPCrawler.scriptRun(webMainX, strSeqNo);
                        SetLog(String.Format("팝업 => 제목 : {0}, URL : {1}", strTitle, strSubURL));
                        Popup_Navigate(strSubURL);
                        return;
                    }
                    else
                    {
                        SetLog(String.Format("등록된 게시물 => SEQNO : {0}, 제목 : {1}", strSeqNo, strTitle));
                        mPopupIndex++;
                    }
                }
                //현재페이지에서 조회된 게시물중 새로운개시물이 없는경우
                //크롤링을 종료하거나 다음페이지를 조회하도록 한다.
                if (mPopupIndex >= dt.Rows.Count)
                {
                    trNextJob.Enabled = true;
                }
            }
            else
            {
                mPageIndex++;

                if (mPageIndex > mLimitePage)
                {
                    mPageIndex = -1;
                    //검색 종료시간을 저장한다. 채증완료 후 로그를 기록할때 사용한다.
                    mSearchETime = clsUtil.GetToday();
                    List<string> listResult = new List<string>();
                    listResult.Clear();

                    DriveInfo drv = new DriveInfo("C:\\");
                    string str = drv.AvailableFreeSpace.ToString(); // 하드디스크의 사용가능용량
                    double dDriveSize = Convert.ToDouble(str);
                    dDriveSize /= 1048576;
                    string strFilePath = "C:\\evidence_img\\" + mOSPInfo.SITE_ID;
                    int nFileCount = clsUtil.GetFileCount(strFilePath);
                    clsDBProc.InsertSiteHist(mOSPInfo.SITE_ID, mSearchGenre, mSearchSTime, mSearchETime, mLocalIP, ref listResult);
                    clsDBProc.InsertSiteHist2(mOSPInfo.SITE_ID, mSearchGenre, mSearchSTime, mSearchETime, mLocalIP, dDriveSize.ToString("0"), nFileCount.ToString(), ref listResult);
                    Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
                    trTimeoutClose.Enabled = false;

                    //유뷰는 한 장르의 전체 카테고리 모두 크롤할때까지 반복
                    if ((cbOSPType.SelectedIndex == 100 || cbOSPType.SelectedIndex == 101) && mCategoryCount != mCategory)
                        btnRun_Click(null, null);
                    else if ((cbOSPType.SelectedIndex == 140 || cbOSPType.SelectedIndex == 141))
                    {
                        bFirst = false;
                        if (m_nIndex == 3)
                        {
                            SetLog(String.Format("1사이클 수집완료 , {0}분대기", m_nDelayTime));
                            //m_nIndex = 0; m_nJob = 1; // 재수집안하게 바꿈..
                            m_nIndex = 0; m_nJob = 0;
                            clsUtil.SetErrorLog(String.Format("1사이클 수집완료 , {0}분대기", m_nDelayTime));
                            clsUtil.Delay(m_nDelayTime * 1000 * 60);
                            StartSearch(m_nIndex, m_nJob);
                        }
                        else
                        {
                            m_nIndex++;
                            SetLog(String.Format("{0} 수집완료 , {1}분대기", mHomeURL.Rows[mHomeURLIndex]["TOP_GENRE"].ToString(), m_nDelayTimeGenre));
                            clsUtil.SetErrorLog(String.Format("{0} 수집완료 , {1}분대기", mHomeURL.Rows[mHomeURLIndex]["TOP_GENRE"].ToString(), m_nDelayTimeGenre));
                            clsUtil.Delay(m_nDelayTimeGenre * 1000 * 60);
                            StartSearch(m_nIndex, m_nJob);
                        }
                    }
                    else Close();
                }
                else
                {
                    trNextPageAndPopup.Enabled = true;//페이지이동 타임아웃을 시작한다.
                    //Timer방식으로 페이지변경을 확인하기 때문에 이전 페이지를 수집하는 문제가있어 페이지가 초기화되었는지 여부를 확인한다.
                    mInitDocumentCompleted = false;
                    SetLog(String.Format("이동 => 페이지 : {0}", mPageIndex.ToString()));
                    await mOSPCrawlerEdge.setPage(webMain2, mPageIndex.ToString());
                    SetLog(String.Format("이동 => 페이지2 : {0}", mPageIndex.ToString()));
                    await GetOSPBoardParsing();
                    trOSPBoardParsing.Enabled = true; //페이지 갱신 후 게시물의 유효성을 확인 및 파싱하는 타이머실행
                }
            }
        }

        private async void trPopupParsing_Tick(object sender, EventArgs e)
        {
            trPopupParsing.Enabled = false;
            Console.WriteLine("(trPopupParsing_Tick)trPopupParsing.Enabled = " + trPopupParsing.Enabled);

            if (mJobType == 1)
            {

                string html = await webMain2.ExecuteScriptAsync("document.documentElement.outerHTML");
                html = Regex.Unescape(html);
                html = html.Remove(0, 1);
                html = html.Remove(html.Length - 1, 1);
                bool isValid = await GetReCollectionPopupParsing(mPopupURL, html);
                if (isValid == false)
                {
                    trPopupParsing.Enabled = true;
                    Console.WriteLine("(trPopupParsing_Tick)trPopupParsing.Enabled = " + trPopupParsing.Enabled);
                }
            }
            else
            {
                if ((cbOSPType.SelectedIndex == 48))
                {
                    m_html = await webMain2.ExecuteScriptAsync("document.documentElement.outerHTML");
                    m_html = Regex.Unescape(m_html);
                    m_html = m_html.Remove(0, 1);
                    m_html = m_html.Remove(m_html.Length - 1, 1);
                }
                string strtmp = string.Empty;
                if ((cbOSPType.SelectedIndex == 75))
                {
                    string strNumber = "ExecuteScriptAsync(" + clsUtil.SubStringEx(mPopupURL, "https://m.todisk.com/mobile/storage.php?act=view&idx=", 1, "") + ")";

                    strtmp = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
                }
                if (cbOSPType.SelectedIndex == 3)
                {
                    string strScript = "openDnWin(" + clsUtil.SubStringEx(mPopupURL, "contentsID=", 1, "") + ", 'N')";
                    await webMain2.CoreWebView2.ExecuteScriptAsync(strScript);
                    clsUtil.Delay(1000);
                }

                bool bRet = await GetPopupParsing(mPopupURL);
                if (bRet == false)
                {
                    trPopupParsing.Enabled = true;
                    Console.WriteLine("(trPopupParsing_Tick)trPopupParsing.Enabled = " + trPopupParsing.Enabled);
                }
            }
        }

        private async Task<bool> GetPopupParsing(string strURL)
        {
            if (mPopupIndex < 0) return true;
            BOARD_INFO boardInfo = new BOARD_INFO();
            
            if (cbOSPType.SelectedIndex == 94)
                await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByTagName('input')['allCheck'].click()");
            if (cbOSPType.SelectedIndex == 135)
                await webMain2.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('popup_layer action_view_slide_info')[0].remove()");
            if (cbOSPType.SelectedIndex == 48 || cbOSPType.SelectedIndex == 38)  //스마트파일PC,모바일 페이지 광고팝업 제거
            {
                webMain2.CoreWebView2.ExecuteScriptAsync("btnClose20('10800')");
            }
            if (cbOSPType.SelectedIndex == 134)  //스마트파일PC,모바일 페이지 광고팝업 제거
            {
                webMain2.CoreWebView2.ExecuteScriptAsync("install_layer_toggle()");
            }

            bool bSucc = false;

            string html = await webMain2.ExecuteScriptAsync("document.documentElement.outerHTML");
            if (cbOSPType.SelectedIndex == 137) html = await webMain2.ExecuteScriptAsync("document.documentElement.innerHTML");
            html = Regex.Unescape(html);
            html = html.Remove(0, 1);
            html = html.Remove(html.Length - 1, 1);
            bSucc = mOSPCrawlerEdge.getPopupInfo(html, strURL, ref boardInfo, listPopup2);

            if (bSucc)
            {
                mBoardNo = boardInfo.DESC_URL;
                // 애플파일 모바일 : 차단 될 경우 모든 팝업이 삭제 된 컨텐츠라고 표시되므로 관련 예외처리.
                if (cbOSPType.SelectedIndex == 56 && mJobType == 0)
                {
                    mOptStr = string.Empty;
                    mOptInt = 0;
                }
                //웹페이지 조회가 성공했지만 수집제외대상인경우 금액을 0원으로 적용하여 저장하지않고 다음게시물을 조회한다.
                if (boardInfo.MONEY.CompareTo("UNKNOWN") == 0 || boardInfo.MONEY.CompareTo("DELETE") == 0)
                {
                    trNextJob.Enabled = true;//페이지 또는 상세정보 인덱스증가 및 조회
                    return true;
                }
                List<string> listResult = new List<string>();
                DataTable dt = mSearchData;
                if (boardInfo.UPLOADER_ID.Length <= 0) boardInfo.UPLOADER_ID = dt.Rows[mPopupIndex]["NAME"].ToString();
                if (boardInfo.MONEY.Length <= 0) boardInfo.MONEY = dt.Rows[mPopupIndex]["MONEY"].ToString();
                if (boardInfo.FILE_SIZE.Length <= 0) boardInfo.FILE_SIZE = dt.Rows[mPopupIndex]["FILESIZE"].ToString();
                if (cbOSPType.SelectedIndex == 44)
                {
                //    if (boardInfo.TITLE != dt.Rows[mPopupIndex]["TITLE"].ToString()) return false;
                }
                if (boardInfo.TITLE.Length <= 0) boardInfo.TITLE = dt.Rows[mPopupIndex]["TITLE"].ToString();
                boardInfo.SEQNO = dt.Rows[mPopupIndex]["SEQNO"].ToString();
                boardInfo.GENRE = dt.Rows[mPopupIndex]["TYPE"].ToString();
                boardInfo.REG_DATE = clsUtil.GetToday();

                if (cbOSPType.SelectedIndex == 37)
                {
                    string strtmp1 = string.Empty;
                    string subURL = dt.Rows[mPopupIndex]["SUBURL"].ToString();
                    int strURLNum = "https://www.todisk.com/_main/popup.php?doc=bbsInfo&idx=".Length;
                    string strNumber = string.Format("php_base64_encode({0})", subURL.Substring(strURLNum - 1).Trim());
                    strtmp1 = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);

                    boardInfo.DESC_URL = subURL + "&eidx=" + strtmp1;
                }
                else if (cbOSPType.SelectedIndex == 75)
                {
                    string strtmp2 = string.Empty;
                    string subURL = dt.Rows[mPopupIndex]["SUBURL"].ToString();
                    int strURLNum = "https://m.todisk.com/mobile/storage.php?act=view&idx=".Length;
                    string strNumber = string.Format("php_base64_encode({0})", subURL.Substring(strURLNum - 1).Trim());
                    strtmp2 = await webMain2.CoreWebView2.ExecuteScriptAsync(strNumber);
                    boardInfo.DESC_URL = subURL + "&eidx=" + strtmp2;
                }
                else
                {
                    boardInfo.DESC_URL = dt.Rows[mPopupIndex]["SUBURL"].ToString();
                }
                
                boardInfo.RESULT_STATUS = "00";
                string strtmp = mOSPInfo.SITE_ID + boardInfo.SEQNO + boardInfo.REG_DATE;
                boardInfo.CRAWL_ID = clsUtil.toMD5(strtmp);
                SetLog(String.Format("상세정보 => 식별자 : {0}, 제휴 : {1}, 게시자 : {2}, 금액 : {3}, 파일용량 : {4}",
                    boardInfo.CRAWL_ID, boardInfo.LICENSE, boardInfo.UPLOADER_ID, boardInfo.MONEY, boardInfo.FILE_SIZE));
#if true    // imagetest               
#else
                if (IsCrawl4(mOSPInfo.SITE_ID, boardInfo.SEQNO, boardInfo.TITLE, mPageIndex.ToString()) == false)
#endif
                {
                    boardInfo.FILE_PATH = boardInfo.CRAWL_ID + "_" + boardInfo.REG_DATE + "_" + mOSPInfo.OSP_ID + clsSFtp.mServerType + ".jpg";
                    await WebBrowserCapture(boardInfo.FILE_PATH, dt.Rows[mPopupIndex]["SUBURL"].ToString());
                    if (SaveCaptureImg(boardInfo.FILE_PATH, true) == false) return false;
                    listResult.Clear();
                    bool bInsertNoticeDetailInfo2 = false;
                    clsDBProc.InsertNoticeInfo(mOSPInfo.SITE_ID, boardInfo, mSearchGenre, ref listResult, false);
                    Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
                    if (listResult[1] == "성공")
                        bInsertNoticeDetailInfo2 = true;
                    listResult.Clear();
                    if (bInsertNoticeDetailInfo2)
                    {
                        clsDBProc.InsertNoticeDetailInfo2(mOSPInfo.SITE_ID, boardInfo, ref listResult, false);
                        Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
                    }
                    string strUploadID = boardInfo.UPLOADER_ID;
                    //업로드정보를 사용가능한 OSP만 업로더 정보를 사용한다.
                    if (mOSPInfo.CHECK_UPLOADER_ID == false) strUploadID = string.Empty;
                    bool isImageSave = true;
                    if (false) { }
                    else
                    {
                        List<string> listData = new List<string>();
                        clsUtil.RateOfConcordanceParse(boardInfo.TITLE, ref listData);

                        if (listData.Count < 4)
                        {
                            int nCount = 4 - listData.Count + 1;
                            for (int i = 0; i < nCount; i++)
                            {
                                listData.Add("");
                            }
                        }
                        listResult.Clear();
                        //수동매핑 비교처리
                        clsDBProc.InsertContentMapping2(boardInfo.CRAWL_ID, boardInfo.TITLE, listData[0], listData[1], listData[2], listData[3], mSearchGenre, ref listResult);
                        //결과코드가 98인경우 맴핑후 TBMO_CRAWL_DEL_HIST로 옮겨간경우이다.
                        //조건에따라 삭제된경우 이미지를 저장하지 않는다.
                        if (listResult[0].CompareTo("98") == 0)
                        {
                            //imagetest
                          //  isImageSave = false;
                        }
                        Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
                    }

                    if (isImageSave == true)
                        SaveCaptureImg(boardInfo.FILE_PATH);
                    //뒤로가기
                    if (cbOSPType.SelectedIndex != 44)
                        webMain2.GoBack();
                    clsUtil.Delay(1000);

                    if (mJobType == 1)
                    {
                        webMain2.CoreWebView2.Navigate(strURL);
                    }
                }
                //페이지 또는 상세정보 인덱스증가 및 조회
                trNextJob.Enabled = true;
                return true;
            }
            else if (cbOSPType.SelectedIndex == 56 && mJobType == 0)    // 애플파일_모바일 
            {
                // 애플파일 모바일 : 차단 될 경우 모든 팝업이 삭제 된 컨텐츠라고 표시되므로 연속적으로 삭제 컨텐츠라고 인식되면 프록시를 변경해줌.
                if (mOptStr != mSearchData.Rows[mPopupIndex]["TITLE"].ToString())
                {
                    mOptStr = mSearchData.Rows[mPopupIndex]["TITLE"].ToString();
                    if (mOptInt++ >= 10) mProxyList.SetProxy2();
                }
            }
            return false;
        }

        // 위디스크, 파일노리 해당 OSP 홈페이지에서
        // 게시물 타이틀로 검색하여 있으면
        //(true/블라인드 안됨) 없으면(flase/블라인드 됨)
        /*
        private bool GetReCollectionBilndCheck()
        {
            DataTable dt = mSearchData;
            HtmlDocument doc = mOSPCrawler.GetDoc(webMainX);

            string strTitle = dt.Rows[mPopupIndex]["TITLE"].ToString();
            string strSeqno = dt.Rows[mPopupIndex]["SEQNO"].ToString();

            if (cbOSPType.SelectedIndex == 3)
            {
                mBlindNode = clsWebDocument.setInnerText(doc, "input", "id", "searchBox", strTitle);
                mBlindNode = clsWebDocument.InvokeMember(doc, "div", "classname", "searchbtn", "검색", "Click"); ;
            }

            else if (cbOSPType.SelectedIndex == 7)
            {
                mBlindNode = clsWebDocument.setInnerText(doc, "input", "id", "searchWord", strTitle);
                mBlindNode = clsWebDocument.InvokeMember(doc, "input", "title", "검 색", "", "Click"); ;
            }

            clsUtil.Delay(1000);

            //webMain 값 갱신..
            doc = mOSPCrawler.GetDoc(webMainX);

            clsHTMLParser parser = new clsHTMLParser();

            if (doc.Body.OuterHtml.IndexOf(strSeqno) < 0)
                return false;
            else
                return true;
        }
        */
        private async Task<bool> GetReCollectionPopupParsing(string strURL, string html)
        {
            if (mPopupIndex < 0) return true;
            BOARD_INFO boardInfo = new BOARD_INFO();

            // 위디스크, 파일노리 블라인드 게시물 체크
            // 재수집중 블라인드 유 무를 확인하여 블라인드 된 데이터는 삭제 처리
            /*
            if (cbOSPType.SelectedIndex == 3 || cbOSPType.SelectedIndex == 7)
            {
                if (GetReCollectionBilndCheck() != true)
                {   // OSP SITE 리뉴얼 되었을때 대비..
                    if (mBlindNode == true)
                    {
                        boardInfo.CRAWL_ID = mSearchData.Rows[mPopupIndex]["CRAWL_ID"].ToString();
                        // 금액 테이블을 0보다 낮게 입력하면 삭제된 게시물로 판별 됨. (feat.DB)
                        boardInfo.MONEY = "-1";
                        boardInfo.RESULT_STATUS = "00";
                        // DB 요청으로 REG_DATE 추가.
                        boardInfo.REG_DATE = clsUtil.GetToday();

                        string strCWLDesc = String.Format("[{0}] 회차 재수집", clsUtil.IntParse(mSearchData.Rows[mPopupIndex]["RECOLLECTION_COUNT"].ToString()) + 1);

                        List<string> listResult = new List<string>();

                        //재수집 중 삭제로 DB에 저장
                        clsDBProc.InsertReCollectionHist(boardInfo, "채증", strCWLDesc, ref listResult, false);
                        Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);

                        SetLog(String.Format("블라인드 데이터 삭제처리 : {0}, {1}, {2}", cbOSPType.SelectedIndex, boardInfo.CRAWL_ID, boardInfo.REG_DATE));

                        clsUtil.SetErrorLog(String.Format("블라인드 데이터 삭제처리 : {0}, {1}, {2}", cbOSPType.SelectedIndex, boardInfo.CRAWL_ID, boardInfo.REG_DATE));
                        clsUtil.SetErrorLog(String.Format("결과코드 = {0} 결과메세지 = {1}", listResult[0], listResult[1]));
                        //페이지 또는 상세정보 인덱스증가 및 조회
                        NextReCollection();

                        return true;
                    }
                    else                        
                    {
                        List<string> listResult = new List<string>();
                        string strCWLDesc = String.Format("[{0}] 회차 재수집 블라인드확인불가", clsUtil.IntParse(mSearchData.Rows[mPopupIndex]["RECOLLECTION_COUNT"].ToString()) + 1);
                        clsDBProc.InsertReCollectionHist(boardInfo, "채증", strCWLDesc, ref listResult, false);
                        Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
                        clsUtil.SetErrorLog(String.Format("블라인드 데이터 확인 불가 : {0}", cbOSPType.SelectedIndex));
                        NextReCollection();

                        return true;
                    }
                }
            }          
            */


            if (mOSPCrawlerEdge.getPopupInfo(html, strURL, ref boardInfo, listPopup2) == true)
            {
                DataTable dt = mSearchData;
                boardInfo.CRAWL_ID = dt.Rows[mPopupIndex]["CRAWL_ID"].ToString();
                boardInfo.SEQNO = dt.Rows[mPopupIndex]["SEQNO"].ToString();
                boardInfo.TITLE = dt.Rows[mPopupIndex]["TITLE"].ToString();
                boardInfo.GENRE = dt.Rows[mPopupIndex]["TYPE"].ToString();
                boardInfo.FILE_SIZE = dt.Rows[mPopupIndex]["FILESIZE"].ToString();
                boardInfo.REG_DATE = clsUtil.GetToday();
                boardInfo.DESC_URL = dt.Rows[mPopupIndex]["SUBURL"].ToString();
                boardInfo.RESULT_STATUS = "00";
                boardInfo.FILE_PATH = boardInfo.CRAWL_ID + "_" + boardInfo.REG_DATE + "_" + mOSPInfo.OSP_ID + clsSFtp.mServerType + ".jpg";

                //재수집시에는 이미지를 그냥저장하지만 일반수집시 수집제외대상인경우 이미지를 저장하지않기때문에
                //2개함수를 이용해서 이미지를 저장한다.
                //수집시 이미지 로직변경시 재수집시 이미지저장방법도 확인필요
                //재수집시 임시로 다른 폴더에 스크린샷을 저장 한다.
                await WebBrowserCapture(boardInfo.FILE_PATH, strURL);
                if (SaveCaptureImg(boardInfo.FILE_PATH, true) == false) return false;
                SetLog(String.Format("상세정보 => 식별자 : {0}, 제휴 : {1}, 게시자 : {2}, 금액 : {3}, 파일용량 : {4}",
                    boardInfo.CRAWL_ID, boardInfo.LICENSE, boardInfo.UPLOADER_ID, boardInfo.MONEY, boardInfo.FILE_SIZE));
                string strCWLDesc = String.Format("[{0}] 회차 재수집", clsUtil.IntParse(dt.Rows[mPopupIndex]["RECOLLECTION_COUNT"].ToString()) + 1);
                List<string> listResult = new List<string>();
                //2022.02.15 예스파일_모바일 재수집 가격0원이면 DB Insert하지 않도록 설정 ( 로딩중이슈.. )
                if (cbOSPType.SelectedIndex == 53 && boardInfo.MONEY == "0")
                {
                    NextReCollection();
                    return true;
                }
                //재수집중 정상 조회로 저장.
                clsDBProc.InsertReCollectionHist(boardInfo, "채증", strCWLDesc, ref listResult, false);
                Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
                // DB 에서 필요한 이미지라고 판단해주면, 임시폴더에서 원래 폴더로 옮긴다.
                ImageMoveAndDel(boardInfo.FILE_PATH, !(listResult[1].Contains("삭제")));
                NextReCollection();//페이지 또는 상세정보 인덱스증가 및 조회
                return true;
            }
            return false;
        }
        // 게시물이 등록된경우 True, 미등록상태인경우 False.       
        private bool IsCrawl4(string strSiteID, string strPostID, string strPostName, string strPageNum)
        {
            List<string> listResult = new List<string>();
            clsDBProc.IsCrawl4(strSiteID, strPostID, strPostName, strPageNum, ref listResult);
            Console.WriteLine("결과코드 = " + listResult[0] + " 결과메세지 = " + listResult[1]);
            return clsUtil.isCompare(listResult[0], "00");
        }

        private void dgvSearchData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            if (dgvSearchData.Columns[e.ColumnIndex].Name.CompareTo("SUBURL") == 0)
            {
                string strSubURL = dgvSearchData.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                {
                    mOSPCrawlerEdge.scriptRun(webMain2, dgvSearchData.Rows[e.RowIndex].Cells["SEQNO"].Value.ToString());
                }
                dlgPopup dlg = new dlgPopup();
                dlg.mURL = strSubURL;
                dlg.ShowDialog();
            }
        }

        public async void BoardListCapture()
        {
            try
            {
                Application.DoEvents();//캡쳐하기전 윈도우이벤트를 모두처리하고 캡쳐한다.
                if (mImgBoardList != null)
                {
                    mImgBoardList.Dispose();
                    mImgBoardList = null;
                }
                mImgBoardList = await setCapture3(webMain2);
            }
            catch { }
        }

        public static async Task<Bitmap> setCapture3(Microsoft.Web.WebView2.WinForms.WebView2 web2)
        {
            dynamic scl = null;
            Size siz;
            siz = scl != null ?
                        new Size((int)scl.w > web2.Width ? (int)scl.w : web2.Width,
                        (int)scl.h > web2.Height ? (int)scl.h : web2.Height) : web2.Size;

            dynamic clip = new JObject();
            clip.x = 0; clip.y = 0;
            clip.width = web2.Width; clip.height = web2.Height;
            clip.scale = 1;

            dynamic settings = new JObject();
            settings.format = "jpeg"; settings.clip = clip;
            settings.fromSurface = true;
            settings.captureBeyondViewport = true;

            var p = settings.ToString(Newtonsoft.Json.Formatting.None);
            var devData = "";
            try
            {
                devData = await web2.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", p);
            }
            catch (Exception e)
            {
                string stre = e.Message;
            }
            var imgData = (string)((dynamic)JObject.Parse(devData)).data;
            var ms = new MemoryStream(Convert.FromBase64String(imgData));
            return (Bitmap)Image.FromStream(ms);
        }

        private async Task<bool> WebBrowserCapture(string strFileName, string strURL)
        {
            try
            {
                Application.DoEvents();
                Image imgOSP;
                {
                    Bitmap image = new Bitmap(webMain2.Width, 1280);
                    Rectangle bitmapRect = new Rectangle(0, 0, webMain2.Width, 1280);
                    image = await setCapture3(webMain2);
                    imgOSP = image as Image;
                }
                Image imgClock = clsUtil.ScreenShot3("UTCk3");
                if (imgClock == null) return false;
                int nWidth = 0; int nHegiht = 0;
                int nPopupWidth = 0; int nMergeWidth = 400;
                if (mImgBoardList != null)
                {
                    nMergeWidth = (mImgBoardList.Width / 100 * 30);
                    nWidth = mImgBoardList.Width + imgOSP.Width - nMergeWidth;
                    nHegiht = mImgBoardList.Height;
                    if (nHegiht > 2500)
                        nHegiht = 2500;
                    nPopupWidth = mImgBoardList.Width - nMergeWidth;
                }
                else
                {
                    nWidth = imgOSP.Width; nHegiht = imgOSP.Height;
                }
                if (mImgCapture != null)
                {
                    mImgCapture.Dispose();
                    mImgCapture = null;
                }
                mImgCapture = new Bitmap(nWidth, nHegiht);
                Graphics g = Graphics.FromImage(mImgCapture);
                //바탕색초기화
                g.Clear(Color.Gray);
                int nClockPrintX = -1;
                int nClockPrintY = -1;
                if (mImgBoardList != null)
                {
                    g.DrawImage(mImgBoardList, 0, 0, mImgBoardList.Width, nHegiht);
                    GetImageIDLocation(cbOSPType.SelectedIndex, ref nClockPrintX, ref nClockPrintY);
                }
                g.DrawImage(imgOSP, new Point(nPopupWidth, 0));
                if (nClockPrintX < 0 || nClockPrintY < 0)
                {
                    nClockPrintX = nWidth - imgClock.Width;
                    nClockPrintY = imgOSP.Height - imgClock.Height;
                }
                g.DrawImage(imgClock, new Point(nClockPrintX, nClockPrintY));

                //20230206 url을 캡쳐화면 하단부에 추가..일단 막아놈
                //g.DrawString(strURL, mGFont, Brushes.Black, 0, nHegiht-50);
                // 추가적으로 가려야 하는 ID 정보.
                if (mRemoveID)
                {
                    SolidBrush WhiteBrush = new SolidBrush(mBrushColor);
                    g.FillRectangle(WhiteBrush, mRemoveRect);
                }

                if (mOSPInfo.OSP_ID.CompareTo("MEGAFILE") == 0
                    && mOSPInfo.OSP_TYPE.CompareTo("SITE0020") == 0)
                {
                    if (mImgBoardList != null)
                        g.FillRectangle(Brushes.White, 0, 207, 500, 31);

                    if (mImgBoardList != null)
                        g.FillRectangle(Brushes.White, 600, 122, 640, 31);
                    else
                        g.FillRectangle(Brushes.White, 0, 122, 640, 31);
                }
                //캡쳐가 정상적으로 되고있는지 확인용으로 출력한다.
                pibCapture.Image = mImgCapture;
                lbCaptureTime.Text = String.Format("{0}", strFileName);
            }
            catch (Exception ex)
            {
                clsUtil.SetErrorLog("이미지캡쳐에러 : " + ex.Message);
                return false;
            }
            return true;
        }

        private void ImageMoveAndDel(string strFileName, bool isMove)
        {
            try
            {
                string strSrcPath = String.Format(@"{0}{1}_TMP\{2}", clsSFtp.mLOCALDIR, mOSPInfo.SITE_ID, strFileName);
                string strDstPath = String.Format(@"{0}{1}\{2}", clsSFtp.mLOCALDIR, mOSPInfo.SITE_ID, strFileName);

                if (isMove)
                {
                    System.IO.File.Move(strSrcPath, strDstPath);
                }
                else
                {
                    System.IO.File.Delete(strSrcPath);
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        private bool SaveCaptureImg(string strFileName, bool isReCollect = false)
        {
            if (mImgCapture != null)
            {
                try
                {
                    string strTmpPath = mOSPInfo.SITE_ID;
                    if (isReCollect) strTmpPath += "_TMP";
                    string strDirPath = String.Format(@"{0}{1}", clsSFtp.mLOCALDIR, strTmpPath);
                    string strFullPath = String.Format(@"{0}{1}\{2}", clsSFtp.mLOCALDIR, strTmpPath, strFileName);
                    clsUtil.MakeDirectory(strDirPath);

                    mImgCapture.Save(strFullPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                    return true;
                }
                catch (System.Exception ex)
                {
                    clsUtil.SetErrorLog("이미지저장에러 : " + ex.Message);
                    return false;
                }
            }

            return false;
        }


        private void GetImageIDLocation(int nIndex, ref int nX, ref int nY)
        {
            #region 캡쳐이미지 아이디위치 관리,utck위치
            switch (nIndex)
            {
                case 1: nX = 50; nY = 154; break; // 1 : 피디팝  
                case 2: nX = 50; nY = 179; break; // 2 : 쉐어박스
                case 3: nX = 50; nY = 136; break; // 3 : 위디스크
                case 4: nX = 50; nY = 99; break;  // 4 : 메가파일
                case 5: nX = 50; nY = 113; break; // 5 : 파일시티
                case 7: nX = 30; nY = 74; break;  // 7 : 파일노리
                case 11: nX = 815; nY = 109; break; // 11 : G파일
                case 12: nX = 50; nY = 203; break; // 12 : 빅파일
                case 13: nX = 10; nY = 156; break; // 13 : 티플
                case 15: nX = 50; nY = 160; break; // 15 : 미투디스크
                case 17: nX = 271; nY = 161; break; // 17 : 파일쿠키
                case 23: nX = 50; nY = 156; break; // 23 : 파일조
                case 24: nX = 10; nY = 154; break; // 24 : 애플파일
                case 25: nX = 10; nY = 154; break; // 25 : 예스파일
                case 31: nX = 30; nY = 166; break; // 31 : 온디스크
                case 32: nX = 50; nY = 170; break; // 32 : 케이디스크
                case 36: nX = 137; nY = 189; break; // 36 : 새디스크
                case 37: nX = 50; nY = 130; break; // 37 : 투디스크
                case 38: nX = 10; nY = 138; break; // 38 : 스마트파일
                case 42: nX = 300; nY = 80; break; // 42 : 파일캐스트     
                case 43: nX = 0; nY = 0; break; // 43 : 위디스크_모바일                    
                case 44: nX = 0; nY = 0; break; // 44 : 메가파일_모바일
                case 45: nX = 0; nY = 185; break; // 45 : 파일조_모바일                    
                case 46: nX = 0; nY = 0; break; // 46 : 케이디스크_모바일                    
                case 48: nX = 750; nY = 0; break; // 48 : 스마트파일_모바일     
                case 49: nX = 0; nY = 0; break; // 49 : 파일노리_모바일                                        
                case 50: nX = 0; nY = 0; break; // 50 : 피디팝_모바일                       
                case 51: nX = 0; nY = 0; break; // 51 : 티플_모바일                    
                case 53: nX = 645; nY = 165; break; // 53 : 예스파일_모바일                    
                case 54: nX = 0; nY = 0; break; // 54 : 빅파일_모바일
                case 55: nX = 0; nY = 0; break; // 55 : 온디스크_모바일   
                //case 56: nX = 705; nY = 195; break; // 56 : 애플파일_모바일 
                case 64: nX = 0; nY = 0; break; // 64 : 미투디스크_모바일                                   
                case 65: nX = 0; nY = 0; break; // 65 : 파일시티_모바일               
                case 85: nX = 30; nY = 100; break; // 85 : 지디스크      
                case 100: nX = 957; nY = 10; break; // 100 : 유뷰                    
                case 102: nX = 10; nY = 154; break; // 102 : 파일이즈
                case 126: nX = 10; nY = 150; break; // 126 : 파일썬
                case 132: nX = 420; nY = 62; break; // 132 : 파일몽
                case 154: nX = 10; nY = 62; break; // 154 : 메타파일
            }
            #endregion
        }

        public void SetLog(string strLog, bool isOnly = false)
        {
            if (chkLog.Checked == true || isOnly == true)
            {
                if (mLogLineCount > 500)
                {
                    mLogLineCount = 0;
                    rtbLog.Clear();
                }

                string strNowTime = DateTime.Now.ToString("HH:mm:ss");

                rtbLog.Text = "[" + strNowTime + "] " + strLog + "\r\n" + rtbLog.Text;

                mLogLineCount++;
            }
        }

        private void btnGC_Click(object sender, EventArgs e)
        {
            clsAutoUpdate update = new clsAutoUpdate();
            if (update.Init() == true)
            {
                if (update.StartUpdate() == true)
                {
                    MessageBox.Show("업데이트 성공");
                }
                else
                {
                    MessageBox.Show("업데이트 실패");
                }
            }
        }

        private void trTimeoutClose_Tick(object sender, EventArgs e)
        {
            trTimeoutClose.Enabled = false;

            trNextPageAndPopup.Enabled = false;
            trOSPBoardParsing.Enabled = false;
            trPopupParsing.Enabled = false;
            Console.WriteLine("(trTimeoutClose_Tick)trPopupParsing.Enabled = " + trPopupParsing.Enabled);

            mSearchETime = clsUtil.GetToday();

            DriveInfo drv = new DriveInfo("C:\\");
            string str = drv.AvailableFreeSpace.ToString(); // 하드디스크의 사용가능용량

            double dDriveSize = Convert.ToDouble(str);
            dDriveSize /= 1048576;

            string strFilePath = "C:\\evidence_img\\" + mOSPInfo.SITE_ID;
            int nFileCount = clsUtil.GetFileCount(strFilePath);

            List<string> listResult = new List<string>();

            clsDBProc.InsertSiteHist2(mOSPInfo.SITE_ID, "타임아웃", mSearchSTime, mSearchETime, mLocalIP, dDriveSize.ToString("0"), nFileCount.ToString(), ref listResult);

            clsUtil.SetErrorLog("비정상 프로그램 정지, 프로그램 종료");
            Close();
        }

        private void chkLog_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLog.Checked == true)
            {
                rtbLog.Clear();
            }
        }

        private void btnEmpty_Click(object sender, EventArgs e)
        {
            SetLog(String.Format("FLAG = {0}, TIMER상태 = {1}, JOB =  {2}",
                mInitDocumentCompleted, trOSPBoardParsing.Enabled, mNowStatus));
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            mProcessExitFlag = false;
            //프로그램 종료시 FTP연결을 해제한다.
            if (mFTP != null)
            {
                while (mFTP.IsFile() == true) { Thread.Sleep(1000); }
                mFTP.DisConnect();
            }
            if (mProxyList != null)
                mProxyList.mIsProxyRun = false;
        }
        private void rtbLog_TextChanged(object sender, EventArgs e)
        {
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void pibCapture_Click(object sender, EventArgs e)
        {

        }

        private void lbCaptureTime_Click(object sender, EventArgs e)
        {

        }

        private void pibCapture_Click_1(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click_1(object sender, EventArgs e)
        {

        }

        private void trFileLogin_Tick(object sender, EventArgs e)
        {
            trFileLogin.Enabled = false;
        }

    }

}
