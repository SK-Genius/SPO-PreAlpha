using System.Text;

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);

var server = new LspServer(
	Console.OpenStandardInput(),
	Console.OpenStandardOutput(),
	new SpoLanguageService()
);

return server.Run();
