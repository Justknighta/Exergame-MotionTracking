using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string name;
    public int score;
    public long insertId;   // ใช้แก้ tie-break (newer wins)
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

    // เก็บ counter ต่อเนื่องเพื่อ generate insertId ใหม่เสมอ
    public long nextInsertId = 1;
}