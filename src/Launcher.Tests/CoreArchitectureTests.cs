using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using Launcher.Core;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

public sealed class CoreArchitectureTests
{
    [Fact]
    public void CoreはアプリとWinFormsのアセンブリを参照しない()
    {
        var core = typeof(Command).Assembly;
        Assert.NotSame(typeof(WindowHelper).Assembly, core);
        core.GetReferencedAssemblies().Select(name => name.Name).Should()
            .NotContain(name => name == "System.Windows.Forms" || name == typeof(WindowHelper).Assembly.GetName().Name);
    }

    [Fact]
    public void 本体のDllImportは全てWin32名前空間で宣言する()
    {
        var imports = new[] { typeof(Command).Assembly, typeof(WindowHelper).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<DllImportAttribute>() is not null)
            .ToList();
        imports.Should().NotBeEmpty();
        foreach (var method in imports)
            method.DeclaringType!.Namespace.Should().Be("Launcher.Win32", method.ToString());
    }

    [Fact]
    public void Commandからボタンへの変換は全公開プロパティをコピーする()
    {
        var command = new Command
        {
            Name = "名前",
            FileName = "command.exe",
            Param = "--arg",
            WorkDir = "work",
            Show = WindowStyle.Maximized,
            Priority = ProcessPriorityLevel.High,
            RunAsAdmin = true,
            IconIndex = 37,
        };
        var button = ButtonEntry.FromCommand(command, 2, 3);
        foreach (var property in typeof(Command).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            property.GetValue(button).Should().Be(property.GetValue(command), property.Name);
        button.Row.Should().Be(2);
        button.Col.Should().Be(3);
        button.Name = "変更";
        command.Name.Should().Be("名前");
    }
}
