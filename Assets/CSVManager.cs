using UnityEngine;
using System.IO;

public static class CSVManager
{
    public static void SaveTrial(string fileName, int id, string weather, bool lying, string action, bool intervened, int numInterventions, float reactionTime, string aiSuggestedlLaneLabel, string finalLane, bool success, int currentScore, string dangerousObstacle, string safeObstacle)
    {
        string fullFileName = fileName + ".csv";
        string filePath = Path.Combine(Application.dataPath, fullFileName);

        // If the file doesn't exist, create it and write the header
        if (!File.Exists(filePath))
        {
            string header = "Trial_ID,Weather_Condition,AI_Lying,AI_Action,Intervened,Num_Interventions,Reaction_Time_Seconds,AI_Suggested_Lane,Final_Lane,Success,Total_Score, Dangerous_Object,Safe_Object,Timestamp" + System.Environment.NewLine;
            File.WriteAllText(filePath, header);
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        //Format the Reaction Time: if it's 0 (no intervention), we can write "N/A" or 0
        string reactionStr = (reactionTime > 0) ? reactionTime.ToString("F3") : "0";

        // Create a CSV row with the trial data
        string row = $"{id},{weather},{lying},{action},{intervened},{numInterventions},{reactionStr},{aiSuggestedlLaneLabel},{finalLane},{success}, {currentScore},{dangerousObstacle},{safeObstacle},{timestamp}" + System.Environment.NewLine;
        // Append the row to the CSV file
        File.AppendAllText(filePath, row);
    }
}