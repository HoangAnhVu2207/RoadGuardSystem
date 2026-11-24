import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  testMatch: /(?:contract|server)\.spec\.ts$/,
  timeout: 90 * 60 * 1000,
  workers: 1,
  retries: 0,
  reporter: 'line',
  outputDir: '../results/real-artifacts',
  use: { trace: 'off', screenshot: 'off', video: 'off' },
});
