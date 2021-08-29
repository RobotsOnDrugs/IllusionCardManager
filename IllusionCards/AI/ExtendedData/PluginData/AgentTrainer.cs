﻿using System.Collections.Immutable;

using MessagePack;

namespace IllusionCards.AI.ExtendedData.PluginData
{
	public record AgentTrainerData : ExtendedPluginData
	{
		public const string DataKey = DefinitionMetadata.DataKey;
		private readonly struct DefinitionMetadata
		{
			internal const string PluginGUID = "com.fairbair.agenttrainer";
			internal const string DataKey = "AgentTrainer.StatsController";
			internal readonly Version PluginVersion = new("1.2.0");
			internal const string RepoURL = "https://github.com/IllusionMods/KK_Plugins";
			internal const string ClassDefinitionsURL = "https://github.com/IllusionMods/KK_Plugins/blob/master/src/EyeControl.Core/EyeControl.CharaController.cs";
			internal const string License = "GPL 3.0";
		}
		public override Type DataType { get; } = typeof(AgentTrainerOptions);
		// The integer indices of these have meaning for "AgentActors" in the game, but it seems to be very specific to AI
		public readonly struct AgentTrainerOptions
		{
			public ImmutableDictionary<int, float> LockedStats { get; init; }
			public ImmutableDictionary<int, float> LockedDesires { get; init; }
			public ImmutableDictionary<int, int> LockedFlavors { get; init; }
		}
		public AgentTrainerOptions Data { get; }
		public AgentTrainerData(int version, Dictionary<object, object> dataDict) : base(version, dataDict)
		{
			MessagePackSerializerOptions _lz4Option = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);
			Data = new AgentTrainerOptions()
			{
				LockedStats = dataDict.TryGetValue((object)"lockedStats", out object? _rawData) && _rawData is not null ?
					MessagePackSerializer.Deserialize<ImmutableDictionary<int, float>>((byte[])_rawData, _lz4Option) : ImmutableDictionary<int, float>.Empty,
				LockedDesires = dataDict.TryGetValue((object)"lockedDesires", out _rawData) && _rawData is not null ?
					MessagePackSerializer.Deserialize<ImmutableDictionary<int, float>>((byte[])_rawData, _lz4Option) : ImmutableDictionary<int, float>.Empty,
				LockedFlavors = dataDict.TryGetValue((object)"lockedFlavors", out _rawData) && _rawData is not null ?
					MessagePackSerializer.Deserialize<ImmutableDictionary<int, int>>((byte[])_rawData, _lz4Option) : ImmutableDictionary<int, int>.Empty
			};
		}
	}
}