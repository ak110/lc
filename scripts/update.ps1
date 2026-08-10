param(
    [Parameter(Mandatory = $true)]
    [string]$Dotnet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Command,
        [int[]]$AllowedExitCodes = @(0)
    )

    & $Command
    $exitCode = $LASTEXITCODE
    if ($exitCode -notin $AllowedExitCodes) {
        throw "外部コマンドが終了コード $exitCode で失敗した。"
    }
}

# dotnet package updateは更新対象がない場合に終了コード2を返す。
Invoke-NativeCommand -AllowedExitCodes @(0, 2) -Command { & $Dotnet package update --project Launcher.sln }
Invoke-NativeCommand -Command { & corepack use pnpm@latest }
Invoke-NativeCommand -Command { & corepack enable pnpm }
Invoke-NativeCommand -Command { & corepack pnpm update --latest }

$originalGithubToken = [Environment]::GetEnvironmentVariable('GITHUB_TOKEN', 'Process')
$githubToken = ((Invoke-NativeCommand -Command { & gh auth token }) -join [Environment]::NewLine).Trim()
if ([string]::IsNullOrWhiteSpace($githubToken)) {
    throw 'gh auth tokenが空文字列を返したため、pinactを実行できない。'
}

try {
    [Environment]::SetEnvironmentVariable('GITHUB_TOKEN', $githubToken, 'Process')
    Invoke-NativeCommand -Command { & pinact run --update --min-age=1 }
}
finally {
    [Environment]::SetEnvironmentVariable('GITHUB_TOKEN', $originalGithubToken, 'Process')
}
