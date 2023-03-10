using BoundsDecl;
using CallConventionDecl;
using IdentifierDecl;
using MethodDecl;
using ParameterDecl;
using ResourceDecl;
using SigArgumentDecl;
using System.Text;

using static Core;
using static ExtraTools.Extensions;
namespace TypeDecl;
using GenArgs = Type.Collection;

[GenerateParser] public partial record TypeSpecification : IDeclaration<TypeSpecification>;
[GenerationOrderParser(Order.Last)]
public record NamedModuleSpecification(DottedName Name, bool IsModule) : TypeSpecification, IDeclaration<NamedModuleSpecification>
{
    public override string ToString() => $"{(IsModule ? ".module " : String.Empty)}{Name}";
    public static Parser<NamedModuleSpecification> MainParser => RunAll(
        converter: (vals) => new NamedModuleSpecification(vals[1].Name, vals[0].IsModule),
        TryRun(
            converter: (module) => new NamedModuleSpecification(null, module is not null),
            Discard<NamedModuleSpecification, string>(ConsumeWord(Id, ".module")),
            Empty<NamedModuleSpecification>()
        ),
        Map(
            converter: (name) => new NamedModuleSpecification(name, false),
            DottedName.AsParser
        )
    );
    public static Parser<NamedModuleSpecification> AsParser => RunAll(
        converter: (vals) => vals[1],
        Discard<NamedModuleSpecification, char>(ConsumeChar(Id, '[')),
        NamedModuleSpecification.MainParser,
        Discard<NamedModuleSpecification, char>(ConsumeChar(Id, ']'))
    );
}

[WrapParser<TypeReference>] public partial record TypeSpecificationReference : TypeSpecification, IDeclaration<TypeSpecificationReference>;
[WrapParser<Type>] public partial record TypeSpecificationInlined : TypeSpecification, IDeclaration<TypeSpecificationInlined>;

public record NativeType(NativeType TypeComponent, bool IsArray, INT Length, INT Supplied) : IDeclaration<NativeType>
{
    public override string ToString()
    {
        StringBuilder sb = new();
        if (TypeComponent is not null)
        {
            sb.Append(TypeComponent.ToString());
        }

        if (IsArray)
        {
            sb.Append("[");
            if (Length is not null)
            {
                sb.Append($"{Length}");
            }
            if (Supplied is not null)
            {
                sb.Append($"+{Supplied}");
            }
            sb.Append("]");
        }
        return sb.ToString();
    }
    public record NativeTypePrimitive(String TypeName) : NativeType(null, false, null, null), IDeclaration<NativeTypePrimitive>
    {
        private static String[] _primitives = new String[] { "[]", "bool", "float32", "float64", "int", "int8", "int16", "int32", "int64", "lpstr", "lpwstr", "method", "unsigned" };
        public override string ToString() => TypeName;
        public static Parser<NativeTypePrimitive> AsParser => TryRun(
            converter: (vals) => new NativeTypePrimitive(vals),
            _primitives.Select((primitive) =>
            {
                if (primitive == "unsigned")
                {
                    return RunAll(
                        converter: (vals) => $"{primitive} {vals[1]}",
                        TryRun(
                            converter: Id,
                            ConsumeWord(Id, primitive),
                            ConsumeWord(Id, "u")
                        ),
                        TryRun(
                            converter: (vals) => vals,
                            _primitives.Take(4..9).Select((primitive2) => ConsumeWord(Id, primitive2)).ToArray()
                        )
                    );
                }
                else
                {
                    return ConsumeWord(Id, primitive);
                }
            }).ToArray()
        );
    }

    public static Parser<NativeType> AsParser => RunAll(
        converter: (vals) => vals[1].Aggregate(vals[0][0], (acc, val) => new NativeType(acc, val.IsArray, val.Length, val.Supplied)),
        Map(primType => new[] { primType as NativeType }, NativeTypePrimitive.AsParser),
        RunMany(
            converter: Id,
            0, Int32.MaxValue, true,
            RunAll(
                converter: (vals) => new NativeType(null, true, vals[1], vals[2]),
                Discard<INT, char>(ConsumeChar(Id, '[')),
                TryRun(Id, Map(Id, INT.AsParser), Empty<INT>()),
                TryRun(Id,
                    RunAll(
                        converter: (vals) => vals[1],
                        Discard<INT, char>(ConsumeChar(Id, '+')),
                        Map(Id, INT.AsParser)
                    ),
                    Empty<INT>()
                ),
                Discard<INT, char>(ConsumeChar(Id, ']'))
            )
        )
    );
}

