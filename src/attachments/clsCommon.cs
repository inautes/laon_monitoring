using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSPAutoSearch_AutoLogin
{
    public class OSP_INFO
    {
        public string OSP_ID;
        public string OSP_TYPE;

        public string SITE_ID;
        public string SITE_NAME;
        public string SITE_TYPE;

        public string SITE_EQU;
        public bool CHECK_BOARD_ID;
        public bool CHECK_UPLOADER_ID;
        public bool CHECK_TITLE;
        public bool CHECK_FILE_SIZE;
        public bool CHECK_GENRE;

        public OSP_INFO()
        {
            OSP_ID = string.Empty;
            OSP_TYPE = string.Empty;

            SITE_ID = string.Empty;
            SITE_NAME = string.Empty;
            SITE_TYPE = string.Empty;

            SITE_EQU = string.Empty;
            CHECK_BOARD_ID = false;
            CHECK_UPLOADER_ID = false;
            CHECK_TITLE = false;
            CHECK_FILE_SIZE = false;
            CHECK_GENRE = false;
        }
    }

    public class POPUP_INFO
    {
        public string TAG;
        public string ATTRIBUTE;
        public string VALUE;
    }


    public class BOARD_INFO
    {
        public string CRAWL_ID;
        public string CRAWL_ID_MD;
        public string SEQNO;
        public string TITLE;
        public string GENRE;
        public string FILE_SIZE;
        public string UPLOADER_ID;
        public string REG_DATE;
        public string DESC_URL;

        public string MONEY;
        public string MONEY_DN;
        public string LICENSE;
        public string FILE_PATH;
        public string FILE_PATH_DN;
        public string RESULT_STATUS;

        public string IS_SRM;

        public List<string> FILE_LIST;

        public BOARD_INFO()
        {
            CRAWL_ID = string.Empty;
            SEQNO = string.Empty;
            TITLE = string.Empty;
            GENRE = string.Empty;          
            FILE_SIZE = string.Empty;
            UPLOADER_ID = string.Empty;
            REG_DATE = string.Empty;
            DESC_URL = string.Empty;

            MONEY = string.Empty;
            MONEY_DN = string.Empty;
            LICENSE = string.Empty;
            FILE_PATH = string.Empty;
            RESULT_STATUS = string.Empty;

            IS_SRM = string.Empty;

            FILE_LIST = new List<string>();
        }
    }

    public class BOARD_DETAIL_INFO
    {
        public string PARTNER;
        public string MONEY;
        public string NAME;
        public string HASH;
        public int FILE_COUNT;
        public string FILE_PATH;

        public List<string> FILE_LIST;

        public BOARD_DETAIL_INFO()
        {
            PARTNER = string.Empty;
            MONEY = string.Empty;
            NAME = string.Empty;
            HASH = string.Empty;
            FILE_COUNT = 0;
            FILE_PATH = string.Empty;

            FILE_LIST = new List<string>();
        }
    }
}
