namespace Cognition.Domains.Documents.Repositories;

internal static class DocumentIds
{
    internal static string DomainManifest(Guid domainId, string version)
    {
        return $"domain-manifests/{domainId:D}/{version}";
    }

    internal static string KnowledgeAsset(Guid knowledgeAssetId)
    {
        return $"knowledge-assets/{knowledgeAssetId:D}";
    }
}
