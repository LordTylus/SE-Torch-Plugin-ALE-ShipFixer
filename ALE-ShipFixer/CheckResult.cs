namespace ALE_ShipFixer
{
    public enum CheckResult
    {
        OK,
        TOO_FEW_GRIDS,
        TOO_MANY_GRIDS,
        UNKNOWN_PROBLEM,
        OWNED_BY_DIFFERENT_PLAYER,
        DIFFERENT_OWNER_ON_CONNECTED_GRID,
        GRID_OCCUPIED,
        SHIP_FIXED,
    }
}
