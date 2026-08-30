using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LoyaltyClub.Sdk.Tests.Core;

/// <summary>
/// Serwer-atrapa oparty na <see cref="TcpListener"/> — pozwala testowac cala warstwe
/// transportowa bez dodatkowej zaleznosci testowej i bez rezerwacji URL-a w systemie,
/// ktorej wymagalby <c>HttpListener</c> na Windowsie.
/// <para>
/// Odpowiedzi sa kolejkowane w kolejnosci, w jakiej maja zostac zwrocone; kazde
/// przychodzace zadanie jest zapisywane i mozna je odczytac przez <see cref="TakeRequest"/>.
/// </para>
/// </summary>
public sealed class MockApiServer : IDisposable
{
    /// <summary>Zapis pojedynczego zadania, ktore trafilo na serwer.</summary>
    public sealed record RecordedRequest(
        string Method,
        string Path,
        string? Query,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
        string Body)
    {
        public string? Header(string name)
        {
            if (!Headers.TryGetValue(name.ToLowerInvariant(), out IReadOnlyList<string>? values))
            {
                return null;
            }

            return values.Count == 0 ? null : values[0];
        }
    }

    private sealed record QueuedResponse(int Status, string? ContentType, string Body);

    private readonly TcpListener _listener;
    private readonly ConcurrentQueue<QueuedResponse> _responses = new ConcurrentQueue<QueuedResponse>();
    private readonly BlockingCollection<RecordedRequest> _requests = new BlockingCollection<RecordedRequest>();
    private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
    private readonly List<TcpClient> _connections = new List<TcpClient>();
    private readonly Thread _acceptThread;
    private bool _disposed;

