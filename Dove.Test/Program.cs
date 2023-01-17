﻿using Dove.Core;
int index = 0;
var result = RootDecl.Declaration.Collection.AsParser(File.ReadAllText("Test.il"), ref index, out var res, out string err);

// TODO : Add MSFT Specific stuff
// TODO : Add TABLES 
// TODO : Make Declaration base check in Source generator check the namespace
// TODO : Optimize the parser it is slow
Console.WriteLine(result  ? $"Success to parse {res.GetType().Name} =>  {res}" : $"Failed to parse {res?.GetType().Name} \n{err}");