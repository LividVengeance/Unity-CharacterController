using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStateMachine
{
    private IState currentState;
    
    // Transitions
    private Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();
    private List<Transition> currentTransitions = new List<Transition>();
    private List<Transition> anyTransitions = new List<Transition>();

    private static List<Transition> EmptyTransitions = new List<Transition>(0);

    private IState previousState;
    
    public void Tick(ref ProjectTwo.PlayerCharacterInputs inputs)
    {
        Transition transition = GetTransition();
        if (transition != null)
        {
            SetState(transition.To);
        }

        currentState?.Tick(ref inputs);

        //Debug.Log("Current State: " + currentState);
    }

    public void SetState(IState newState)
    {
        if (newState == currentState) return;

        currentState?.ExitState();
        previousState = currentState;
        currentState = newState;

        _transitions.TryGetValue(currentState.GetType(), out currentTransitions);
        if (currentTransitions == null)
        {
            currentTransitions = EmptyTransitions;
        }

        currentState.EnterState();
    }

    public void AddTransition(IState from, IState to, Func<bool> predicate)
    {
        if (_transitions.TryGetValue(from.GetType(), out var transitions) == false)
        {
            transitions = new List<Transition>();
            _transitions[from.GetType()] = transitions;
        }

        transitions.Add(new Transition(to, predicate));
    }

    public void AddAnyTransition(IState state, Func<bool> predicate)
    {
        anyTransitions.Add(new Transition(state, predicate));
    }

    private class Transition
    {
        public Func<bool> Condition { get; }
        public IState To { get; }

        public Transition(IState to, Func<bool> condition)
        {
            To = to;
            Condition = condition;
        }
    }

    private Transition GetTransition()
    {
        foreach (var transition in anyTransitions)
        {
            if (transition.Condition()) return(transition);
        }

        foreach (var transition in currentTransitions)
        {
            if (transition.Condition()) return(transition);
        }

        return null;
    }

    public IState GetCurrentState => currentState;
    public IState GetPreviousState => previousState;
}
