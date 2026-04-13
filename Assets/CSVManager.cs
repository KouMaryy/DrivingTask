using UnityEngine;
using System.IO;

public static class CSVManager
{
public static void SaveTrial(string fileName, int id, string weather, bool lying, string action, bool intervened, int numInterventions, float reactionTime, float aiTime, float playerTime, string aiSuggestedlLaneLabel, string finalLane, bool success, int currentScore, string dangerousObstacle, string safeObstacle)    {
        string fullFileName = fileName + ".csv";
        string filePath = Path.Combine(Application.dataPath, fullFileName);

        // If the file doesn't exist, create it and write the header
        if (!File.Exists(filePath))
        {
            string header = "Trial_ID,Weather_Condition,AI_Lying,AI_Action,Intervened,Num_Interventions,Reaction_Time_Seconds,AI_Time_Stamp,Player_Time_Stamp,AI_Suggested_Lane,Final_Lane,Success,Total_Score,Dangerous_Object,Safe_Object,Timestamp" + System.Environment.NewLine;
            File.WriteAllText(filePath, header);
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Create a CSV row with the trial data
        // We use string.Format with InvariantCulture to ensure a dot (.) is always used
        // and :F3 to force 3 decimal places (even for leading zeros)
        string row = string.Format(System.Globalization.CultureInfo.InvariantCulture,
        "{0},{1},{2},{3},{4},{5},{6:F3},{7:F3},{8:F3},{9},{10},{11},{12},{13},{14},{15}{16}",
        id, weather, lying, action, intervened, numInterventions, reactionTime, aiTime, playerTime, aiSuggestedlLaneLabel, finalLane, success, currentScore, dangerousObstacle, safeObstacle, timestamp, System.Environment.NewLine);         
        File.AppendAllText(filePath, row);
    }
}
