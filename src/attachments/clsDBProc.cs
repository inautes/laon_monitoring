using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace OSPAutoSearch_AutoLogin
{
    public class clsDBProc
    {
        /*public static void GetToday(ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("out_today", NpgsqlTypes.NpgsqlDbType.Varchar, 14);

            runDB.DBExecuteNonQuery("PKG_COMMON.GET_TODAY", ref listResult);
        }*/

        public static DataTable GetOSPInfo(string strOSP_Type, string strOSP_ID, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_osp_type", strOSP_Type);
            runDB.setParameter("in_osp_id", strOSP_ID);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Text);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Text);
            runDB.setParameter("fetch_data", NpgsqlTypes.NpgsqlDbType.Refcursor);

            return runDB.DBExecute("pkg_evidence.get_osp_info", ref listResult);
        }

        /*public static void IsCrawl(string strCrawlID, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.IS_CRAWL", ref listResult);
        }

        public static void IsCrawl2(string strSiteID, string strPostID, string strPostName, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("in_post_id", strPostID);
            runDB.setParameter("in_post_name", strPostName);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.IS_CRAWL2", ref listResult);
        }

        public static void IsCrawl3(string strSiteID, string strPostID, string strPostName, string strPageNum, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("in_post_id", strPostID);
            runDB.setParameter("in_post_name", strPostName);
            runDB.setParameter("in_cwl_page", strPageNum);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.IS_CRAWL3", ref listResult);
        }*/

        public static void IsCrawl4(string strSiteID, string strPostID, string strPostName, string strPageNum, ref List<string> listResult)
        {
            
            clsDBExecute runDB = new clsDBExecute();
            bool isErr = false;
            bool isCrawl = false;
            string[] strResult = new string[2];
            string strQuery = @"SELECT COUNT(*)
                                FROM TBMO_CRAWL_MASTER
                                WHERE SITE_ID ='" + strSiteID + 
                                @"' AND   POST_ID ='" + strPostID +
                                @"' AND EXTRACT(EPOCH FROM (CURRENT_DATE - TO_TIMESTAMP(REG_DATE, 'YYYYMMDDHH24MISS'))) < 30 * 24 * 60 * 60";
            runDB.DBExecute2(strQuery, ref strResult[0]);


          

            strQuery = @"SELECT COUNT(*)
                                FROM TBMO_CRAWL_DEL_HIST
                                WHERE SITE_ID ='" + strSiteID +
                                @"' AND   POST_ID ='" + strPostID +
                                @"' AND EXTRACT(EPOCH FROM (CURRENT_DATE - TO_TIMESTAMP(CRAWL_DATE, 'YYYYMMDDHH24MISS'))) < 30 * 24 * 60 * 60";
            if (strPostID != "111")
                runDB.DBExecute2(strQuery, ref strResult[1]);
            else
            {
                strResult[0] = "0";
                strResult[1] = "0";
            }

             if (strPostID == "111")
            {
                listResult.Add("00");
                listResult.Add("이미 등록된 게시물 입니다.");
                isCrawl = true;
            }
            else if (strResult[0].Length > 0 && strResult[1].Length > 0)
            {
                int nResult = clsUtil.IntParse(strResult[0]) + clsUtil.IntParse(strResult[1]);
                if (nResult == 0)
                {
                    listResult.Add("01");
                    listResult.Add("등록된 게시물이 없습니다.");
                }
                else
                {
                    isCrawl = true;
                }
            }            
            else
            {
                isErr = true;
            }

            if (isErr)
            {
                // 쿼리 실패.
                listResult.Add("99");
                listResult.Add("실패");
            }
            else if (isCrawl)
            {   
                // 수집된 이력이 있으므로 스킵.
                listResult.Add("00");
                listResult.Add("이미 등록된 게시물 입니다.");
            }
        }

        //20190409 광고팝업 자동으로 닫기위해서 추가.
        public static void PopClose(string strSiteID,bool bIsDetail ,ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            
            string strQuery = String.Format(@"SELECT site_id, tag, attr, attr_value from TBMO_CWL_POPUP where site_id ='{0}' and loc_cd={1}", strSiteID, bIsDetail == true ? 1 : 0);
            
            runDB.DBExecute3(strQuery, ref listResult);
        }

        /* 
         * 메인 DataGridView 컬럼
         * NO = No.
         * SEQNO = SeqNo
         * PARTNER = 제휴
         * TITLE = 제목
         * FILESIZE = 용량
         * MONEY = 가격
         * TYPE = 분류
         * NAME = 게시자
         * TIME = 시간
         * SUBURL = 서브URL
         * UNIQUE_NUMBER = 고유번호
         * FILECOUNT = 파일갯수
         * */

        public static void InsertNoticeInfo(string strSiteID, BOARD_INFO info, string strGenre, ref List<string> listResult,bool bIsMD)
        {
            //FileSize에 기가단위를 메가단위로 통일한다.         
            int nFileSize = 0;
            if (info.FILE_SIZE.ToUpper().Contains("G") == true
                || info.FILE_SIZE.ToUpper().Contains("GB") == true)
            {
                nFileSize = (int)(clsUtil.DoubleParse(info.FILE_SIZE) * 1024);
            }
            else
            {
                nFileSize = (int)(clsUtil.DoubleParse(info.FILE_SIZE));
            }

            clsDBExecute runDB = new clsDBExecute();
            if (info.TITLE == "")
                info.TITLE = "(제목없음)";

            runDB.setParameter("in_site_id", strSiteID);
            if(bIsMD)
                runDB.setParameter("in_crawl_id", info.CRAWL_ID_MD);
            else
                runDB.setParameter("in_crawl_id", info.CRAWL_ID);
            runDB.setParameter("in_post_id", info.SEQNO);
            runDB.setParameter("in_post_name", info.TITLE);            
            runDB.setParameter("in_post_genre", strGenre);
            runDB.setParameter("in_file_count", info.FILE_LIST.Count);
            runDB.setParameter("in_file_size", nFileSize);
            runDB.setParameter("in_uploader_id", info.UPLOADER_ID);
            runDB.setParameter("in_reg_date", info.REG_DATE); 
            runDB.setParameter("in_desc_url", info.DESC_URL);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("pkg_evidence.insert_notice_info", ref listResult);
        }

       /* public static void InsertNoticeDetailInfo(BOARD_INFO info, ref List<string> listResult)
        {
            //금액을 값과 단위로 구분한다.
            int nMoney = clsUtil.IntParse(info.MONEY);
            string strPRUnit = clsUtil.TrimNumber(info.MONEY);

            string strLicense = info.LICENSE;
            if (strLicense.CompareTo("제휴") == 0) strLicense = "Y";
            else if (strLicense.CompareTo("미제휴") == 0) strLicense = "N";
            else if (strLicense.CompareTo("UnKnown") == 0) strLicense = "U";
            else if (strLicense.CompareTo("Unknown") == 0) strLicense = "U";
                                           
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_crawl_id", info.CRAWL_ID);
            runDB.setParameter("in_crawl_date", info.REG_DATE);
            runDB.setParameter("in_crawl_price", nMoney);
            runDB.setParameter("in_crawl_pr_unit", strPRUnit);
            runDB.setParameter("in_post_license", strLicense);
            runDB.setParameter("in_crawl_file_path", info.FILE_PATH);
            runDB.setParameter("in_crawl_status", "00");
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_NOTICE_DETAIL_INFO", ref listResult);
        }*/

        public static void InsertNoticeDetailInfo2(string strSiteID, BOARD_INFO info, ref List<string> listResult,bool bIsMD)
        {
            //FileSize에 기가단위를 메가단위로 통일한다.         
            int nFileSize = 0;
            if (info.FILE_SIZE.ToUpper().Contains("G") == true)
            {
                nFileSize = (int)(clsUtil.DoubleParse(info.FILE_SIZE) * 1024);
            }
            else
            {
                nFileSize = (int)(clsUtil.DoubleParse(info.FILE_SIZE));
            }

            //금액을 값과 단위로 구분한다.
            int nMoney = 0;
            string strPRUnit = string.Empty;
            if (bIsMD)
            {
                nMoney = clsUtil.IntParse(info.MONEY_DN);
                strPRUnit = clsUtil.TrimNumber(info.MONEY_DN);
            }
            else
            {
                nMoney = clsUtil.IntParse(info.MONEY);
                strPRUnit = clsUtil.TrimNumber(info.MONEY);
            }
            

            string strLicense = info.LICENSE;
            if (strLicense.CompareTo("제휴") == 0) strLicense = "Y";
            else if (strLicense.CompareTo("미제휴") == 0) strLicense = "N";
            else if (strLicense.CompareTo("UnKnown") == 0) strLicense = "U";
            else if (strLicense.CompareTo("Unknown") == 0) strLicense = "U";

            clsDBExecute runDB = new clsDBExecute();
            if (bIsMD)
                runDB.setParameter("in_crawl_id", info.CRAWL_ID_MD);
            else
                runDB.setParameter("in_crawl_id", info.CRAWL_ID);
            runDB.setParameter("in_crawl_date", info.REG_DATE);

            //20170523 가격정보가 없을 경우 빈값으로 보냄 ( DB에서 처리하기로함 )
            if(nMoney==0)            
                runDB.setParameter("in_crawl_price", "");            
            else
                runDB.setParameter("in_crawl_price", nMoney);

            runDB.setParameter("in_crawl_pr_unit", strPRUnit);
            runDB.setParameter("in_post_license", strLicense);
            runDB.setParameter("in_crawl_file_path", info.FILE_PATH);
            runDB.setParameter("in_crawl_status", "00");
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("in_file_size", nFileSize);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("pkg_evidence.insert_notice_detail_info2", ref listResult);
        }

        /*public static void InsertDelHist(string strSiteID, string strCrawlID, string strPostID, string strIP, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("in_post_id", strPostID);
            runDB.setParameter("in_ip", strIP);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_AUTO_PROC.INSERT_DEL_HIST", ref listResult);
        }*/

        /*public static void InsertOSPFileName(string strCrawlID, List<string> listFile, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("in_file_name", listFile.ToArray());
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_FILENAME", ref listResult);
        }*/

        public static DataTable GetHomeURL(string strSiteID, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Text);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Text);
            runDB.setParameter("fetch_data", NpgsqlTypes.NpgsqlDbType.Refcursor);

            return runDB.DBExecute("pkg_evidence.get_home_url", ref listResult);
        }

        public static void InsertSiteHist(string strSiteID, string strTopGenre, string strStartDate, string strEndDate, string strCrawlPID, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("in_top_genre", strTopGenre);
            runDB.setParameter("in_start_date", strStartDate);
            runDB.setParameter("in_end_date", strEndDate);
            runDB.setParameter("in_crawl_result", "00");
            runDB.setParameter("in_crawl_pid", strCrawlPID);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_SITE_HIST", ref listResult);
        }

        //20170929 C드라이브 잔여용량과 FTP전송누락되고있는 파일 개수를 포함해서 프로시져 호출하도록 수정
        //C드라이브 잔여용량 1기가 이하이거나 , 파일이 50개이상 쌓이면 평일 9시 11시 14시 17시에 문자발송하도록 되어있음.
        public static void InsertSiteHist2(string strSiteID, string strTopGenre, string strStartDate, string strEndDate, string strCrawlPID,string strDriveSize,string strFileCount,ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("in_top_genre", strTopGenre);
            runDB.setParameter("in_start_date", strStartDate);
            runDB.setParameter("in_end_date", strEndDate);
            runDB.setParameter("in_crawl_result", "00");
            runDB.setParameter("in_crawl_pid", strCrawlPID);
            runDB.setParameter("in_free_space", strDriveSize);
            runDB.setParameter("in_file_cnt", strFileCount);

            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_SITE_HIST", ref listResult);
        }

        public static DataTable GetFilteringList(string strSiteID, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);
            runDB.setParameter("fetch_data", NpgsqlTypes.NpgsqlDbType.Refcursor);

            return runDB.DBExecute("PKG_EVIDENCE.GET_FILTERING_LIST", ref listResult);
        }

        public static void InsertFilteringMapping2(string strCrawlID, string strUploadKey, int nRate, string strRateLog, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("in_upload_key", strUploadKey);
            runDB.setParameter("in_status", 0);
            runDB.setParameter("in_rate", nRate);
            runDB.setParameter("in_rate_log", strRateLog);
            runDB.setParameter("out_judge", NpgsqlTypes.NpgsqlDbType.Integer);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_FILTERING_MAPPING2", ref listResult);
        }

        /*public static void InsertContentMapping(string strCrawlID, string strTitle,
                        string strTitlePiece1, string strTitlePiece2, string strTitlePiece3, string strTitlePiece4,
                        string strGenre, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("in_title", strTitle);
            runDB.setParameter("in_title_piece1", strTitlePiece1);
            runDB.setParameter("in_title_piece2", strTitlePiece2);
            runDB.setParameter("in_title_piece3", strTitlePiece3);
            runDB.setParameter("in_title_piece4", strTitlePiece4);
            runDB.setParameter("in_genre", strGenre);           
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_CONTENT_MAPPING", ref listResult);
        }*/

        public static void InsertContentMapping2(string strCrawlID, string strTitle,
                        string strTitlePiece1, string strTitlePiece2, string strTitlePiece3, string strTitlePiece4,
                        string strGenre, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("in_title", strTitle);
            runDB.setParameter("in_title_piece1", strTitlePiece1);
            runDB.setParameter("in_title_piece2", strTitlePiece2);
            runDB.setParameter("in_title_piece3", strTitlePiece3);
            runDB.setParameter("in_title_piece4", strTitlePiece4);
            runDB.setParameter("in_genre", strGenre);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_CONTENT_MAPPING2", ref listResult);
        }

        //in_recollection_time 분단위
        public static DataTable GetReCollectionList(string strSiteID, int nReCollectionTime, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_site_id", strSiteID);
            runDB.setParameter("in_recollection_time", nReCollectionTime);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);
            runDB.setParameter("fetch_data", NpgsqlTypes.NpgsqlDbType.Refcursor);

            return runDB.DBExecute("PKG_EVIDENCE.GET_RECOLLECTION_LIST", ref listResult);
        }

        public static void InsertReCollectionHist(BOARD_INFO info,
                        string strCWLUser, string strCWLDesc, ref List<string> listResult,bool bIsMD)
        {
            string strLicense = info.LICENSE;
            if (strLicense.CompareTo("제휴") == 0) strLicense = "Y";
            else if (strLicense.CompareTo("미제휴") == 0) strLicense = "N";
            else if (strLicense.CompareTo("UnKnown") == 0) strLicense = "U";
            else if (strLicense.CompareTo("Unknown") == 0) strLicense = "U";

            clsDBExecute runDB = new clsDBExecute();
            if(bIsMD)
                runDB.setParameter("in_crawl_id", info.CRAWL_ID_MD);
            else
                runDB.setParameter("in_crawl_id", info.CRAWL_ID);
            runDB.setParameter("in_price", clsUtil.IntParse(info.MONEY));
            runDB.setParameter("in_license", strLicense);
            runDB.setParameter("in_scfile", info.FILE_PATH);
            runDB.setParameter("in_cwl_user", strCWLUser);
            runDB.setParameter("in_cwl_desc", strCWLDesc);
            // DB 요청으로 REG_DATE 추가.
            runDB.setParameter("in_cwl_date", info.REG_DATE);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            // DB 요청으로 REG_DATE 추가. package INSERT_RECOLLECTION_HIST -> INSERT_RECOLLECTION_HIST2로 변경 됨.
            runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_RECOLLECTION_HIST3", ref listResult);
            //runDB.DBExecuteNonQuery("PKG_EVIDENCE.INSERT_RECOLLECTION_HIST", ref listResult);
        }

        //nJobType (1:업로드맵핑, 2:콘텐츠맵핑)
        public static void InsertMonMaster(int nJobType, string strCrawlID,
                                           string strUploadKey, string strContentKey,
                                           string strPHSAnal, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_job_type", nJobType);
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("in_upload_key", strUploadKey);
            runDB.setParameter("in_content_key", strContentKey);
            runDB.setParameter("in_phs_anal", strPHSAnal);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_AUTO_PROC.INSERT_MON_MASTER", ref listResult);
        }

        public static void UploadKey2ContID(string strUploadKey, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_upload_key", strUploadKey);
            runDB.setParameter("out_cont_id", NpgsqlTypes.NpgsqlDbType.Varchar, 24);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.UPLOADKEY2CONTID", ref listResult);
        }

        public static void UpdateCrawlRate(string strCrawlID,
                                           string strMaxNorUploadKey, int nMaxNorRate,
                                           string strMaxNoJudgUploadKey, int nMaxNoJudgRate,
                                           string strMaxNoPartUploadKey, int nMaxNoPartRate,
                                           string strMaxCutUploadKey, int nMaxCutRate,
                                           string strMMPP, ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
            runDB.setParameter("in_crawl_id", strCrawlID);
            runDB.setParameter("in_max_nor_cont_id", strMaxNorUploadKey);
            runDB.setParameter("in_max_nor_rate", nMaxNorRate);
            runDB.setParameter("in_max_nojudg_cont_id", strMaxNoJudgUploadKey);
            runDB.setParameter("in_max_nojudg_rate", nMaxNoJudgRate);
            runDB.setParameter("in_max_nopart_cont_id", strMaxNoPartUploadKey);
            runDB.setParameter("in_max_nopart_rate", nMaxNoPartRate);
            runDB.setParameter("in_max_cut_cont_id", strMaxCutUploadKey);
            runDB.setParameter("in_max_cut_rate", nMaxCutRate);
            runDB.setParameter("in_mapp", strMMPP);
            runDB.setParameter("out_result_code", NpgsqlTypes.NpgsqlDbType.Varchar, 12);
            runDB.setParameter("out_result_msg", NpgsqlTypes.NpgsqlDbType.Varchar, 1024);

            runDB.DBExecuteNonQuery("PKG_EVIDENCE.UPDATE_CRAWL_RATE", ref listResult);
        }

        public static void QueryForAlive(ref List<string> listResult)
        {
            clsDBExecute runDB = new clsDBExecute();
           
            string strQuery = String.Format(@"select 1 from dual");

            runDB.DBExecute4(strQuery, ref listResult);
        }

    }
}
