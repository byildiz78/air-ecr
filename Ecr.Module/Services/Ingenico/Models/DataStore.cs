using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class DataStore
    {
        internal static GmpPairingDto gmpResult { get; set; } = new GmpPairingDto();

        internal static uint CurrentInterface { get; set; } = 0;
        internal static ConnectionStatus Connection { get; set; } = ConnectionStatus.NotConnected;

        public static ulong ActiveTransactionHandle = 0;
        public static string MergeUniqueID = "";

        public static int OrderID = 0;
        public static int TIMEOUT_CARD_TRANSACTIONS = 100000;
        public static string EcrSerialNo = "";

        public static string gmpxml = "";
        public static string gmpini = "";

    }
}
