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
