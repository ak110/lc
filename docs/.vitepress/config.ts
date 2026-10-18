import { defineConfig } from 'vitepress'

// VitePressの既定の変換（@mdit-vue/shared）にNFCを加え、濁点を含むリンクとIDをそろえる。
const slugify = (text: string) => text
  .normalize('NFKD')
  .replace(/[\u0300-\u036F]/g, '')
  .replace(/[\u0000-\u001f]/g, '')
  .replace(/[\s~`!@#$%^&*()\-_+=[\]{}|\\;:"'“”‘’<>,.?/]+/g, '-')
  .replace(/-{2,}/g, '-')
  .replace(/^-+|-+$/g, '')
  .replace(/^(\d)/, '_$1')
  .toLowerCase()
  .normalize('NFC')

export default defineConfig({
  lang: 'ja',
  title: 'らんちゃ',
  description: 'コマンドラインランチャー＆ボタン型ランチャー',
  base: '/lc/',

  markdown: {
    anchor: { slugify },
    headers: { slugify },
    toc: { slugify },
  },

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
          { text: 'メモパッド', link: '/guide/memopad' },
          { text: '設定・タスクトレイ・通信', link: '/guide/settings' },
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
