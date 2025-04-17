# Laon Monitoring 프로젝트 구조

## 프로젝트 개요
Laon Monitoring은 웹하드 사이트(OSP)에 접속하여 컨텐츠 리스트를 카테고리별로 가져오고, 상세 페이지를 열어 정보를 캡처하며, 특정 키워드가 포함된 컨텐츠를 모니터링하는 시스템입니다.

## 파일 구조

### 주요 디렉토리
- `/src`: 소스 코드
  - `/controllers`: 컨트롤러 파일
  - `/models`: 데이터베이스 모델
  - `/services`: 서비스 로직
  - `/views`: 뷰 템플릿
  - `/config`: 설정 파일
  - `/utils`: 유틸리티 함수
- `/data`: 데이터베이스 파일 저장 위치
- `/screenshots`: 스크린샷 저장 위치
- `/test-fallback`: 테스트 스크립트

### 주요 파일

#### 핵심 파일
- `src/app.js`: 애플리케이션 진입점
- `src/services/browser.js`: 브라우저 자동화 서비스
- `src/services/crawler.js`: 웹 크롤링 서비스
- `src/models/database.js`: 데이터베이스 서비스
- `src/services/screenshot.js`: 스크린샷 서비스
- `src/services/ftp.js`: FTP 서비스

#### 설정 파일
- `.env`: 환경 변수 설정
- `.env.example`: 환경 변수 예제
- `package.json`: 프로젝트 의존성 관리

#### 테스트 파일
- `test-fallback/test-login.js`: 로그인 기능 테스트
- `test-fallback/test-chrome-path.js`: Chrome 경로 테스트
- `test-fallback/test-connection.js`: 연결 테스트
- `test-fallback/test-layer-detail.js`: 레이어 상세 페이지 테스트

## 주요 기능 설명

### 브라우저 서비스 (BrowserService)
- `initialize()`: 브라우저 초기화
- `login()`: 사이트 로그인
- `navigateToCategory()`: 카테고리 페이지로 이동
- `getContentList()`: 컨텐츠 목록 가져오기
- `getContentDetail()`: 컨텐츠 상세 정보 가져오기
- `getContentDetailByUrl()`: URL로 컨텐츠 상세 정보 가져오기
- `searchKeyword()`: 키워드 검색
- `captureScreenshot()`: 스크린샷 촬영
- `goToNextPage()`: 다음 페이지로 이동
- `close()`: 브라우저 종료

### 크롤러 서비스 (CrawlerService)
- `initialize()`: 크롤러 초기화
- `crawlCategory()`: 카테고리 크롤링
- `extractContentDetails()`: 컨텐츠 상세 정보 추출
- `searchByKeyword()`: 키워드로 검색
- `checkForKeyword()`: 키워드 포함 여부 확인
- `captureContent()`: 컨텐츠 캡처
- `saveContent()`: 컨텐츠 저장

### 데이터베이스 서비스 (DatabaseService)
- `initialize()`: 데이터베이스 초기화
- `saveOSPInfo()`: OSP 정보 저장
- `saveContent()`: 컨텐츠 정보 저장
- `getContentByKeyword()`: 키워드로 컨텐츠 검색
- `getAllContents()`: 모든 컨텐츠 가져오기
- `close()`: 데이터베이스 연결 종료

### 스크린샷 서비스 (ScreenshotService)
- `initialize()`: 스크린샷 서비스 초기화
- `captureElement()`: 요소 캡처
- `captureFullPage()`: 전체 페이지 캡처
- `captureBySelector()`: 선택자로 요소 캡처

### FTP 서비스 (FTPService)
- `connect()`: FTP 서버 연결
- `uploadFile()`: 파일 업로드
- `disconnect()`: FTP 연결 종료

## 데이터베이스 구조

### 테이블 구조

#### OSP_INFO 테이블
OSP(웹하드 사이트) 기본 정보를 저장합니다.

| 필드명 | 타입 | 설명 |
|--------|------|------|
| id | INTEGER | 기본 키 |
| site_id | TEXT | 사이트 ID |
| site_name | TEXT | 사이트 이름 |
| site_type | TEXT | 사이트 타입 (PC/모바일) |
| site_equ | INTEGER | 사이트 EQU 값 |
| login_id | TEXT | 로그인 ID |
| login_pw | TEXT | 로그인 비밀번호 |
| created_at | TEXT | 생성 시간 |

#### CONTENT 테이블
크롤링한 컨텐츠 정보를 저장합니다.

| 필드명 | 타입 | 설명 |
|--------|------|------|
| id | INTEGER | 기본 키 |
| content_id | TEXT | 컨텐츠 ID |
| title | TEXT | 제목 |
| detail_url | TEXT | 상세 URL |
| file_size | TEXT | 파일 크기 |
| uploader_id | TEXT | 업로더 ID |
| price | TEXT | 가격 |
| price_unit | TEXT | 가격 단위 |
| partnership_status | TEXT | 파트너십 상태 |
| category | TEXT | 카테고리 |
| contains_keyword | INTEGER | 키워드 포함 여부 (0/1) |
| screenshot_path | TEXT | 스크린샷 경로 |
| timestamp | TEXT | 타임스탬프 |

#### FILE_LIST 테이블
컨텐츠에 포함된 파일 목록을 저장합니다.

| 필드명 | 타입 | 설명 |
|--------|------|------|
| id | INTEGER | 기본 키 |
| content_id | INTEGER | 컨텐츠 ID (외래 키) |
| filename | TEXT | 파일 이름 |
| file_size | TEXT | 파일 크기 |

## 환경 변수 설정

### 필수 환경 변수
- `FILEIS_URL`: Fileis.com URL
- `FILEIS_USERNAME`: Fileis.com 로그인 아이디
- `FILEIS_PASSWORD`: Fileis.com 로그인 비밀번호

### 선택적 환경 변수
- `HEADLESS`: 헤드리스 모드 활성화 여부 (true/false)
- `BROWSER_TIMEOUT`: 브라우저 타임아웃 (ms)
- `BROWSER_RETRY_COUNT`: 브라우저 재시도 횟수
- `BROWSER_RETRY_DELAY`: 브라우저 재시도 지연 시간 (ms)
- `CHROME_PATH`: Chrome 실행 파일 경로
- `DB_PATH`: 데이터베이스 파일 경로
- `TARGET_KEYWORD`: 검색 키워드
- `USE_FALLBACK_SCREENSHOT`: 대체 스크린샷 기능 사용 여부
- `DISABLE_FTP`: FTP 비활성화 여부
