#!/usr/bin/env python3
"""PostToolUse hook: Edit/Write 直後の C# ファイルを dotnet format で自動整形する。

整形により内容が変化した場合、stdout に JSON で additionalContext を返し、
Claude に「ファイルが書き換わったので Read し直すべき」と通知する。

Claude Code 公式仕様で hook の cwd は保証されないため、リポジトリルートは
環境変数 CLAUDE_PROJECT_DIR を最優先で参照し、未設定時のみスクリプト位置から
解決する。
"""

from __future__ import annotations

import hashlib
import json
import os
import subprocess
import sys
from pathlib import Path

# bin/obj/publish/node_modules 配下は除外 (誤フォーマット防止)
EXCLUDE_DIR_PARTS = {"bin", "obj", "publish", "node_modules"}


def emit_additional_context(message: str) -> None:
    """Claude へ追加コンテキストを返して exit 0 する。"""
    # Windows Python のデフォルト stdout エンコーディングは cp932 のため、
    # additionalContext の日本語が文字化けしないよう UTF-8 を明示する。
    sys.stdout.reconfigure(encoding="utf-8")
    payload = {
        "hookSpecificOutput": {
            "hookEventName": "PostToolUse",
            "additionalContext": message,
        }
    }
    json.dump(payload, sys.stdout, ensure_ascii=False)
    sys.stdout.write("\n")
    sys.exit(0)


def resolve_repo_root() -> Path:
    env_root = os.environ.get("CLAUDE_PROJECT_DIR")
    if env_root:
        return Path(env_root).resolve()
    # フォールバック: スクリプト位置 (.claude/hooks/) からリポジトリルート
    return Path(__file__).resolve().parents[2]


def file_sha256(path: Path) -> str | None:
    try:
        return hashlib.sha256(path.read_bytes()).hexdigest()
    except OSError:
        return None


def main() -> None:
    try:
        payload = json.load(sys.stdin)
    except json.JSONDecodeError:
        sys.exit(0)

    file_path_str = (payload.get("tool_input") or {}).get("file_path") or ""
    if not file_path_str.lower().endswith(".cs"):
        sys.exit(0)

    repo_root = resolve_repo_root()
    try:
        abs_path = Path(file_path_str).resolve()
        rel_path = abs_path.relative_to(repo_root)
    except (OSError, ValueError):
        # リポジトリ外のファイル
        sys.exit(0)

    if EXCLUDE_DIR_PARTS.intersection(rel_path.parts):
        sys.exit(0)

    rel_str = rel_path.as_posix()
    before_hash = file_sha256(abs_path)

    try:
        result = subprocess.run(
            [
                "mise",
                "exec",
                "--",
                "dotnet",
                "format",
                "Launcher.sln",
                "--include",
                rel_str,
                "--verbosity",
                "quiet",
            ],
            cwd=repo_root,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
    except FileNotFoundError as exc:
        emit_additional_context(
            f"dotnet-format-changed hook: mise が起動できませんでした: {exc}"
        )
        return  # for type checkers

    if result.returncode != 0:
        emit_additional_context(
            "dotnet format が失敗しました ("
            f"file={rel_str}, exit={result.returncode}): "
            f"{(result.stderr or result.stdout).strip()[:500]}"
        )
        return

    after_hash = file_sha256(abs_path)
    if before_hash is not None and after_hash is not None and before_hash != after_hash:
        emit_additional_context(
            f"dotnet format が {rel_str} を自動整形しました。"
            "直後の編集前に必ず Read で最新内容を取得してください。"
        )

    # 変化なしの場合は何も出力しない
    sys.exit(0)


if __name__ == "__main__":
    main()
