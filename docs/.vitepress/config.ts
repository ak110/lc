import { defineConfig } from "vitepress";

export default defineConfig({
  lang: "ja",
  title: "らんちゃ",
  description: "コマンドラインランチャー＆ボタン型ランチャー",
  base: "/lc/",

  themeConfig: {
    nav: [
      { text: "ホーム", link: "/" },
      {
        text: "機能",
        items: [
          { text: "コマンド型ランチャー", link: "/command-launcher" },
          { text: "ボタン型ランチャー", link: "/button-launcher" },
          { text: "スケジューラー", link: "/scheduler" },
        ],
      },
    ],
    sidebar: [
      {
        text: "ユーザーガイド",
        items: [
          { text: "ホーム", link: "/" },
          { text: "コマンド型ランチャー", link: "/command-launcher" },
          { text: "ボタン型ランチャー", link: "/button-launcher" },
          { text: "スケジューラー", link: "/scheduler" },
        ],
      },
      {
        text: "開発者向け",
        items: [
          { text: "アーキテクチャ", link: "/architecture" },
          { text: "開発ガイド", link: "/development" },
        ],
      },
    ],
    socialLinks: [{ icon: "github", link: "https://github.com/ak110/lc" }],
    docFooter: { prev: "前のページ", next: "次のページ" },
    darkModeSwitchLabel: "外観",
    returnToTopLabel: "トップに戻る",
    outline: { label: "目次" },
  },
});