public record Type(TypeComponent.Collection Components) : IDeclaration<Type> {
    public override string ToString() => Components.ToString();
    public record Collection(ARRAY<Type> Types) : IDeclaration<Collection>
    {
        public override string ToString() => Types.ToString(',');
        public static Parser<Collection> AsParser => Map(
            converter: (types) => new Collection(types),
            ARRAY<Type>.MakeParser(new ARRAY<Type>.ArrayOptions {
                Delimiters = ('\0', ',', '\0')
            })
        );
    }

    public static Parser<Type> AsParser => RunAll(
        converter: (vals) => new Type(
            new TypeComponent.Collection(
                new ARRAY<TypeComponent>(vals[0].Concat(vals[1]).ToArray()) {
                    Options = new ARRAY<TypeComponent>.ArrayOptions {
                        Delimiters = ('\0', ' ', '\0')
                    }
                }
            )
        ),
        Map(
            converter: comp => new TypeComponent[] {comp as TypeComponent  },
            TypePrefix.AsParser
        ),
        RunMany(
            converter: Id,
            0, Int32.MaxValue, true,
            Map(
                converter: comp => comp as TypeComponent,
                TypeSuffix.AsParser
            )
        )
    );
}
[GenerateParser] public partial record TypeComponent : IDeclaration<TypeComponent> {
    public record Collection(ARRAY<TypeComponent> Types) : IDeclaration<Collection>
    {
        public override string ToString() => Types.ToString(' ');
        public static Parser<Collection> AsParser => Map(
            converter: (types) => new Collection(types),
            ARRAY<TypeComponent>.MakeParser(new ARRAY<TypeComponent>.ArrayOptions {
                Delimiters = ('\0', '\0', '\0')
            })
        );
    }
}
[GenerateParser] public partial record TypePrefix : TypeComponent, IDeclaration<TypePrefix>;
[GenerateParser] public partial record TypeSuffix : TypeComponent, IDeclaration<TypeSuffix>;
public record ReferenceSuffix(bool IsRawPointer) : TypeSuffix, IDeclaration<ReferenceSuffix>
{
    public override string ToString() => IsRawPointer ? "*" : "&";
    public static Parser<ReferenceSuffix> AsParser => TryRun(
        converter: (vals) => new ReferenceSuffix(vals == '*'),
        ConsumeChar(Id, '*'), ConsumeChar(Id, '&')
    );
}

public record BoundedSuffix(Bound.Collection Bounds) : TypeSuffix, IDeclaration<BoundedSuffix>
{
    public override string ToString() => Bounds.ToString();
    public static Parser<BoundedSuffix> AsParser => Map(
        converter: (vals) => new BoundedSuffix(vals),
        Bound.Collection.AsParser
    );
}

public record GenericSuffix(GenArgs GenericArguments) : TypeSuffix, IDeclaration<GenericSuffix>
{
    public override string ToString() => $"<{GenericArguments}>";
    public static Parser<GenericSuffix> AsParser => RunAll(
        converter: (vals) => new GenericSuffix(vals[1]),
        Discard<GenArgs, char>(ConsumeChar(Id, '<')),
        Lazy(() => GenArgs.AsParser),
        Discard<GenArgs, char>(ConsumeChar(Id, '>'))
    );
}

[GenerationOrderParser(Order.First)]
public record ModifierSuffix(String Modifier, TypeReference ReferencedType) : TypeSuffix, IDeclaration<ModifierSuffix>
{
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"{Modifier}");
        if (ReferencedType is not null)
        {
            sb.Append($" ({ReferencedType})");
        }
        return sb.ToString();
    }
    public static Parser<ModifierSuffix> AsParser => RunAll(
        converter: (vals) => new ModifierSuffix(vals[0].Modifier, vals[2].ReferencedType),
        Map(
            val => new ModifierSuffix(val, null), 
            TryRun(Id, ConsumeWord(Id, "modopt"), ConsumeWord(Id, "modreq"))
        ),
        Discard<ModifierSuffix, char>(ConsumeChar(Id, '(')),
        Map(
            val => new ModifierSuffix(null, val), 
            TypeReference.AsParser
        ),
        Discard<ModifierSuffix, char>(ConsumeChar(Id, ')'))
    );
}

public record TypePrimitive(String TypeName) : TypePrefix, IDeclaration<TypePrimitive>
{
    private static String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "object", "string", "typedref", "void", "unsigned", "native" };

    public override string ToString() => TypeName;
    public static Parser<TypePrimitive> AsParser => TryRun(
        converter: (vals) => new TypePrimitive(vals),
        _primitives.Select((primitive) =>
        {
            if (primitive == "unsigned")
            {
                return RunAll(
                    converter: (vals) => $"{primitive} {vals[1]}",
                    TryRun(
                        converter: Id,
                        ConsumeWord(Id, primitive),
                        ConsumeWord(Id, "u")
                    ),
                    TryRun(
                        converter: (vals) => vals,
                        _primitives.Take(4..8).Select((primitive2) => ConsumeWord(Id, primitive2)).ToArray()
                    )
                );
            }
            else if (primitive == "native")
            {
                return RunAll(
                    converter: (vals) =>
                    {
                        StringBuilder sb = new();
                        sb.Append(vals[0]);
                        if (vals[1] is not null)
                        {
                            sb.Append($" {vals[1]}");
                        }
                        sb.Append($" {vals[2]} ");
                        return sb.ToString();
                    },
                    ConsumeWord(Id, primitive),
                    TryRun(
                        converter: Id,
                        ConsumeWord(Id, "unsigned"),
                        ConsumeWord(Id, "u"),
                        Empty<String>()
                    ),
                    ConsumeWord(Id, "int")
                );
            }
            else
            {
                return ConsumeWord(Id, primitive);
            }
        }).ToArray()
    );
}

