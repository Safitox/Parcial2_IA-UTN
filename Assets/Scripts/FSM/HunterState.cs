public abstract class HunterState
{
    protected readonly HunterNPC h;

    protected HunterState(HunterNPC hunter)
    {
        h = hunter;
    }

    public virtual void Enter()
    {
    }

    public abstract void Update();

    public virtual void Exit()
    {
    }

    protected bool Threat()
    {
        if (h.DetectWolf())
        {
            h.FSM.ChangeState(h.Flee);
            return true;
        }

        return false;
    }
}
