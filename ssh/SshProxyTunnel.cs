// SshProxyTunnel.exe - tunnel HTTP CONNECT + pompage bidirectionnelle stdin/stdout
// Usage : SshProxyTunnel.exe <proxyHost> <proxyPort> <proxyUser> <proxyPass> <targetHost> <targetPort>
// Compile : csc /nologo /optimize /target:exe /out:SshProxyTunnel.exe SshProxyTunnel.cs
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class SshProxyTunnel
{
    static int Main(string[] args)
    {
        if (args.Length != 6)
        {
            Console.Error.WriteLine("Usage: SshProxyTunnel.exe <proxyHost> <proxyPort> <proxyUser> <proxyPass> <targetHost> <targetPort>");
            return 2;
        }

        string proxyHost = args[0];
        int proxyPort = int.Parse(args[1]);
        string proxyUser = args[2];
        string proxyPass = args[3];
        string targetHost = args[4];
        int targetPort = int.Parse(args[5]);

        try
        {
            using (var tcp = new TcpClient())
            {
                tcp.Connect(proxyHost, proxyPort);
                tcp.NoDelay = true;
                var stream = tcp.GetStream();
                var enc = Encoding.ASCII;

                string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(proxyUser + ":" + proxyPass));
                string req = "CONNECT " + targetHost + ":" + targetPort + " HTTP/1.1\r\n" +
                             "Host: " + targetHost + ":" + targetPort + "\r\n" +
                             "Proxy-Connection: keep-alive\r\n" +
                             "Proxy-Authorization: Basic " + basic + "\r\n" +
                             "\r\n";
                byte[] reqBytes = enc.GetBytes(req);
                stream.Write(reqBytes, 0, reqBytes.Length);
                stream.Flush();

                // Lecture de la reponse HTTP octet par octet pour ne pas
                // pre-consommer les donnees du tunnel.
                var resp = new StringBuilder(512);
                while (!resp.ToString().EndsWith("\r\n\r\n"))
                {
                    int b = stream.ReadByte();
                    if (b < 0)
                    {
                        Console.Error.WriteLine("Proxy " + proxyHost + ":" + proxyPort + " : connexion fermee pendant la lecture de la reponse CONNECT");
                        return 1;
                    }
                    resp.Append((char)b);
                }

                string statusLine = resp.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None)[0];
                string code = statusLine.Split(' ')[1];
                if (code != "200")
                {
                    Console.Error.WriteLine("Proxy a refuse le tunnel vers " + targetHost + ":" + targetPort + " : " + statusLine);
                    return 1;
                }

                var stdin = Console.OpenStandardInput();
                var stdout = Console.OpenStandardOutput();

                // stdin -> cible
                var toTarget = new Thread(() =>
                {
                    var buf = new byte[16384];
                    try
                    {
                        while (true)
                        {
                            int n = stdin.Read(buf, 0, buf.Length);
                            if (n <= 0) break;
                            stream.Write(buf, 0, n);
                            stream.Flush();
                        }
                        try { tcp.Client.Shutdown(SocketShutdown.Send); } catch { }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Pompage stdin->cible : " + ex.Message);
                    }
                });

                // cible -> stdout
                var fromTarget = new Thread(() =>
                {
                    var buf = new byte[16384];
                    try
                    {
                        while (true)
                        {
                            int n = stream.Read(buf, 0, buf.Length);
                            if (n <= 0) break;
                            stdout.Write(buf, 0, n);
                            stdout.Flush();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Pompage cible->stdout : " + ex.Message);
                    }
                });

                toTarget.IsBackground = true;
                fromTarget.IsBackground = true;
                toTarget.Start();
                fromTarget.Start();
                fromTarget.Join();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Erreur tunnel : " + ex.Message);
            return 1;
        }
        return 0;
    }
}
