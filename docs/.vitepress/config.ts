import { defineConfig } from 'vitepress'

export default defineConfig({
  lang: 'ja',
  title: 'らんちゃ',
  description: 'コマンドラインランチャー＆ボタン型ランチャー',
  base: '/lc/',

  themeConfig: {
    nav: [
      { text: 'ホーム', link: '/' },
      { text: 'はじめに', link: '/guide/getting-started' },
    ],

    sidebar: [
      {
        text: 'ユーザーガイド',
        items: [
          { text: 'はじめに', link: '/guide/getting-started' },
          { text: 'コマンド型ランチャー', link: '/guide/command-launcher' },
          { text: 'ボタン型ランチャー', link: '/guide/button-launcher' },
          { text: 'スケジューラー', link: '/guide/scheduler' },
        ],
      },
      {
        text: '開発者向け',
        items: [
          { text: 'アーキテクチャ', link: '/development/architecture' },
          { text: '開発ガイド', link: '/development/development' },
        ],
      },
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/ak110/lc' },
    ],

    search: {
      provider: 'local',
    },

    docFooter: {
      prev: '前のページ',
      next: '次のページ',
    },
    darkModeSwitchLabel: '外観',
    returnToTopLabel: 'トップに戻る',
    outline: {
      label: '目次',
    },
  },
})
