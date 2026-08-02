#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives = true
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property OutputType = Exe
#:property AllowUnsafeBlocks = true
#:property PublishAot=false
#:property OutputPath = ../server/
#:include _GlobalUsings.cs
#:include LspServer.cs
#:include mSPO_Diagnostics.cs
#:include mSPO_Navigation.cs
#:include SpoLanguageService.cs


using System.Text;

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);

var server = new LspServer(
	Console.OpenStandardInput(),
	Console.OpenStandardOutput(),
	new SpoLanguageService()
);

return server.Run();
