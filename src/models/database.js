import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import initSqlJs from 'sql.js';
import md5 from 'md5';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

class DatabaseService {
  constructor(dbPath) {
    this.dbPath = dbPath || path.join(__dirname, '../../data/monitoring.db');
    this.db = null;
    this.SQL = null;
    this.ensureDatabaseDirectory();
  }

  ensureDatabaseDirectory() {
    const dbDir = path.dirname(this.dbPath);
    if (!fs.existsSync(dbDir)) {
      fs.mkdirSync(dbDir, { recursive: true });
    }
  }

  async initialize() {
    try {
      const SQL = await initSqlJs();
      this.SQL = SQL;
      
      if (fs.existsSync(this.dbPath)) {
        const data = fs.readFileSync(this.dbPath);
        this.db = new SQL.Database(new Uint8Array(data));
      } else {
        this.db = new SQL.Database();
      }
      
      this.createTables();
      return this;
    } catch (error) {
      console.error('데이터베이스 초기화 오류:', error);
      throw error;
    }
  }

  createTables() {
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS osp (
        site_id TEXT PRIMARY KEY,
        site_name TEXT NOT NULL,
        site_type TEXT NOT NULL,
        site_equ INTEGER NOT NULL,
        login_id TEXT NOT NULL,
        login_pw TEXT NOT NULL
      );
    `);

    this.db.exec(`
      CREATE TABLE IF NOT EXISTS content (
        crawl_id TEXT PRIMARY KEY,
        site_id TEXT NOT NULL,
        content_id TEXT NOT NULL,
        title TEXT NOT NULL,
        genre TEXT NOT NULL,
        file_count INTEGER,
        file_size TEXT,
        uploader_id TEXT,
        collection_time DATETIME NOT NULL,
        detail_url TEXT NOT NULL,
        FOREIGN KEY (site_id) REFERENCES osp(site_id)
      );
    `);

    this.db.exec(`
      CREATE TABLE IF NOT EXISTS content_detail (
        crawl_id TEXT PRIMARY KEY,
        collection_time DATETIME NOT NULL,
        price TEXT,
        price_unit TEXT,
        partnership_status TEXT,
        capture_filename TEXT,
        status TEXT,
        FOREIGN KEY (crawl_id) REFERENCES content(crawl_id)
      );
    `);

    this.db.exec(`
      CREATE TABLE IF NOT EXISTS file_list (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        crawl_id TEXT NOT NULL,
        filename TEXT NOT NULL,
        file_size TEXT,
        FOREIGN KEY (crawl_id) REFERENCES content(crawl_id)
      );
    `);
    
    this.saveToFile();
  }

  saveToFile() {
    try {
      const data = this.db.export();
      const buffer = Buffer.from(data);
      fs.writeFileSync(this.dbPath, buffer);
    } catch (error) {
      console.error('데이터베이스 파일 저장 오류:', error);
    }
  }

  generateCrawlId(siteId, contentId, regDate) {
    const data = `${siteId}${contentId}${regDate}`;
    return md5(data);
  }

  saveOSPInfo(ospInfo) {
    try {
      const safeOspInfo = {
        siteId: ospInfo.siteId || 'default',
        siteName: ospInfo.siteName || 'Default Site',
        siteType: ospInfo.siteType || 'SITE0010',
        siteEqu: ospInfo.siteEqu || 0,
        loginId: ospInfo.loginId || 'anonymous',
        loginPw: ospInfo.loginPw || ''
      };
      
      const stmt = this.db.prepare(`
        INSERT OR REPLACE INTO osp (site_id, site_name, site_type, site_equ, login_id, login_pw)
        VALUES (?, ?, ?, ?, ?, ?)
      `);
      
      stmt.bind([
        safeOspInfo.siteId,
        safeOspInfo.siteName,
        safeOspInfo.siteType,
        safeOspInfo.siteEqu,
        safeOspInfo.loginId,
        safeOspInfo.loginPw
      ]);
      
      stmt.step();
      stmt.free();
      
      this.saveToFile();
      
      return true;
    } catch (error) {
      console.error('OSP 정보 저장 오류:', error);
      return false;
    }
  }

  saveContentInfo(contentInfo) {
    try {
      const safeContentInfo = {
        crawlId: contentInfo.crawlId || md5(Date.now().toString()),
        siteId: contentInfo.siteId || 'default',
        contentId: contentInfo.contentId || 'unknown',
        title: contentInfo.title || 'No Title',
        genre: contentInfo.genre || 'unknown',
        fileCount: contentInfo.fileCount || 0,
        fileSize: contentInfo.fileSize || '0',
        uploaderId: contentInfo.uploaderId || 'anonymous',
        collectionTime: contentInfo.collectionTime || new Date().toISOString(),
        detailUrl: contentInfo.detailUrl || ''
      };
      
      const stmt = this.db.prepare(`
        INSERT OR REPLACE INTO content (
          crawl_id, site_id, content_id, title, genre, file_count, 
          file_size, uploader_id, collection_time, detail_url
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      `);
      
      stmt.bind([
        safeContentInfo.crawlId,
        safeContentInfo.siteId,
        safeContentInfo.contentId,
        safeContentInfo.title,
        safeContentInfo.genre,
        safeContentInfo.fileCount,
        safeContentInfo.fileSize,
        safeContentInfo.uploaderId,
        safeContentInfo.collectionTime,
        safeContentInfo.detailUrl
      ]);
      
      stmt.step();
      stmt.free();
      
      this.saveToFile();
      
      return true;
    } catch (error) {
      console.error('콘텐츠 정보 저장 오류:', error);
      return false;
    }
  }

  saveContentDetailInfo(detailInfo) {
    try {
      const safeDetailInfo = {
        crawlId: detailInfo.crawlId || md5(Date.now().toString()),
        collectionTime: detailInfo.collectionTime || new Date().toISOString(),
        price: detailInfo.price || '0',
        priceUnit: detailInfo.priceUnit || 'KRW',
        partnershipStatus: detailInfo.partnershipStatus || 'unknown',
        captureFilename: detailInfo.captureFilename || '',
        status: detailInfo.status || 'captured'
      };
      
      const stmt = this.db.prepare(`
        INSERT OR REPLACE INTO content_detail (
          crawl_id, collection_time, price, price_unit, 
          partnership_status, capture_filename, status
        )
        VALUES (?, ?, ?, ?, ?, ?, ?)
      `);
      
      stmt.bind([
        safeDetailInfo.crawlId,
        safeDetailInfo.collectionTime,
        safeDetailInfo.price,
        safeDetailInfo.priceUnit,
        safeDetailInfo.partnershipStatus,
        safeDetailInfo.captureFilename,
        safeDetailInfo.status
      ]);
      
      stmt.step();
      stmt.free();
      
      this.saveToFile();
      
      return true;
    } catch (error) {
      console.error('콘텐츠 상세 정보 저장 오류:', error);
      return false;
    }
  }

  saveFileList(crawlId, fileList) {
    try {
      const safeCrawlId = crawlId || md5(Date.now().toString());
      const safeFileList = Array.isArray(fileList) ? fileList : [];
      
      this.db.exec('BEGIN TRANSACTION;');
      
      for (const item of safeFileList) {
        const stmt = this.db.prepare(`
          INSERT INTO file_list (crawl_id, filename, file_size)
          VALUES (?, ?, ?)
        `);
        
        stmt.bind([
          safeCrawlId, 
          item.filename || 'unknown', 
          item.fileSize || '0'
        ]);
        stmt.step();
        stmt.free();
      }
      
      this.db.exec('COMMIT;');
      
      this.saveToFile();
      
      return true;
    } catch (error) {
      this.db.exec('ROLLBACK;');
      console.error('파일 목록 저장 오류:', error);
      return false;
    }
  }

  getContentByCrawlId(crawlId) {
    try {
      const query = `
        SELECT c.*, cd.*
        FROM content c
        LEFT JOIN content_detail cd ON c.crawl_id = cd.crawl_id
        WHERE c.crawl_id = '${crawlId}'
      `;
      
      const result = this.db.exec(query);
      
      if (result.length > 0 && result[0].values.length > 0) {
        const columns = result[0].columns;
        const values = result[0].values[0];
        
        const content = {};
        columns.forEach((col, idx) => {
          content[col] = values[idx];
        });
        
        return content;
      }
      
      return null;
    } catch (error) {
      console.error('콘텐츠 조회 오류:', error);
      return null;
    }
  }

  getFileListByCrawlId(crawlId) {
    try {
      const query = `
        SELECT * FROM file_list
        WHERE crawl_id = '${crawlId}'
      `;
      
      const result = this.db.exec(query);
      
      if (result.length > 0) {
        const columns = result[0].columns;
        const values = result[0].values;
        
        return values.map(row => {
          const item = {};
          columns.forEach((col, idx) => {
            item[col] = row[idx];
          });
          return item;
        });
      }
      
      return [];
    } catch (error) {
      console.error('파일 목록 조회 오류:', error);
      return [];
    }
  }

  getContentByKeyword(keyword) {
    try {
      const query = `
        SELECT c.*, cd.*
        FROM content c
        LEFT JOIN content_detail cd ON c.crawl_id = cd.crawl_id
        WHERE c.title LIKE '%${keyword}%'
        ORDER BY c.collection_time DESC
      `;
      
      const result = this.db.exec(query);
      
      if (result.length > 0) {
        const columns = result[0].columns;
        const values = result[0].values;
        
        return values.map(row => {
          const item = {};
          columns.forEach((col, idx) => {
            item[col] = row[idx];
          });
          return item;
        });
      }
      
      return [];
    } catch (error) {
      console.error('키워드 검색 오류:', error);
      return [];
    }
  }

  getAllContent() {
    try {
      const query = `
        SELECT c.*, cd.*
        FROM content c
        LEFT JOIN content_detail cd ON c.crawl_id = cd.crawl_id
        ORDER BY c.collection_time DESC
      `;
      
      const result = this.db.exec(query);
      
      if (result.length > 0) {
        const columns = result[0].columns;
        const values = result[0].values;
        
        return values.map(row => {
          const item = {};
          columns.forEach((col, idx) => {
            item[col] = row[idx];
          });
          return item;
        });
      }
      
      return [];
    } catch (error) {
      console.error('모든 콘텐츠 조회 오류:', error);
      return [];
    }
  }

  close() {
    if (this.db) {
      this.saveToFile();
      this.db.close();
      this.db = null;
    }
  }
}

export default DatabaseService;
