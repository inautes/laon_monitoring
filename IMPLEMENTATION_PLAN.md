# Laon Monitoring System Implementation Plan

## Overview
This document outlines the implementation plan for the Laon Monitoring System, a web content monitoring tool designed to monitor content on fileis.com. The system will log in to the website, navigate through content categories, collect content information, capture screenshots, and identify content containing specific keywords.

## Architecture
The system will follow the MVC (Model-View-Controller) architecture using Node.js with ES modules:

- **Models**: Database schema and data access layer
- **Views**: Simple UI for monitoring status and results
- **Controllers**: Request handling and business logic coordination
- **Services**: Core functionality implementation
- **Utils**: Helper functions and utilities
- **Config**: Configuration settings

## Database Design (SQLite)
The system will use SQLite for local data storage with the following schema:

### Tables

#### 1. OSP (Online Service Provider)
```sql
CREATE TABLE osp (
  site_id TEXT PRIMARY KEY,
  site_name TEXT NOT NULL,
  site_type TEXT NOT NULL,
  site_equ INTEGER NOT NULL,
  login_id TEXT NOT NULL,
  login_pw TEXT NOT NULL
);
```

#### 2. Content
```sql
CREATE TABLE content (
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
```

#### 3. ContentDetail
```sql
CREATE TABLE content_detail (
  crawl_id TEXT PRIMARY KEY,
  collection_time DATETIME NOT NULL,
  price TEXT,
  price_unit TEXT,
  partnership_status TEXT,
  capture_filename TEXT,
  status TEXT,
  FOREIGN KEY (crawl_id) REFERENCES content(crawl_id)
);
```

#### 4. FileList
```sql
CREATE TABLE file_list (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  crawl_id TEXT NOT NULL,
  filename TEXT NOT NULL,
  file_size TEXT,
  FOREIGN KEY (crawl_id) REFERENCES content(crawl_id)
);
```

## Component Implementation

### 1. Web Browser Component
Using Puppeteer for browser automation:

```javascript
// Browser Service
class BrowserService {
  constructor(config) {
    this.config = config;
  }

  async initialize() {
    // Initialize browser with stealth plugin
  }

  async login(url, credentials) {
    // Navigate to login page and authenticate
  }

  async navigateToCategory(category) {
    // Navigate to specific content category
  }

  async getContentList(page, category) {
    // Extract content list from current page
  }

  async getContentDetail(url) {
    // Navigate to detail page and extract information
  }

  async captureScreenshot(selector) {
    // Capture screenshot of specific element or full page
  }

  async close() {
    // Close browser instance
  }
}
```

### 2. Web Crawling Module
Responsible for orchestrating the crawling process:

```javascript
// Crawler Service
class CrawlerService {
  constructor(browserService, databaseService) {
    this.browserService = browserService;
    this.databaseService = databaseService;
    this.categories = {
      MOVIE: 'CG001',
      DRAMA: 'CG002',
      VIDEO: 'CG003',
      ANIME: 'CG005'
    };
  }

  async initialize() {
    // Initialize services
  }

  async crawlCategory(category, pages = 1) {
    // Crawl specific category for given number of pages
  }

  async processContentList(contentList) {
    // Process each content item in the list
  }

  async processContentDetail(content) {
    // Process content detail page
  }

  async searchKeyword(content, keyword) {
    // Search for specific keyword in content
  }
}
```

### 3. Screen Capture and Image Processing
Handles screenshot capture and image composition:

```javascript
// Screenshot Service
class ScreenshotService {
  constructor(browserService) {
    this.browserService = browserService;
  }

  async captureListingPage(category) {
    // Capture content listing page
  }

  async captureDetailPage(url) {
    // Capture content detail page
  }

  async captureUTCK3() {
    // Capture or generate UTCK3 timestamp image
  }

  async composeEvidenceImage(listingImage, detailImage, utck3Image) {
    // Compose final evidence image
  }

  async maskSensitiveInfo(image) {
    // Mask login information with white rectangle
  }
}
```

### 4. Data Storage Module
Handles data persistence and MD5 hash generation:

