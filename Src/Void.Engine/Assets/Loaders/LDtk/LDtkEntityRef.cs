namespace Void.Engine.Assets.Loaders.LDtk;

public readonly struct LDtkEntityRef
{
	public string EntityId { get; }
	public string LayerId { get; }
	public string LevelId { get; }
	public string WorldId { get; }

	internal LDtkEntityRef(string entityId, string layerId, string levelId, string worldId)
	{
		EntityId = entityId;
		LayerId = layerId;
		LevelId = levelId;
		WorldId = worldId;
	}

	internal static LDtkEntityRef Process(JsonElement e)
	{
		var entityId = e.GetPropertyOrDefault("entityIid", string.Empty);
		var layerId = e.GetPropertyOrDefault("layerIid", string.Empty);
		var levelId = e.GetPropertyOrDefault("levelIid", string.Empty);
		var worldId = e.GetPropertyOrDefault("worldIid", string.Empty);

		return new LDtkEntityRef(entityId, layerId, levelId, worldId);
	}
}