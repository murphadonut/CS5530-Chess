using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessBrowser
{
    public class ChessGame
    {
        public string eventName { private set; get; }
        public string eventSite { private set; get; }
        public string eventDate { private set; get; }
        public string round { private set; get; }
        public string whiteName { private set; get; }
        public string blackName { private set; get; }
        public int whiteELO { private set; get; }
        public int blackELO { private set; get; }
        public char result { private set; get; }
        public string moves { private set; get; }

        public ChessGame(string[] lines)
        {
            // Remove any extra blank lines
            lines = lines.Where(line => !string.IsNullOrEmpty(line)).ToArray();
            // For checking whether all the other data has been constructed.
            bool ontoMoves = false;
            foreach (string line in lines)
            {
                // I assumed every moves first line starts with 1
                if (ontoMoves || line.StartsWith("1"))
                {
                    moves = moves + line;
                    ontoMoves = true;
                    continue;
                }
                // Get last index of tag
                int last = line.IndexOf(' ');
                string value = line.Substring(last + 2, line.Length - last - 4);
                string tag = line.Substring(1, last - 1);
                switch (tag)
                {
                    case "Event":
                        eventName = value;
                        break;
                    case "Site":
                        eventSite = value;
                        break;
                    case "EventDate":
                        eventDate = value;
                        if (value.Contains('?'))
                        {
                            eventDate = "0000.00.00";
                        }                     
                        break;
                    case "Round":
                        round = value;
                        break;
                    case "White":
                        whiteName = value;
                        break;
                    case "Black":
                        blackName = value;
                        break;
                    case "WhiteElo":
                        whiteELO = int.Parse(value);
                        break;
                    case "BlackElo":
                        blackELO = int.Parse(value);
                        break;
                    case "Result":
                        switch (value)
                        {
                            case "1-0":
                                result = 'W';
                                break;
                            case "0-1":
                                result = 'B';
                                break;
                            case "1/2-1/2":
                                result = 'D';
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        override
        public string ToString()
        {
            return
                "Event: " + eventName +
                "\nSite: " + eventSite +
                "\nDate: " + eventDate.Substring(5, 2) +
                "/" + eventDate.Substring(8, 2) +
                "/" + eventDate.Substring(0, 4) +
                " 12:00:00 AM\nWhite: " + whiteName +
                " (" + whiteELO +
                ")\nBlack: " + blackName +
                " (" + blackELO +
                ")\nResult: " + result;
        }
    }
}
