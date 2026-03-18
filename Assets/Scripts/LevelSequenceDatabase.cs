using UnityEngine;

public static class LevelSequenceDatabase
{
    public static PoseSequenceConfigData GetLevel(int level)
    {
        switch (level)
        {
            case 1: return Level1();
            case 2: return Level2();
            case 3: return Level3();
            case 4: return Level4();
            case 5: return Level5();
            default: return Level1();
        }
    }

    static PoseSequenceConfigData Level1()
    {
        return new PoseSequenceConfigData
        {
            routinePoseIDs = new int[]
            {
                13,13,
                7,8,
                13,13,
                13,13,
                1,2,
                13,13
            },
            bossPoseIDs = new int[]
            {
                7,8,
                1,2,
                7,8,
                1,2
            }
        };
    }

    static PoseSequenceConfigData Level2()
    {
        return new PoseSequenceConfigData
        {
            routinePoseIDs = new int[]
            {
                9,9,
                3,4,
                9,9,
                9,9,
                11,11,
                9,9
            },
            bossPoseIDs = new int[]
            {
                3,4,
                11,11,
                3,4,
                11,11
            }
        };
    }

    static PoseSequenceConfigData Level3()
    {
        return new PoseSequenceConfigData
        {
            routinePoseIDs = new int[]
            {
                14,15,
                5,6,
                14,15,
                14,15,
                12,12,
                14,15
            },
            bossPoseIDs = new int[]
            {
                5,6,
                12,12,
                5,6,
                12,12
            }
        };
    }

    static PoseSequenceConfigData Level4()
    {
        return new PoseSequenceConfigData
        {
            routinePoseIDs = new int[]
            {
                10,10,
                1,2,
                10,10,
                10,10,
                13,13,
                10,10
            },
            bossPoseIDs = new int[]
            {
                1,2,
                13,13,
                1,2,
                13,13
            }
        };
    }

    static PoseSequenceConfigData Level5()
    {
        return new PoseSequenceConfigData
        {
            routinePoseIDs = new int[]
            {
                14,15,
                5,6,
                14,15,
                14,15,
                7,8,
                14,15
            },
            bossPoseIDs = new int[]
            {
                5,6,
                7,8,
                5,6,
                7,8
            }
        };
    }
}

public class PoseSequenceConfigData
{
    public int[] routinePoseIDs;
    public int[] bossPoseIDs;
}