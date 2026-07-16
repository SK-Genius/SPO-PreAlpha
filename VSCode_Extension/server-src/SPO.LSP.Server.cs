#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives = true
#:property OutputType = Exe
#:property AllowUnsafeBlocks = true
#:property PublishAot=false
#:property OutputPath = ../server/
#:ref _GlobalUsings.cs
#:ref LspServer.cs
#:ref mSPO_Diagnostics.cs
#:ref mSPO_Navigation.cs
#:ref SpoLanguageService.cs


using System.Text;

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);

var server = new LspServer(
	Console.OpenStandardInput(),
	Console.OpenStandardOutput(),
	new SpoLanguageService()
);

return server.Run();
