using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.Utils;

namespace TicketSystem.Redis
{
    /// <summary>
    /// Redis host information
    /// </summary>
    public class RedisHost
    {
        /// <summary>
		/// Defaul SSL
		/// </summary>
		public static bool DefaultSSL { get; internal set; }

        /// <summary>
        ///Default redis server password
        /// </summary>
        internal static String DefaultPassword { get; set; }

        /// <summary>
        /// Default database of redis
        /// </summary>
        public static int DefaultDB { get; internal set; }

        private string host = "127.0.0.1";
        /// <summary>
        /// IP
        /// </summary>
        public string Host { get { return host; } set { host = value; } }

        private Int32 port = 6379;
        /// <summary>
        /// PORT
        /// </summary>
        public Int32 Port { get { return port; } set { port = value; } }


        private bool ssl = false;
        /// <summary>
        /// Is SSL enable or not
        /// </summary>
        public bool SSL { get { return ssl; } set { ssl = value; } }

        /// <summary>
        /// password
        /// </summary>
        internal String Password
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DefaultPassword))
                    return null;

                return MD5Encryption.Decrypt(DefaultPassword, true);
            }
        }
        private int db = 0;
        /// <summary>
        /// Database id
        /// </summary>
        public int DB { get { return db; } set { db = value; } }


        /// <summary>
        /// Constructor
        /// Standard format：IP:Port:DB:SSL
        /// </summary>
        /// <param name="host"></param>
        public RedisHost(string host)
        {
            try
            {
                SSL = DefaultSSL;
                DB = DefaultDB;

                var parts = host.Split(new char[] { '#', ':' });

                if (parts.Length > 1)
                {
                    Host = parts[0];
                    Port = Int32.Parse(parts[1]);

                    if (parts.Length > 2)
                        DB = Int32.Parse(parts[2]);

                    if (parts.Length > 3)
                        SSL = string.Compare(parts[3], "ssl", true) == 0;
                }
                else
                {
                    throw new Exception("Invalid Redis host format：" + host);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
