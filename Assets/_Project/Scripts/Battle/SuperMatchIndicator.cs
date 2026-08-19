namespace M3P
{
    /// <summary>Shown for a line of 4 or more, and only replaced by a longer line.</summary>
    public class SuperMatchIndicator : BattleIndicator
    {
        const string Title = "Super Match";

        protected override void Awake()
        {
            base.Awake();
            SetTitle(Title);
        }
    }
}
