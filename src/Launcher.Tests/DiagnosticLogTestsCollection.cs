using Xunit;

namespace Launcher.Tests;

/// <summary>
/// DiagnosticLogの静的可変状態を扱うテスト群を直列実行するためのコレクション定義。
/// </summary>
[CollectionDefinition("DiagnosticLog", DisableParallelization = true)]
public class DiagnosticLogTestsCollection
{
}
