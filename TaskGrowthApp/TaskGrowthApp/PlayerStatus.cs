namespace TaskGrowthApp
{
    public class PlayerStatus
    {
        public string PageId { get; set; } = "";
        public int Level { get; set; } = 1;
        public int TotalExp { get; set; } = 0;
        public int CurrentExp { get; set; } = 0;
        public int Coin { get; set; } = 0;
        public DateTime? LastEmergencyCompletedDate { get; set; }

        /// <summary>現レベルで必要な総EXP</summary>
        public int ExpToNextLevel => Level * 100;
    }
}
