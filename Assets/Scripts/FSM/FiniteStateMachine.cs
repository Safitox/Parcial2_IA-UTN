using System.Collections.Generic;
using UnityEngine;

public class FiniteStateMachine
{
    private HunterState _currentState;
    public HunterState CurrentState { get; private set; }
    private HashSet<HunterState> _allStates = new();
    public void AddState(HunterState state)
    {
        if (!_allStates.Contains(state))
            _allStates.Add(state);
    }

    public void ChangeState(HunterState state)
    {
        if (!_allStates.Contains(state))
        {
            Debug.LogError("Missing State!");
            return;
        }

        _currentState?.Exit();
        _currentState = state;
        _currentState.Enter();
        CurrentState = state;
    }

    public void Update()
    {
        if (_currentState != null) _currentState.Update();
    }
}
