import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.seqi.app',
  appName: 'SEQI Evaluation',
  webDir: 'dist',
  server: {
    url: 'https://seqi.vercel.app',
    androidScheme: 'https',
    cleartext: true
  }
};

export default config;
