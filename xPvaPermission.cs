namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaPermission
    {
        public static PermissionEvent Evaluate(int barIndex, PriceCase pc)
        {
            if (xPvaPriceCases.IsTranslation(pc))
                return new PermissionEvent(barIndex, Permission.Granted, "Translation");

            // Later: allow some internal synthesis rules; deny laterals explicitly.
            return new PermissionEvent(barIndex, Permission.Denied, "Non-translation (Phase1 policy)");
        }
    }
}
