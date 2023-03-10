using AttributeDecl;
using CallConventionDecl;
using IdentifierDecl;
using MethodDecl;
using ParameterDecl;
using ResourceDecl;
using TypeDecl;

using static Core;
using static ExtraTools.Extensions;

namespace PropertyDecl;
public record Property(Prefix Header, Member.Collection Members) : IDeclaration<Property>
{
    public override string ToString() => $".property {Header} \n{{\n{Members}\n}}";
    public static Parser<Property> AsParser => RunAll(
        converter: parts => new Property(parts[0].Header, parts[1].Members),
        RunAll(
            converter: header => Construct<Property>(2, 0, header[1]),
            Discard<Prefix, string>(ConsumeWord(Core.Id, ".property")),
            Prefix.AsParser
        ),
        RunAll(
            converter: parts => Construct<Property>(2, 1, parts[1]),
            Discard<Member.Collection, char>(ConsumeChar(Core.Id, '{')),
            Member.Collection.AsParser,
            Discard<Member.Collection, char>(ConsumeChar(Core.Id, '}'))
        )
    );
}

public record Prefix(PropertyAttribute.Collection Attributes, CallConvention Convention, TypeDecl.Type Type, Identifier Id, Parameter.Collection Parameters) : IDeclaration<Prefix>
{
    public override string ToString() => $"{Attributes} {Convention} {Type} {Id}{Parameters}";
    public static Parser<Prefix> AsParser => RunAll(
        converter: parts => new Prefix(
            parts[0].Attributes,
            parts[1].Convention,
            parts[2].Type,
            parts[3].Id,
            parts[4].Parameters
        ),
        Map(
            converter: attrs => Construct<Prefix>(5, 0, attrs),
            PropertyAttribute.Collection.AsParser
        ),
        Map(
            converter: conv => Construct<Prefix>(5, 1, conv),
            CallConvention.AsParser
        ),
        Map(
            converter: type => Construct<Prefix>(5, 2, type),
            TypeDecl.Type.AsParser
        ),
        Map(
            converter: id => Construct<Prefix>(5, 3, id),
            Identifier.AsParser
        ),
        Map(
            converter: pars => Construct<Prefix>(5, 4, pars),
            Map(
                converter: parts => parts,
                Parameter.Collection.AsParser
            )
        )
    );
}

[GenerateParser]
public partial record Member : IDeclaration<Member>
{
    public record Collection(ARRAY<Member> Members) : IDeclaration<Collection>
    {
        public override string ToString() => Members.ToString('\n');
        public static Parser<Collection> AsParser => Map(
            converter: members => new Collection(members),
            ARRAY<Member>.MakeParser(new ARRAY<Member>.ArrayOptions {
                Delimiters = ('\0', '\0', '\0')
            })
        );
    }
}
[WrapParser<CustomAttribute>] public partial record PropertyAttributeItem : Member, IDeclaration<PropertyAttributeItem>;
[WrapParser<ExternSource>] public partial record ExternalSourceItem : Member, IDeclaration<ExternalSourceItem>;

public record SpecialMethodReference(String SpecialName, CallConvention Convention, TypeDecl.Type Type, TypeSpecification? Specification, MethodName Name, Parameter.Collection Parameters) : Member, IDeclaration<SpecialMethodReference>
{
    public override string ToString() => $"{SpecialName} {Convention} {Type} {(Specification is null ? "" : $"{Specification}::")}{Name} {Parameters}";
    public static string[] SpecialNames = new string[] { ".get", ".other", ".set" };
    public static Parser<SpecialMethodReference> AsParser => RunAll(
        converter: parts => new SpecialMethodReference(
            parts[0].SpecialName,
            parts[1].Convention,
            parts[2].Type,
            parts[3].Specification,
            parts[4].Name,
            parts[5].Parameters
        ),
        TryRun(
            converter: name => Construct<SpecialMethodReference>(6, 0, name),
            SpecialNames.Select(methname => ConsumeWord(Id, methname)).ToArray()
        ),
        Map(
            converter: conv => Construct<SpecialMethodReference>(6, 1, conv),
            CallConvention.AsParser
        ),
        Map(
            converter: type => Construct<SpecialMethodReference>(6, 2, type),
            TypeDecl.Type.AsParser
        ),
        TryRun(
            converter: spec => Construct<SpecialMethodReference>(6, 3, spec),
            RunAll(
                converter: specs => specs[0],
                TypeSpecification.AsParser,
                Discard<TypeSpecification, string>(ConsumeWord(Core.Id, "::"))
            ),
            Empty<TypeSpecification>()
        ),
        Map(
            converter: name => Construct<SpecialMethodReference>(6, 4, name),
            MethodName.AsParser
        ),
        Map(
            converter: param => Construct<SpecialMethodReference>(6, 5, param),
            Parameter.Collection.AsParser
        )
    );
}
