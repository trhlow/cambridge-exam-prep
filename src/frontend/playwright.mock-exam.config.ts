import { defineConfig, devices } from '@playwright/test'

/**
 * VE-42: the mock exam of setup/mock-exam/KICH_BAN_THI_THU.md, played by browsers (six candidates, an invigilator,
 * an examiner and two administrators, each with their own context) against the isolated Docker stack on port 8090.
 * It waits for the real Listening time to run out, so one run takes about 50 minutes:
 *   npm run mock-exam
 */
export default defineConfig({
  testDir: './mock-exam',
  workers: 1,
  retries: 0,
  timeout: 90 * 60_000,
  expect: { timeout: 15_000 },
  reporter: [['list']],
  outputDir: './test-results/mock-exam',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8090',
    locale: 'en-GB',
    timezoneId: 'Asia/Ho_Chi_Minh',
    acceptDownloads: true,
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], viewport: { width: 1366, height: 768 } } }],
})
