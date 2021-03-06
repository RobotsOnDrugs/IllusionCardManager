//using System.Runtime.CompilerServices;

//[assembly: InternalsVisibleTo("MessagePack"), InternalsVisibleTo("CardManagerCLI")]
namespace IllusionCards.AI.Chara;

public readonly record struct AiChara : IIllusionChara
{
	//public static AiChara Tsubomi { get; }
	//public static AiChara Hero { get; }
	//public static AiChara HatanoMiwa { get; }
	//public static AiRawParameter2Data HS2NewChara { get; }
	//public static AiRawGameInfo2Data HS2NewGameData { get; }

	//internal AiChara DefaultChara => Sex is CharaSex.Male ? Hero : Tsubomi;

	public CharaSex Sex => CharaInfo.Sex;
	public string Name => CharaInfo.Name;

	public Version LoadVersion { get; init; }
	public int Language { get; init; }
	public string UserID { get; init; }
	public string DataID { get; init; }
	internal AiRawCustomData Custom { get; init; }
	internal AiRawCoordinateData Coordinate { get; init; }
	internal AiRawParameterData Parameter { get; init; }
	internal AiRawParameter2Data? Parameter2 { get; init; } = null;// = HS2NewChara;
	internal AiRawGameInfoData GameInfo { get; init; }
	internal AiRawGameInfo2Data? GameInfo2 { get; init; } = null;// = HS2NewGameData;
	internal AiRawStatusData Status { get; init; }

	public AiFaceData Face { get; init; }
	public AiBodyData Body { get; init; }
	public AiHairData Hair { get; init; }
	public AiClothingData Clothing { get; init; }
	public AiAccessoriesData Accessories { get; init; }
	public AiCharaInfoData CharaInfo { get; init; }
	public AISGameData AISGameInfo { get; init; }
	public HS2GameData? HS2GameInfo { get; init; }
	public ImmutableHashSet<AiPluginData>? ExtendedData { get; init; } = null;
	public ImmutableHashSet<NullPluginData>? NullData { get; init; } = null;

	private static void CheckInfoVersion(in BlockHeader.Info info, Version expectedVersion)
	{
		if (new Version(info.version) > expectedVersion)
			throw new InternalCardException($"{info.name} version {info.version} was greater than the expected version {expectedVersion}");
	}

	[MessagePackObject(true), SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Uses MessagePack convention")]
	public readonly record struct BlockHeader
	{
		public ImmutableArray<Info> lstInfo { get; init; }

		[MessagePackObject(true), SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Uses MessagePack convention")]
		public readonly record struct Info
		{
			public string name { get; init; } = "";
			public string version { get; init; } = "";
			public long pos { get; init; } = 0;
			public long size { get; init; } = 0;
		}
	}
	public AiChara(BinaryReader binaryReader)
	{
		ParseAiCharaTypeData(binaryReader, Constants.AICharaIdentifier, out Version _version, out int _language);
		LoadVersion = _version;
		Language = _language;
		string _userID;
		string _dataID;
		int _count;
		byte[] _bhBytes;
		BlockHeader _blockHeader;
		long _num;
		try
		{
			_userID = ReadString(binaryReader);
			_dataID = ReadString(binaryReader);
			_count = binaryReader.ReadInt32();
			_bhBytes = binaryReader.ReadBytes(_count);
			_blockHeader = MessagePackSerializer.Deserialize<BlockHeader>(_bhBytes);
			_num = binaryReader.ReadInt64();
		}
		catch (Exception ex)
		{
			throw new InternalCardException("Could not parse character header information", ex);
		}
		UserID = _userID;
		DataID = _dataID;

		AiRawCustomData? _custom = null;
		AiRawCoordinateData? _coordinate = null;
		AiRawParameterData? _parameter = null;
		AiRawParameter2Data? _parameter2 = null;
		AiRawGameInfoData? _gameInfo = null;
		AiRawGameInfo2Data? _gameInfo2 = null;
		AiRawStatusData? _status = null;

		long _postNumPosition = binaryReader.BaseStream.Position;
		List<InvalidDataException> _exList = new();
		bool[] _blockHits = new bool[5];
		foreach (BlockHeader.Info info in _blockHeader.lstInfo)
		{
			long _infoPos = _postNumPosition + info.pos;
			binaryReader.BaseStream.Seek(_infoPos, SeekOrigin.Begin);
			byte[] _infoData = binaryReader.ReadBytes((int)info.size);
			ImmutableHashSet<AiPluginData>.Builder _pluginData = ImmutableHashSet.CreateBuilder<AiPluginData>();
			ImmutableHashSet<NullPluginData>.Builder _nullData = ImmutableHashSet.CreateBuilder<NullPluginData>();
			try
			{
				switch (info.name)
				{
					case Constants.AiCustomBlockName:
						CheckInfoVersion(info, AiCustomVersion);
						_custom = new AiRawCustomData(_infoData);
						_blockHits[0] = true;
						break;
					case Constants.AiCoordinateBlockName:
						CheckInfoVersion(info, AiCoordinateVersion);
						_coordinate = new(_infoData, LoadVersion, Language);
						_blockHits[1] = true;
						break;
					case Constants.AiParameterBlockName:
						CheckInfoVersion(info, AiParameterVersion);
						_parameter = MessagePackSerializer.Deserialize<AiRawParameterData>(_infoData);
						_blockHits[2] = true;
						break;
					case Constants.AiParameter2BlockName:
						CheckInfoVersion(info, AiParameter2Version);
						_parameter2 = MessagePackSerializer.Deserialize<AiRawParameter2Data>(_infoData);
						break;
					case Constants.AiGameInfoBlockName:
						CheckInfoVersion(info, AiGameInfoVersion);
						_gameInfo = MessagePackSerializer.Deserialize<AiRawGameInfoData>(_infoData);
						_blockHits[3] = true;
						break;
					case Constants.AiGameInfo2BlockName:
						CheckInfoVersion(info, AiGameInfo2Version);
						_gameInfo2 = MessagePackSerializer.Deserialize<AiRawGameInfo2Data>(_infoData);
						break;
					case Constants.AiStatusBlockName:
						CheckInfoVersion(info, AiStatusVersion);
						_status = MessagePackSerializer.Deserialize<AiRawStatusData>(_infoData);
						_blockHits[4] = true;
						break;
					case Constants.AiPluginDataBlockName:
						Dictionary<string, AiRawPluginData?> _rawExtendedData = MessagePackSerializer.Deserialize<Dictionary<string, AiRawPluginData?>>(_infoData);
						foreach (KeyValuePair<string, AiRawPluginData?> kvp in _rawExtendedData)
						{
							if (kvp.Value?.data is null)
							{
								_nullData.Add(new NullPluginData() { DataKey = kvp.Key });
								continue;
							}
							AiRawPluginData _rawPluginData = (AiRawPluginData)kvp.Value;
							if (kvp.Value is not null)
							{
								AiPluginData _aiPluginData = AiPluginData.GetExtendedPluginData(kvp.Key, _rawPluginData);
								_pluginData.Add(_aiPluginData);
							}
						}
						ExtendedData = _pluginData.ToImmutable();
						NullData = _nullData.ToImmutable();
						break;
					default:
						throw new InternalCardException($"This character has an unknown data section {info.name}");
				}
			}
			catch (InvalidDataException ex) { _exList.Add(ex); }
		}
		binaryReader.BaseStream.Seek(_postNumPosition + _num, SeekOrigin.Begin);

		if (_exList.Count != 0) throw new AggregateException("Some critical data was missing from this character.", _exList);

		Custom = _custom?.IsInitialized ?? false ? (AiRawCustomData)_custom : throw new InvalidDataException("This character has no Custom data");
		Coordinate = _coordinate?.IsInitialized ?? false ? (AiRawCoordinateData)_coordinate : throw new InvalidDataException("This character has no Coordinate data");
		Parameter = _parameter?.version is not null ? (AiRawParameterData)_parameter : throw new InvalidDataException("This character has no Parameter data");
		GameInfo = _gameInfo?.version is not null ? (AiRawGameInfoData)_gameInfo : throw new InvalidDataException("This character has no GameInfo data");
		Status = _status?.version is not null ? (AiRawStatusData)_status : throw new InvalidDataException("This character has no Status data");
		if (_parameter2?.version is not null) Parameter2 = (AiRawParameter2Data)_parameter2;
		if (_gameInfo2 is not null) GameInfo2 = (AiRawGameInfo2Data)_gameInfo2;

		for (int i = 0; i < _blockHits.Length; i++)
			if (!_blockHits[i]) throw new InternalCardException($"Failed to detect missing blocks normally. This is a bug. (Missed block at index {i})");

		(Face, Body, Hair) = GetAllFriendlyBodyData(Custom);
		(Clothing, Accessories) = GetAllFriendlyCoordinateData(Coordinate);
		CharaInfo = GetFriendlyCharaInfoData(Parameter);
		AISGameInfo = GetFriendlyAISGameData(GameInfo, Parameter.hsWish);
		HS2GameInfo = GameInfo2 != null && Parameter2 != null
			? GetFriendlyHS2GameInfoData((AiRawGameInfo2Data)GameInfo2, (AiRawParameter2Data)Parameter2)
			: null;
	}
}
