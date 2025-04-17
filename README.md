# laon_monitoring
laon contents monitoring solution

## 시스템 요구사항

### 기본 요구사항
- Node.js v18.0.0 이상 (v20.17.0 및 v22.12.0 테스트 완료)
- npm v7.0.0 이상

### Canvas 패키지 의존성 (선택사항)
Canvas 패키지는 스크린샷 기능의 일부를 향상시키는 데 사용되지만, 필수는 아닙니다. Canvas가 설치되지 않은 경우 Puppeteer 기반 대체 기능이 자동으로 활성화됩니다.

#### macOS에서 Canvas 설치하기
macOS에서 Canvas를 설치하려면 다음 명령어로 필요한 시스템 의존성을 먼저 설치하세요:
```bash
brew install pkg-config cairo pango libpng jpeg giflib librsvg
```

Canvas 없이 앱을 실행하려면 .env 파일에서 다음 설정을 추가하세요:
```
USE_FALLBACK_SCREENSHOT=true
```

## 네트워크 문제 해결

### Socket Hang Up 오류 해결
네트워크 연결 문제로 인한 "socket hang up" 오류가 발생할 경우 다음과 같이 설정할 수 있습니다:

1. 브라우저 타임아웃 증가:
```
BROWSER_TIMEOUT=60000
```

2. 브라우저 초기화 재시도 설정:
```
BROWSER_RETRY_COUNT=3  # 재시도 횟수
BROWSER_RETRY_DELAY=2000  # 재시도 지연 시간(ms)
```

3. macOS에서 Chrome 경로 지정:
```
CHROME_PATH=/Applications/Google Chrome.app/Contents/MacOS/Google Chrome
```

4. 브라우저 디버깅 로그 활성화:
```
BROWSER_DEBUG=true
```

5. FTP 비활성화:
```
DISABLE_FTP=true
```

### 재시도 메커니즘
네트워크 요청에 대한 자동 재시도 메커니즘이 구현되어 있어 일시적인 네트워크 문제를 자동으로 복구합니다. 다음과 같은 기능이 포함되어 있습니다:

- 지수 백오프 방식의 재시도 로직
- 네트워크 요청 실패 시 자동 재시도 (최대 3회)
- 일시적인 연결 오류 자동 복구
- 상세한 오류 로깅

이 기능은 불안정한 네트워크 환경에서도 모니터링 작업의 안정성을 높여줍니다.
