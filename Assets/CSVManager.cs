using UnityEngine;
using System.IO;

public static class CSVManager
{
    public static void SaveTrial(string fileName,int id, string weather, bool lying, string action, bool intervened, float reactionTime, string finalLane, bool success, string dangerousObstacle, string safeObstacle)
    {
        string fullFileName = fileName + ".csv";
        string filePath = Path.Combine(Application.dataPath, fullFileName);

        // If the file doesn't exist, create it and write the header
        if (!File.Exists(filePath))
        {
            string header = "Trial_ID,Weather_Intensity,AI_Lying,AI_Action,Intervened,Reaction_Time_Seconds,Final_Lane,Success,Dangerous_Object,Safe_Object,Timestamp" + System.Environment.NewLine;
            File.WriteAllText(filePath, header);
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        //Format the Reaction Time: if it's 0 (no intervention), we can write "N/A" or 0
        string reactionStr = (reactionTime > 0) ? reactionTime.ToString("F3") : "0";

        // Create a CSV row with the trial data
        string row = $"{id},{weather},{lying},{action},{intervened},{reactionStr},{finalLane},{success},{dangerousObstacle},{safeObstacle},{timestamp}" + System.Environment.NewLine;

        // Append the row to the CSV file
        File.AppendAllText(filePath, row);
    }
}