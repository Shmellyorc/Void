namespace Void.Engine.Assets.Loaders.LDtk;

public sealed class LDtkTileset
{
	public uint Id { get; }
	public string Name { get; }
	public Vect2 CellSize { get; }
	public Vect2 Size { get; }
	public string Path { get; }
	public int TileSize { get; }
	public int Spacing { get; }
	public int Padding { get; }
	public List<string> Tags { get; }

	internal LDtkTileset(uint id, string name, Vect2 cellSize, Vect2 size,
		string path, int tileSize, int spacing, int padding, List<string> tags)
	{
		Id = id;
		Name = name;
		CellSize = cellSize;
		Size = size;
		Path = path;
		TileSize = tileSize;
		Spacing = spacing;
		Padding = padding;
		Tags = tags;
	}

	internal static List<LDtkTileset> Process(JsonElement e)
	{
		var result = new List<LDtkTileset>(e.GetArrayLength());

		foreach (var t in e.EnumerateArray())
		{
			var cWidth = t.GetPropertyOrDefault<int>("__cWid");
			var cHeight = t.GetPropertyOrDefault<int>("__cHei");
			var name = t.GetPropertyOrDefault<string>("identifier");
			var id = t.GetPropertyOrDefault<uint>("uid");
			var path = t.GetPropertyOrDefault("relPath", string.Empty);
			var pxWid = t.GetPropertyOrDefault<int>("pxWid");
			var pxHei = t.GetPropertyOrDefault<int>("pxHei");
			var tileSize = t.GetPropertyOrDefault<int>("tileGridSize");
			var spacing = t.GetPropertyOrDefault<int>("spacing");
			var padding = t.GetPropertyOrDefault<int>("padding");
			var tags = t.GetProperty("enumTags").EnumerateArray()
				.Where(x => x.ValueKind != JsonValueKind.Null)
				.Select(x => x.GetString()!)
				.ToList();

			result.Add(
				new LDtkTileset(
					id,
					name,
					new(cWidth, cHeight),
					new(pxWid, pxHei),
					path,
					tileSize,
					spacing,
					padding,
					tags
				)
			);
		}

		return result;
	}
}