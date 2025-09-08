using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ecr.Module.Services.Ingenico.GmpIngenico
{
    public static class IReceiptMethods
    {
        public static byte[] GMP_AscToBcd(string Value)
        {
            byte[] tempbuf = new byte[32];

            byte[] tempbufbcd = new byte[16];
            Encoding asen = Encoding.Default;

            ushort fieldlen = (ushort)Value.Length;
            tempbuf = asen.GetBytes(Value);
            Utility.GMP_AscToBcd(ref tempbufbcd, (ushort)(fieldlen / 2), ref tempbuf, (ushort)fieldlen);

            return tempbufbcd;
        }
        public static string GetValOfStructMemberAscii(byte[] member, int len)
        {
            string str = "";
            byte[] new_arr = new byte[len];

            int j = 0;
            for (int i = 0; i < len; i++)
            {
                if (member[i] != 0x00)
                {
                    new_arr[j] = member[i];
                    j++;
                }
            }
            str += Encoding.Default.GetString(new_arr);

            //for (int i = 0; i < len; i++)
            //{
            //    str += member[i].ToString("X2");
            //}
            return str;
        }
    }
}
