# 開発ガイド

## 開発環境の構築手順

### 必要環境

- Windows 10/11
- mise
- Visual Studio Code

### 初回セットアップ

```cmd
mise install && mise run setup
```

## 開発コマンド

| コマンド          | 説明                                                        |
| ----------------- | ----------------------------------------------------------- |
| `mise run setup`  | 開発環境のセットアップ                                      |
| `mise run format` | フォーマット + 軽量lint（開発時の手動実行用。自動修正あり） |
| `mise run test`   | 全チェック実行（これを通過すればコミット可能）              |
| `mise run build`  | リリースビルド                                              |
| `mise run clean`  | ビルド成果物の削除                                          |
| `mise run update` | 依存パッケージの更新                                        |
| `mise run docs`   | ドキュメントのローカルプレビュー                            |

Linux環境ではドキュメントのlintのみ実行できる（`mise exec -- uvx --exclude-newer-package pyfltr=false pyfltr run docs/ README.md AGENTS.md`）。
全チェック（`mise run test`）はWindowsのみで実行する。

## サプライチェーン攻撃対策

ロック尊重・公開待機・ピン留め運用・脆弱性検知の4点を基本方針とする。

- ロック尊重: 文書・開発用のNode依存は`pnpm-lock.yaml`に従って復元する
- 公開待機: Node依存の新しい版は`pnpm-workspace.yaml`の公開待機設定に従って採用する
- ピン留め運用: GitHub Actionsを`pinact`でコミットのハッシュへ固定し、`mise run update`で更新する
- 脆弱性検知: Dependabot alertsと定期監査を併用する

lcは実行可能なアプリとして配布され、依存の問題がエンドユーザーの実行環境へ波及するため、脆弱性を検知する仕組みを設ける。
Dependabot alertsはGitHubの依存関係情報に基づく通知を担う。
依存を更新しない期間にもアドバイザリが追加・変更されるため、`Audit`ワークフローを定期実行する。
手動実行にも対応し、結果は[ActionsのAudit](https://github.com/ak110/lc/actions/workflows/audit.yaml)とSecurityのDependabot alertsで確認する。

監査は`Launcher.sln`を復元し、NuGetパッケージの直接・推移依存を対象にする。
アプリ用だけでなくAnalyzerとテスト用の依存も含む。
文書・開発用のNode依存は`pnpm audit`で確認する。
NuGet Auditはパッケージ依存の監査であり、配布exeへ同梱する.NETランタイム全体の脆弱性を網羅しない。

NuGetの`NU1901`〜`NU1904`は脆弱性の検出、`NU1900`と`NU1905`は監査情報の取得に関する問題を示す。
いずれも監査を失敗させるので、Actionsの診断コードとメッセージから原因を確認する。
pnpmも脆弱性の一覧とレジストリへの接続エラーを区別して確認し、監査の失敗を「脆弱性なし」と扱わない。
仕様は[NuGet Audit](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)と[pnpm audit](https://pnpm.io/cli/audit)を参照。

検出後は影響と修正版を確認し、依存を更新して検証する。
Dependabot security updatesによる自動修正PRの作成は無効にし、検出への対応をその機能へ委ねない。
`.github/dependabot.yaml`の週次のversion updatesは、NuGet・GitHub Actions・npmの新しい版を提案する別の機能として維持する。

## ドキュメントサイト運用

ドキュメントはGitHub Pagesでホストしている。

- ローカルプレビュー: `mise run docs`
- 自動デプロイ: masterブランチへのpush時に`Docs`ワークフローが自動実行される（`docs/`配下または`package.json`の変更時のみ）

## Analyzerルールの導入

新しいAnalyzerルールを導入する際は、まず`.editorconfig`で`none`に抑制し、
修正完了後に`warning`へ昇格する。
`TreatWarningsAsErrors=true`環境では`suggestion`もビルドに表れないため、
`dotnet format --diagnostics`で対象箇所を列挙する。

## 環境制限

- `dotnet-format`・`dotnet-build`・`dotnet-test`はWindowsターゲットのためLinuxでは実行不可
- WinForms Designer.csのマルチバイト文字を含むテーブル等ではmarkdownlint MD060が発生する場合がある

## リリース手順

`releaser`でリリースする。これは作者の[dotfiles](https://github.com/ak110/dotfiles)が提供するコマンドであり、
lcの`mise install`と`mise run setup`には含まれない。
GitとGitHub CLI（`gh`）をPATHへ追加し、`gh auth login`で公開先へ書き込めるアカウントに認証する。
GitのpushとActionsのworkflow起動ができる権限を用意する。
GitHub CLIは[公式の導入案内](https://cli.github.com/)から導入する。

未導入の環境では、lcと同じ親フォルダーへdotfilesを取得する。
以下はlcのルートで実行するWindowsのコマンド例である。
`uv run --project`はdotfilesの依存とコマンドを解決し、リリース対象の作業フォルダーはlcのまま保つ。

```cmd
git clone https://github.com/ak110/dotfiles.git ..\dotfiles
uv run --project "..\dotfiles" --frozen --no-dev releaser --help
```

dotfilesが既に別の場所にある場合は`..\dotfiles`をそのパスに置き換え、空白を含む場合も引用符で囲む。
Pythonの必要バージョンと依存はdotfilesの`pyproject.toml`と`uv.lock`に従う。
`--frozen`を付け、導入のためにロックファイルを更新しない。

既定ブランチで未コミット変更がないことを確認し、更新種別を1つ選ぶ。
未pushのcommitがあれば`releaser`がpushし、CI完了を待ってからリリースする。
majorはユーザーが明示的に指定した場合だけ実行する。

```cmd
uv run --project "..\dotfiles" --frozen --no-dev releaser patch
uv run --project "..\dotfiles" --frozen --no-dev releaser minor
uv run --project "..\dotfiles" --frozen --no-dev releaser major
```

dotfilesのコマンドが既にPATH上にあれば、同じ処理を`releaser patch`などで実行できる。
`releaser`はCI完了を待ち、リリースworkflowを起動・監視してローカルを同期する。
結果の確認: <https://github.com/ak110/lc/actions>
