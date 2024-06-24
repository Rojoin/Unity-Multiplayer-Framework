using System.Collections.Generic;

namespace RojoinNetworkSystem
{
    public static class NetObjectFactory
    {
        public static int intanceCount = 0;

        public static int getNewIntances() => intanceCount++;
    }
}