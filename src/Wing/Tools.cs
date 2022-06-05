﻿using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Wing
{
    public class Tools
    {
        public static string LocalIp
        {
            get
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses)
                .Where(p => p.PrefixOrigin == PrefixOrigin.Dhcp || p.PrefixOrigin == PrefixOrigin.Manual)
                .OrderByDescending(p => p.PrefixOrigin)
                .FirstOrDefault(p => p.IsDnsEligible && p.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(p.Address))?.Address.ToString();
            }
        }

        public static T DeepCopy<T>(T value)
        {
            using var ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, value);
            ms.Seek(0, SeekOrigin.Begin);
            var result = bf.Deserialize(ms);
            return (T)result;
        }
    }
}