```javascript
// Database Service
class DatabaseService {
  constructor(dbPath) {
    this.db = null;
    this.dbPath = dbPath;
  }

  async initialize() {
    // Initialize database connection
  }

  generateCrawlId(siteId, contentId, regDate) {
    // Generate MD5 hash from site_id + content_id + reg_date
  }

  async saveOSPInfo(ospInfo) {
    // Save OSP information
  }

  async saveContentInfo(contentInfo) {
    // Save content information
  }

  async saveContentDetailInfo(detailInfo) {
    // Save content detail information
  }

  async saveFileList(crawlId, fileList) {
    // Save file list
  }

  async getContentByCrawlId(crawlId) {
    // Retrieve content by crawl ID
  }

  async close() {
    // Close database connection
  }
}
```

### 5. FTP Upload Module
Handles image upload to FTP server:

```javascript
// FTP Service
class FTPService {
  constructor(config) {
    this.config = config;
  }

  async connect() {
    // Connect to FTP server
  }

  async uploadFile(localPath, remotePath) {
    // Upload file to FTP server
  }

  async disconnect() {
    // Disconnect from FTP server
  }

  generateRemotePath(filename) {
    // Generate remote path based on date and server info
  }
}
```

### 6. Main Application
Orchestrates the entire monitoring process:

```javascript
// Main Application
class MonitoringApp {
  constructor(config) {
    this.config = config;
    this.browserService = new BrowserService(config.browser);
    this.databaseService = new DatabaseService(config.database.path);
    this.crawlerService = new CrawlerService(this.browserService, this.databaseService);
    this.screenshotService = new ScreenshotService(this.browserService);
    this.ftpService = new FTPService(config.ftp);
  }

  async initialize() {
    // Initialize all services
  }

  async run() {
    // Run monitoring process
    await this.browserService.initialize();
    await this.databaseService.initialize();
    
    try {
      await this.browserService.login(this.config.site.loginUrl, {
        username: this.config.site.username,
        password: this.config.site.password
      });
      
      for (const [categoryName, categoryCode] of Object.entries(this.crawlerService.categories)) {
        await this.crawlerService.crawlCategory(categoryCode);
      }
    } finally {
      await this.browserService.close();
      await this.databaseService.close();
    }
  }
}
```

## Implementation Steps

1. **Setup Project Structure**
   - Initialize Node.js project
   - Install required dependencies
   - Create directory structure

2. **Database Implementation**
   - Create SQLite database schema
   - Implement database service

3. **Browser Automation**
   - Implement browser service
   - Implement login functionality
   - Implement navigation functionality

4. **Content Crawling**
   - Implement crawler service
   - Implement content listing extraction
   - Implement content detail extraction

5. **Screenshot Capture**
   - Implement screenshot service
   - Implement image composition
   - Implement sensitive info masking

6. **FTP Upload**
   - Implement FTP service
   - Implement file upload functionality

7. **Main Application**
   - Implement main application class
   - Integrate all services
   - Implement configuration loading

8. **Testing**
   - Test login functionality
   - Test content extraction
   - Test screenshot capture
   - Test FTP upload
   - Test end-to-end monitoring process

## Configuration
The system will use a configuration file to store settings:

```javascript
// Configuration
export default {
  site: {
    id: 'fileis',
    name: 'FileIs',
    type: 'SITE0010',
    equ: 15, // 1+2+4+8 (ID, Uploader, Title, Size)
    loginUrl: 'https://fileis.com/',
    username: 'ehlwlqk14s',
    password: 'edcrfv#$as'
  },
  browser: {
    headless: true,
    timeout: 30000
  },
  database: {
    path: './data/monitoring.db'
  },
  ftp: {
    host: 'ftp.example.com',
    port: 21,
    user: 'username',
    password: 'password',
    basePath: '/images'
  },
  keyword: '폭싹속았수다'
};
```

## Conclusion
This implementation plan outlines the architecture, components, and steps required to build the Laon Monitoring System. The system will use Node.js with MVC architecture, SQLite for data storage, Puppeteer for web automation, and FTP for image upload.