    private MockApiServer(TcpListener listener)
    {
        _listener = listener;
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "MockApiServer" };
    }

    public static MockApiServer Start()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        MockApiServer mock = new MockApiServer(listener);
        mock._acceptThread.Start();
        return mock;
    }

    public string BaseUrl() => "http://127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>Kolejkuje odpowiedz JSON.</summary>
    public MockApiServer EnqueueJson(int status, string body)
    {
        _responses.Enqueue(new QueuedResponse(status, "application/json", body));
        return this;
    }

    /// <summary>Kolejkuje odpowiedz RFC 7807.</summary>
    public MockApiServer EnqueueProblem(int status, string body)
    {
        _responses.Enqueue(new QueuedResponse(status, "application/problem+json", body));
        return this;
    }

    /// <summary>Kolejkuje odpowiedz bez ciala.</summary>
    public MockApiServer EnqueueEmpty(int status)
    {
        _responses.Enqueue(new QueuedResponse(status, null, string.Empty));
        return this;
    }

    /// <summary>Zdejmuje najstarsze zapisane zadanie; czeka az do timeoutu, jesli jeszcze nie doszlo.</summary>
    public RecordedRequest TakeRequest()
    {
        if (!_requests.TryTake(out RecordedRequest? request, TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("Zadne zadanie nie dotarlo do serwera-atrapy");
        }

        return request;
    }

    /// <summary>Liczba zadan, ktore dotarly na serwer i nie zostaly jeszcze odczytane.</summary>
    public int ReceivedRequestCount => _requests.Count;

    private void AcceptLoop()
    {
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                TcpClient connection = _listener.AcceptTcpClient();
                lock (_connections)
                {
                    _connections.Add(connection);
                }

                Thread worker = new Thread(() => HandleConnection(connection)) { IsBackground = true };
                worker.Start();
            }
        }
        catch (SocketException)
        {
            // listener zamkniety przy Dispose
        }
        catch (ObjectDisposedException)
        {
            // listener zamkniety przy Dispose
        }
    }

    private void HandleConnection(TcpClient connection)
    {
        try
        {
            using (connection)
            using (NetworkStream stream = connection.GetStream())
            {
                // Klient SDK trzyma polaczenie przy zyciu, wiec na jednym gniezdzie
                // moze przyjsc kilka zadan pod rzad.
                while (!_shutdown.IsCancellationRequested && HandleRequest(stream))
                {
                    // kolejne zadanie na tym samym polaczeniu
                }
            }
        }
        catch (IOException)
        {
            // klient rozlaczyl sie w trakcie
        }
        catch (ObjectDisposedException)
        {
            // polaczenie zamkniete przy Dispose
        }
        catch (InvalidOperationException)
        {
            // strumien zamkniety przy Dispose
        }
    }

    private bool HandleRequest(NetworkStream stream)
    {
        string? requestLine = ReadLine(stream);
        if (string.IsNullOrEmpty(requestLine))
        {
            return false;
        }

        string[] parts = requestLine.Split(' ');
        if (parts.Length < 2)
        {
            return false;
        }

        string method = parts[0];
        string target = parts[1];

        Dictionary<string, IReadOnlyList<string>> headers =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        int contentLength = 0;
        while (true)
        {
            string? headerLine = ReadLine(stream);
            if (string.IsNullOrEmpty(headerLine))
            {
                break;
            }

            int separator = headerLine.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            string name = headerLine.Substring(0, separator).Trim().ToLowerInvariant();
            string value = headerLine.Substring(separator + 1).Trim();
            if (headers.TryGetValue(name, out IReadOnlyList<string>? existing))
            {
                List<string> values = new List<string>(existing) { value };
                headers[name] = values;
            }
            else
            {
                headers[name] = new List<string> { value };
            }

            if (name == "content-length")
            {
                int.TryParse(value, out contentLength);
            }
        }

        string body = contentLength > 0 ? ReadBody(stream, contentLength) : string.Empty;

        int queryStart = target.IndexOf('?');
        string path = queryStart < 0 ? target : target.Substring(0, queryStart);
        string? query = queryStart < 0 ? null : target.Substring(queryStart + 1);

        _requests.Add(new RecordedRequest(method, path, query, headers, body));

        if (!_responses.TryDequeue(out QueuedResponse? response))
        {
            WriteResponse(stream, new QueuedResponse(500, null, string.Empty));
            return true;
        }

        WriteResponse(stream, response);
        return true;
    }

    private static string? ReadLine(NetworkStream stream)
    {
        StringBuilder line = new StringBuilder();
        while (true)
        {
            int next = stream.ReadByte();
            if (next < 0)
            {
                return line.Length == 0 ? null : line.ToString();
            }

            if (next == '\n')
            {
                return line.ToString();
            }

            if (next != '\r')
            {
                line.Append((char)next);
            }
        }
    }

    private static string ReadBody(NetworkStream stream, int contentLength)
    {
        byte[] buffer = new byte[contentLength];
        int read = 0;
        while (read < contentLength)
        {
            int chunk = stream.Read(buffer, read, contentLength - read);
            if (chunk <= 0)
            {
                break;
            }

            read += chunk;
        }

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    private static void WriteResponse(NetworkStream stream, QueuedResponse response)
    {
        byte[] payload = Encoding.UTF8.GetBytes(response.Body);
        StringBuilder head = new StringBuilder();
        head.Append("HTTP/1.1 ").Append(response.Status).Append(' ').Append(ReasonPhrase(response.Status))
            .Append("\r\n");
        if (response.ContentType != null)
        {
            head.Append("Content-Type: ").Append(response.ContentType).Append("\r\n");
        }

        head.Append("Content-Length: ").Append(payload.Length).Append("\r\n");
        head.Append("Connection: keep-alive\r\n\r\n");

        byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
        stream.Write(headBytes, 0, headBytes.Length);
        if (payload.Length > 0)
        {
            stream.Write(payload, 0, payload.Length);
        }

        stream.Flush();
    }

    private static string ReasonPhrase(int status) => status switch
    {
        200 => "OK",
        201 => "Created",
        204 => "No Content",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        408 => "Request Timeout",
        409 => "Conflict",
        425 => "Too Early",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        504 => "Gateway Timeout",
        _ => "Status"
    };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shutdown.Cancel();
        _listener.Stop();

        lock (_connections)
        {
            foreach (TcpClient connection in _connections)
            {
                try
                {
                    connection.Close();
                }
                catch (SocketException)
                {
                    // polaczenie juz zerwane
                }
            }

            _connections.Clear();
        }

        _requests.Dispose();
        _shutdown.Dispose();
    }
}
