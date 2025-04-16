import sqlite3 from 'better-sqlite3';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import md5 from 'md5';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

class Database {
  constructor(dbPath) {
    this.dbPath = dbPath || path.join(__dirname, '../../data/monitoring.db');
    this.ensureDatabaseDirectory();
    this.db = null;
  }

  ensureDatabaseDirectory() {
    const dbDir = path.dirname(this.dbPath);
    if (!fs.existsSync(dbDir)) {
      fs.mkdirSync(dbDir, { recursive: true });
    }
  }

  initialize() {
    this.db = sqlite3(this.dbPath);
    this.createTables();
    return this;
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
  }

  generateCrawlId(siteId, contentId, regDate) {
    const data = `${siteId}${contentId}${regDate}`;
    return md5(data);
  }

  saveOSPInfo(ospInfo) {
    const stmt = this.db.prepare(`
      INSERT OR REPLACE INTO osp (site_id, site_name, site_type, site_equ, login_id, login_pw)
      VALUES (?, ?, ?, ?, ?, ?)
    `);
    
    const result = stmt.run(
      ospInfo.siteId,
      ospInfo.siteName,
      ospInfo.siteType,
      ospInfo.siteEqu,
      ospInfo.loginId,
      ospInfo.loginPw
    );
    
    return result;
  }

  saveContentInfo(contentInfo) {
    const stmt = this.db.prepare(`
      INSERT OR REPLACE INTO content (
        crawl_id, site_id, content_id, title, genre, file_count, 
        file_size, uploader_id, collection_time, detail_url
      )
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `);
    
    const result = stmt.run(
      contentInfo.crawlId,
      contentInfo.siteId,
      contentInfo.contentId,
      contentInfo.title,
      contentInfo.genre,
      contentInfo.fileCount,
      contentInfo.fileSize,
      contentInfo.uploaderId,
      contentInfo.collectionTime,
      contentInfo.detailUrl
    );
    
    return result;
  }

  saveContentDetailInfo(detailInfo) {
    const stmt = this.db.prepare(`
      INSERT OR REPLACE INTO content_detail (
        crawl_id, collection_time, price, price_unit, 
        partnership_status, capture_filename, status
      )
      VALUES (?, ?, ?, ?, ?, ?, ?)
    `);
    
    const result = stmt.run(
      detailInfo.crawlId,
      detailInfo.collectionTime,
      detailInfo.price,
      detailInfo.priceUnit,
      detailInfo.partnershipStatus,
      detailInfo.captureFilename,
      detailInfo.status
    );
    
    return result;
  }

  saveFileList(crawlId, fileList) {
    const stmt = this.db.prepare(`
      INSERT INTO file_list (crawl_id, filename, file_size)
      VALUES (?, ?, ?)
    `);
    
    const insertMany = this.db.transaction((items) => {
      for (const item of items) {
        stmt.run(crawlId, item.filename, item.fileSize);
      }
    });
    
    insertMany(fileList);
    return true;
  }

  getContentByCrawlId(crawlId) {
    const stmt = this.db.prepare(`
      SELECT c.*, cd.*
      FROM content c
      LEFT JOIN content_detail cd ON c.crawl_id = cd.crawl_id
      WHERE c.crawl_id = ?
    `);
    
    return stmt.get(crawlId);
  }

  getFileListByCrawlId(crawlId) {
    const stmt = this.db.prepare(`
      SELECT * FROM file_list
      WHERE crawl_id = ?
    `);
    
    return stmt.all(crawlId);
  }

  close() {
    if (this.db) {
      this.db.close();
    }
  }
}

export default Database;
