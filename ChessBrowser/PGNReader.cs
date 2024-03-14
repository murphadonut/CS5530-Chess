using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessBrowser
{
    public static class PGNReader
    {
        /// <summary>
        /// Takes a text file location and attempts to parse it into a ChessGame object
        /// </summary>
        /// <param name="fileLocation">Location of the file to open</param>
        /// <returns>An array of all the chess games in the provided text file</returns>
        public static ChessGame[] parsePGN(string fileLocation)
        {
            List<ChessGame> games = new List<ChessGame>();
            try
            {
                // Get all the lines from the text file and store as array
                string[] allLines = File.ReadAllLines(fileLocation);
                // Used to separate games by every other empty line.
                bool onSecondLine = false;
                // Store the lines for each separate game one at a time
                // Is a list because it grows
                List<string> tempLines = new List<string>();
                // Go through the whole text document and build string arrays containing all the info
                // needed for a chess game then construct said chess game 
                for (int i = 0; i < allLines.Length; i++)
                {
                    // Check if its at the end of the file
                    if (i == allLines.Length - 1)
                    {
                        games.Add(new ChessGame(tempLines.ToArray()));
                        continue;
                    }
                    // Check if there is an empty line
                    if (string.IsNullOrEmpty(allLines[i]))
                    {
                        // If the moves tag data has already been added to tempLines, all the info is 
                        // collected and chess game can be contructed now.
                        if (onSecondLine)
                        {
                            games.Add(new ChessGame(tempLines.ToArray()));
                            // Reset array to start buildling next game
                            tempLines.Clear();
                        }
                        onSecondLine = !onSecondLine;
                        continue;
                    }
                    tempLines.Add(allLines[i]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading from file: " + fileLocation + "\n" + ex);
            }
            return games.ToArray();
        }
    }
}
