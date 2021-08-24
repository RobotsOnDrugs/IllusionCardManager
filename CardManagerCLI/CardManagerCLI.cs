﻿using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Security;

using IllusionCards.AI.Cards;
using IllusionCards.AI.ExtendedData.PluginData;
using IllusionCards.Cards;

using NLog;
using NLog.Config;
using NLog.Targets;

using static IllusionCards.CardUtils;

namespace IllusionCards
{
	class CardManagerCLI
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


		static void Main(string[] args)
		{

			LoggingConfiguration _logconfig = LogManager.Configuration;
			if (_logconfig.LoggingRules.Count != 0)
			{
#if DEBUG
				const string _debugLayout = "[${longdate}][${logger}][${callsite-fileName:includeSourcePath=false}:${callsite-linenumber}] ${level:uppercase=true}: ${message}";
				_logconfig.FindTargetByName<ColoredConsoleTarget>("logcolors").Layout = _debugLayout;
				_logconfig.FindRuleByName("console").SetLoggingLevels(LogLevel.Debug, LogLevel.Fatal);
#else
				_logconfig.FindRuleByName("console").SetLoggingLevels(LogLevel.Warn, LogLevel.Fatal);
#endif
			}
			LogManager.Configuration = _logconfig;


			List<FileInfo> CardFiles = new();
			Option<string> cardsOption = new("--cards", "Cards to process") { Arity = ArgumentArity.OneOrMore };
			Option<string> cardListOption = new("--card-list", "Cards to process") { Arity = ArgumentArity.ExactlyOne };
			Option<string> gameDirectoryOption = new("--game-dir", "Cards to process") { Arity = ArgumentArity.ExactlyOne };
			RootCommand RootCommand = new() { cardsOption, cardListOption, gameDirectoryOption };
			RootCommand.Description = "Illusion Card CLI Utility";
			RootCommand.Handler = CommandHandler.Create<string[], string, string>((cards, cardList, gameDir) =>
			{
				if (cards.Length != 0)
				{
					Logger.Info("Adding cards from the command line.");
					bool _successArgList = QueueCardsFromArgList(cards, ref CardFiles);
					// if (!_success) { return; }
				}
				if (cardList.Length != 0)
				{
					Logger.Info("Adding cards from the text file.");
					bool _successFileList = QueueCardsFromFile(cardList, ref CardFiles);
					// if (!_success) { return; }
				}
				if (gameDir.Length != 0)
				{
					Logger.Info("Adding cards from the game directory.");
					bool _successGameDir = QueueCardsFromGameDir(gameDir, ref CardFiles);
					// if (!_success) { return; }
				}
			});
			RootCommand.Invoke(args);
			HashSet<IllusionCard> Cards = new();
			HashSet<string> UnknownPlugins = new();
			IllusionCard _card;
			//int _i = 0;
			foreach (FileInfo CardFile in CardFiles)
			{
				Logger.Info("Processing {cardfile:l}.", CardFile.Name);
				try { _card = GetIllusionCardFromFile(CardFile); }
				catch (UnsupportedCardException ex) { Logger.Error(ex, "Could not parse card: {card}: {reason:l}", ex.CardPath, ex.Message); continue; }
				_ = Cards.Add(_card);
				//if (_i < 1)
				//{
				//	using FileStream _fstream = new("dump.json", FileMode.Create, FileAccess.Write, FileShare.None);
				//	using StreamWriter _swriter = new(_fstream);
				//	//_swriter.Write(ObjectDumper.Dump(((AiCharaCard)_card).Custom, new DumpOptions() { DumpStyle = DumpStyle.Console }));
				//	_i++;
				//}
				if (_card.GetType() == typeof(AiCharaCard))
				{
					ImmutableHashSet<AiPluginData>? _extendedData = ((AiCharaCard)_card).Chara.ExtendedData;
					if (_extendedData is null)
						continue;
					foreach (AiPluginData pluginData in _extendedData)
					{
						if (pluginData.PluginDataInfo?.GetType() == typeof(UnknownPluginData))
							_ = UnknownPlugins.Add(pluginData.DataKey);
					}
				}
			}
			foreach (string unknownPlugin in UnknownPlugins)
			{
				Logger.Warn(unknownPlugin);
			}
			NLog.LogManager.Shutdown();
		}

		private static bool QueueCardsFromArgList(string[] args, ref List<FileInfo> cards)
		{
			bool _success = true;
			foreach (string cardPath in args)
			{
				TestFileAccess(cardPath, out FileInfo? _card);
				if (_card is null) { _success = false; break; }
				Logger.Debug("Added {cardPath} to list of cards.", _card.FullName);
				cards.Add(_card);
			}
			return _success;
		}

		private static bool QueueCardsFromFile(string cardList, ref List<FileInfo> cards)
		{
			bool _success = true;
			TestFileAccess(cardList, out FileInfo? _cardListFile);
			if (_cardListFile is null) { return false; }
			using StreamReader _sr = new(cardList);
			string? _line;
			while ((_line = _sr.ReadLine()) is not null)
			{
				TestFileAccess(_line, out FileInfo? _cardFile);
				if (_cardFile is null) { return false; }
				Logger.Debug("Added {cardPath} to list of cards.", _line);
				cards.Add(_cardFile);
			}

			return _success;
		}
		private static bool QueueCardsFromGameDir(string dirPath, ref List<FileInfo> cards)
		{
			bool _success = true;
			string _cardsPath = Path.Join(dirPath, "UserData", "chara");
			if (!Directory.Exists(_cardsPath)) { return false; }
			IEnumerable<string> _potentialCardFiles = Directory.EnumerateFiles(_cardsPath, "*.png", enumerationOptions: new() { AttributesToSkip = FileAttributes.Device | FileAttributes.System, RecurseSubdirectories = true, IgnoreInaccessible = true });
			foreach (string potentialCardFile in _potentialCardFiles)
			{
				Logger.Debug("Added {cardPath} to list of cards.", potentialCardFile);
				cards.Add(new FileInfo(potentialCardFile));
			}
			return _success;
		}

		private static void TestFileAccess(string filePath, out FileInfo? fileInfo)
		{
			FileInfo? _fileInfo = default;
			try
			{
				if (!File.Exists(filePath)) { throw new FileNotFoundException(); }
				_fileInfo = new(filePath);
			}
			catch (Exception ex) when (ex is SecurityException
				|| ex is UnauthorizedAccessException
				|| ex is NotSupportedException
				|| ex is FileNotFoundException)
			{ Logger.Error(ex, "{filePath} could not be accessed: {reason:l}", filePath, ex.Message); }
			fileInfo = _fileInfo;
		}
	}
}
