using Microsoft.Maui.Controls;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
  Author: Daniel Kopta and Murphy Rickett and Simon
  Chess browser backend 
*/

namespace ChessBrowser
{
    internal class Queries
    {

        /// <summary>
        /// This function runs when the upload button is pressed.
        /// Given a filename, parses the PGN file, and uploads
        /// each chess game to the user's database.
        /// </summary>
        /// <param name="PGNfilename">The path to the PGN file</param>
        internal static async Task InsertGameData(string PGNfilename, MainPage mainPage)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = mainPage.GetConnectionString();

            // Load and parse the PGN file
            ChessGame[] allGames = PGNReader.parsePGN(PGNfilename);

            // Use this to tell the GUI's progress bar how many total work steps there are           
            mainPage.SetNumWorkItems(allGames.Length);

            // Start sql stuff
            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // Gonna cache the command
                    MySqlCommand cmd = conn.CreateCommand();

                    // We will batch 4 commands together
                    cmd.CommandText =
                        "INSERT IGNORE INTO Players (Name, Elo) values (@whiteName, @whiteELO) ON DUPLICATE KEY UPDATE Elo = IF(@whiteELO > Elo, @whiteELO, Elo);" +
                        "INSERT IGNORE INTO Players (Name, Elo) values (@blackName, @blackELO) ON DUPLICATE KEY UPDATE Elo = IF(@blackELO > Elo, @blackELO, Elo);" +
                        "INSERT IGNORE INTO Events (Name, Site, Date) values (@eventName, @eventSite, @eventDate);" +
                        "INSERT IGNORE INTO Games (Round, Result, Moves, BlackPlayer, WhitePlayer, eID) values (@round, @result, @moves, " +
                        "(select pID from Players where Name = @blackName), " +
                        "(select pID from Players where Name = @whiteName), " +
                        "(select eID from Events where Name = @eventName));";

                    // Put temporary placeholder strings for each of the parameters
                    cmd.Parameters.AddWithValue("@whiteName", "");
                    cmd.Parameters.AddWithValue("@whiteELO", "");
                    cmd.Parameters.AddWithValue("@blackName", "");
                    cmd.Parameters.AddWithValue("@blackELO", "");
                    cmd.Parameters.AddWithValue("@eventName", "");
                    cmd.Parameters.AddWithValue("@eventSite", "");
                    cmd.Parameters.AddWithValue("@eventDate", "");
                    cmd.Parameters.AddWithValue("@round", "");
                    cmd.Parameters.AddWithValue("@result", "");
                    cmd.Parameters.AddWithValue("@moves", "");

                    // Cache the command
                    cmd.Prepare();

                    // Iterate over each chess game
                    foreach (ChessGame game in allGames)
                    {
                        // Set parameters accordingly
                        cmd.Parameters["@whiteName"].Value = game.whiteName;
                        cmd.Parameters["@whiteELO"].Value = game.whiteELO;
                        cmd.Parameters["@blackName"].Value = game.blackName;
                        cmd.Parameters["@blackELO"].Value = game.blackELO;
                        cmd.Parameters["@eventName"].Value = game.eventName;
                        cmd.Parameters["@eventSite"].Value = game.eventSite;
                        cmd.Parameters["@eventDate"].Value = game.eventDate;
                        cmd.Parameters["@round"].Value = game.round;
                        cmd.Parameters["@result"].Value = game.result;
                        cmd.Parameters["@moves"].Value = game.moves;

                        // Execute the command and return nothing
                        cmd.ExecuteNonQuery();

                        // Update the progress bar
                        await mainPage.NotifyWorkItemCompleted();
                    }

                }

                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

        }


        /// <summary>
        /// Queries the database for games that match all the given filters.
        /// The filters are taken from the various controls in the GUI.
        /// </summary>
        /// <param name="white">The white player, or null if none</param>
        /// <param name="black">The black player, or null if none</param>
        /// <param name="opening">The first move, e.g. "1.e4", or null if none</param>
        /// <param name="winner">The winner as "W", "B", "D", or null if none</param>
        /// <param name="useDate">True if the filter includes a date range, False otherwise</param>
        /// <param name="start">The start of the date range</param>
        /// <param name="end">The end of the date range</param>
        /// <param name="showMoves">True if the returned data should include the PGN moves</param>
        /// <returns>A string separated by newlines containing the filtered games</returns>
        internal static string PerformQuery(string white, string black, string opening,
          string winner, bool useDate, DateTime start, DateTime end, bool showMoves,
          MainPage mainPage)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = mainPage.GetConnectionString() + ";Allow Zero Datetime=True";

            // Build up this string containing the results from your query
            string parsedResult = "";

            // Use this to count the number of rows returned by your query
            // (see below return statement)
            int numRows = 0;

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // Gonna cache the command
                    MySqlCommand cmd = conn.CreateCommand();

                    // Made a single query that can filter everything, cause I'm that cool
                    cmd.CommandText = "select " +
                        "Events.Name, Site, Date, White, WhiteElo, Black, BlackElo, Result" + (showMoves ? ", Moves" : "") +
                        " from Events natural join " +
                            "(select eID, White, WhiteElo, blackP.Name as Black, blackP.Elo as BlackElo," +
                            " Result, Moves from Players blackP join " +
                                "(select Moves, Result, eID, BlackPlayer, whiteP.Name as White, whiteP.Elo " +
                                "as WhiteELo from Players whiteP join Games g on g.WhitePlayer = whiteP.pID " +
                                "where whiteP.Name like @whiteNameVar) " +
                            "as first on BlackPlayer = blackP.pID where blackP.Name like @blackNameVar) " +
                        "as second where Moves like @movesVar && Result like @winnerVar" + (useDate ? " && Date between '" + start.Date.ToString("yyyy/MM/dd") + "' and '" + end.Date.ToString("yyyy/MM/dd") + "'" : "") + ";";

                    cmd.Parameters.AddWithValue("@whiteNameVar", "%" + white);
                    cmd.Parameters.AddWithValue("@blackNameVar", "%" + black);
                    cmd.Parameters.AddWithValue("@movesVar", opening + "%");
                    cmd.Parameters.AddWithValue("@winnerVar", "%" + winner);

                    // Cache the command
                    cmd.Prepare();

                    // Execute Query
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Could have made a helper method to fromat the string but oh well
                            numRows++;
                            parsedResult +=
                                "\nEvent: " + reader["Name"] +
                                "\nSite: " + reader["Site"] +
                                "\nDate: " + reader["Date"] +
                                "\nWhite: " + reader["White"] +
                                " (" + reader["WhiteElo"] +
                                ")\nBlack: " + reader["Black"] +
                                " (" + reader["BlackElo"] +
                                ")\nResult: " + reader["Result"] + 
                                (showMoves ? "\nMoves: " + reader["Moves"] : "") + "\n";
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            return numRows + " results\n" + parsedResult;
        }

    }
}
