using UnityEngine;

[CreateAssetMenu(fileName = "User_Data", 
    menuName = "NanoRes_Studios/User Data", order = 1)]
public class UserData : ScriptableObject
{
    public double totalSolanaTokens = 0;
    public ulong totalDogelanaTokens = 0;

    private void OnEnable()
    {
        totalSolanaTokens = 0;
        totalDogelanaTokens = 0;
    }
}