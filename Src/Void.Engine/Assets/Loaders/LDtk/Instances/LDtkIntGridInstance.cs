namespace Void.Engine.Assets.Loaders.LDtk.Instances;

public sealed class LDtkIntGridInstance : ILDtkInstance
{
	public int Index { get; }
	public T IndexAsEnum<T>() where T : Enum => (T)Enum.ToObject(typeof(T), Index);
	public bool IsSolid => Index > 0;
	public Vect2 Location { get; }
	public Vect2 Position { get; }

	internal LDtkIntGridInstance(int index, Vect2 location, Vect2 position)
	{
		Index = index;
		Location = location;
		Position = position;
	}

	internal static List<ILDtkInstance> Process(JsonElement e, Vect2 gridSize)
	{
		var result = new List<ILDtkInstance>(e.GetArrayLength());
		var index = 0;

		foreach (var t in e.EnumerateArray())
		{
			var location = new Vect2(index % (int)gridSize.X, index / (int)gridSize.X);
			var position = gridSize * location;

			result.Add(new LDtkIntGridInstance(t.GetInt32(), location, position));

			index++;
		}

		return result;
	}
}