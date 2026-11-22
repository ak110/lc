using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Launcher.Tests;

/// <summary>
/// 製品アセンブリのメタデータとILを読み、製品のどのメソッド本体からも参照されない非privateのメンバーを列挙する。
/// 型を読み込まずに走査するため、WinFormsの型を含むアセンブリもWindows以外で読める。
/// </summary>
/// <remarks>
/// ILに参照が現れない利用は、次の区分ごとに使用中として扱う（個別のメンバー名では除外しない）。
/// <list type="bullet">
/// <item>仮想メソッド、override、インターフェイスの実装: 呼び出しが基底の定義やインターフェイスへのトークンで書かれる</item>
/// <item>エントリポイントと静的コンストラクター: ランタイムが呼ぶ</item>
/// <item>コンパイラーが生成するメンバー（recordの<c>Deconstruct</c>・等値演算子など）: ソースに定義が無く、削除できない</item>
/// <item>XmlSerializerとSystem.Text.Jsonで読み書きする型の公開プロパティと引数なしのコンストラクター: シリアライザーが実行時にリフレクションで使う</item>
/// <item>P/InvokeとCOMインターフェイスのメソッドへ渡す構造体のフィールド: ネイティブ側が読み書きし、ILには現れない</item>
/// <item>const値とenum値: 参照元のILへ値として埋め込まれる</item>
/// <item>C#が対での定義を求める演算子のうち使われない側: 片方だけを定義するとコンパイルエラーになる</item>
/// <item>Equalsをoverrideする値型の等値演算子: アナライザーのCA2231が定義を求め、ビルドが失敗する</item>
/// <item>使われるアクセサーを持つプロパティとイベントの、使われない側のアクセサー: 1つの宣言から生成され、片方だけを除くと読取専用・書込専用への仕様変更になる</item>
/// <item>属性のコンストラクター: カスタム属性のメタデータから参照される</item>
/// </list>
/// </remarks>
internal static class ProductMemberUsage
{
    const string XmlSerializerRootType = "Launcher.Infrastructure.ConfigStore";

    static readonly (string, string)[] PairedOperators =
    [
        ("op_Equality", "op_Inequality"),
        ("op_LessThan", "op_GreaterThan"),
        ("op_LessThanOrEqual", "op_GreaterThanOrEqual"),
        ("op_True", "op_False"),
    ];

