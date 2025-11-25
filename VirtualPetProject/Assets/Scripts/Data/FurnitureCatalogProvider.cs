using UnityEngine;

public static class FurnitureCatalogProvider
{
    private const string ResourcesPath = "GameData/FurnitureCatalog";
    private static FurnitureCatalog cachedCatalog;

    public static FurnitureCatalog Catalog
    {
        get
        {
            if (cachedCatalog == null)
            {
                cachedCatalog = Resources.Load<FurnitureCatalog>(ResourcesPath);
                if (cachedCatalog == null)
                {
                    Debug.LogWarning($"FurnitureCatalog not found at Resources/{ResourcesPath}. Create it via the Furniture Catalog window.");
                }
            }

            return cachedCatalog;
        }
    }

    public static void SetCatalog(FurnitureCatalog catalog)
    {
        cachedCatalog = catalog;
    }
}

