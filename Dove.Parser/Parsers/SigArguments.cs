using System.Text;
using AttributeDecl;
using ExtraTools;
using IdentifierDecl;
using MethodDecl;
using TypeDecl;

using static Core;
using static ExtraTools.Extensions;

namespace SigArgumentDecl;

public record SigArgument(AttributeDecl.ParamAttribute.Collection Attributes, TypeDecl.Type Type, Identifier? Id, NativeType? NativeType) : IDeclaration<SigArgument>
{
    public record Collection(ARRAY<SigArgument> Arguments) : IDeclaration<Collection>
    {
        public override string ToString() => Arguments.ToString(',');
        public static Parser<Collection> AsParser => RunAll(
            converter: parts => new Collection(parts[0].Arguments),
            Map(
                converter: arguments => Construct<Collection>(1, 0, arguments),
                ARRAY<SigArgument>.MakeParser(new ARRAY<SigArgument>.ArrayOptions
                {
                    Delimiters = ('(', ',', ')')
                })
            )
        );
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"{Attributes} ");
        sb.Append($"{Type} ");
        if (NativeType != null) sb.Append($"marshal ({NativeType})");
        if (Id != null) sb.Append($"{Id} ");
        return sb.ToString();
    }

    public static Parser<SigArgument> AsParser => RunAll(
        converter: parts => new SigArgument(parts[0].Attributes, parts[1].Type, parts[2].Id, parts[3].NativeType),
        Map(
            converter: attributes => Construct<SigArgument>(4, 0, attributes),
            AttributeDecl.ParamAttribute.Collection.AsParser
        ),
        Map(
            converter: type => Construct<SigArgument>(4, 1, type),
            TypeDecl.Type.AsParser
        ),
        TryRun(
            converter: nativeType => Construct<SigArgument>(4, 3, nativeType),
            RunAll(
                converter: parts => parts[1],
                Discard<NativeType, string>(ConsumeWord(Core.Id, "marshal")),
                NativeType.AsParser
            ),
            Empty<NativeType>()
        ),
        TryRun(
            converter: id => Construct<SigArgument>(4, 2, id),
            Identifier.AsParser,
            Empty<Identifier>()
        )
    );
}