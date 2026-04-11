# カスタム指示 (プロジェクト固有)

## 開発手順

- dotnet, node, pnpmなどはmise経由で実行する

## リリース

- リリースは GitHub Actions の Release ワークフローで行う (詳細は docs/development/development.md)
- バグフィックス/小規模改善 → 「バグフィックス」、新機能追加 → 「マイナーバージョンアップ」を選択する
- `git commit --amend` はリリースのバージョンバンプコミットと混ざるリスクがあるため、push済みコミットには使わない

## 設計上の注意点

- `*.cfg` (設定) と `*.dat` (ランタイムデータ) の分離を徹底する。頻繁に更新されるデータは `*.dat` へ
- WinForms Designer.cs のマルチバイト文字を含むテーブル等は markdownlint (MD060) に注意
- カタカナ語の長音: 「スケジューラー」「ランチャー」など末尾に長音符を付ける

## 関連ドキュメント

- @README.md
- @docs/development/architecture.md
- @docs/development/development.md
