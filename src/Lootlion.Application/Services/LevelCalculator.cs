namespace Lootlion.Application.Services;

public static class LevelCalculator
{
    public const int ExpPerLevel = 100;

    public static int FromExp(int expTotal)
    {
        if (expTotal < 0)
            return 1;
        return (expTotal / ExpPerLevel) + 1;
    }

    /// <summary>EXP ที่สะสมในเลเวลปัจจุบัน (0–99).</summary>
    public static int ExpInCurrentLevel(int expTotal) => Math.Max(0, expTotal % ExpPerLevel);

    /// <summary>EXP ที่ต้องได้เพิ่มเพื่อเลเวลถัดไป (ในช่วงเลเวลปัจจุบัน).</summary>
    public static int ExpToNextLevel(int expTotal) => ExpPerLevel - ExpInCurrentLevel(expTotal);

    /// <summary>ความคืบหน้าไปเลเวลถัดไป 0.0–1.0.</summary>
    public static double ProgressToNextLevel(int expTotal)
    {
        if (expTotal < 0)
            return 0;
        return ExpInCurrentLevel(expTotal) / (double)ExpPerLevel;
    }
}
