using LRCLIBee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRCLIBee_Tester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LRCLIBClient client = new LRCLIBClient("");
            //client.getLyrics("Chetta", "MEET ME AT MY WORST", "SACRIFICE & SABOTAGE", 284);
            client.getLyrics("Paradise Lost", "Darker Thoughts", "Obsidian", 346);
        }
    }
}
