using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Config;

public class DataSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DataSourceType DataSourceType { get; set; } = DataSourceType.JsonStore;
    public string CollectionName { get; set; } = string.Empty;
    public Dictionary<string, object?>? Config { get; set; }
}