public record GenericTypeParameter(GenericParameterReference Index, GenericTypeParameter.TypeComponent TypeParameterType) : TypePrefix, IDeclaration<GenericTypeParameter>
{
    public enum TypeComponent { Method, Class, None }
    public override string ToString() => $"{(TypeParameterType is TypeComponent.Method ? "!!" : "!")}{Index}";
    public static Parser<GenericTypeParameter> AsParser => RunAll(
        converter: (vals) => new GenericTypeParameter(vals[1].Index, vals[0].TypeParameterType),
        TryRun(
            converter: (indicator) => new GenericTypeParameter(null, indicator == "!!" ? TypeComponent.Method : TypeComponent.Class),
            ConsumeWord(Id, "!!"),
            ConsumeWord(Id, "!")
        ),
        Map(val => new GenericTypeParameter(val, TypeComponent.None), GenericParameterReference.AsParser)
    );
}

public record MethodDefinition(CallConvention CallConvention, Type TypeTarget, Parameter.Collection Parameters) : TypePrefix, IDeclaration<MethodDefinition>
{
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"method {CallConvention} {TypeTarget}* \n(");
        sb.Append(Parameters);
        sb.Append("\n)");
        return sb.ToString();
    }
    public static Parser<MethodDefinition> AsParser => RunAll(
        converter: parts => new MethodDefinition(parts[1].CallConvention, parts[2].TypeTarget, parts[4].Parameters),
        Discard<MethodDefinition, String>(ConsumeWord(Id, "method")),
        Map(
            converter: part1 => new MethodDefinition(part1, null, null),
            CallConvention.AsParser
        ),
        Map(
            converter: part2 => new MethodDefinition(null, part2, null),
            Lazy(() => Type.AsParser)
        ),
        Discard<MethodDefinition, char>(ConsumeChar(Id, '*')),
        Map(
            converter: part3 => new MethodDefinition(null, null, part3),
            Parameter.Collection.AsParser
        )
    );
}

public record CustomTypeReference(string TypeBehaviorIndice, TypeReference Reference) : TypePrefix, IDeclaration<CustomTypeReference>
{
    public override string ToString() => $"{TypeBehaviorIndice} {Reference}";
    public static Parser<CustomTypeReference> AsParser => RunAll(
        converter: (vals) => new CustomTypeReference(vals[0].TypeBehaviorIndice, vals[1].Reference),
        TryRun(
            converter: word => new CustomTypeReference(word, null),
            ConsumeWord(Id, "class"),
            ConsumeWord(Id, "valuetype")
        ),
        Map(
            converter: (vals) => new CustomTypeReference(null, vals),
            Lazy(() => TypeReference.AsParser)
        )
    );
}


[GenerateParser] public partial record OwnerType : IDeclaration<OwnerType>;
[WrapParser<TypeSpecification>] public partial record TypeSpecReference : OwnerType, IDeclaration<TypeSpecReference>;
[GenerateParser] public partial record MemberReference : OwnerType, IDeclaration<MemberReference>;

public record MethodMemberReference(MethodReference MethodRef) : MemberReference, IDeclaration<MethodMemberReference>
{
    public override string ToString() => $"method {MethodRef}";
    public static Parser<MethodMemberReference> AsParser => RunAll(
        converter: (vals) => new MethodMemberReference(vals[1].MethodRef),
        Discard<MethodMemberReference, string>(ConsumeWord(Id, "method")),
        Map(
            converter: (vals) => new MethodMemberReference(vals),
            Lazy(() => MethodReference.AsParser)
        )
    );
}

public record FieldMemberReference(FieldTypeReference FieldRef) : MemberReference, IDeclaration<FieldMemberReference>
{
    public override string ToString() => $"field {FieldRef}";
    public static Parser<FieldMemberReference> AsParser => RunAll(
        converter: (vals) => new FieldMemberReference(vals[1].FieldRef),
        Discard<FieldMemberReference, string>(ConsumeWord(Id, "field")),
        Map(
            converter: (vals) => new FieldMemberReference(vals),
            Lazy(() => FieldTypeReference.AsParser)
        )
    );
}

