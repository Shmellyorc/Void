namespace Void.Engine.Assets.Loaders.LDtk.Instances;

public sealed class LDtkEntityInstance : ILDtkInstance
{
	public string Name { get; }
	public Vect2 Pivot { get; }
	public string Id { get; }
	public Vect2 Size { get; }
	public Vect2 Coords { get; }
	public List<string> Tags { get; }
	public float Width => Size.X;
	public float Height => Size.Y;
	public Dictionary<uint, LDtkSetting> Settings { get; }
	public Vect2 Location { get; }
	public Vect2 Position { get; }

	public List<TEnum> TagsAs<TEnum>() where TEnum : Enum
	{
		var result = new List<TEnum>(Tags.Count);

		for (int i = 0; i < Tags.Count; i++)
		{
			var tag = Tags[i];

			if (!Enum.TryParse(typeof(TEnum), tag, true, out var eResult))
				continue;

			result.Add((TEnum)eResult);
		}

		return result;
	}

	internal LDtkEntityInstance(string name, Vect2 pivot, string id, Vect2 size,
		Vect2 coords, List<string> tags, Vect2 location, Vect2 position,
		Dictionary<uint, LDtkSetting> settings)
	{
		Name = name;
		Pivot = pivot;
		Id = id;
		Size = size;
		Coords = coords;
		Tags = tags;
		Settings = settings;
		Location = location;
		Position = position;
	}

	internal static List<ILDtkInstance> Process(JsonElement e)
	{
		var result = new List<ILDtkInstance>(e.GetArrayLength());

		foreach (var t in e.EnumerateArray())
		{
			var name = t.GetPropertyOrDefault("__identifier", string.Empty);
			var location = t.GetPosition("__grid");
			var pivot = t.GetPosition("__pivot");
			var id = t.GetPropertyOrDefault("iid", string.Empty);
			var cX = t.GetPropertyOrDefault<int>("width");
			var cY = t.GetPropertyOrDefault<int>("height");
			var position = t.GetPosition("px");
			var worldX = t.GetPropertyOrDefault<int>("__worldX");
			var worldY = t.GetPropertyOrDefault<int>("__worldY");
			var tags = t.GetProperty("__tags")
				.EnumerateArray()
				.Where(x => x.ValueKind != JsonValueKind.Null)
				.Select(x => x.GetString()!)
				.ToList();

			var settings = JsonHelper.GetSettings(t.GetProperty("fieldInstances"));

			result.Add(new LDtkEntityInstance(name, pivot, id, new(cX, cY),
				new(worldX, worldY), tags, location, position, settings));
		}

		return result;
	}
}