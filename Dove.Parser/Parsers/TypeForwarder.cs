using IdentifierDecl;

using static Core;
using static ExtraTools.Extensions;

namespace TypeForwarderDecl;
public record TypeForwarder(Prefix Header, Member Members) : IDeclaration<TypeForwarder>
{
    public override string ToString() => $".class {Header} \n{{\n{Members}\n}}";

    public static Parser<TypeForwarder> AsParser => RunAll(
        converter: parts => new TypeForwarder(parts[1].Header, parts[3].Members),
        Discard<TypeForwarder, string>(ConsumeWord(Core.Id, ".class")),
        Map(
            converter: header => Construct<TypeForwarder>(2, 0, header),
            Prefix.AsParser
        ),
        Discard<TypeForwarder, string>(ConsumeWord(Core.Id, "{")),
        Map(
            converter: members => Construct<TypeForwarder>(2, 1, members),
            Member.AsParser
        ),
        Discard<TypeForwarder, string>(ConsumeWord(Core.Id, "}"))
    );
}

public record Prefix(DottedName Name) : IDeclaration<Prefix>
{
    public override string ToString() => $"extern forwarder {Name}";
    public static Parser<Prefix> AsParser => RunAll(
        converter: parts => new Prefix(parts[2].Name),
        Discard<Prefix, string>(ConsumeWord(Core.Id, "extern")),
        Discard<Prefix, string>(ConsumeWord(Core.Id, "forwarder")),
        Map(
            converter: name => Construct<Prefix>(2, 1, name),
            DottedName.AsParser
        )
    );
}

[WrapParser<ExternAssemblyDecl.Prefix>] public partial record Member : IDeclaration<Member>;
