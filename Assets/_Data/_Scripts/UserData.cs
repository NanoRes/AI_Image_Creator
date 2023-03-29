using UnityEngine;

[CreateAssetMenu(fileName = "User_Data", 
    menuName = "NanoRes_Studios/User Data", order = 1)]
public class UserData : ScriptableObject
{
    public double totalSolanaTokens = 0;
    public ulong totalDogelanaTokens = 0;
    public string dogelanaTokenAddress = string.Empty;

    public void ResetData()
    {
        totalSolanaTokens = 0;
        totalDogelanaTokens = 0;
        dogelanaTokenAddress = string.Empty;
    }

    private void OnEnable()
    {
        ResetData();
    }
}