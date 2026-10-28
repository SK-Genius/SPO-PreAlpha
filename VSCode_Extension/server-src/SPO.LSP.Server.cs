#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives = true
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property OutputType = Exe
#:property AllowUnsafeBlocks = true
#:property PublishAot=false
#:include _GlobalUsings.cs
#:include LspServer.cs
#:include mSPO_Diagnostics.cs
#:include mSPO_Navigation.cs
#:include SpoLanguageService.cs

using System.Text;

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);
var ExitCode = 1;
// The unoptimized recursive compiler needs more than the Windows main-thread stack.
var ServerThread = new System.Threading.Thread(
	() => {
		var Server = new LspServer(
			Console.OpenStandardInput(),
			Console.OpenStandardOutput(),
			new SpoLanguageService()
		);
		ExitCode = Server.Run();
	},
	maxStackSize: 32 * 1024 * 1024
);
ServerThread.Start();
ServerThread.Join();
return ExitCode;