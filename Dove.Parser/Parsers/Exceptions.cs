using LabelDecl;

using MethodDecl;
using TypeDecl;
using static Core;
using static ExtraTools.Extensions;

namespace ExceptionDecl;
public record StructuralExceptionBlock(TryClause TryBlock, WireBlock.Collection Clauses) : IDeclaration<StructuralExceptionBlock>
{
    public override string ToString() => $"{TryBlock} {Clauses}";
    public static Parser<StructuralExceptionBlock> AsParser => RunAll(
        converter: parts => new StructuralExceptionBlock(parts[0].TryBlock, parts[1].Clauses),
        Map(
            converter: TryClause => new StructuralExceptionBlock(TryClause, null),
            TryClause.AsParser
        ),
        Map(
            converter: clauses => new StructuralExceptionBlock(null, clauses),
            WireBlock.Collection.AsParser
        )
    );
}

[GenerateParser]
public partial record WireBlock : IDeclaration<WireBlock>
{
    public record Collection(ARRAY<WireBlock> Blocks) : IDeclaration<WireBlock>
    {
        public override string ToString() => Blocks.ToString('\n');
        public static Parser<Collection> AsParser => Map(
            converter: blocks => new Collection(blocks),
            ARRAY<WireBlock>.MakeParser(new ARRAY<WireBlock>.ArrayOptions
            {
                Delimiters = ('\0', '\0', '\0')
            })
        );
    }
}

public record CatchBlock(TypeReference Type, HandlerBlock Block) : WireBlock, IDeclaration<CatchBlock>
{
    public override string ToString() => $"catch {Type} {Block}";
    public static Parser<CatchBlock> AsParser => RunAll(
        converter: parts => new CatchBlock(parts[1].Type, parts[2].Block),
        Discard<CatchBlock, string>(ConsumeWord(Core.Id, "catch")),
        Map(
            converter: type => new CatchBlock(type, null),
            TypeReference.AsParser
        ),
        Map(
            converter: block => new CatchBlock(null, block),
            HandlerBlock.AsParser
        )
    );
}

public record FaultBlock(HandlerBlock Block) : WireBlock, IDeclaration<FaultBlock>
{
    public override string ToString() => $"fault {Block}";
    public static Parser<FaultBlock> AsParser => RunAll(
        converter: parts => new FaultBlock(parts[1].Block),
        Discard<FaultBlock, string>(ConsumeWord(Core.Id, "fault")),
        Map(
            converter: block => new FaultBlock(block),
            HandlerBlock.AsParser
        )
    );
}

public record FilterBlock(LabelOrOffset.Collection Labels, HandlerBlock Block) : WireBlock, IDeclaration<FilterBlock>
{
    public override string ToString() => $"filter {Labels} {Block}";
    public static Parser<FilterBlock> AsParser => RunAll(
        converter: parts => new FilterBlock(parts[1].Labels, parts[2].Block),
        Discard<FilterBlock, string>(ConsumeWord(Core.Id, "filter")),
        Map(
            converter: label => new FilterBlock(label, null),
            LabelOrOffset.Collection.AsParser
        ),
        Map(
            converter: block => new FilterBlock(null, block),
            HandlerBlock.AsParser
        )
    );
}

public record FinallyBlock(HandlerBlock Block) : WireBlock, IDeclaration<FinallyBlock>
{
    public override string ToString() => $"finally {Block}";
    public static Parser<FinallyBlock> AsParser => RunAll(
        converter: parts => new FinallyBlock(parts[1].Block),
        Discard<FinallyBlock, string>(ConsumeWord(Core.Id, "finally")),
        Map(
            converter: block => new FinallyBlock(block),
            HandlerBlock.AsParser
        )
    );
}

public partial record TryClause(HandlerBlock Block) : IDeclaration<TryClause>
{
    public override string ToString() => $".try {Block}";
    public static Parser<TryClause> AsParser => RunAll(
        converter: parts => new TryClause(parts[1]),
        Discard<HandlerBlock, string>(ConsumeWord(Core.Id, ".try")),
        HandlerBlock.AsParser
    );
}

[GenerateParser]
public partial record HandlerBlock : IDeclaration<HandlerBlock>;
public record HandlerWithLabel(LabelOrOffset.Collection From, LabelOrOffset.Collection To) : HandlerBlock, IDeclaration<HandlerWithLabel>
{
    public override string ToString() => $"handler {From} to {To}";
    public static Parser<HandlerWithLabel> AsParser => RunAll(
        converter: parts => new HandlerWithLabel(parts[1].From, parts[3].To),
        Discard<HandlerWithLabel, string>(ConsumeWord(Core.Id, "handler")),
        Map(
            converter: from => new HandlerWithLabel(from, null),
            LabelOrOffset.Collection.AsParser
        ),
        Discard<HandlerWithLabel, string>(ConsumeWord(Core.Id, "to")),
        Map(
            converter: to => new HandlerWithLabel(null, to),
            LabelOrOffset.Collection.AsParser
        )
    );
}

[WrapParser<ScopeBlock>] public partial record HandlerWithScope : HandlerBlock, IDeclaration<HandlerWithScope>;