using UnityEngine;

public class DeadP2 : HunterState
{
    float timer;

    public DeadP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        timer = 0;
        h.SetVisible(false);
        Debug.Log("Cazador capturado. Respawneando...");
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= h.RespawnTime)
            h.ResetCycle();
    }
}
