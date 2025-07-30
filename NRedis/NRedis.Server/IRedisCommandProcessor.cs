using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace NRedis.Server
{

    // Command processor interface (placeholder - you'll implement this)
    public interface IRedisCommandProcessor
    {
        Task<object> ProcessCommandAsync(string[] command, string clientId);
    }
}