    public static IReadOnlyList<string> FindUnusedMembers(IEnumerable<string> assemblyPaths)
    {
        var modules = assemblyPaths.Select(path => new ModuleInfo(path)).ToList();
        try
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var module in modules) module.CollectReferences(used);
            var serializedTypes = SerializedTypes(modules);
            var pinvokeTypes = PInvokeStructTypes(modules);
            var unused = new List<string>();
            foreach (var module in modules)
                unused.AddRange(module.FindUnused(used, serializedTypes, pinvokeTypes));
            return unused.Order(StringComparer.Ordinal).ToList();
        }
        finally
        {
            foreach (var module in modules) module.Dispose();
        }
    }

    static HashSet<string> SerializedTypes(List<ModuleInfo> modules)
    {
        var types = TypeTable(modules);
        var result = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(types.Values.Where(t => DerivesFrom(t, XmlSerializerRootType, types)).Select(t => t.Name)
            .Concat(modules.SelectMany(m => m.JsonSerializerTypeArguments)));
        while (queue.Count > 0)
        {
            string name = queue.Dequeue();
            if (!result.Add(name) || !types.TryGetValue(name, out var type)) continue;
            foreach (string referenced in type.PublicPropertyTypeNames)
                if (types.ContainsKey(referenced)) queue.Enqueue(referenced);
        }
        return result;
    }

    static Dictionary<string, TypeInfo> TypeTable(List<ModuleInfo> modules) =>
        modules.SelectMany(m => m.Types).DistinctBy(t => t.Name, StringComparer.Ordinal).ToDictionary(t => t.Name, StringComparer.Ordinal);

    static bool DerivesFrom(TypeInfo type, string baseName, Dictionary<string, TypeInfo> types)
    {
        for (string? current = type.BaseTypeName; current is not null; current = types.GetValueOrDefault(current)?.BaseTypeName)
            if (current == baseName) return true;
        return false;
    }

    static HashSet<string> PInvokeStructTypes(List<ModuleInfo> modules)
    {
        var types = TypeTable(modules);
        var result = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(modules.SelectMany(m => m.PInvokeParameterTypeNames));
        while (queue.Count > 0)
        {
            string name = queue.Dequeue();
            if (!types.TryGetValue(name, out var type) || !result.Add(name)) continue;
            foreach (string field in type.FieldTypeNames) queue.Enqueue(field);
        }
        return result;
    }

    sealed record TypeInfo(string Name, string? BaseTypeName, IReadOnlyList<string> PublicPropertyTypeNames, IReadOnlyList<string> FieldTypeNames);

    sealed class ModuleInfo : IDisposable
    {
        readonly PEReader pe;
        readonly MetadataReader md;
        readonly NameProvider names;

        public ModuleInfo(string path)
        {
            pe = new PEReader(File.OpenRead(path));
            md = pe.GetMetadataReader();
            names = new NameProvider(md);
            Types = md.TypeDefinitions.Select(ReadType).ToList();
        }

        public IReadOnlyList<TypeInfo> Types { get; }

        public HashSet<string> JsonSerializerTypeArguments { get; } = new(StringComparer.Ordinal);

        public IEnumerable<string> PInvokeParameterTypeNames =>
            md.MethodDefinitions.Select(md.GetMethodDefinition)
                .Where(method => (method.Attributes & MethodAttributes.PinvokeImpl) != 0
                    || (md.GetTypeDefinition(method.GetDeclaringType()).Attributes & TypeAttributes.Import) != 0)
                .SelectMany(method => method.DecodeSignature(names, null).ParameterTypes)
                .Select(NameProvider.ElementName);

        public void Dispose() => pe.Dispose();

        TypeInfo ReadType(TypeDefinitionHandle handle)
        {
            var type = md.GetTypeDefinition(handle);
            string? baseName = type.BaseType.IsNil ? null : names.TypeName(type.BaseType);
            var properties = type.GetProperties().Select(md.GetPropertyDefinition)
                .Where(property => IsPublic(property.GetAccessors().Getter))
                .SelectMany(property => NameProvider.ComponentNames(property.DecodeSignature(names, null).ReturnType))
                .ToList();
            var fields = type.GetFields().Select(md.GetFieldDefinition)
                .Where(field => (field.Attributes & FieldAttributes.Static) == 0)
                .Select(field => NameProvider.ElementName(field.DecodeSignature(names, null)))
                .ToList();
            return new(names.TypeName(handle), baseName, properties, fields);
        }

        bool IsPublic(MethodDefinitionHandle handle) =>
            !handle.IsNil && (md.GetMethodDefinition(handle).Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        public void CollectReferences(HashSet<string> used)
        {
            foreach (var handle in md.MethodDefinitions)
            {
                var method = md.GetMethodDefinition(handle);
                if (method.RelativeVirtualAddress == 0) continue;
                var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILReader();
                while (il.RemainingBytes > 0)
                {
                    var opCode = ReadOpCode(ref il);
                    switch (opCode.OperandType)
                    {
                        case OperandType.InlineMethod:
                        case OperandType.InlineField:
                        case OperandType.InlineTok:
                            AddReference(MetadataTokens.EntityHandle(il.ReadInt32()), used);
                            break;
                        case OperandType.InlineSwitch:
                            int targets = il.ReadInt32();
                            il.Offset += targets * 4;
                            break;
                        default:
                            il.Offset += OperandSize(opCode.OperandType);
                            break;
                    }
                }
            }
            foreach (var handle in md.CustomAttributes)
                AddReference(md.GetCustomAttribute(handle).Constructor, used);
        }

        void AddReference(EntityHandle handle, HashSet<string> used)
        {
            switch (handle.Kind)
            {
                case HandleKind.MethodDefinition:
                    used.Add(MethodKey((MethodDefinitionHandle)handle));
                    break;
                case HandleKind.FieldDefinition:
                    used.Add(FieldKey((FieldDefinitionHandle)handle));
                    break;
                case HandleKind.MethodSpecification:
                    var specification = md.GetMethodSpecification((MethodSpecificationHandle)handle);
                    if (specification.Method.Kind == HandleKind.MemberReference
                        && names.TypeName(md.GetMemberReference((MemberReferenceHandle)specification.Method).Parent) == "System.Text.Json.JsonSerializer")
                    {
                        foreach (string argument in specification.DecodeSignature(names, null))
                            JsonSerializerTypeArguments.Add(argument);
                    }
                    AddReference(specification.Method, used);
                    break;
                case HandleKind.MemberReference:
                    var reference = md.GetMemberReference((MemberReferenceHandle)handle);
                    string owner = names.TypeName(reference.Parent);
                    string name = md.GetString(reference.Name);
                    used.Add(reference.GetKind() == MemberReferenceKind.Method
                        ? $"{owner}::{name}{NameProvider.Signature(reference.DecodeMethodSignature(names, null))}"
                        : $"{owner}::{name}");
                    break;
            }
        }

        string MethodKey(MethodDefinitionHandle handle)
        {
            var method = md.GetMethodDefinition(handle);
            return $"{names.TypeName(method.GetDeclaringType())}::{md.GetString(method.Name)}{NameProvider.Signature(method.DecodeSignature(names, null))}";
        }

        string FieldKey(FieldDefinitionHandle handle)
        {
            var field = md.GetFieldDefinition(handle);
            return $"{names.TypeName(field.GetDeclaringType())}::{md.GetString(field.Name)}";
        }

        public IEnumerable<string> FindUnused(HashSet<string> used, HashSet<string> serializedTypes, HashSet<string> pinvokeTypes)
        {
            var entryPoint = pe.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress;
            foreach (var typeHandle in md.TypeDefinitions)
            {
                var type = md.GetTypeDefinition(typeHandle);
                string typeName = names.TypeName(typeHandle);
                if (IsCompilerGenerated(type.GetCustomAttributes()) || md.GetString(type.Name).StartsWith('<')) continue;
                bool isSerialized = serializedTypes.Contains(typeName);
                bool isRecord = type.GetMethods().Any(m => md.GetString(md.GetMethodDefinition(m).Name) == "<Clone>$")
                    || HasCompilerGeneratedRecordMembers(type);
                var accessorKinds = AccessorKinds(type);
                var usedDeclarations = UsedAccessorDeclarations(type, used);
                foreach (var handle in type.GetMethods())
                {
                    var method = md.GetMethodDefinition(handle);
                    string name = md.GetString(method.Name);
                    string key = MethodKey(handle);
                    if (!IsNonPrivate(method.Attributes) || used.Contains(key)) continue;
                    if ((method.Attributes & MethodAttributes.Virtual) != 0) continue;
                    if (MetadataTokens.GetToken(handle) == entryPoint || name == ".cctor") continue;
                    if (IsCompilerGenerated(method.GetCustomAttributes()) && !accessorKinds.ContainsKey(handle)) continue;
                    if (isRecord && IsRecordSynthesized(name, accessorKinds.GetValueOrDefault(handle))) continue;
                    if (isSerialized && IsSerializerMember(method, name, accessorKinds.GetValueOrDefault(handle))) continue;
                    if (IsUnusedSideOfPairedOperator(type, typeName, name, used)) continue;
                    if (name is "op_Equality" or "op_Inequality" && IsValueTypeOverridingEquals(type)) continue;
                    if (IsAttributeType(type) && name == ".ctor") continue;
                    if (usedDeclarations.Contains(handle)) continue;
                    yield return key;
                }
                foreach (var handle in type.GetFields())
                {
                    var field = md.GetFieldDefinition(handle);
                    string key = FieldKey(handle);
                    if (!IsNonPrivate(field.Attributes) || used.Contains(key)) continue;
                    if ((field.Attributes & (FieldAttributes.Literal | FieldAttributes.RTSpecialName)) != 0) continue;
                    if (pinvokeTypes.Contains(typeName)) continue;
                    if (IsCompilerGenerated(field.GetCustomAttributes())) continue;
                    yield return key;
                }
            }
        }

        static bool IsNonPrivate(MethodAttributes attributes) => (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public or MethodAttributes.Assembly or MethodAttributes.FamORAssem => true,
            _ => false,
        };

        static bool IsNonPrivate(FieldAttributes attributes) => (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public or FieldAttributes.Assembly or FieldAttributes.FamORAssem => true,
            _ => false,
        };

        Dictionary<MethodDefinitionHandle, string> AccessorKinds(TypeDefinition type)
        {
            var kinds = new Dictionary<MethodDefinitionHandle, string>();
            foreach (var property in type.GetProperties().Select(md.GetPropertyDefinition))
            {
                var accessors = property.GetAccessors();
                if (!accessors.Getter.IsNil) kinds[accessors.Getter] = "get";
                if (!accessors.Setter.IsNil) kinds[accessors.Setter] = "set";
            }
            foreach (var @event in type.GetEvents().Select(md.GetEventDefinition))
            {
                var accessors = @event.GetAccessors();
                if (!accessors.Adder.IsNil) kinds[accessors.Adder] = "add";
                if (!accessors.Remover.IsNil) kinds[accessors.Remover] = "remove";
            }
            return kinds;
        }

        HashSet<MethodDefinitionHandle> UsedAccessorDeclarations(TypeDefinition type, HashSet<string> used)
        {
            var result = new HashSet<MethodDefinitionHandle>();
            var declarations = type.GetProperties().Select(md.GetPropertyDefinition)
                .Select(property => new[] { property.GetAccessors().Getter, property.GetAccessors().Setter })
                .Concat(type.GetEvents().Select(md.GetEventDefinition)
                    .Select(@event => new[] { @event.GetAccessors().Adder, @event.GetAccessors().Remover }));
            foreach (var accessors in declarations)
            {
                var present = accessors.Where(handle => !handle.IsNil).ToList();
                if (present.Any(handle => used.Contains(MethodKey(handle)))) result.UnionWith(present);
            }
            return result;
        }

        /// <summary>record構造体は<c>&lt;Clone&gt;$</c>を持たないため、コンパイラーが生成する等値演算子で判定する。</summary>
        bool HasCompilerGeneratedRecordMembers(TypeDefinition type) =>
            type.GetMethods().Select(md.GetMethodDefinition).Any(method =>
                md.GetString(method.Name) == "op_Equality" && IsCompilerGenerated(method.GetCustomAttributes()));

        static bool IsRecordSynthesized(string name, string? accessorKind) =>
            name is "Deconstruct" or "op_Equality" or "op_Inequality" or "Equals" or "PrintMembers" or "<Clone>$"
            || accessorKind == "set";

        /// <summary>シリアライザーは公開プロパティのget・setと引数なしのコンストラクターをリフレクションで呼ぶ。</summary>
        bool IsSerializerMember(MethodDefinition method, string name, string? accessorKind)
        {
            bool isPublic = (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
            if (!isPublic) return false;
            if (accessorKind is "get" or "set") return true;
            return name == ".ctor" && method.DecodeSignature(names, null).ParameterTypes.Length == 0;
        }

        bool IsUnusedSideOfPairedOperator(TypeDefinition type, string typeName, string name, HashSet<string> used)
        {
            foreach (var (left, right) in PairedOperators)
            {
                string? partner = name == left ? right : name == right ? left : null;
                if (partner is null) continue;
                return type.GetMethods().Any(handle =>
                    md.GetString(md.GetMethodDefinition(handle).Name) == partner && used.Contains(MethodKey(handle)));
            }
            return false;
        }

        bool IsValueTypeOverridingEquals(TypeDefinition type) =>
            !type.BaseType.IsNil && names.TypeName(type.BaseType) == "System.ValueType"
            && type.GetMethods().Select(md.GetMethodDefinition).Any(method =>
                md.GetString(method.Name) == "Equals" && (method.Attributes & MethodAttributes.Virtual) != 0);

        bool IsAttributeType(TypeDefinition type) =>
            !type.BaseType.IsNil && names.TypeName(type.BaseType) == "System.Attribute";

        bool IsCompilerGenerated(CustomAttributeHandleCollection attributes) =>
            attributes.Any(handle => AttributeTypeName(md.GetCustomAttribute(handle)) == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

        string AttributeTypeName(CustomAttribute attribute) => attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => names.TypeName(md.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent),
            HandleKind.MethodDefinition => names.TypeName(md.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType()),
            _ => "",
        };
    }

    static OpCode ReadOpCode(ref BlobReader il)
    {
        byte first = il.ReadByte();
        return first == 0xFE ? TwoByteOpCodes[il.ReadByte()] : OneByteOpCodes[first];
    }

    static int OperandSize(OperandType type) => type switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        _ => 4,
    };

    static readonly OpCode[] OneByteOpCodes = new OpCode[256];
    static readonly OpCode[] TwoByteOpCodes = new OpCode[256];

    static ProductMemberUsage()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var opCode = (OpCode)field.GetValue(null)!;
            ushort value = unchecked((ushort)opCode.Value);
            if (value < 0x100) OneByteOpCodes[value] = opCode;
            else if ((value & 0xFF00) == 0xFE00) TwoByteOpCodes[value & 0xFF] = opCode;
        }
    }

    sealed class NameProvider(MetadataReader md) : ISignatureTypeProvider<string, object?>
    {
        public string TypeName(EntityHandle handle) => handle.Kind switch
        {
            HandleKind.TypeDefinition => TypeName((TypeDefinitionHandle)handle),
            HandleKind.TypeReference => TypeName((TypeReferenceHandle)handle),
            HandleKind.TypeSpecification => GenericDefinitionName(md.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(this, null)),
            _ => "",
        };

        public string TypeName(TypeDefinitionHandle handle)
        {
            var type = md.GetTypeDefinition(handle);
            string name = md.GetString(type.Name);
            var declaring = type.GetDeclaringType();
            return declaring.IsNil ? Qualify(md.GetString(type.Namespace), name) : TypeName(declaring) + "/" + name;
        }

        string TypeName(TypeReferenceHandle handle)
        {
            var type = md.GetTypeReference(handle);
            string name = md.GetString(type.Name);
            return type.ResolutionScope.Kind == HandleKind.TypeReference
                ? TypeName((TypeReferenceHandle)type.ResolutionScope) + "/" + name
                : Qualify(md.GetString(type.Namespace), name);
        }

        static string Qualify(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;

        static string GenericDefinitionName(string name)
        {
            int index = name.IndexOf('<', StringComparison.Ordinal);
            return index < 0 ? name : name[..index];
        }

        public static string[] ComponentNames(string name) =>
            name.Split(['<', '>', ',', '[', ']', '&', '*'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public static string ElementName(string name) => name.TrimEnd('&', '*', ']', '[');

        public static string Signature(MethodSignature<string> signature) =>
            $"`{signature.GenericParameterCount}({string.Join(",", signature.ParameterTypes)}):{signature.ReturnType}";

        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr" + Signature(signature);
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => $"{genericType}<{string.Join(",", typeArguments)}>";
        public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;
        public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetPinnedType(string elementType) => elementType;
        public string GetPointerType(string elementType) => elementType + "*";
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => TypeName(handle);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => TypeName(handle);
        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }
}
