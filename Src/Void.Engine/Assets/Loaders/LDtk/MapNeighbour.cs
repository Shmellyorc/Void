namespace Void.Engine.Assets.Loaders.LDtk;

// N, NE, E, SE, S, SW, W, NW

/// <summary>
/// Represents the possible directions for neighboring map tiles or cells.
/// </summary>
public enum LDtkNeighbourDirection
{
	/// <summary>
	/// No direction; used when there is no neighboring tile.
	/// </summary>
	None,

	/// <summary>
	/// The tile directly to the north (up).
	/// </summary>
	North,

	/// <summary>
	/// The tile to the northeast (up and right).
	/// </summary>
	NorthEast,

	/// <summary>
	/// The tile directly to the east (right).
	/// </summary>
	East,

	/// <summary>
	/// The tile to the southeast (down and right).
	/// </summary>
	SouthEast,

	/// <summary>
	/// The tile directly to the south (down).
	/// </summary>
	South,

	/// <summary>
	/// The tile to the southwest (down and left).
	/// </summary>
	SouthWest,

	/// <summary>
	/// The tile directly to the west (left).
	/// </summary>
	West,

	/// <summary>
	/// The tile to the northwest (up and left).
	/// </summary>
	NorthWest
}

/// <summary>
/// Represents the neighboring tiles of a map tile, indexed by direction.
/// </summary>
public sealed class MapNeighbour
{
	public string North => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.North), out var v) ? v : string.Empty;
	public string NorthEast => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.NorthEast), out var v) ? v : string.Empty;
	public string East => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.East), out var v) ? v : string.Empty;
	public string SouthEast => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.SouthEast), out var v) ? v : string.Empty;
	public string South => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.South), out var v) ? v : string.Empty;
	public string SouthWest => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.SouthWest), out var v) ? v : string.Empty;
	public string West => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.West), out var v) ? v : string.Empty;
	public string NorthWest => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.NorthWest), out var v) ? v : string.Empty;
	public IReadOnlyDictionary<uint, string> Neighbours { get; }
	public MapNeighbour(Dictionary<uint, string> neighbours) =>
		Neighbours = neighbours;

	internal static MapNeighbour Process(JsonElement e)
	{
		var result = new Dictionary<uint, string>(e.GetArrayLength()); // Pre-allocate
		foreach (var element in e.EnumerateArray())
		{
			(LDtkNeighbourDirection dir, string id) data = element.GetPropertyOrDefault("dir", string.Empty) switch
			{
				var v when v == "n" => (LDtkNeighbourDirection.North, element.GetPropertyOrDefault("levelIid", string.Empty)),
				var v when v == "ne" => (LDtkNeighbourDirection.NorthEast, element.GetPropertyOrDefault("levelIid", string.Empty)),
				var v when v == "e" => (LDtkNeighbourDirection.East, element.GetPropertyOrDefault("levelIid", string.Empty)),
				var v when v == "se" => (LDtkNeighbourDirection.SouthEast, element.GetPropertyOrDefault("levelIid", string.Empty)),
				var v when v == "s" => (LDtkNeighbourDirection.South, element.GetPropertyOrDefault("levelIid", string.Empty)),
				var v when v == "sw" => (LDtkNeighbourDirection.SouthWest, element.GetPropertyOrDefault("levelIid", string.Empty)),
				var v when v == "w" => (LDtkNeighbourDirection.West, element.GetPropertyOrDefault("levelIid", string.Empty)),
				var v when v == "nw" => (LDtkNeighbourDirection.NorthWest, element.GetPropertyOrDefault("levelIid", string.Empty)),
				_ => (LDtkNeighbourDirection.None, string.Empty)
			};

			if (data.dir == LDtkNeighbourDirection.None || string.IsNullOrWhiteSpace(data.id))
				continue;

			result[HashHelper.Cache32(data.dir)] = data.id;
		}

		return new MapNeighbour(result);
	}
}