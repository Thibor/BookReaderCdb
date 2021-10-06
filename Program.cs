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
		static void Main(string[] args)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			bool getMove = true;
			List<string> movesEng = new List<string>();
			CChess Chess = new CChess();
			CUci Uci = new CUci();
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
					return "";
				}
				string msg = Encoding.UTF8.GetString(data);
				string[] tokens = msg.Split(':');
				if (tokens.Length > 1) {
					string umo = tokens[1].Substring(0,4);
					if (Chess.IsValidMove(umo, out _))
						return umo;
				}
				return "";
			}

			do{
				string msg = Console.ReadLine();
				Uci.SetMsg(msg);
				if ((Uci.command != "go") && (engineFile != ""))
					engineProcess.StandardInput.WriteLine(msg);
				switch (Uci.command)
				{
					case "ucinewgame":
						getMove = true;
						break;
					case "position":
						string fen = Uci.GetValue("fen", "moves");
						string moves = Uci.GetValue("moves","fen");
						Chess.SetFen(fen);
						Chess.MakeMoves(moves);
						break;
					case "go":
						string move = "";
						if (getMove)
						{
							move = GetMove(Chess.GetFen());
							getMove = move != "";
						}
						if (getMove)
							Console.WriteLine($"bestmove {move}");
						else if (engineFile == "")
							Console.WriteLine("enginemove");
						else
							engineProcess.StandardInput.WriteLine(msg);
						break;
				}
			} while (Uci.command != "quit");

		}
	}
}
