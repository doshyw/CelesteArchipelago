using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatLog
    {
        private List<ChatLine> log = new List<ChatLine>();

        public ChatLog()
        {
        }

        private static bool WithinTimeout(DateTime now, DateTime then, float timeout)
        {
            return (now - then).TotalSeconds <= timeout;
        }

        public void Add(ChatLine line)
        {
            log.Add(line);
        }

        public IEnumerable<ChatLine> GetLog()
        {
            return log.AsEnumerable();
        }

        public IEnumerable<ChatLine> GetLogWithTimeout(float timeout) 
        {
            DateTime now = DateTime.Now;
            return log.Reverse<ChatLine>().TakeWhile(line => WithinTimeout(now, line.createdTime, timeout));
        }
    }
}
