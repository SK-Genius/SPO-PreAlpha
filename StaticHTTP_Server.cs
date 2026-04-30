#:property EnableSourceControlManagerQueries=false

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

var rootPath = Path.GetFullPath(Directory.GetCurrentDirectory());
var rootPathWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar)
	? rootPath
	: rootPath + Path.DirectorySeparatorChar;
var suppressBrowserLaunch = IsTruthyEnvironmentVariable("STATIC_HTTP_SERVER_NO_BROWSER");

using var listener = CreateListener(out var baseUrl);
using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
	eventArgs.Cancel = true;
	shutdown.Cancel();
	listener.Stop();
};

Console.WriteLine($"Serving: {rootPath}");
Console.WriteLine($"URL: {baseUrl}");
Console.WriteLine("Press Ctrl+C to stop.");

var startUrl = new Uri(new Uri(baseUrl), "index.html").ToString();
if (!suppressBrowserLaunch)
{
	TryOpenBrowser(startUrl);
}

try
{
	while (!shutdown.IsCancellationRequested)
	{
		HttpListenerContext context;

		try
		{
			context = await listener.GetContextAsync();
		}
		catch (HttpListenerException) when (shutdown.IsCancellationRequested)
		{
			break;
		}
		catch (ObjectDisposedException) when (shutdown.IsCancellationRequested)
		{
			break;
		}

		_ = Task.Run(() => HandleRequestAsync(context, rootPath, rootPathWithSeparator, shutdown.Token));
	}
}
finally
{
	listener.Close();
}

static HttpListener CreateListener(out string baseUrl)
{
	var triedPorts = new HashSet<int>();

	foreach (var port in CandidatePorts())
	{
		if (!triedPorts.Add(port))
		{
			continue;
		}

		var prefix = $"http://127.0.0.1:{port}/";
		var listener = new HttpListener();
		listener.Prefixes.Add(prefix);

		try
		{
			listener.Start();
			baseUrl = prefix;
			return listener;
		}
		catch (HttpListenerException)
		{
			listener.Close();
		}
	}

	throw new InvalidOperationException("Could not start a local HTTP listener.");
}

static IEnumerable<int> CandidatePorts()
{
	yield return 8080;

	for (var port = 8081; port <= 8090; port++)
	{
		yield return port;
	}

	yield return GetFreePort();
}

static int GetFreePort()
{
	using var probe = new TcpListener(IPAddress.Loopback, 0);
	probe.Start();
	return ((IPEndPoint)probe.LocalEndpoint).Port;
}

static async Task HandleRequestAsync(
	HttpListenerContext context,
	string rootPath,
	string rootPathWithSeparator,
	CancellationToken cancellationToken)
{
	try
	{
		var request = context.Request;
		var response = context.Response;

		if (request.HttpMethod is not ("GET" or "HEAD"))
		{
			response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
			response.Headers["Allow"] = "GET, HEAD";
			response.Close();
			return;
		}

		var requestPath = Uri.UnescapeDataString(request.Url?.AbsolutePath ?? "/");
		var relativePath = requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
		var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));

		if (!IsInsideRoot(fullPath, rootPath, rootPathWithSeparator))
		{
			await WriteTextResponseAsync(response, HttpStatusCode.Forbidden, "Forbidden", cancellationToken);
			return;
		}

		if (Directory.Exists(fullPath))
		{
			fullPath = Path.Combine(fullPath, "index.html");
		}

		if (!File.Exists(fullPath))
		{
			await WriteTextResponseAsync(response, HttpStatusCode.NotFound, "Not Found", cancellationToken);
			return;
		}

		var fileInfo = new FileInfo(fullPath);
		response.StatusCode = (int)HttpStatusCode.OK;
		response.ContentType = GetContentType(fullPath);
		response.ContentLength64 = fileInfo.Length;

		if (request.HttpMethod == "HEAD")
		{
			response.Close();
			return;
		}

		await using var fileStream = File.OpenRead(fullPath);
		await fileStream.CopyToAsync(response.OutputStream, cancellationToken);
		response.Close();
	}
	catch (OperationCanceledException)
	{
		context.Response.Close();
	}
	catch (Exception exception)
	{
		try
		{
			await WriteTextResponseAsync(
				context.Response,
				HttpStatusCode.InternalServerError,
				$"Internal Server Error{Environment.NewLine}{exception.Message}",
				cancellationToken);
		}
		catch
		{
			context.Response.Close();
		}
	}
}

static bool IsInsideRoot(string fullPath, string rootPath, string rootPathWithSeparator)
{
	return string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase)
		|| fullPath.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase);
}

static async Task WriteTextResponseAsync(
	HttpListenerResponse response,
	HttpStatusCode statusCode,
	string content,
	CancellationToken cancellationToken)
{
	response.StatusCode = (int)statusCode;
	response.ContentType = "text/plain; charset=utf-8";

	var payload = System.Text.Encoding.UTF8.GetBytes(content);
	response.ContentLength64 = payload.Length;
	await response.OutputStream.WriteAsync(payload, cancellationToken);
	response.Close();
}

static void TryOpenBrowser(string url)
{
	try
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true,
		});
	}
	catch
	{
		Console.WriteLine($"Open this URL in your browser: {url}");
	}
}

static bool IsTruthyEnvironmentVariable(string variableName)
{
	var value = Environment.GetEnvironmentVariable(variableName);

	return value is not null
		&& (value.Equals("1", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("true", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("yes", StringComparison.OrdinalIgnoreCase));
}

static string GetContentType(string path)
{
	return Path.GetExtension(path).ToLowerInvariant() switch
	{
		".css" => "text/css; charset=utf-8",
		".gif" => "image/gif",
		".htm" => "text/html; charset=utf-8",
		".html" => "text/html; charset=utf-8",
		".ico" => "image/x-icon",
		".jpeg" => "image/jpeg",
		".jpg" => "image/jpeg",
		".js" => "text/javascript; charset=utf-8",
		".json" => "application/json; charset=utf-8",
		".map" => "application/json; charset=utf-8",
		".mp3" => "audio/mpeg",
		".mp4" => "video/mp4",
		".otf" => "font/otf",
		".pdf" => "application/pdf",
		".png" => "image/png",
		".svg" => "image/svg+xml",
		".txt" => "text/plain; charset=utf-8",
		".ttf" => "font/ttf",
		".wasm" => "application/wasm",
		".webp" => "image/webp",
		".woff" => "font/woff",
		".woff2" => "font/woff2",
		".xml" => "application/xml; charset=utf-8",
		_ => "application/octet-stream",
	};
}
