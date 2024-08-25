using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using NSUci;
using NSChess;

namespace BookReaderCdb
{
	class Program
	{
        public static int bookLimit = 0;

        static void Main(string[] args)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			bool getMove = true;
			List<string> movesEng = new List<string>();
			CChess chess = new CChess();
			CUci uci = new CUci();
			string ax = "-ef";
			List<string> listEf = new List<string>();
			List<string> listEa = new List<string>();
			for (int n = 0; n < args.Length; n++)
			{
				string ac = args[n];
				switch (ac)
				{
					case "-ef":
					case "-ea":
						ax = ac;
						break;
					default:
						switch (ax)
						{
							case "-ef":
								listEf.Add(ac);
								break;
							case "-ea":
								listEa.Add(ac);
								break;
						}
						break;
				}
			}
			string script = "http://www.chessdb.cn/cdb.php";
			string engineFile = String.Join(" ", listEf);
			string arguments = String.Join(" ", listEa);
			Process engineProcess = null;
			if (File.Exists(engineFile))
			{
				engineProcess = new Process();
				engineProcess.StartInfo.FileName = engineFile;
				engineProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineFile);
				engineProcess.StartInfo.UseShellExecute = false;
				engineProcess.StartInfo.RedirectStandardInput = true;
				engineProcess.StartInfo.Arguments = arguments;
				engineProcess.Start();
				Console.WriteLine($"info string engine on");
			}
			else
			{
				if (engineFile != String.Empty)
					Console.WriteLine($"info string missing engine  [{engineFile}]");
				engineFile = String.Empty;
			}
			if (script == "")
			{
				Console.WriteLine("info string missing script");
				return;
			}

			string GetMove(string fen)
			{
				var col = new NameValueCollection
				{
					{ "action","querybest" },
					{ "board", fen}
				};
				byte[] data;
				try
				{
					data = new WebClient().UploadValues(@script, col);
				}
				catch
				{
					return string.Empty;
				}
				string msg = Encoding.UTF8.GetString(data);
				string[] tokens = msg.Split(':');
				if (tokens.Length > 1) {
					string umo = tokens[1].Substring(0,4);
					if (chess.IsValidMove(umo, out _))
						return umo;
				}
				return string.Empty;
			}

			do{
				string msg = Console.ReadLine();
				uci.SetMsg(msg);
                if (uci.command == "book")
                {
                    switch (uci.tokens[1])
                    {
                        case "isready":
                            Console.WriteLine("book readyok");
                            break;
                        case "getoption":
                            Console.WriteLine($"option name limit type spin default {bookLimit} min 0 max 100");
                            Console.WriteLine("optionend");
                            break;
                        case "setoption":
                            switch (uci.GetValue("name", "value").ToLower())
                            {
                                case "limit":
                                    bookLimit = uci.GetInt("value");
                                    break;
                            }
                            break;
                        default:
                            Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
                            break;
                    }
                    continue;
                }
                if ((uci.command != "go") && (engineFile != ""))
					engineProcess.StandardInput.WriteLine(msg);
				switch (uci.command)
				{
					case "ucinewgame":
						getMove = true;
						break;
					case "position":
						string fen = uci.GetValue("fen", "moves");
						string moves = uci.GetValue("moves","fen");
						chess.SetFen(fen);
						chess.MakeMoves(moves);
						break;
					case "go":
						string move = string.Empty;
						if ((bookLimit>=0)&&((chess.g_moveNumber >> 1) >= bookLimit))
							getMove = false;
						if (getMove)
						{
							move = GetMove(chess.GetFen());
							getMove = move != string.Empty;
						}
						if (getMove)
							Console.WriteLine($"bestmove {move}");
						else if (engineFile == string.Empty)
							Console.WriteLine("enginemove");
						else
							engineProcess.StandardInput.WriteLine(msg);
						break;
				}
			} while (uci.command != "quit");

		}
	}
}
