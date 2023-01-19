using static Core;


namespace IdentifierDecl;

[GenerateParser] public partial record Identifier : IDeclaration<Identifier> {
    public static string[] reservedWords = { "#line","addon","assembly","cctor","class","corflags","ctor","custom","data","emitbyte","entrypoint","event","export","field","file","fire","get","hash","imagebase","import","language","line","locale","localized","locals","manifestres","maxstack","method","module","mresource","namespace","other","override","pack","param","pdirect","permission","permissionset","property","publickey","publickeytoken","removeon","set","size","subsystem","try","ver","vtable","vtentry","vtfixup","zeroinit","^THE_END^","abstract","add","add.ovf","add.ovf.un","algorithm","alignment","and","ansi","any","arglist","array","as","assembly","assert","at","auto","autochar","beforefieldinit","beq","beq.s","bge","bge.s","bge.un","bge.un.s","bgt","bgt.s","bgt.un","bgt.un.s","ble","ble.s","ble.un","ble.un.s","blob","blob_object","blt","blt.s","blt.un","blt.un.s","bne.un","bne.un.s","bool","box","br","br.s","break","brfalse","brfalse.s","brinst","brinst.s","brnull","brnull.s","brtrue","brtrue.s","brzero","brzero.s","bstr","bytearray","byvalstr","call","calli","callmostderived","callvirt","carray","castclass","catch","cdecl","ceq","cf","cgt","cgt.un","char","cil","ckfinite","class", "valuetype","clsid","clt","clt.un","const","constrained.","conv.i","conv.i1","conv.i2","conv.i4","conv.i8","conv.ovf.i","conv.ovf.i.un","conv.ovf.i1","conv.ovf.i1.un","conv.ovf.i2","conv.ovf.i2.un","conv.ovf.i4","conv.ovf.i4.un","conv.ovf.i8","conv.ovf.i8.un","conv.ovf.u","conv.ovf.u.un","conv.ovf.u1","conv.ovf.u1.un","conv.ovf.u2","conv.ovf.u2.un","conv.ovf.u4","conv.ovf.u4.un","conv.ovf.u8","conv.ovf.u8.un","conv.r.un","conv.r4","conv.r8","conv.u","conv.u1","conv.u2","conv.u4","conv.u8","cpblk","cpobj","currency","custom","date","decimal","default","default","demand","deny","div","div.un","dup","endfault","endfilter","endfinally","endmac","enum","error","explicit","extends","extern","false","famandassem","family","famorassem","fastcall","fastcall","fault","field","filetime","filter","final","finally","fixed","float","float32","float64","forwardref","fromunmanaged","handler","hidebysig","hresult","idispatch","il","illegal","implements","implicitcom","implicitres","import","in","inheritcheck","init","initblk","initobj","initonly","instance","int","int16","int32","int64","int8","interface","internalcall","isinst","iunknown","jmp","lasterr","lcid","ldarg","ldarg.0","ldarg.1","ldarg.2","ldarg.3","ldarg.s","ldarga","ldarga.s","ldc.i4","ldc.i4.0","ldc.i4.1","ldc.i4.2","ldc.i4.3","ldc.i4.4","ldc.i4.5","ldc.i4.6","ldc.i4.7","ldc.i4.8","ldc.i4.M1","ldc.i4.m1","ldc.i4.s","ldc.i8","ldc.r4","ldc.r8","ldelem","ldelem.i","ldelem.i1","ldelem.i2","ldelem.i4","ldelem.i8","ldelem.r4","ldelem.r8","ldelem.ref","ldelem.u1","ldelem.u2","ldelem.u4","ldelem.u8","ldelema","ldfld","ldflda","ldftn","ldind.i","ldind.i1","ldind.i2","ldind.i4","ldind.i8","ldind.r4","ldind.r8","ldind.ref","ldind.u1","ldind.u2","ldind.u4","ldind.u8","ldlen","ldloc","ldloc.0","ldloc.1","ldloc.2","ldloc.3" };
}

[GenerationOrderParser(Order.Middle)]
public record DottedName(ARRAY<SimpleName> Values) : Identifier, IDeclaration<DottedName>
{
    public override string ToString() => Values.ToString("");
    public static Parser<DottedName> AsParser => Map(
        converter: Ids => new DottedName(Ids),
        ARRAY<SimpleName>.MakeParser(new ARRAY<SimpleName>.ArrayOptions
        {
            Delimiters = ('\0', '.', '\0'),
            MinLength = 1,
            AllowEmpty = false
        })
    );
}

[GenerationOrderParser(Order.First)]
public record SlashedName(ARRAY<DottedName> Values) : Identifier, IDeclaration<SlashedName>
{
    public override string ToString() => Values.ToString("/");
    public static Parser<SlashedName> AsParser => Map(
        converter: Ids => new SlashedName(Ids),
        ARRAY<DottedName>.MakeParser(new ARRAY<DottedName>.ArrayOptions
        {
            Delimiters = ('\0', '/', '\0'),
            MinLength = 1,
            AllowEmpty = false
        })
    );
}

[GenerationOrderParser(Order.Last)]
public record SimpleName(string Value) : Identifier, IDeclaration<SimpleName>
{
    public override string ToString() => Value;
    public static Parser<SimpleName> AsParser => TryRun(
        converter: (vals) => new SimpleName(vals),
        Map((id) => id.ToString(), ID.AsParser),
        Map((qstring) => qstring.ToString(), QSTRING.AsParser)
    );
}