public record FieldTypeReference(TypeDecl.Type Type, TypeDecl.TypeSpecification Spec, Identifier Name)
    : IDeclaration<FieldTypeReference>
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"{Type} ");
        if (Spec != null)
        {
            sb.Append($"{Spec}::");
        }
        sb.Append($"{Name}");
        return sb.ToString();
    }
    public static Parser<FieldTypeReference> AsParser => RunAll(
        converter: parts => new FieldTypeReference(
            parts[0].Type,
            parts[1]?.Spec,
            parts[2].Name
        ),
        Map(
            converter: TypeComponent => Construct<FieldTypeReference>(3, 0, TypeComponent),
            TypeDecl.Type.AsParser
        ),
        TryRun(
            converter: spec => Construct<FieldTypeReference>(3, 1, spec),
            RunAll(
                converter: parts => parts[0],
                TypeDecl.TypeSpecification.AsParser,
                Discard<TypeDecl.TypeSpecification, string>(ConsumeWord(Core.Id, "::"))
            ),
            Empty<TypeDecl.TypeSpecification>()
        ),
        Map(
            converter: name => Construct<FieldTypeReference>(3, 2, name),
            Identifier.AsParser
        )
    );
}

public record MethodReference(CallConvention? Convention, TypeDecl.Type TypeComponent, TypeSpecification Spec, MethodName Name, GenArgs? TypeParameters, SigArgumentDecl.SigArgument.Collection SigArgs)
    : IDeclaration<MethodReference>
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Convention is not null)
        {
            sb.Append($"{Convention}");
        }
        sb.Append($"{TypeComponent} ");
        if (Spec is not null)
        {
            sb.Append($"{Spec}::");
        }
        sb.Append($"{Name} ");
        if (TypeParameters is not null)
        {
            sb.Append($"<{TypeParameters}> ");
        }
        sb.Append($"{SigArgs}");
        return sb.ToString();
    }
    public static Parser<MethodReference> AsParser => RunAll(
        converter: parts => new MethodReference(
            parts[0].Convention,
            parts[1].TypeComponent,
            parts[2]?.Spec,
            parts[3].Name,
            parts[4]?.TypeParameters,
            parts[5].SigArgs
        ),
        Map(
            converter: conv => Construct<MethodReference>(6, 0, conv),
            CallConvention.AsParser
        ),
        Map(
            converter: TypeComponent => Construct<MethodReference>(6, 1, TypeComponent),
            TypeDecl.Type.AsParser
        ),
        TryRun(
            converter: TypeComponent => Construct<MethodReference>(6, 2, TypeComponent),
            RunAll(
                converter: parts => parts[0],
                TypeSpecification.AsParser,
                Discard<TypeSpecification, string>(ConsumeWord(Core.Id, "::"))
            ),
            Empty<TypeSpecification>()
        ),
        Map(
            converter: name => Construct<MethodReference>(6, 3, name),
            MethodName.AsParser
        ),
        TryRun(
            converter: typeParams => Construct<MethodReference>(6, 4, typeParams),
            RunAll(
                converter: parts => parts[1],
                Discard<GenArgs, char>(ConsumeChar(Id, '<')),
                GenArgs.AsParser,
                Discard<GenArgs, char>(ConsumeChar(Id, '>'))
            ),
            Empty<GenArgs>()
        ),
        Map(
            converter: pars => Construct<MethodReference>(6, 5, pars),
            SigArgument.Collection.AsParser
        )
    );
}


public record TypeReference(ResolutionScope Scope, Identifier Name, GenArgs GenericTypes) : IDeclaration<TypeReference>
{
    public override string ToString()
    {
        StringBuilder sb = new();
        if (Scope is not null)
        {
            sb.Append($"{Scope.ToString(true)} ");
        }
        sb.Append(Name);
        if (GenericTypes is not null)
        {
            sb.Append($"<{GenericTypes}>");
        }
        return sb.ToString();
    }
    public static Parser<TypeReference> AsParser => RunAll(
        converter: (vals) => new TypeReference(vals[0]?.Scope, vals[1].Name, vals[2]?.GenericTypes),
        TryRun(
            converter: (scope) => Construct<TypeReference>(3, 0, scope),
            ResolutionScope.AsParser,
            Empty<ResolutionScope>()
        ),
        Map(
            converter: (name) => Construct<TypeReference>(3, 1, name),
            Identifier.AsParser
        ),
        TryRun(
            converter: (typeParams) => Construct<TypeReference>(3, 2, typeParams),
            RunAll(
                converter: parts => parts[1],
                Discard<GenArgs, char>(ConsumeChar(Id, '<')),
                GenArgs.AsParser,
                Discard<GenArgs, char>(ConsumeChar(Id, '>'))
            ),
            Empty<GenArgs>()
        )
    );
}