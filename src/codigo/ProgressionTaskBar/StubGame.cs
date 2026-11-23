public class Inventory
{
    public int CountItems(string name)
    {
        return 0;
    }
}

public class Player
{
    public static Player m_localPlayer;

    public Inventory GetInventory()
    {
        return null;
    }
}

public class ZoneSystem
{
    public static ZoneSystem instance;

    public bool GetGlobalKey(string name)
    {
        return false;
    }
}
