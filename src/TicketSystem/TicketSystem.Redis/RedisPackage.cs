using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Redis
{
    /// <summary>
    /// The data used to interact with redis cilent the redis server
    /// </summary>
    public class RedisPackage
    {
        /// <summary>
        /// queue name
        /// </summary>
        public string Service { get; set; }
        /// <summary>
        /// Request id
        /// </summary>
        public long RequestId { get; set; }

        /// <summary>
        /// the session id of the message
        /// </summary>
        public long SessionId { get; set;}
        /// <summary>
        /// Time occured
        /// </summary>
        public long OccurTime { get; set; }
    
        /// <summary>
        /// The body data of the package
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Serialization
        /// </summary>
        /// <returns></returns>
        internal byte[] ToBytes()
        {
            using (var ms = new MemoryStream((Data == null ? 0 : Data.Length) + 200))
            {
                using (var bw = new BinaryWriter(ms))
                {
                    //queue name
                    byte[] bytes = Encoding.UTF8.GetBytes(this.Service.ToUpper());
                    bw.Write((Int16)bytes.Length);
                    bw.Write(bytes);

                    //request id
                    bw.Write(this.RequestId);

                    //client id
                    bw.Write(this.SessionId);

                    //request time
                    bw.Write(this.OccurTime);

                    //request data
                    bytes = this.Data ?? new byte[0];
                    bw.Write((Int32)bytes.Length);
                    bw.Write(bytes);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserialization
        /// </summary>
        internal void FromBytes(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            {
                using (var br = new BinaryReader(ms))
                {
                    //Queue name
                    this.Service = Encoding.UTF8.GetString(br.ReadBytes(br.ReadInt16())).ToUpper();

                    //The number of the original request
                    this.RequestId = br.ReadInt64();
                    // the id the the client
                    this.SessionId = br.ReadInt64();
                    //The time reponsed
                    this.OccurTime = br.ReadInt64();

                    //data
                    this.Data = br.ReadBytes(br.ReadInt32()); 
                }
            }
        }
    }
}
